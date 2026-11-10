using Pim.Domain.Excecoes;

namespace Pim.Domain.Entidades;

/// <summary>Refresh token: o valor em texto nunca é guardado, apenas o hash SHA-256.</summary>
public class RefreshToken
{
    public int Id { get; private set; }
    public int UsuarioId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime DataCriacao { get; private set; }
    public DateTime DataExpiracao { get; private set; }
    public DateTime? DataRevogacao { get; private set; }
    public string? SubstituidoPorHash { get; private set; }

    private RefreshToken() { } // EF Core

    public static RefreshToken Criar(int usuarioId, string tokenHash, DateTime expiraEm)
    {
        if (string.IsNullOrWhiteSpace(tokenHash)) throw new RegraDeNegocioException("Token inválido.");
        return new RefreshToken { UsuarioId = usuarioId, TokenHash = tokenHash, DataCriacao = DateTime.UtcNow, DataExpiracao = expiraEm };
    }

    public bool Revogado => DataRevogacao.HasValue;
    public bool EstaAtivo(DateTime? agoraUtc = null) => !Revogado && DataExpiracao > (agoraUtc ?? DateTime.UtcNow);
    public void Revogar(string? substituidoPorHash = null)
    {
        DataRevogacao ??= DateTime.UtcNow;
        SubstituidoPorHash = substituidoPorHash ?? SubstituidoPorHash;
    }
}
