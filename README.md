# PIM IV — Backend (ASP.NET Core 8 + PostgreSQL/Supabase)

API REST em camadas para gerenciamento de colaboradores e tarefas. É consumida pelos projetos independentes [PIMFrontend](https://github.com/kZgiorgetti/PIMFrontend) e [PIMMobile](https://github.com/kZgiorgetti/PIMMobile), por meio da API `/api/v1`.

## Pré-requisitos

- Ubuntu/Linux e [.NET SDK 8](https://dotnet.microsoft.com/download/dotnet/8.0).
- Um projeto PostgreSQL no [Supabase](https://supabase.com).
- `psql` (opcional, para aplicar scripts SQL pelo terminal).

Confira a instalação do SDK com `dotnet --version`; a versão principal deve ser `8`.

## Configurar o Supabase

1. Crie um projeto no Supabase e guarde a senha do banco.
2. No **SQL Editor**, execute o conteúdo de `database/pim_completo.sql`. O script é idempotente. Alternativamente, use `psql "<URI>" -v ON_ERROR_STOP=1 -f database/pim_completo.sql`.
3. Em **Connect**, copie a URI do **Session pooler** (porta `5432`, usuário `postgres.<ref>`). Esse modo funciona em redes IPv4. Codifique caracteres especiais da senha na URI (por exemplo, `@` como `%40`).

## Configurar e executar a API

Crie a configuração local a partir do modelo:

```bash
cp src/Pim.Api/appsettings.Development.json.example src/Pim.Api/appsettings.Development.json
```

Edite `appsettings.Development.json` e configure:

- `DB_CONNECTION`: URI do Session pooler copiada do Supabase.
- `JWT_SECRET`: segredo aleatório com pelo menos 32 caracteres (gere com `openssl rand -base64 48`).
- `ADMIN_EMAIL` e `ADMIN_PASSWORD`: credenciais usadas para criar o primeiro administrador no primeiro início. Use uma senha com pelo menos 8 caracteres, letras e números.
- `CORS_ORIGINS`: origens autorizadas, separadas por vírgula. Para desenvolvimento web, o padrão é `http://localhost:8080`.

O `dotnet run` carrega `appsettings.Development.json` quando o ambiente Development está ativo, como nos perfis do `launchSettings.json`. Inicie a API na raiz do repositório:

```bash
dotnet run --project src/Pim.Api
```

Para permitir acesso de um celular na rede local, inicie com o perfil `rede` e permita a porta `5000` no firewall:

```bash
dotnet run --project src/Pim.Api --launch-profile rede
```

O aplicativo mobile deve usar o IP da máquina que executa a API em `EXPO_PUBLIC_API_URL`, não `localhost`. Não é necessário modificar os projetos Frontend ou Mobile.

## Swagger e health check

- Swagger: <http://localhost:5000/swagger>
- Health check: <http://localhost:5000/health>

A raiz <http://localhost:5000/> redireciona para o Swagger. Se o banco não estiver acessível, a inicialização tenta conectar por aproximadamente 45 segundos e registra avisos no console.

## Testes automatizados

Na raiz do repositório:

```bash
dotnet restore
dotnet build
dotnet test
```

Os testes automatizados usam banco em memória e não precisam de acesso ao Supabase. Os testes SQL opcionais também podem ser executados contra o Supabase:

```bash
psql "<URI>" -v ON_ERROR_STOP=1 -f database/tests/smoke_test.sql
```

## Solução de problemas

| Sintoma | Causa / solução |
| --- | --- |
| Timeout ou `Network is unreachable` ao conectar ao banco | Use a URI do Session pooler; a conexão direta pode depender de IPv6. |
| `password authentication failed` | Confira a senha e codifique os caracteres especiais na URI. |
| Erro informando que `JWT_SECRET` não foi definido | Configure um segredo com pelo menos 32 caracteres. |
| Erro informando que `DB_CONNECTION` não foi definido | Confira `src/Pim.Api/appsettings.Development.json` e o ambiente `Development`. |
| Erro de CORS no navegador | Inclua a origem exata do frontend em `CORS_ORIGINS` e reinicie a API. |
| `relation "pim.usuarios" does not exist` | Execute `database/pim_completo.sql` no Supabase. |
| Não consegue autenticar como Admin | O Admin só é criado quando ainda não há usuários; defina `ADMIN_EMAIL` e `ADMIN_PASSWORD` antes do primeiro início. |

Arquitetura: [docs/ARQUITETURA.md](docs/ARQUITETURA.md) · Contrato da API: [docs/API.md](docs/API.md) · Decisões: [docs/DECISOES.md](docs/DECISOES.md) · MER: [docs/MER.md](docs/MER.md).
