using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Pim.Application.Dtos;
using Pim.Application.Services;

namespace Pim.Api.Controllers;

[Route("api/v1/auth")]
public class AuthController : BaseApiController
{
    private readonly AuthService _auth;
    public AuthController(AuthService auth) => _auth = auth;

    /// <summary>Autentica o usuário e devolve access token (curto) e refresh token.</summary>
    /// <remarks>Exemplo: <c>{ "email": "admin@pim.local", "senha": "Admin@12345" }</c>. Limite mais restrito de requisições e bloqueio temporário após várias falhas.</remarks>
    [HttpPost("login"), AllowAnonymous, EnableRateLimiting("login")]
    [ProducesResponseType(typeof(TokenResponse), 200)]
    public async Task<ActionResult<TokenResponse>> Login(LoginRequest req, CancellationToken ct) => Ok(await _auth.LoginAsync(req, ct));

    /// <summary>Troca um refresh token válido por um novo par de tokens (o anterior é revogado).</summary>
    [HttpPost("refresh"), AllowAnonymous, EnableRateLimiting("login")]
    [ProducesResponseType(typeof(TokenResponse), 200)]
    public async Task<ActionResult<TokenResponse>> Refresh(RefreshRequest req, CancellationToken ct) => Ok(await _auth.RefreshAsync(req, ct));

    /// <summary>Revoga o refresh token informado (encerra a sessão).</summary>
    [HttpPost("logout"), AllowAnonymous]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Logout(RefreshRequest req, CancellationToken ct) { await _auth.LogoutAsync(req, ct); return NoContent(); }

    /// <summary>Dados do usuário autenticado.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UsuarioResponse), 200)]
    public async Task<ActionResult<UsuarioResponse>> Me(CancellationToken ct)
    {
        var id = int.Parse(User.FindFirst("sub")!.Value);
        return Ok(await _auth.MeAsync(id, ct));
    }
}
