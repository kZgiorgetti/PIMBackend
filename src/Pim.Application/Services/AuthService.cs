using Microsoft.Extensions.Logging;
using Pim.Application.Dtos;
using Pim.Application.Excecoes;
using Pim.Application.Interfaces;
using Pim.Application.Mapeamentos;
using Pim.Domain.Entidades;

namespace Pim.Application.Services;

public class AuthService
{
    private const string MsgCredenciais = "E-mail ou senha inválidos.";
    private readonly IUsuarioRepository _usuarios;
    private readonly IColaboradorRepository _colaboradores;
    private readonly IRefreshTokenRepository _refresh;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly IUnitOfWork _uow;
    private readonly AuthOptions _opt;
    private readonly ILogger<AuthService> _log;

    public AuthService(IUsuarioRepository usuarios, IColaboradorRepository colaboradores, IRefreshTokenRepository refresh,
        IPasswordHasher hasher, ITokenService tokens, IUnitOfWork uow, AuthOptions opt, ILogger<AuthService> log)
    {
        _usuarios = usuarios; _colaboradores = colaboradores; _refresh = refresh; _hasher = hasher;
        _tokens = tokens; _uow = uow; _opt = opt; _log = log;
    }

    public async Task<TokenResponse> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var u = await _usuarios.GetByEmailAsync(email, ct);

        if (u == null || !u.Ativo)
        {
            _log.LogWarning("Segurança: login recusado (usuário inexistente ou inativo).");
            throw new UnauthorizedException(MsgCredenciais);
        }
        if (u.EstaBloqueado())
        {
            _log.LogWarning("Segurança: login recusado, conta bloqueada. UsuarioId={UsuarioId}", u.Id);
            throw new UnauthorizedException("Conta temporariamente bloqueada por excesso de tentativas. Tente novamente mais tarde.");
        }
        if (!_hasher.Verificar(req.Senha, u.SenhaHash))
        {
            u.RegistrarFalhaLogin(_opt.MaxTentativas, TimeSpan.FromMinutes(_opt.BloqueioMinutos));
            await _uow.SalvarAsync(null, ct);
            _log.LogWarning("Segurança: falha de login. UsuarioId={UsuarioId} Tentativas={Tentativas}", u.Id, u.TentativasFalhas);
            throw new UnauthorizedException(MsgCredenciais);
        }

        u.RegistrarLoginOk();
        var resp = await EmitirAsync(u, ct);
        _log.LogInformation("Segurança: login realizado. UsuarioId={UsuarioId}", u.Id);
        return resp;
    }

    /// <summary>Rotaciona o refresh token. Reutilização de um token já usado revoga todos os tokens do usuário.</summary>
    public async Task<TokenResponse> RefreshAsync(RefreshRequest req, CancellationToken ct = default)
    {
        var hash = _tokens.Hash(req.RefreshToken);
        var atual = await _refresh.GetByHashAsync(hash, ct) ?? throw new UnauthorizedException("Sessão inválida. Faça login novamente.");

        if (atual.Revogado)
        {
            await _refresh.RevogarTodosDoUsuarioAsync(atual.UsuarioId, ct);
            await _uow.SalvarAsync(null, ct);
            _log.LogWarning("Segurança: reutilização de refresh token detectada. UsuarioId={UsuarioId}", atual.UsuarioId);
            throw new UnauthorizedException("Sessão inválida. Faça login novamente.");
        }
        if (!atual.EstaAtivo()) throw new UnauthorizedException("Sessão expirada. Faça login novamente.");

        var u = await _usuarios.GetByIdAsync(atual.UsuarioId, ct);
        if (u == null || !u.Ativo) throw new UnauthorizedException("Sessão inválida. Faça login novamente.");

        var (tokenNovo, hashNovo) = _tokens.GerarRefreshToken();
        atual.Revogar(hashNovo);
        return await EmitirAsync(u, ct, tokenNovo, hashNovo);
    }

    public async Task LogoutAsync(RefreshRequest req, CancellationToken ct = default)
    {
        var t = await _refresh.GetByHashAsync(_tokens.Hash(req.RefreshToken), ct);
        if (t == null) return;
        t.Revogar();
        await _uow.SalvarAsync(null, ct);
    }

    public async Task<UsuarioResponse> MeAsync(int usuarioId, CancellationToken ct = default)
    {
        var u = await _usuarios.GetByIdAsync(usuarioId, ct) ?? throw new NotFoundException("Usuário não encontrado.");
        var c = await _colaboradores.GetByUsuarioIdAsync(u.Id, ct);
        return u.ParaResponse(c?.Id);
    }

    private async Task<TokenResponse> EmitirAsync(Usuario u, CancellationToken ct, string? refreshPronto = null, string? hashPronto = null)
    {
        var colab = await _colaboradores.GetByUsuarioIdAsync(u.Id, ct);
        var (access, expira) = _tokens.GerarAccessToken(u, colab?.Id);
        var (refreshTexto, refreshHash) = refreshPronto != null ? (refreshPronto, hashPronto!) : _tokens.GerarRefreshToken();
        await _refresh.AddAsync(RefreshToken.Criar(u.Id, refreshHash, DateTime.UtcNow.AddDays(_opt.RefreshDias)), ct);
        await _uow.SalvarAsync(null, ct);
        return new TokenResponse(access, refreshTexto, expira, u.ParaResponse(colab?.Id));
    }
}
