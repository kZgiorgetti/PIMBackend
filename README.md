# PIM IV — Backend (ASP.NET Core 8 + PostgreSQL/Supabase)

API REST em camadas para gerenciamento de **colaboradores e tarefas** (UNIP / ADS — PIM IV).
É a base dos outros dois repositórios: **pim-frontend** (web) e **pim-mobile** (Expo). O contrato entre eles é a API `/api/v1`.

```
src/Pim.Api            Controllers, Program.cs, middlewares, Swagger
src/Pim.Application    Services, DTOs, validators, interfaces
src/Pim.Domain         Entidades, enums, regras de negócio
src/Pim.Infrastructure EF Core (Npgsql), repositórios, JWT, BCrypt
tests/Pim.Tests        xUnit (domínio, serviços, autenticação, integração)
database/              Scripts PostgreSQL (pim_completo.sql) e teste de fumaça
legacy-node/           Backend Node antigo (referência)
docs/                  ARQUITETURA, API, DECISOES, MER
```

## Pré-requisitos

| Para                                   | Precisa de                                                     |
| -------------------------------------- | -------------------------------------------------------------- |
| Qualquer forma de rodar                | Conta gratuita no [Supabase](https://supabase.com)             |
| Rodar com Docker                       | Docker Desktop (ou Docker Engine + Compose v2)                 |
| Rodar sem Docker / testes              | [.NET SDK 8](https://dotnet.microsoft.com/download/dotnet/8.0) |
| Opcional (aplicar o SQL pelo terminal) | `psql` (cliente do PostgreSQL)                                 |

## Passo 1 — Criar o banco no Supabase

1. No Supabase, crie um projeto (**guarde a senha do banco**).
2. Abra **SQL Editor → New query**, cole todo o conteúdo de `database/pim_completo.sql` e clique em **Run**.
   O script é idempotente: pode ser executado de novo sem problemas.
   _(Alternativa pelo terminal:_ `psql "<URI>" -v ON_ERROR_STOP=1 -f database/pim_completo.sql`_)_
3. Clique em **Connect** (topo do painel), escolha **Session pooler** e copie a URI, parecida com:
   `postgresql://postgres.abcdefghij:[YOUR-PASSWORD]@aws-0-sa-east-1.pooler.supabase.com:5432/postgres`
   - Troque `[YOUR-PASSWORD]` pela senha do banco. Se ela tiver caracteres especiais, codifique-os na URI (`@` → `%40`, `#` → `%23`, `/` → `%2F`).
   - Use o **Session pooler** (porta 5432): funciona em redes IPv4. A conexão direta (`db.<ref>.supabase.co`) é IPv6 por padrão no plano gratuito e costuma dar timeout. Evite o _Transaction pooler_ (6543).

## Passo 2 — Configurar

```bash
cp .env.example .env
```

Edite o `.env`:

| Variável                         | O que é                                                                                               |
| -------------------------------- | ----------------------------------------------------------------------------------------------------- |
| `DB_CONNECTION`                  | A URI do Session pooler (passo 1.3)                                                                   |
| `ADMIN_EMAIL` / `ADMIN_PASSWORD` | Primeiro usuário Admin, criado pela API no primeiro start (senha com 8+ caracteres, letras e números) |
| `JWT_SECRET`                     | Texto aleatório com 32+ caracteres (assina os tokens)                                                 |
| `CORS_ORIGINS`                   | URL(s) do front, separadas por vírgula. Padrão: `http://localhost:8080`                               |

Gerar um segredo: `openssl rand -base64 48`.

## Passo 3 — Rodar a API

### Opção A — Docker

```bash
docker compose up --build
```

### Opção B — .NET (sem Docker)

O `dotnet run` **não lê o `.env`**; use o arquivo de configuração local (ignorado pelo git):

```bash
cp src/Pim.Api/appsettings.Development.json.example src/Pim.Api/appsettings.Development.json
# edite o arquivo: DB_CONNECTION, JWT_SECRET, ADMIN_EMAIL, ADMIN_PASSWORD, CORS_ORIGINS
dotnet run --project src/Pim.Api
```

Para testar com o **app mobile em um celular real**, a API precisa escutar na rede (e o firewall deve permitir a porta 5000):

```bash
dotnet run --project src/Pim.Api --launch-profile rede
```

### Conferir

- http://localhost:5000/swagger — documentação interativa (`/` redireciona para cá)
- http://localhost:5000/health — deve responder `Healthy`
- Se o banco estiver inacessível, a API tenta se conectar por ~45 s e registra os avisos no console.

## Passo 4 — Primeiro acesso e usuários de teste

Não há senhas fixas no repositório. Entre como o Admin do `.env` e crie os demais perfis:

**Pelo Swagger:** `POST /api/v1/auth/login` → copie o `accessToken` → botão **Authorize** → cole o token → use `POST /api/v1/usuarios`.

**Pelo terminal:**

```bash
TOKEN=$(curl -s -X POST http://localhost:5000/api/v1/auth/login -H "Content-Type: application/json" \
  -d '{"email":"admin@pim.local","senha":"SUA_SENHA"}' | jq -r .accessToken)

curl -X POST http://localhost:5000/api/v1/usuarios -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"nome":"Gestora","email":"gestor@pim.local","senha":"Senha@123","perfil":"Gestor"}'
curl -X POST http://localhost:5000/api/v1/usuarios -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"nome":"Ana","email":"ana@pim.local","senha":"Senha@123","perfil":"Colaborador"}'
```

Um usuário **Colaborador** já ganha um colaborador vinculado e só vê/altera as próprias tarefas. Mais exemplos em [docs/API.md](docs/API.md).

## Testes

```bash
dotnet restore Pim.sln
dotnet build Pim.sln
dotnet test Pim.sln            # unidade + integração (usa banco em memória; não precisa de Supabase)
```

Teste do banco (opcional; roda em transação e desfaz tudo):

```bash
psql "<URI>" -v ON_ERROR_STOP=1 -f database/tests/smoke_test.sql
```

## Subindo os três projetos juntos

1. **Backend** (este repositório): `docker compose up --build` → porta **5000**.
2. **Frontend** ([pim-frontend](../pim-frontend)): porta **8080** → http://localhost:8080
3. **Mobile** ([pim-mobile](../pim-mobile)): `npx expo start`, com `EXPO_PUBLIC_API_URL` apontando para o **IP da sua máquina** (não `localhost`).

## Problemas comuns

| Sintoma                                                 | Causa / solução                                                                                                            |
| ------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| Timeout / `Network is unreachable` ao conectar no banco | Está usando a conexão direta (IPv6). Use a URI do **Session pooler**                                                       |
| `password authentication failed`                        | Senha errada ou com caractere especial sem codificar na URI                                                                |
| `Defina JWT_SECRET ...` ao iniciar                      | `JWT_SECRET` ausente ou com menos de 32 caracteres                                                                         |
| `Defina DB_CONNECTION ...`                              | Variável não definida (Docker: `.env`; sem Docker: `appsettings.Development.json`)                                         |
| Erro de **CORS** no navegador                           | Inclua exatamente a origem do front em `CORS_ORIGINS` (`http://localhost` ≠ `http://127.0.0.1`) e reinicie a API           |
| `relation "pim.usuarios" does not exist`                | O `pim_completo.sql` ainda não foi executado no Supabase                                                                   |
| Não consigo logar como Admin                            | O Admin só é criado se **não existir** nenhum; `ADMIN_EMAIL`/`ADMIN_PASSWORD` precisavam estar definidos no primeiro start |
| 429 Too Many Requests                                   | Limite de requisições por minuto (login: 10/min). Aguarde 1 minuto                                                         |
| Conta bloqueada                                         | 5 senhas erradas bloqueiam por 15 min                                                                                      |

Decisões técnicas (incluindo a migração para o Supabase): [docs/DECISOES.md](docs/DECISOES.md) · Arquitetura: [docs/ARQUITETURA.md](docs/ARQUITETURA.md) · MER: [docs/MER.md](docs/MER.md).
