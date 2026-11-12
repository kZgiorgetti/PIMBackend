# PIM IV — Backend

Backend do PIM IV, projeto acadêmico voltado ao gerenciamento de colaboradores, equipes e tarefas. Esta aplicação disponibiliza uma API REST para os clientes Web e Mobile do projeto.

O Backend centraliza as regras de negócio, autenticação, autorização e acesso aos dados. O PIMFrontend e o PIMMobile são aplicações independentes e consomem a mesma API, usando o prefixo `/api/v1`.

## Tecnologias

- **C# e ASP.NET Core 8** para a API REST.
- **Entity Framework Core 8** para persistência e acesso a dados.
- **PostgreSQL 15+**, hospedado no **Supabase**. O esquema e os objetos do banco são definidos pelos scripts SQL em `database/`.
- **Npgsql** e **EFCore.NamingConventions** para conexão PostgreSQL e nomes `snake_case`.
- **JWT Bearer** para autenticação própria da API; o Supabase Auth não é utilizado.
- **BCrypt.Net-Next** para hash de senhas.
- **FluentValidation** para validação de requisições.
- **Swagger/OpenAPI (Swashbuckle)** para documentação interativa.
- **ASP.NET Core Health Checks** para verificar a saúde da aplicação e do banco.
- **xUnit** e **Microsoft.AspNetCore.Mvc.Testing** para testes automatizados.

## Arquitetura e pastas

A solução `Pim.sln` separa a aplicação em camadas: `Pim.Api` recebe requisições HTTP; `Pim.Application` contém serviços, DTOs e validações; `Pim.Domain` concentra entidades e regras de domínio; e `Pim.Infrastructure` implementa persistência, repositórios e segurança.

| Pasta | Conteúdo |
| --- | --- |
| `src/` | Projetos da API e das camadas Application, Domain e Infrastructure. |
| `tests/` | Projeto `Pim.Tests`, com testes de domínio, serviços e integração HTTP. |
| `database/` | Scripts do esquema, procedures, triggers, segurança, dados iniciais e teste SQL. `pim_completo.sql` reúne a instalação do banco. |
| `docs/` | Documentação da arquitetura, contrato da API, decisões técnicas e modelo de dados. |
| `.github/` | Workflow de CI para restaurar, compilar e testar a solução e validar os scripts SQL. |

## Pré-requisitos

- Windows, Linux ou macOS compatível com .NET 8.
- [.NET SDK 8](https://dotnet.microsoft.com/download/dotnet/8.0).
- Uma conta no [Supabase](https://supabase.com) e um projeto com banco PostgreSQL.
- `psql` é opcional; pode ser usado para executar os scripts SQL pelo terminal.

Verifique o SDK instalado:

```bash
dotnet --version
```

A versão principal exibida deve ser `8`. Os comandos `dotnet` desta documentação devem ser executados na raiz do repositório, salvo quando indicado o contrário.

## Banco de dados no Supabase

1. Crie um projeto no Supabase e defina uma senha para o banco. Guarde-a com segurança.
2. Abra o **SQL Editor** do projeto e execute o conteúdo de `database/pim_completo.sql`. Esse script reúne o esquema, as procedures, triggers, dados iniciais e configurações de segurança; é idempotente, podendo ser executado novamente.
3. Alternativamente, com `psql` instalado, execute da raiz do repositório:

   ```bash
   psql "<URI_DO_SESSION_POOLER>" -v ON_ERROR_STOP=1 -f database/pim_completo.sql
   ```

4. No painel do Supabase, abra **Connect** e copie a conexão **Session pooler**. Use o host `...pooler.supabase.com`, a porta `5432` e o usuário no formato `postgres.<referência-do-projeto>`. O Session Pooler é indicado para redes IPv4. A API não usa o modo Transaction Pooler.
5. Insira a senha do banco na URI. Caracteres especiais devem ser codificados para URL: por exemplo, `@` como `%40`. Não compartilhe nem publique essa URI.

Exemplo ilustrativo, com valores fictícios:

```text
postgresql://postgres.PROJECT_REF:SENHA_CODIFICADA@aws-0-regiao.pooler.supabase.com:5432/postgres
```

A API normaliza a URI do painel e configura SSL para a conexão. Se não conectar, confira host, porta, usuário, senha codificada, acesso à rede e se o script SQL foi executado. Mensagens como `password authentication failed` indicam credenciais incorretas; timeout ou `Network is unreachable` podem indicar host/método de conexão inadequado. Consulte os logs da API e as configurações de conexão do Supabase.

## Configuração local do Backend

Crie o arquivo de desenvolvimento a partir do exemplo, na raiz do repositório:

```bash
cp src/Pim.Api/appsettings.Development.json.example src/Pim.Api/appsettings.Development.json
```

No PowerShell, o comando equivalente é:

```powershell
Copy-Item src/Pim.Api/appsettings.Development.json.example src/Pim.Api/appsettings.Development.json
```

Edite o arquivo criado e substitua os valores de exemplo:

| Configuração | Finalidade |
| --- | --- |
| `DB_CONNECTION` | URI do PostgreSQL no Supabase, preferencialmente do Session Pooler. |
| `JWT_SECRET` | Chave usada para assinar e validar tokens JWT. Deve ter pelo menos 32 caracteres. Gere uma chave aleatória; por exemplo, `openssl rand -base64 48`. |
| `ADMIN_EMAIL` | E-mail usado para criar o primeiro usuário administrador, quando ainda não há Admin no banco. |
| `ADMIN_PASSWORD` | Senha inicial desse administrador. Use uma senha forte. O hash é gerado pela aplicação. |
| `CORS_ORIGINS` | Origens web autorizadas pelo CORS, separadas por vírgula e sem barra final. Exemplo: `http://localhost:8080`. |

Exemplo fictício:

```json
{
  "DB_CONNECTION": "postgresql://postgres.PROJECT_REF:SENHA_CODIFICADA@aws-0-regiao.pooler.supabase.com:5432/postgres",
  "JWT_SECRET": "segredo-local-ficticio-com-mais-de-32-caracteres",
  "ADMIN_EMAIL": "admin@exemplo.local",
  "ADMIN_PASSWORD": "Senha-Ficticia-123",
  "CORS_ORIGINS": "http://localhost:8080"
}
```

O administrador inicial só é criado se o banco ainda não tiver um usuário com perfil Admin. Defina e guarde as credenciais antes da primeira execução. `appsettings.Development.json` está listado no `.gitignore`: mantenha nele apenas segredos locais e nunca envie arquivos com credenciais, tokens ou senhas ao GitHub.

## Restaurar, compilar e executar

Na raiz do repositório:

```bash
dotnet restore Pim.sln
dotnet build Pim.sln
dotnet run --project src/Pim.Api/Pim.Api.csproj
```

O perfil `http` está configurado para `http://localhost:5000` e define o ambiente `Development`, que carrega `appsettings.Development.json`. Para selecioná-lo explicitamente:

```bash
dotnet run --project src/Pim.Api/Pim.Api.csproj --launch-profile http
```

Há também o perfil `rede`, que escuta em todas as interfaces da máquina na porta `5000`:

```bash
dotnet run --project src/Pim.Api/Pim.Api.csproj --launch-profile rede
```

Use `rede` quando outro dispositivo na mesma rede precisar acessar a API. No firewall do computador, permita conexões de entrada na porta `5000` e use o endereço IP local do computador servidor, por exemplo `http://192.168.1.20:5000`. O IP é apenas um exemplo; descubra o endereço da sua rede.

A aplicação tenta conectar ao banco e pode repetir a inicialização por até 15 tentativas, com intervalos de 3 segundos. Se o banco não estiver acessível, verifique os logs do terminal e a configuração do Supabase.

## Integração com Frontend e Mobile

Frontend e Mobile são clientes separados: cada um envia requisições HTTP à API em `/api/v1`, incluindo o token JWT no cabeçalho `Authorization: Bearer` quando a rota exigir autenticação. Os endpoints e exemplos estão em [docs/API.md](docs/API.md).

Para o Frontend Web, configure em `CORS_ORIGINS` a origem exata em que ele é servido, como `http://localhost:8080`. Para mais de uma origem, separe-as por vírgulas, por exemplo `http://localhost:8080,https://web.exemplo.com`. Reinicie a API após alterar a configuração. CORS é uma política aplicada por navegadores; um cliente Mobile nativo não depende dela como um navegador, embora ainda precise alcançar o endereço da API.

No mesmo computador, `localhost` aponta para a própria máquina e pode ser usado pelo cliente local. Em um celular na rede local, use o IP local do computador e execute a API com o perfil `rede`. Em ambientes remotos como FlutLab, `localhost` aponta para o próprio ambiente remoto, não para seu computador. Para testes externos, exponha temporariamente a porta local usando um túnel:

```bash
ngrok http 5000
```

ou, com o Cloudflare Tunnel instalado e autenticado conforme a configuração do ambiente:

```bash
cloudflared tunnel --url http://localhost:5000
```

Use a URL HTTPS pública fornecida pelo túnel como base da API no cliente Flutter, configurando a variável **`PIM_API_URL`** (com o sufixo `/api/v1` conforme o formato esperado pelo cliente). A URL temporária pode mudar ao reiniciar o túnel. Não coloque segredos do banco ou `JWT_SECRET` no aplicativo cliente.

## Endpoints e documentação da API

- Swagger: <http://localhost:5000/swagger>
- Health check: <http://localhost:5000/health> — inclui verificação do banco de dados.
- Prefixo da API: `/api/v1`.
- Contrato, recursos, exemplos e autenticação: [docs/API.md](docs/API.md).

A raiz <http://localhost:5000/> redireciona para o Swagger. Consulte o contrato existente antes de integrar novos recursos; este README não substitui a documentação detalhada da API.

## Testes e validação

Na raiz do repositório, execute:

```bash
dotnet restore Pim.sln
dotnet build Pim.sln
dotnet test Pim.sln
```

Os testes .NET usam um provedor EF Core InMemory e uma aplicação de teste configurada para o ambiente `Testing`; não dependem do Supabase. O banco real não é necessário para esses testes.

Há também um teste SQL opcional para triggers, procedures e restrições. Ele precisa de um PostgreSQL com o esquema preparado; para executá-lo contra o Supabase, rode da raiz:

```bash
psql "<URI_DO_SESSION_POOLER>" -v ON_ERROR_STOP=1 -f database/tests/smoke_test.sql
```

O teste executa em transação e desfaz suas alterações com `ROLLBACK`. O workflow em `.github/workflows/ci.yml` também valida os scripts em um PostgreSQL temporário.

## Problemas comuns

| Sintoma | Possíveis causas | Soluções |
| --- | --- | --- |
| SDK .NET não encontrado ou versão incompatível | .NET SDK 8 não instalado ou não disponível no `PATH`. | Instale o SDK 8, abra um novo terminal e confira `dotnet --version`. |
| Projeto ou caminho `.csproj` não encontrado | Comando executado fora da raiz ou caminho digitado incorretamente. | Volte à raiz do repositório e use `src/Pim.Api/Pim.Api.csproj`; confira os nomes com `Get-ChildItem` ou `ls`. |
| Perfil de inicialização inexistente | Nome diferente dos perfis declarados no `launchSettings.json`. | Use `http` ou `rede`, definidos em `src/Pim.Api/Properties/launchSettings.json`. |
| Erro de conexão com Supabase, timeout ou `Network is unreachable` | Host incorreto, indisponibilidade de rede ou uso de conexão direta incompatível com a rede. | Confira **Connect** no Supabase e use o Session Pooler na porta `5432`; valide a rede e o estado do projeto. |
| `password authentication failed` ou outra falha de senha/connection string | Senha, usuário ou URI incorretos; caracteres especiais sem codificação. | Confira a URI copiada, usuário `postgres.<referência>`, senha e codificação URL de caracteres especiais. |
| `JWT_SECRET` ausente ou inválido | Chave não configurada ou com menos de 32 caracteres. | Preencha `JWT_SECRET` no arquivo de Development com uma chave aleatória de pelo menos 32 caracteres. |
| `DB_CONNECTION` ausente | Arquivo local não criado, valor vazio ou ambiente Development não ativo. | Copie o `.example`, configure a URI e execute pelo perfil `http` ou `rede`. |
| Erro de CORS no navegador | Origem do Frontend ausente ou divergente em protocolo, host ou porta. | Inclua a origem exata em `CORS_ORIGINS` e reinicie a API. Não inclua caminhos da URL. |
| Porta 5000 já utilizada | Outro processo está escutando na porta configurada. | Encerre o processo que ocupa a porta ou ajuste a porta em `launchSettings.json` e acesse a nova URL. |
| Swagger indisponível | API não iniciou, porta diferente ou erro durante inicialização/conexão com banco. | Confira o terminal, confirme o endereço/porta e valide `DB_CONNECTION`; tente `/swagger` após a API iniciar. |
| Endpoint `/health` indisponível ou retorna falha | API fora do ar ou verificação do banco falhou. | Confira os logs e a conectividade ao PostgreSQL; `/health` também verifica o banco. |
| Não consegue autenticar o administrador | Admin ainda não criado, credenciais diferentes ou usuário bloqueado após tentativas falhas. | Confirme `ADMIN_EMAIL` e `ADMIN_PASSWORD` usados na primeira inicialização. Se já houver Admin, a inicialização não redefine sua senha; use o fluxo autorizado de redefinição ou a administração existente. |

## Documentação complementar

- [Arquitetura](docs/ARQUITETURA.md)
- [Contrato e exemplos da API](docs/API.md)
- [Decisões técnicas](docs/DECISOES.md)
- [Modelo entidade-relacionamento e dicionário de dados](docs/MER.md)
