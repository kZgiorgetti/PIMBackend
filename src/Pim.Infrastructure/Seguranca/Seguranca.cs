using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Pim.Application.Interfaces;
using Pim.Domain.Entidades;

namespace Pim.Infrastructure.Seguranca;

public class JwtOptions
{
    public string Secret { get; set; } = "";
    public string Issuer { get; set; } = "pim-api";
    public string Audience { get; set; } = "pim-clientes";
    public int AccessMinutos { get; set; } = 15;

    public static JwtOptions De(IConfiguration c)
    {
        var o = new JwtOptions
        {
            Secret = c["JWT_SECRET"] ?? c["Jwt:Secret"] ?? "",
            Issuer = c["Jwt:Issuer"] ?? "pim-api",
            Audience = c["Jwt:Audience"] ?? "pim-clientes",
            AccessMinutos = int.TryParse(c["Jwt:AccessMinutos"], out var m) ? m : 15
        };
        if (o.Secret.Length < 32)
            throw new InvalidOperationException("Defina JWT_SECRET (variável de ambiente) com no mínimo 32 caracteres.");
        return o;
    }
}

public class PasswordHasher : IPasswordHasher
{
    public string Hash(string senha) => BCrypt.Net.BCrypt.HashPassword(senha, workFactor: 11);
    public bool Verificar(string senha, string hash)
    {
        try { return BCrypt.Net.BCrypt.Verify(senha, hash); }
        catch (BCrypt.Net.SaltParseException) { return false; }
    }
}

public class TokenService : ITokenService
{
    private readonly JwtOptions _o;
    public TokenService(JwtOptions o) => _o = o;

    public (string Token, DateTime ExpiraEm) GerarAccessToken(Usuario u, int? colaboradorId)
    {
        var expira = DateTime.UtcNow.AddMinutes(_o.AccessMinutos);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, u.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, u.Email),
            new("name", u.Nome),
            new("role", u.Perfil.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        if (colaboradorId.HasValue) claims.Add(new Claim("colaboradorId", colaboradorId.Value.ToString()));

        var cred = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_o.Secret)), SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(_o.Issuer, _o.Audience, claims, notBefore: DateTime.UtcNow, expires: expira, signingCredentials: cred);
        return (new JwtSecurityTokenHandler().WriteToken(jwt), expira);
    }

    public (string Token, string Hash) GerarRefreshToken()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        return (token, Hash(token));
    }

    public string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}
