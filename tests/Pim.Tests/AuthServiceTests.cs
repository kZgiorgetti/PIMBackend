using Microsoft.Extensions.Logging.Abstractions;
using Pim.Application.Dtos;
using Pim.Application.Excecoes;
using Pim.Application.Interfaces;
using Pim.Application.Services;
using Pim.Domain.Entidades;
using Pim.Domain.Enums;
using Pim.Tests.Fakes;

namespace Pim.Tests;

public class AuthServiceTests
{
    private readonly FakeUsuarioRepository _usuarios = new();
    private readonly FakeColaboradorRepository _colabs = new();
    private readonly FakeRefreshTokenRepository _refresh = new();
    private readonly AuthService _svc;

    public AuthServiceTests()
    {
        var hasher = new FakeHasher();
        _usuarios.Itens.Add(Usuario.Criar("Ana", "ana@pim.local", hasher.Hash("Senha@123"), PerfilUsuario.Colaborador).ComId(1));
        _colabs.Itens.Add(Colaborador.Criar("Ana", usuarioId: 1).ComId(7));
        _svc = new AuthService(_usuarios, _colabs, _refresh, hasher, new FakeTokenService(), new FakeUow(),
            new AuthOptions { MaxTentativas = 3, BloqueioMinutos = 15, RefreshDias = 7 }, NullLogger<AuthService>.Instance);
    }

    [Fact]
    public async Task Login_Ok_DevolveTokensEColaboradorId()
    {
        var r = await _svc.LoginAsync(new LoginRequest("ANA@pim.local", "Senha@123"));
        Assert.StartsWith("access-1-7", r.AccessToken);
        Assert.Equal(7, r.Usuario.ColaboradorId);
        Assert.Single(_refresh.Itens);
    }

    [Fact]
    public async Task Login_SenhaErrada_Lanca401()
    {
        await Assert.ThrowsAsync<UnauthorizedException>(() => _svc.LoginAsync(new LoginRequest("ana@pim.local", "errada")));
    }

    [Fact]
    public async Task Login_UsuarioInexistente_Lanca401()
    {
        await Assert.ThrowsAsync<UnauthorizedException>(() => _svc.LoginAsync(new LoginRequest("x@x.com", "Senha@123")));
    }

    [Fact]
    public async Task Login_UsuarioInativo_Lanca401()
    {
        _usuarios.Itens[0].Inativar();
        await Assert.ThrowsAsync<UnauthorizedException>(() => _svc.LoginAsync(new LoginRequest("ana@pim.local", "Senha@123")));
    }

    [Fact]
    public async Task Login_BloqueiaAposTentativasEMesmoComSenhaCorreta()
    {
        for (var i = 0; i < 3; i++)
            await Assert.ThrowsAsync<UnauthorizedException>(() => _svc.LoginAsync(new LoginRequest("ana@pim.local", "errada")));
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => _svc.LoginAsync(new LoginRequest("ana@pim.local", "Senha@123")));
        Assert.Contains("bloqueada", ex.Message);
    }

    [Fact]
    public async Task Refresh_RotacionaEInvalidaOTokenAntigo()
    {
        var login = await _svc.LoginAsync(new LoginRequest("ana@pim.local", "Senha@123"));
        var novo = await _svc.RefreshAsync(new RefreshRequest(login.RefreshToken));
        Assert.NotEqual(login.RefreshToken, novo.RefreshToken);
        // reutilizar o token antigo falha e revoga a família inteira
        await Assert.ThrowsAsync<UnauthorizedException>(() => _svc.RefreshAsync(new RefreshRequest(login.RefreshToken)));
        await Assert.ThrowsAsync<UnauthorizedException>(() => _svc.RefreshAsync(new RefreshRequest(novo.RefreshToken)));
    }

    [Fact]
    public async Task Refresh_TokenDesconhecido_Lanca401()
    {
        await Assert.ThrowsAsync<UnauthorizedException>(() => _svc.RefreshAsync(new RefreshRequest("qualquer")));
    }
}
