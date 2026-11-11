# Decisões técnicas

## Banco de dados
- **PostgreSQL no Supabase** (migrado do SQL Server). Fonte da verdade do schema: os scripts em `database/` (idempotentes; `pim_completo.sql` roda do zero e pode rodar de novo). Teste: `database/tests/smoke_test.sql` (triggers, procedures, restrições; roda em transação com ROLLBACK).
- **Schema `pim`** (não exposto pela Data API do Supabase) + **RLS ligado sem policies** + revogação para `anon`/`authenticated`. A API conecta com a role `postgres` (ignora RLS). O **Supabase Auth não é usado**: a autenticação é o JWT próprio da API.
- **snake_case** no banco; o EF Core usa `EFCore.NamingConventions` (`UseSnakeCaseNamingConvention`). Nomes de tabela são definidos explicitamente nas configurações.
- **Procedures × funções:** no PostgreSQL uma PROCEDURE não devolve linhas. `sp_concluir_tarefa` e `sp_reatribuir_tarefa` são procedures (`CALL`); os relatórios são FUNCTIONs `RETURNS TABLE` (`SELECT * FROM`). TRY/CATCH/THROW ⇒ `BEGIN … EXCEPTION … RAISE`.
- **Erros de negócio** (SQLSTATE): `PM001` não encontrada (404), `PM002` tarefa excluída (422), `PM003` já concluída (409), `PM004` colaborador inválido (422).
- **Usuário no histórico:** a API grava `set_config('app.usuario_id', id, true)` dentro da transação do `SaveChanges` (valor local à transação ⇒ seguro com pool); a trigger lê `current_setting('app.usuario_id', true)`.
- **Exclusão de colaborador:** o PostgreSQL não tem `INSTEAD OF DELETE` em tabelas; usa-se `BEFORE DELETE` que inativa e retorna `NULL` (cancela o DELETE) quando há tarefas abertas. A API executa `DELETE` por SQL direto (`Remove()` do EF veria 0 linhas afetadas).
- **`data_atualizacao`** usa `clock_timestamp()` (e não `now()`, que é o início da transação). O sync mobile devolve `servidorEm` com margem de 5 s para tolerar diferença de relógio e transações longas; reenviar tarefas já vistas é inofensivo.
- **Tabelas extras:** `refresh_tokens` e as colunas `tentativas_falhas`/`bloqueado_ate` em `usuarios` (refresh com rotação e bloqueio de login). Perfis são `CHECK` em `usuarios.perfil`.
- **Admin inicial** não é inserido pelo SQL: a API cria no primeiro start (`ADMIN_EMAIL`/`ADMIN_PASSWORD`), pois o hash BCrypt não deve ser gerado em SQL.
- **Status:** `Pendente`, `EmAndamento`, `Concluida`. Prioridade: `Baixa`, `Media`, `Alta`, `Urgente`.

## Conexão com o Supabase
- `DB_CONNECTION` aceita a **URI do painel** (`postgresql://…`); `ConexaoBanco.Normalizar` converte para a connection string do Npgsql com `SSL Mode=Require` e `Trust Server Certificate=true`.
- Use o **Session pooler** (host `aws-…pooler.supabase.com`, porta **5432**, usuário `postgres.<ref>`): funciona em redes IPv4. A conexão direta (`db.<ref>.supabase.co`) é IPv6 por padrão no plano gratuito. Evite o modo *transaction* (6543): a API usa transações e conexões persistentes.
- Senha com caracteres especiais deve ir codificada na URI (`@` = `%40`).

## Aplicação
- **Migrations do EF Core não foram geradas**: o schema vale pelos scripts SQL e a API não executa `Migrate()`. Se for necessário: `dotnet ef migrations add Baseline -p src/Pim.Infrastructure -s src/Pim.Api` (sem aplicá-la sobre o banco criado pelos scripts).
- **Pacote `Microsoft.EntityFrameworkCore.InMemory`** na Infrastructure, só para os testes de integração (`Database:Provider=InMemory`); os repositórios têm ramo de fallback quando o provider não é relacional.
- **Versionamento:** prefixo fixo `/api/v1`. **Validação:** FluentValidation via `ValidacaoFilter` (400); regra de domínio ⇒ 422; conflito ⇒ 409.
- **Rate limiting:** janela fixa por IP (geral 300/min; login/refresh 10/min; `RateLimit:*`). Atrás de proxy reverso, configure *forwarded headers*.
- **Bloqueio de login:** 5 falhas ⇒ 15 min (`Auth:MaxTentativas`, `Auth:BloqueioMinutos`).
- **Sync mobile:** tarefa reatribuída a outra pessoa não é removida do cache do colaborador anterior até nova sincronização completa (limitação conhecida).

## Infra
- **Execução:** a API é executada diretamente pelo .NET 8; o banco PostgreSQL é hospedado no Supabase. TLS é configurado conforme o ambiente; em produção real use proxy reverso com HTTPS.
- **CI:** build, testes e `dotnet format` (não bloqueante até a base ser formatada uma vez); job `database-scripts` roda `pim_completo.sql` duas vezes + teste de fumaça em um PostgreSQL de serviço.

## Separação em três repositórios
- `pim-backend` (esta API + banco), `pim-frontend` (web estático servido por nginx) e `pim-mobile` (Expo). O front é outra origem: a API **não** serve arquivos estáticos, usa CORS (`CORS_ORIGINS`) e a CSP da API é `default-src 'none'`. Contrato entre os repositórios: a API REST `/api/v1` (`docs/API.md`, `/swagger`).
