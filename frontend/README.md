# Frontend — Dashboard de Pagamentos

Dashboard em React 19 + Vite + TypeScript + Tailwind CSS 4 + shadcn/ui para o desafio técnico
da Sabemi TEC. Instruções completas de execução estão no [README da raiz do repositório](../README.md).

## Scripts

```bash
npm install   # instala as dependências
npm run dev   # inicia o servidor de desenvolvimento (http://localhost:5173)
npm run build # gera o build de produção em dist/
npm run lint  # roda o oxlint
```

Em desenvolvimento, o Vite faz proxy de `/api` e `/webhooks` para a API .NET (`VITE_API_BASE_URL`,
padrão `http://localhost:5166`), então não é necessário configurar CORS.
