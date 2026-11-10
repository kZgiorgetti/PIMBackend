# PIM IV — Backend (ASP.NET Core 8 + SQL Server)

API REST em camadas para gerenciamento de **colaboradores e tarefas** (UNIP / ADS — PIM IV).
Repositórios relacionados: **pim-frontend** (web) e **pim-mobile** (Expo/React Native). Contrato entre eles: a API `/api/v1`.

```
src/Pim.Api            Controllers, Program.cs, middlewares, Swagger
src/Pim.Application    Services, DTOs, validators, interfaces
src/Pim.Domain         Entidades, enums, regras de negócio
src/Pim.Infrastructure EF Core, repositórios, JWT, BCrypt
tests/Pim.Tests        xUnit (domínio, serviços, auth, integração)
database/              Scripts SQL (pim_completo.sql roda de ponta a ponta)
legacy-node/           Backend Node antigo (referência)
docs/                  ARQUITETURA, API, DECISOES, MER
```

## Subir com Docker
```bash
cp .env.example .env        # troque SA_PASSWORD, ADMIN_PASSWORD e JWT_SECRET
docker compose up --build
```
Swagger: http://localhost:5000/swagger · Saúde: http://localhost:5000/health.
O serviço `db-init` executa `database/pim_completo.sql`; o primeiro **Admin** é criado pela API (`ADMIN_EMAIL`/`ADMIN_PASSWORD`).

**CORS:** `CORS_ORIGINS` precisa conter a URL do front (padrão `http://localhost:8080`, a porta do `pim-frontend`). O app mobile não usa CORS.

## Rodar sem Docker
1. `sqlcmd -S localhost -U sa -P '<senha>' -I -b -i database/pim_completo.sql`
2. Copie `src/Pim.Api/appsettings.Development.json.example` para `appsettings.Development.json` (ignorado pelo git) e ajuste.
3. `dotnet run --project src/Pim.Api`

## Testes
```bash
dotnet build Pim.sln
dotnet test Pim.sln
```

## Usuários
Sem senhas fixas no repositório: entre como o Admin definido no `.env` e crie um Gestor e um Colaborador (`POST /api/v1/usuarios`).
Detalhes dos endpoints e `curl` em [docs/API.md](docs/API.md).
