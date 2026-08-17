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
- Validação de segurança via `Signature`/`ApiKey` no header
- Idempotência: o mesmo `id_transacao` nunca é processado duas vezes, mesmo com reenvio do banco
- Persistência em PostgreSQL, com tabela de **Log de Eventos Brutos** e tabela de
  **Status do Contrato**
- Resiliência: o processamento da regra de negócio (simulado como pesado, ~2s) roda em background;
  o endpoint responde rápido ao banco

**Frontend (React)**
- Dashboard listando os pagamentos recebidos (tempo real / refresh)
- Filtros por status (Sucesso/Erro) e por ID do contrato
- Alerta visual claro para eventos que falharem na validação

## Stack

| Camada | Tecnologia |
|---|---|
| Backend | .NET 10 (Minimal APIs), Entity Framework Core, PostgreSQL |
| Fila em background | `System.Threading.Channels` + `BackgroundService` |
| Documentação da API | OpenAPI + Scalar |
| Frontend | React 19, Vite, TypeScript, Tailwind CSS 4, shadcn/ui |
| Testes | xUnit (backend) |
| Infra local | Docker Compose (PostgreSQL) |
| CI | GitHub Actions |

## Status do projeto

🚧 Em desenvolvimento incremental — este README e a estrutura de pastas são o ponto de partida.
Cada etapa a seguir será um commit próprio:

- [ ] Scaffold da API .NET com EF Core e conexão com o Postgres
- [ ] Modelo de dados: eventos brutos e status de contrato
- [ ] Validação de assinatura (ApiKey / HMAC)
- [ ] Endpoint de webhook com idempotência
- [ ] Processamento assíncrono em background
- [ ] Endpoints de consulta, métricas e documentação OpenAPI/Scalar
- [ ] Testes automatizados (xUnit)
- [ ] Dashboard em React (Vite + Tailwind + shadcn)
- [ ] Filtros e alertas visuais de erro no dashboard
- [ ] Pipeline de CI no GitHub Actions
- [ ] Documentação final de execução

## Estrutura do repositório

```
processamento-webhooks-pagamento/
├── backend/     # API .NET (webhook, processamento, persistência)
├── frontend/    # Dashboard React (Vite + Tailwind + shadcn/ui)
├── docs/        # Decisões de arquitetura e material de apoio
└── docker-compose.yml
```

## Como executar

Instruções completas de execução (backend, frontend e banco via Docker) serão adicionadas conforme
o projeto avança.
