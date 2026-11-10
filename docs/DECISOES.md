# Decisões técnicas

- **Fonte da verdade do schema:** os scripts em `database/` (SQL Server). As migrations do EF Core devem espelhá-los; em caso de divergência, vale o SQL.
- **Tabelas extras:** `RefreshTokens` e as colunas `TentativasFalhas`/`BloqueadoAte` em `Usuarios` (exigidas pela Fase 4: refresh com rotação e bloqueio de login).
- **Perfis** são um `CHECK` em `Usuarios.Perfil`, não uma tabela.
- **Admin inicial** não é inserido pelo SQL: a API cria no primeiro start (`ADMIN_EMAIL`/`ADMIN_PASSWORD`), pois o hash BCrypt não deve ser gerado em SQL.
- **Status** normalizados: `Pendente`, `EmAndamento`, `Concluida` (legado: `pendente`, `completed`). Prioridade: `Baixa`, `Media`, `Alta`, `Urgente`.
- **Usuário no histórico:** a trigger lê `SESSION_CONTEXT('UsuarioId')`; a API deve defini-lo (`sp_set_session_context`) antes de atualizar status fora das procedures.
- **EF Core + triggers:** as entidades `Tarefa`, `Colaborador` e `Usuario` precisam de `ToTable(t => t.HasTrigger(...))`. A exclusão de `Colaborador` NÃO deve usar `Remove()` (a trigger INSTEAD OF DELETE faz o EF ver 0 linhas afetadas); usar `ExecuteSql` com `DELETE`.
- **Sem `ON DELETE CASCADE` para Colaboradores:** o SQL Server não permite cascata em tabela com trigger INSTEAD OF DELETE; a limpeza é feita na própria trigger.
- **Exclusão de colaborador com tarefas abertas** inativa silenciosamente (sem erro), conforme o enunciado; a API deve devolver o resultado ("inativado" vs "excluído").
- **Códigos de erro das procedures:** 50001 não encontrada, 50002 tarefa excluída, 50003 já concluída, 50004 colaborador inválido; a API os mapeia para 404/422/409/422.

## Decisões das fases 2–8
- **Migrations do EF Core não foram geradas** (exigem restaurar pacotes e a ferramenta `dotnet-ef`). O schema vale pelos scripts SQL. Para gerar uma baseline alinhada: `dotnet tool install -g dotnet-ef && dotnet ef migrations add Baseline -p src/Pim.Infrastructure -s src/Pim.Api` (e **não** aplicá-la sobre um banco criado pelos scripts). A API não executa `Migrate()`.
- **Pacote `Microsoft.EntityFrameworkCore.InMemory`** na Infrastructure, usado só pelos testes de integração (`Database:Provider=InMemory`); repositórios têm ramo de fallback quando o provider não é relacional (procedures não existem no InMemory).
- **Versionamento:** prefixo fixo `/api/v1` (sem `Asp.Versioning`).
- **Validação:** FluentValidation aplicada automaticamente por `ValidacaoFilter` (400). Violação de regra de domínio → 422; conflito → 409.
- **Exemplos no Swagger:** como `<remarks>` XML nos endpoints principais (sem pacote extra de exemplos).
- **Rate limiting:** janela fixa por IP (geral 300/min; login/refresh 10/min, configurável em `RateLimit:*`). Atrás de proxy reverso, configure *forwarded headers*.
- **Bloqueio de login:** 5 falhas ⇒ 15 min (`Auth:MaxTentativas`, `Auth:BloqueioMinutos`).
- **Sync mobile:** tarefa reatribuída a outra pessoa não é removida do cache do colaborador anterior até nova sincronização completa (limitação conhecida, documentada em `mobile/README.md`).
- **Docker:** TLS não é terminado no container (`Seguranca__ForcarHttps=false`); em produção, use proxy reverso com HTTPS.
- **CI:** `dotnet format --verify-no-changes` roda como não bloqueante até a base ser formatada uma vez.

## Separação em três repositórios
- `pim-backend` (esta API + banco + Docker + CI), `pim-frontend` (web estático servido por nginx) e `pim-mobile` (Expo). O front é outra origem: a API **não** serve mais `wwwroot`, usa CORS (`CORS_ORIGINS`) e a CSP da API é `default-src 'none'` (a CSP do front é definida pelo nginx do front).
- Contrato entre os repositórios: apenas a API REST `/api/v1` (ver `docs/API.md` e `/swagger`).
