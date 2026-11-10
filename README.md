# PIM IV — Backend (ASP.NET Core 8 + PostgreSQL/Supabase)

API REST em camadas para gerenciamento de **colaboradores e tarefas** (UNIP / ADS — PIM IV).
Repositórios relacionados: **pim-frontend** (web) e **pim-mobile** (Expo/React Native). Contrato entre eles: a API `/api/v1`.

```
src/Pim.Api            Controllers, Program.cs, middlewares, Swagger
src/Pim.Application    Services, DTOs, validators, interfaces
src/Pim.Domain         Entidades, enums, regras de negócio
src/Pim.Infrastructure EF Core (Npgsql), repositórios, JWT, BCrypt
tests/Pim.Tests        xUnit (domínio, serviços, auth, integração)
database/              Scripts PostgreSQL (pim_completo.sql) e teste de fumaça
legacy-node/           Backend Node antigo (referência)
docs/                  ARQUITETURA, API, DECISOES, MER
```

## 1. Banco no Supabase
1. Crie um projeto no [Supabase](https://supabase.com) (guarde a senha do banco).
2. Abra **SQL Editor**, cole o conteúdo de `database/pim_completo.sql` e execute (pode executar de novo sem problemas). Ou, com `psql`:
   `psql "<URI do Session pooler>" -v ON_ERROR_STOP=1 -f database/pim_completo.sql`
3. Painel **Connect → Session pooler**: copie a URI (`postgresql://postgres.<ref>:…@aws-…pooler.supabase.com:5432/postgres`).
   Use o *Session pooler* (funciona em redes IPv4; a conexão direta é IPv6). Se a senha tiver `@`, `#` etc., codifique (`@` = `%40`).
4. (Opcional) teste de fumaça do banco: `psql "<URI>" -v ON_ERROR_STOP=1 -f database/tests/smoke_test.sql` (desfaz tudo no final).

## 2. Rodar a API
```bash
cp .env.example .env     # preencha DB_CONNECTION, ADMIN_PASSWORD, JWT_SECRET
```
**Com Docker:** `docker compose up --build` → http://localhost:5000/swagger · `/health`
**Sem Docker:** copie `src/Pim.Api/appsettings.Development.json.example` para `appsettings.Development.json` (ignorado pelo git), preencha e rode `dotnet run --project src/Pim.Api`.

**CORS:** `CORS_ORIGINS` deve conter a URL do front (padrão `http://localhost:8080`). O app mobile não usa CORS.

*Desenvolvimento sem internet (opcional):* `docker compose -f docker-compose.yml -f docker-compose.localdb.yml up --build` sobe um PostgreSQL local já com o script aplicado (veja o cabeçalho do arquivo para o `DB_CONNECTION`).

## Testes
```bash
dotnet build Pim.sln
dotnet test Pim.sln
```

## Usuários
Sem senhas fixas no repositório: entre como o Admin definido no `.env` e crie um Gestor e um Colaborador (`POST /api/v1/usuarios`).
Endpoints e `curl` em [docs/API.md](docs/API.md). Decisões (incl. migração para o Supabase) em [docs/DECISOES.md](docs/DECISOES.md).
