using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Pim.Application.Dtos;
using Pim.Domain.Enums;

namespace Pim.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Database:Provider", "InMemory");
        builder.UseSetting("Database:InMemoryName", Guid.NewGuid().ToString());
        builder.UseSetting("JWT_SECRET", "chave-de-teste-com-mais-de-trinta-e-dois-caracteres");
        builder.UseSetting("ADMIN_EMAIL", "admin@teste.local");
        builder.UseSetting("ADMIN_PASSWORD", "Admin@12345");
        builder.UseSetting("RateLimit:Login", "1000");
    }
}

public class ApiIntegrationTests : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private readonly ApiFactory _factory;
    public ApiIntegrationTests(ApiFactory factory) => _factory = factory;

    private async Task<HttpClient> ClienteAsync(string email, string senha)
    {
        var c = _factory.CreateClient();
        var r = await c.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, senha), Json);
        r.EnsureSuccessStatusCode();
        var t = (await r.Content.ReadFromJsonAsync<TokenResponse>(Json))!;
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", t.AccessToken);
        return c;
    }

    [Fact]
    public async Task Login_Admin_Retorna200ComToken()
    {
        var r = await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin@teste.local", "Admin@12345"), Json);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var t = await r.Content.ReadFromJsonAsync<TokenResponse>(Json);
        Assert.False(string.IsNullOrWhiteSpace(t!.AccessToken));
        Assert.Equal(PerfilUsuario.Admin, t.Usuario.Perfil);
    }

    [Fact]
    public async Task Login_SenhaErrada_Retorna401ComProblemDetails()
    {
        var r = await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin@teste.local", "errada"), Json);
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        Assert.Equal("application/problem+json", r.Content.Headers.ContentType?.MediaType);
        Assert.Contains("\"error\"", await r.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_CorpoInvalido_Retorna400()
    {
        var r = await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("", ""), Json);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Tarefas_SemToken_Retorna401()
    {
        var r = await _factory.CreateClient().GetAsync("/api/v1/tarefas");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Tarefas_ComToken_Retorna200EPaginacao()
    {
        var admin = await ClienteAsync("admin@teste.local", "Admin@12345");
        var r = await admin.GetAsync("/api/v1/tarefas?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var p = await r.Content.ReadFromJsonAsync<PaginaResponse<TarefaResponse>>(Json);
        Assert.Equal(1, p!.Page);
    }

    [Fact]
    public async Task Tarefas_PageSizeAcimaDe100_Retorna400()
    {
        var admin = await ClienteAsync("admin@teste.local", "Admin@12345");
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/api/v1/tarefas?pageSize=101")).StatusCode);
    }

    [Fact]
    public async Task FluxoCompleto_ColaboradorSoEnxergaEAlteraAsProprias()
    {
        var admin = await ClienteAsync("admin@teste.local", "Admin@12345");

        // Admin cria usuário Colaborador (colaborador vinculado é criado junto) e um segundo colaborador
        var criar = await admin.PostAsJsonAsync("/api/v1/usuarios",
            new CriarUsuarioRequest("Ana", "ana@teste.local", "Senha@123", PerfilUsuario.Colaborador), Json);
        Assert.Equal(HttpStatusCode.Created, criar.StatusCode);
        Assert.NotNull(criar.Headers.Location);
        var ana = (await criar.Content.ReadFromJsonAsync<UsuarioResponse>(Json))!;
        Assert.NotNull(ana.ColaboradorId);

        var outro = (await (await admin.PostAsJsonAsync("/api/v1/colaboradores", new ColaboradorRequest("Bruno"), Json))
            .Content.ReadFromJsonAsync<ColaboradorResponse>(Json))!;

        var tAna = (await (await admin.PostAsJsonAsync("/api/v1/tarefas",
            new CriarTarefaRequest("Tarefa da Ana", colaboradorId: ana.ColaboradorId), Json)).Content.ReadFromJsonAsync<TarefaResponse>(Json))!;
        var tBruno = (await (await admin.PostAsJsonAsync("/api/v1/tarefas",
            new CriarTarefaRequest("Tarefa do Bruno", colaboradorId: outro.Id), Json)).Content.ReadFromJsonAsync<TarefaResponse>(Json))!;

        var anaCli = await ClienteAsync("ana@teste.local", "Senha@123");

        // lista: só a dela
        var lista = (await (await anaCli.GetAsync("/api/v1/tarefas")).Content.ReadFromJsonAsync<PaginaResponse<TarefaResponse>>(Json))!;
        Assert.Single(lista.Itens);
        Assert.Equal(tAna.Id, lista.Itens[0].Id);

        // acesso e alteração da tarefa de outro: 403
        Assert.Equal(HttpStatusCode.Forbidden, (await anaCli.GetAsync($"/api/v1/tarefas/{tBruno.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await anaCli.PatchAsJsonAsync($"/api/v1/tarefas/{tBruno.Id}/status", new AlterarStatusRequest(StatusTarefa.Concluida), Json)).StatusCode);

        // alteração da própria: 200
        var ok = await anaCli.PatchAsJsonAsync($"/api/v1/tarefas/{tAna.Id}/status", new AlterarStatusRequest(StatusTarefa.EmAndamento), Json);
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        // sem permissão de gestão e de usuários
        Assert.Equal(HttpStatusCode.Forbidden, (await anaCli.PostAsJsonAsync("/api/v1/tarefas", new CriarTarefaRequest("x"), Json)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await anaCli.GetAsync("/api/v1/usuarios")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await anaCli.GetAsync("/api/v1/colaboradores")).StatusCode);
    }

    [Fact]
    public async Task Health_Responde200()
    {
        Assert.Equal(HttpStatusCode.OK, (await _factory.CreateClient().GetAsync("/health")).StatusCode);
    }
}
