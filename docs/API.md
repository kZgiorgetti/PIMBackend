# API REST — resumo (`/api/v1`)

Documentação interativa: `/swagger`. Saúde: `/health`. Erros seguem ProblemDetails (RFC 7807) e incluem a chave `error` (mensagem) e, na validação, `errors`.

| Recurso | Endpoints | Perfis |
|---|---|---|
| auth | `POST /auth/login`, `POST /auth/refresh`, `POST /auth/logout`, `GET /auth/me` | público / autenticado |
| colaboradores | `GET/POST /colaboradores`, `GET/PUT/DELETE /colaboradores/{id}` (DELETE inativa se houver tarefas abertas) | Admin, Gestor |
| tarefas | `GET/POST /tarefas`, `GET/PUT/DELETE /tarefas/{id}`, `PATCH /tarefas/{id}/status`, `POST /tarefas/{id}/concluir`, `POST /tarefas/{id}/reatribuir`, `GET /tarefas/{id}/historico`, `GET/POST /tarefas/{id}/comentarios` | todos (Colaborador: só as próprias; criar/editar/excluir/reatribuir: Admin, Gestor) |
| equipes | `GET/POST /equipes`, `GET/PUT/DELETE /equipes/{id}`, `POST/DELETE /equipes/{id}/colaboradores/{colaboradorId}` | Admin, Gestor |
| usuarios | `GET/POST /usuarios`, `GET/PUT/DELETE /usuarios/{id}`, `POST /usuarios/{id}/redefinir-senha`, `/ativar`, `/inativar` | Admin |
| relatorios | `GET /relatorios/resumo-por-colaborador`, `GET /relatorios/atrasadas` | Admin, Gestor |
| sync | `GET /sync/tarefas?atualizadoDesde=<ISO-8601>` (inclui `excluida: true`) | todos (Colaborador: só as próprias) |

`GET /tarefas`: `status`, `colaboradorId`, `prioridade`, `busca`, `page` (≥1), `pageSize` (1–100). Resposta: `{ itens, total, page, pageSize }`.
Códigos: 200, 201 (com `Location`), 204, 400, 401, 403, 404, 409, 422, 429.

## Exemplos (`curl`)

```bash
# login
TOKEN=$(curl -s -X POST http://localhost:5000/api/v1/auth/login -H "Content-Type: application/json" \
  -d '{"email":"admin@pim.local","senha":"SUA_SENHA"}' | jq -r .accessToken)

# criar usuário colaborador
curl -X POST http://localhost:5000/api/v1/usuarios -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"nome":"Ana","email":"ana@pim.local","senha":"Senha@123","perfil":"Colaborador"}'

# criar tarefa
curl -X POST http://localhost:5000/api/v1/tarefas -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"titulo":"Fechar inventário","prioridade":"Alta","dataPrazo":"2026-12-01T00:00:00Z","colaboradorId":1}'

# listar com filtros e paginação
curl -H "Authorization: Bearer $TOKEN" "http://localhost:5000/api/v1/tarefas?status=Pendente&busca=invent&page=1&pageSize=20"

# alterar status (gera linha em TarefaHistorico via trigger)
curl -X PATCH http://localhost:5000/api/v1/tarefas/1/status -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"status":"EmAndamento"}'

# sincronização incremental
curl -H "Authorization: Bearer $TOKEN" "http://localhost:5000/api/v1/sync/tarefas?atualizadoDesde=2026-11-01T00:00:00Z"
```
