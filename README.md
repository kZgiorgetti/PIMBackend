# PIMBackend

Backend do Projeto Integrador Multidisciplinar (PIM IV), voltado ao gerenciamento de colaboradores, equipes e tarefas. Expõe uma API REST que centraliza as regras de negócio, autenticação JWT e acesso ao PostgreSQL. Os clientes Web e Flutter consomem a API pelo prefixo `/api/v1`.

## Tecnologias

- C# e ASP.NET Core 8 para a API.
- Entity Framework Core 8 e Npgsql para persistência em PostgreSQL.
- Supabase como hospedagem do PostgreSQL.
- JWT Bearer para autenticação própria da API (não usa Supabase Auth).
- BCrypt.Net-Next, FluentValidation, Swagger/OpenAPI e Health Checks.
- xUnit e ASP.NET Core MVC Testing para testes.

## Arquitetura e pastas

`Pim.sln` organiza a API em camadas: `Pim.Api` contém a entrada HTTP, controllers e middlewares; `Pim.Application` contém casos de uso, DTOs e validações; `Pim.Domain` contém entidades e regras de domínio; `Pim.Infrastructure` implementa persistência, repositórios e segurança. A API é executada diretamente pelo .NET e o PostgreSQL é hospedado no Supabase.

| Caminho | Conteúdo |
| --- | --- |
| `src/Pim.Api/` | API, configuração, controllers, middlewares e perfis de execução. |
| `src/Pim.Application/` | Serviços, DTOs, interfaces e validações. |
| `src/Pim.Domain/` | Entidades e regras do domínio. |
| `src/Pim.Infrastructure/` | EF Core, PostgreSQL, repositórios e segurança. |
| `tests/Pim.Tests/` | Testes automatizados .NET. |
| `database/` | Schema e scripts SQL; `pim_completo.sql` reúne a instalação. |
| `.github/workflows/` | Integração contínua. |

## Pré-requisitos

- [.NET SDK 8](https://dotnet.microsoft.com/download/dotnet/8.0).
- Projeto PostgreSQL no [Supabase](https://supabase.com).
- `psql` somente se preferir executar os scripts SQL pelo terminal.

Confira o SDK com `dotnet --version`; a versão principal deve ser `8`. Os comandos abaixo partem da raiz deste repositório.

## Configurar Supabase e PostgreSQL

1. Crie um projeto Supabase e defina a senha do banco.
2. No painel, abra **Connect** e obtenha a conexão **Session pooler**. Para redes IPv4, use o host `...pooler.supabase.com`, porta `5432` e usuário `postgres.<referência-do-projeto>`. A aplicação não foi configurada para o Transaction Pooler.
3. Execute o script inicial `database/pim_completo.sql` pelo SQL Editor do Supabase. O arquivo reúne o schema, procedures, triggers, segurança e dados iniciais. Não é necessário executar também os scripts componentes.

Alternativamente, com `psql`:

```bash
psql "<URI_DO_SESSION_POOLER>" -v ON_ERROR_STOP=1 -f database/pim_completo.sql
```

Use uma URI no formato `postgresql://usuario:senha@host:5432/postgres`. Codifique caracteres reservados na senha (por exemplo, `@` como `%40`). `DB_CONNECTION` aceita essa URI ou uma connection string Npgsql; a aplicação converte a URI e exige SSL para a conexão Supabase. Não publique a senha.

## Configuração local

Copie o exemplo de configuração:

```powershell
Copy-Item src/Pim.Api/appsettings.Development.json.example src/Pim.Api/appsettings.Development.json
```

Edite `src/Pim.Api/appsettings.Development.json` com os valores do seu ambiente. O arquivo local está ignorado pelo Git.

| Configuração | Uso |
| --- | --- |
| `DB_CONNECTION` | URI do Session pooler ou connection string Npgsql do PostgreSQL. |
| `JWT_SECRET` | Segredo para assinatura JWT, com pelo menos 32 caracteres. Use um valor aleatório e privado. |
| `ADMIN_EMAIL` | E-mail do administrador inicial criado pela API se ainda não houver usuário Admin. |
| `ADMIN_PASSWORD` | Senha desse administrador inicial; a aplicação armazena o hash. |
| `CORS_ORIGINS` | Lista de origens web autorizadas, separadas por vírgula, sem caminhos ou barra final. |

O exemplo já traz as origens locais:

```text
http://localhost:8080,http://localhost:5001
```

Elas correspondem ao Frontend e ao Flutter Web, respectivamente. Para gerar um segredo JWT local, pode usar `openssl rand -base64 48`. Não coloque credenciais reais em documentação ou no controle de versão.

## Executar a API

Restaure, compile e inicie:

```bash
dotnet restore Pim.sln
dotnet build Pim.sln
dotnet run --project src/Pim.Api/Pim.Api.csproj --launch-profile http
```

O perfil `http` executa em `http://localhost:5000` e define `Development`, carregando o arquivo de configuração acima. Na inicialização, a API conecta ao banco e tenta criar o administrador inicial quando necessário.

## Acesso na rede local

Para permitir que outros dispositivos na mesma rede alcancem a API, inicie pelo perfil `rede`:

```bash
dotnet run --project src/Pim.Api/Pim.Api.csproj --launch-profile rede
```

Esse perfil escuta em `0.0.0.0:5000`. Permita a porta 5000 no firewall e acesse usando o IP local do computador que executa a API, por exemplo `http://192.168.1.20:5000`. O endereço `localhost` só aponta para a própria máquina de cada dispositivo.

## Frontend e Mobile

Os clientes separados consomem a mesma API e enviam o JWT em `Authorization: Bearer` nas rotas protegidas. O Swagger em `/swagger` apresenta os endpoints e seus contratos.

- [Repositório Frontend (PIMFrontend)](https://github.com/kZgiorgetti/PIMFrontend)
- [Repositório Mobile Flutter (PIMMobile)](https://github.com/kZgiorgetti/PIMMobile)

No computador de desenvolvimento, o Frontend pode chamar `http://localhost:5000/api/v1`. Para Flutter Web, a origem local esperada é `http://localhost:5001`, que já consta em `CORS_ORIGINS`. Para um dispositivo Flutter na rede local, configure a base da API para o IP da máquina servidora, por exemplo `http://192.168.1.20:5000/api/v1`.

### Local, rede local e ferramentas remotas

- **API local:** o perfil `http` escuta em `localhost:5000`; clientes no mesmo computador acessam essa URL.
- **Rede local:** o perfil `rede` escuta nas interfaces da máquina; outros dispositivos da mesma rede usam o IP local do computador e precisam de conectividade pela porta 5000.
- **Ferramenta remota, como FlutLab:** ela não consegue acessar o `localhost` nem o IP privado do seu computador. Use uma URL pública HTTPS fornecida por um túnel ou hospedagem acessível publicamente, e configure-a no Flutter pela variável `PIM_API_URL`, conforme o formato esperado pelo aplicativo. Para um túnel local, exemplos são `ngrok http 5000` ou `cloudflared tunnel --url http://localhost:5000`, caso essas ferramentas estejam instaladas e configuradas. URLs temporárias podem mudar.

Uma URL pública HTTPS permite que a ferramenta remota alcance sua API; CORS é relevante para clientes executados em navegadores e não substitui conectividade de rede nem autenticação. Não embuta `DB_CONNECTION` ou `JWT_SECRET` nos clientes.

## CORS

A API usa somente a política `Padrao`. `CORS_ORIGINS` aceita uma ou várias origens exatas separadas por vírgula. A configuração permite qualquer header e método para as origens listadas, sem liberar qualquer origem indiscriminadamente. A política é aplicada antes da autenticação e autorização para que também responda às requisições preflight `OPTIONS`.

Valores locais incluídos em `appsettings.Development.json.example`:

- `http://localhost:8080` — Frontend.
- `http://localhost:5001` — Flutter Web.

Inclua a origem exata usada pelo navegador, incluindo protocolo e porta, e reinicie a API após mudar a configuração. Clientes Flutter nativos não aplicam as restrições CORS do navegador, mas ainda precisam alcançar a URL da API.

## Swagger e Health Check

- Swagger UI: <http://localhost:5000/swagger>
- Health Check: <http://localhost:5000/health> (inclui verificação do banco)
- Raiz: <http://localhost:5000/> (redireciona para o Swagger)
- Prefixo dos endpoints: `/api/v1`

## Testes

Os testes .NET não dependem do banco Supabase:

```bash
dotnet test Pim.sln
```

O teste SQL opcional `database/tests/smoke_test.sql` exige PostgreSQL com o schema preparado. Ele pode ser executado contra o banco configurado com:

```bash
psql "<URI_DO_SESSION_POOLER>" -v ON_ERROR_STOP=1 -f database/tests/smoke_test.sql
```

## Problemas comuns

| Sintoma | O que verificar |
| --- | --- |
| SDK ausente ou incompatível | Instale .NET SDK 8, reabra o terminal e confira `dotnet --version`. |
| Erro ou timeout ao conectar ao Supabase | Confira host, porta, usuário e projeto em **Connect**; use Session pooler na porta 5432 e valide a rede. |
| Falha de autenticação PostgreSQL | Confira a senha e codifique caracteres reservados na URI. |
| `DB_CONNECTION` ausente | Crie o `appsettings.Development.json`, preencha a conexão e rode no perfil `http` ou `rede`. |
| `JWT_SECRET` ausente/inválido | Configure um segredo privado com pelo menos 32 caracteres. |
| Erro CORS no navegador | Confira protocolo, host e porta da página e adicione a origem exata a `CORS_ORIGINS`; reinicie a API. |
| Outro dispositivo não acessa a API | Use o perfil `rede`, o IP local do servidor e libere a porta 5000 no firewall. `localhost` não identifica outro computador. |
| FlutLab não alcança a API | Use uma URL pública HTTPS e defina `PIM_API_URL` no Flutter; endereços localhost e IPs privados não são roteáveis pela ferramenta remota. |
| Porta 5000 ocupada | Encerre o processo que usa a porta ou ajuste `applicationUrl` em `src/Pim.Api/Properties/launchSettings.json`. |
| `/health` indica falha | A rota verifica também o PostgreSQL; confira conectividade e logs da aplicação. |

