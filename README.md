# Processamento de Webhooks de Pagamento — Sabemi TEC

> Projeto desenvolvido para a avaliação de habilidades técnicas da **Sabemi TEC**.

## O cenário

A Sabemi precisa processar notificações de pagamento (webhooks) enviadas por um banco parceiro.
Essas notificações confirmam a liquidação de seguros ou parcelas de empréstimos. Este serviço
recebe esses dados, garante que não haja duplicidade (idempotência) e exibe o status em um painel
administrativo.

## Requisitos da avaliação

**Backend (.NET)**
- Endpoint `POST /webhooks/pagamento` recebendo `id_transacao`, `id_contrato`, `valor`,
  `data_pagamento` e `status`
- Validação de segurança via `ApiKey`/`Signature` no header
- Idempotência: o mesmo `id_transacao` nunca é processado duas vezes, mesmo com reenvio do banco
- Persistência em PostgreSQL, com tabela de **Log de Eventos Brutos** e tabela de
  **Status do Contrato**
- Resiliência: o processamento da regra de negócio (simulado como pesado, ~2s) roda em background;
  o endpoint responde rápido ao banco

**Frontend (React)**
- Dashboard listando os pagamentos recebidos (refresh automático a cada 5s)
- Filtros por status (Sucesso/Erro) e por ID do contrato
- Alerta visual claro para eventos que falharem na validação/processamento

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

**Idempotência** é garantida no banco (índice único em `id_transacao`), não em memória: o código
insere o evento e trata a violação de chave duplicada do Postgres (`23505`) como "já recebido". Isso
é seguro mesmo com requisições concorrentes para o mesmo `id_transacao` — uma checagem `SELECT`
antes do `INSERT` teria uma condição de corrida.

**Durabilidade**: o evento é persistido como `Pendente` *antes* de entrar na fila em memória. Se a
API reiniciar com eventos ainda não processados, uma rotina de recuperação no startup os
reenfileira automaticamente.

## Modelo de dados

- **`eventos_webhook_brutos`** (Log de Eventos Brutos): payload bruto, assinatura recebida e status
  de processamento (`Pendente` / `Processando` / `Processado` / `Falha`), com índice único em
  `id_transacao`.
- **`status_contrato`** (Status do Contrato): visão agregada por `id_contrato` (valor total pago,
  quantidade de pagamentos, último pagamento, situação), atualizada pelo worker em background.

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
dotnet tool install --global dotnet-ef   # se ainda não tiver
dotnet ef database update --project src/Sabemi.Webhooks.Api
dotnet run --project src/Sabemi.Webhooks.Api
```

A API sobe em `http://localhost:5166`. Em desenvolvimento, a documentação interativa (Scalar) fica
disponível em `http://localhost:5166/scalar`.

Credenciais de desenvolvimento (já configuradas em `appsettings.Development.json`, iguais às do
`docker-compose.yml`):

| Variável | Valor (dev) |
|---|---|
| `WebhookSeguranca:ApiKey` | `dev-api-key-local` |
| `WebhookSeguranca:SegredoAssinatura` | `dev-segredo-local-para-hmac` |

#### Testando o endpoint manualmente

O corpo da requisição precisa de duas coisas: o header `X-Api-Key` e o header `X-Signature`, que é
o HMAC-SHA256 (hex, minúsculo) do corpo bruto usando o segredo acima. Exemplo em PowerShell:

```powershell
$corpo = '{"id_transacao":"tx-001","id_contrato":"contrato-001","valor":150.75,"data_pagamento":"2026-08-17T12:00:00Z","status":"Sucesso"}'
$segredo = "dev-segredo-local-para-hmac"
$hmac = New-Object System.Security.Cryptography.HMACSHA256([Text.Encoding]::UTF8.GetBytes($segredo))
$assinatura = [Convert]::ToHexString($hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($corpo))).ToLower()

Invoke-RestMethod -Method Post -Uri http://localhost:5166/webhooks/pagamento `
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

Acesse `http://localhost:5173`. O Vite faz proxy de `/api` e `/webhooks` para a API .NET, sem
necessidade de configurar CORS em desenvolvimento.

### 4. Testes

```bash
cd backend
dotnet test
```

Os testes de integração sobem a API via `WebApplicationFactory` contra um banco Postgres real e
isolado (`sabemi_webhooks_tests`, criado automaticamente pela migração), cobrindo: ApiKey/assinatura
inválidos, payload inválido, idempotência (incluindo requisições concorrentes com o mesmo
`id_transacao`) e o ciclo completo até o evento ficar `Processado` em background.

## Variáveis de ambiente

Veja `.env.example` na raiz. Em produção, as credenciais devem vir de variáveis de ambiente/gerenciador
de segredos (usando a convenção `Secao__Chave` do ASP.NET Core), nunca de `appsettings.json`.

## CI

O workflow em `.github/workflows/ci.yml` roda em todo push/PR para `main`: build e `dotnet test` do
backend (com um Postgres real como service container do GitHub Actions) e build do frontend.
