# Processamento de Webhooks de Pagamento — Sabemi TEC

> Projeto desenvolvido para a avaliação de habilidades técnicas da **Sabemi TEC**.

## O cenário

A Sabemi precisa processar notificações de pagamento (webhooks) enviadas por um banco parceiro.
Essas notificações confirmam a liquidação de seguros ou parcelas de empréstimos. Este serviço
recebe esses dados, garante que não haja duplicidade (idempotência) e exibe o status em um painel
administrativo.

## Cobertura dos requisitos

| Requisito da avaliação | Onde está |
|---|---|
| `POST /webhooks/pagamento` com `id_transacao`, `id_contrato`, `valor`, `data_pagamento`, `status` | [`WebhookEndpoints.cs`](backend/src/Sabemi.Webhooks.Api/Endpoints/WebhookEndpoints.cs), [`WebhookPagamentoRequest.cs`](backend/src/Sabemi.Webhooks.Api/Contracts/WebhookPagamentoRequest.cs) |
| Validação de `Signature` **ou** `ApiKey` no header | [`AssinaturaWebhookFilter.cs`](backend/src/Sabemi.Webhooks.Api/Security/AssinaturaWebhookFilter.cs) — implementados os dois |
| Idempotência por `id_transacao` | [`ProcessamentoWebhookService.cs`](backend/src/Sabemi.Webhooks.Api/Application/ProcessamentoWebhookService.cs) + índice único no banco |
| Persistência com "Log de Eventos Brutos" e "Status do Contrato" | [`EventoWebhookBruto.cs`](backend/src/Sabemi.Webhooks.Api/Domain/EventoWebhookBruto.cs), [`StatusContrato.cs`](backend/src/Sabemi.Webhooks.Api/Domain/StatusContrato.cs) |
| Processamento pesado (~2s) em background, com resposta rápida | [`ProcessadorPagamentosWorker.cs`](backend/src/Sabemi.Webhooks.Api/Application/ProcessadorPagamentosWorker.cs) |
| Dashboard listando os pagamentos recebidos | [`App.tsx`](frontend/src/App.tsx) — refresh automático a cada 5s |
| Filtros por status (Sucesso/Erro) e por ID do contrato | [`FiltrosPagamentos.tsx`](frontend/src/components/dashboard/FiltrosPagamentos.tsx) |
| Alerta visual claro para eventos que falharem | [`App.tsx`](frontend/src/App.tsx) + linha destacada em [`TabelaPagamentos.tsx`](frontend/src/components/dashboard/TabelaPagamentos.tsx) |

## Stack

| Camada | Tecnologia |
|---|---|
| Backend | .NET 10 (Minimal APIs), Entity Framework Core, Npgsql |
| Banco de dados | PostgreSQL 17 (Docker) |
| Fila em background | `System.Threading.Channels` + `BackgroundService` |
| Documentação da API | OpenAPI (`Microsoft.AspNetCore.OpenApi`) + Scalar |
| Frontend | React 19, Vite, TypeScript, Tailwind CSS 4, shadcn/ui, TanStack Query |
| Testes | xUnit + `WebApplicationFactory` (integração contra Postgres real) |
| CI | GitHub Actions (build + testes do backend, build do frontend) |

## Arquitetura

```
POST /webhooks/pagamento
        │
        ├─ 1. Filtro de segurança (ApiKey + HMAC-SHA256)  ──► 401 se inválido
        ├─ 2. Validação do payload                        ──► persiste como Falha + 400
        ├─ 3. INSERT do evento bruto (índice único em id_transacao)
        │       └─ violação 23505 no Postgres ──► 200 "Duplicado", não reprocessa
        ├─ 4. Enfileira o evento (Channel)
        └─ 5. 202 Accepted  ◄── resposta em milissegundos

        BackgroundService (worker)
        ├─ ~2s de delay (simula processamento pesado)
        ├─ upsert em status_contrato
        └─ marca o evento como Processado ou Falha
```

## Modelo de dados

- **`eventos_webhook_brutos`** (Log de Eventos Brutos): payload bruto, assinatura recebida e status
  de processamento (`Pendente` / `Processando` / `Processado` / `Falha`), com índice único em
  `id_transacao`.
- **`status_contrato`** (Status do Contrato): visão agregada por `id_contrato` — valor total pago,
  quantidade de pagamentos liquidados, quantidade de notificações reportadas com erro, último
  pagamento e situação. Atualizada pelo worker em background.

---

## Além dos requisitos

O que foi entregue acima do que a avaliação pedia, e o motivo de cada item.

### Segurança em duas camadas
O enunciado pedia `Signature` **ou** `ApiKey`. Foram implementados os dois: `X-Api-Key` identifica o
emissor e `X-Signature` é o HMAC-SHA256 do **corpo bruto**, o que também garante integridade — uma
ApiKey sozinha não detecta payload adulterado em trânsito. As duas comparações usam
`CryptographicOperations.FixedTimeEquals`, evitando que o tempo de resposta revele quantos caracteres
do segredo estavam corretos (timing attack).

### Idempotência garantida pelo banco, não pela aplicação
A checagem óbvia seria `SELECT` antes do `INSERT`, mas isso tem condição de corrida: duas entregas
simultâneas do mesmo `id_transacao` passam as duas pelo `SELECT` e gravam as duas. Aqui a garantia é
o **índice único** em `id_transacao`; o código simplesmente insere e trata a violação `23505` do
Postgres como "já recebido". Isso é correto mesmo sob concorrência — e há um teste que dispara 5
requisições paralelas com o mesmo `id_transacao` e exige exatamente 1 aceita e 4 duplicadas.

### Durabilidade da fila
A fila é em memória, então um reinício poderia perder eventos já respondidos com `202`. Para evitar
isso, o evento é persistido como `Pendente` **antes** de entrar na fila, e uma rotina no startup
reenfileira o que ficou `Pendente`/`Processando`. O banco é a fonte da verdade; o Channel é só o
mecanismo de entrega.

### Backpressure em vez de fila infinita
O `Channel` é limitado a 500 itens com `FullMode = Wait`. Sob rajada, produtores aguardam em vez de
a fila crescer sem limite até consumir toda a memória do processo. O trade-off está descrito em
[Limitações](#limitações-conhecidas-e-próximos-passos).

### Auditoria completa
O payload bruto é gravado como `jsonb` junto com a assinatura recebida, inclusive para eventos
inválidos. Se o banco parceiro contestar um processamento, dá para reconstruir exatamente o que
chegou. O painel mostra esse payload formatado no detalhe de cada evento.

### Máquina de estados do processamento
Em vez de um booleano "processado", cada evento tem `Pendente` → `Processando` → `Processado`/`Falha`,
com contador de tentativas, mensagem de erro e timestamps de recebimento e conclusão. É o que permite
o painel distinguir "ainda não processou" de "falhou".

### Agregação real por contrato
`status_contrato` não é um espelho do último evento: acumula valor total pago e quantidade de
pagamentos por contrato. Notificações reportadas como `Erro` são contabilizadas separadamente e não
entram no valor liquidado (ver [Decisões](#decisões-técnicas-e-trade-offs)).

### API além do webhook
- `GET /api/pagamentos` com filtros, paginação e teto de 200 itens por página.
- `GET /api/metricas` alimentando os cards do painel sem trazer a lista inteira para o cliente.
- `GET /health` que **verifica o banco** com `CanConnectAsync` — uma API que responde "ok" sem
  conseguir persistir daria falso positivo para quem monitora o serviço.
- OpenAPI + Scalar em desenvolvimento, para explorar a API sem Postman.

### Testes de integração contra Postgres real
A suíte não usa banco in-memory: sobe a API com `WebApplicationFactory` contra um Postgres de
verdade, porque justamente as partes críticas deste projeto (violação de índice único, `timestamptz`,
`jsonb`) não existem no provider in-memory. Um dos testes cronometra a resposta do endpoint para
provar que ela não espera os 2 segundos do processamento.

### Infraestrutura e automação
CI no GitHub Actions rodando build e testes do backend com Postgres como *service container*, mais o
build do frontend; `docker-compose` com healthcheck; `.env.example` documentando cada variável.

### Frontend além do mínimo
Cards de métricas, badges por status, skeletons durante o carregamento, paginação, modal de detalhe
com o payload formatado, e `keepPreviousData` do TanStack Query para que a tabela não pisque a cada
refresh de 5s. O proxy do Vite encaminha `/api` e `/webhooks` para a API, dispensando configuração de
CORS em desenvolvimento.

---

## Decisões técnicas e trade-offs

**`Channel` em memória em vez de RabbitMQ/Kafka.** Uma fila externa seria o certo em produção, mas
adicionaria infraestrutura desproporcional ao escopo da avaliação. O risco real — perder evento em um
reinício — foi resolvido persistindo antes de enfileirar. A troca é barata: tudo está atrás de
[`IFilaProcessamentoPagamentos`](backend/src/Sabemi.Webhooks.Api/Application/IFilaProcessamentoPagamentos.cs).

**`202 Accepted` em vez de `200 OK`.** No momento da resposta o pagamento ainda não foi processado;
`202` comunica exatamente isso ao banco parceiro. Reenvio de um `id_transacao` já conhecido responde
`200` com `"situacao": "Duplicado"` — sinaliza sucesso (o banco não precisa tentar de novo) sem
mentir que houve novo processamento.

**Datas normalizadas para UTC na borda.** As colunas são `timestamp with time zone`, e o Npgsql só
aceita `DateTime` com `Kind=Utc` nelas. Um webhook enviando `"2026-08-17T12:00:00"` (sem fuso) ou
`"...-03:00"` produziria uma exceção no `SaveChanges`, devolvendo `500` — e, pior, sem registrar nada
no Log de Eventos Brutos. A normalização acontece logo após a desserialização, cobrindo os eventos
válidos e os inválidos. Data sem fuso é **assumida como UTC**, seguindo o contrato do banco parceiro;
rejeitá-la como inválida seria intolerante com um formato que webhooks reais emitem com frequência.

**Pagamento com status `Erro` não soma ao valor liquidado.** Uma notificação que o banco reportou
como `Erro` não liquidou nada; somá-la a `ValorTotalPago` inflaria o saldo do contrato. Ela
incrementa `QuantidadePagamentosComErro` e atualiza a situação do contrato, mas fica fora do total e
da contagem de pagamentos. A regra vive na entidade `StatusContrato`, não no worker.

**Duas dimensões de status.** `status` é o que o banco reportou sobre o pagamento (`Sucesso`/`Erro`);
`StatusProcessamento` é o que aconteceu do nosso lado (`Pendente`/`Processando`/`Processado`/`Falha`).
São coisas diferentes: um pagamento pode ser `Sucesso` para o banco e ter falhado no nosso
processamento. Por isso o filtro do painel oferece "Falha de validação" como terceira opção, além de
Sucesso e Erro.

**Migração automática no startup.** `Database.MigrateAsync()` roda ao subir a API, o que torna a
avaliação um `docker compose up` + `dotnet run`. Em produção com múltiplas instâncias isso é
inadequado (duas instâncias migrando ao mesmo tempo) — o passo deveria ir para o pipeline de deploy.

**`127.0.0.1` em vez de `localhost`.** Com o Postgres em Docker sob WSL com rede *mirrored*,
`localhost` pode resolver IPv6 primeiro e demorar ~20s até cair para IPv4 — ou ter a conexão recusada.
Todas as connection strings e URLs do projeto usam o IPv4 explícito.

---

## Limitações conhecidas e próximos passos

Escolhas conscientes, registradas para não passarem por descuido:

- **Vazão de ~0,5 evento/s.** O worker consome a fila sequencialmente e cada item leva ~2s. Sob
  rajada sustentada a fila enche e o backpressure passa a segurar o endpoint, anulando a resposta
  rápida. Solução natural: N consumidores paralelos, limitados por `SemaphoreSlim` ou vários leitores
  do Channel.
- **Sem retry ou dead-letter.** `Tentativas` é incrementado mas nunca consultado: um evento que cai
  em `Falha` permanece assim. Faltam política de retry com backoff e um limite após o qual o evento
  vai para inspeção manual.
- **Payload inválido "queima" o `id_transacao`.** Como o evento inválido também é gravado e ocupa o
  índice único, um reenvio corrigido com o mesmo `id_transacao` volta como `Duplicado`. É o preço da
  idempotência estrita; tratar isso exigiria decidir se um evento inválido pode ser sobrescrito.
- **`status_contrato` não é exposta.** A tabela existe e é mantida corretamente, mas nenhum endpoint
  a lê — o painel lista eventos, não contratos. Um `GET /api/contratos` com uma aba no dashboard é o
  próximo passo óbvio.
- **Frontend sem testes automatizados.** O backend tem cobertura de integração; o frontend é
  verificado apenas por type-check e build no CI.

---

## Estrutura do repositório

```
processamento-webhooks-pagamento/
├── backend/
│   ├── Sabemi.Webhooks.slnx
│   ├── src/Sabemi.Webhooks.Api/     # API (.NET 10)
│   └── tests/Sabemi.Webhooks.Tests/ # Testes de integração (xUnit)
├── frontend/                        # Dashboard (Vite + React 19 + Tailwind + shadcn)
├── .github/workflows/ci.yml
├── docker-compose.yml
└── .env.example
```

## Como executar

### Pré-requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 22+](https://nodejs.org/)
- Docker (usado apenas para o PostgreSQL)

### 1. Subir o banco de dados

```bash
docker compose up -d db
```

### 2. Backend

```bash
cd backend
dotnet run --project src/Sabemi.Webhooks.Api
```

As migrações são aplicadas automaticamente no startup — não é preciso rodar `dotnet ef database update`
à mão. A API sobe em `http://127.0.0.1:5166` e, em desenvolvimento, a documentação interativa (Scalar)
fica em `http://127.0.0.1:5166/scalar`.

> **Nota (WSL com rede mirrored):** se o Postgres roda em Docker dentro do WSL, prefira sempre
> `127.0.0.1` a `localhost` nas URLs e connection strings. Em alguns setups de rede *mirrored*,
> `localhost` tenta resolver IPv6 primeiro e demora ~20s até cair para IPv4 — ou tem a conexão
> recusada.

Credenciais de desenvolvimento (já configuradas em `appsettings.Development.json`, iguais às do
`docker-compose.yml`):

| Variável | Valor (dev) |
|---|---|
| `WebhookSeguranca:ApiKey` | `dev-api-key-local` |
| `WebhookSeguranca:SegredoAssinatura` | `dev-segredo-local-para-hmac` |

#### Testando o endpoint manualmente

A requisição precisa de dois headers: `X-Api-Key` e `X-Signature` — este último é o HMAC-SHA256 (hex,
minúsculo) do corpo bruto usando o segredo acima. Exemplo em PowerShell 5.1+:

```powershell
function ToHex($bytes) { -join ($bytes | ForEach-Object { $_.ToString("x2") }) }

$corpo = '{"id_transacao":"tx-001","id_contrato":"contrato-001","valor":150.75,"data_pagamento":"2026-08-17T12:00:00Z","status":"Sucesso"}'
$segredo = "dev-segredo-local-para-hmac"
$hmac = [System.Security.Cryptography.HMACSHA256]::new([Text.Encoding]::UTF8.GetBytes($segredo))
$assinatura = ToHex $hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($corpo))

Invoke-RestMethod -Method Post -Uri http://127.0.0.1:5166/webhooks/pagamento `
  -Headers @{ "X-Api-Key" = "dev-api-key-local"; "X-Signature" = $assinatura } `
  -ContentType "application/json" -Body $corpo
```

Reenviar a mesma requisição retorna `200 OK` com `"situacao": "Duplicado"` em vez de processar de
novo.

### 3. Frontend

```bash
cd frontend
npm install
npm run dev
```

Acesse `http://127.0.0.1:5173`. O Vite faz proxy de `/api` e `/webhooks` para a API .NET, sem
necessidade de configurar CORS em desenvolvimento.

### 4. Testes

```bash
cd backend
dotnet test
```

Os testes de integração sobem a API via `WebApplicationFactory` contra um banco Postgres real e
isolado (`sabemi_webhooks_tests`, criado automaticamente pela migração). São 13 testes cobrindo:

- ApiKey e assinatura inválidas (401);
- payload que falha na validação (400, registrado como `Falha`);
- campo com formato inválido identificado pelo nome na mensagem de erro;
- idempotência, incluindo 5 requisições concorrentes com o mesmo `id_transacao`;
- `data_pagamento` nos três formatos de fuso (com `Z`, sem fuso e com offset), incluindo a conversão
  correta do instante para UTC;
- ciclo completo até o evento ficar `Processado`, com verificação de que a resposta não espera os 2s;
- agregação do contrato ignorando no valor total um pagamento reportado como `Erro`.

## Variáveis de ambiente

Veja `.env.example` na raiz. Em produção, as credenciais devem vir de variáveis de ambiente/gerenciador
de segredos (usando a convenção `Secao__Chave` do ASP.NET Core), nunca de `appsettings.json`.

## CI

O workflow em [`.github/workflows/ci.yml`](.github/workflows/ci.yml) roda em todo push/PR para `main`:
build e `dotnet test` do backend (com um Postgres real como service container do GitHub Actions) e
build do frontend.
