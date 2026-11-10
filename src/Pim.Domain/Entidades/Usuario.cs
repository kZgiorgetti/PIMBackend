using Pim.Domain.Enums;
using Pim.Domain.Excecoes;

namespace Pim.Domain.Entidades;

public class Usuario
{
    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string SenhaHash { get; private set; } = string.Empty;
    public PerfilUsuario Perfil { get; private set; }
    public bool Ativo { get; private set; } = true;
    public int TentativasFalhas { get; private set; }
    public DateTime? BloqueadoAte { get; private set; }
    public DateTime DataCriacao { get; private set; }
    public DateTime DataAtualizacao { get; private set; }

    private Usuario() { } // EF Core

    public static Usuario Criar(string nome, string email, string senhaHash, PerfilUsuario perfil)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new RegraDeNegocioException("O nome é obrigatório.");
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) throw new RegraDeNegocioException("E-mail inválido.");
        if (string.IsNullOrWhiteSpace(senhaHash)) throw new RegraDeNegocioException("A senha é obrigatória.");
        return new Usuario
        {
            Nome = nome.Trim(), Email = email.Trim().ToLowerInvariant(), SenhaHash = senhaHash, Perfil = perfil,
            DataCriacao = DateTime.UtcNow, DataAtualizacao = DateTime.UtcNow
        };
    }

    public void Atualizar(string nome, string email)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new RegraDeNegocioException("O nome é obrigatório.");
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) throw new RegraDeNegocioException("E-mail inválido.");
        Nome = nome.Trim();
        Email = email.Trim().ToLowerInvariant();
    }

    public void AlterarPerfil(PerfilUsuario perfil) => Perfil = perfil;
    public void DefinirSenhaHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash)) throw new RegraDeNegocioException("A senha é obrigatória.");
        SenhaHash = hash; TentativasFalhas = 0; BloqueadoAte = null;
    }
    public void Ativar() => Ativo = true;
    public void Inativar() => Ativo = false;

    public bool EstaBloqueado(DateTime? agoraUtc = null) => BloqueadoAte.HasValue && BloqueadoAte.Value > (agoraUtc ?? DateTime.UtcNow);

    /// <summary>Registra falha de login; ao atingir o limite bloqueia temporariamente.</summary>
    public void RegistrarFalhaLogin(int maxTentativas, TimeSpan duracaoBloqueio, DateTime? agoraUtc = null)
    {
        var agora = agoraUtc ?? DateTime.UtcNow;
        if (BloqueadoAte.HasValue && BloqueadoAte.Value <= agora) { TentativasFalhas = 0; BloqueadoAte = null; }
        TentativasFalhas++;
        if (TentativasFalhas >= maxTentativas) BloqueadoAte = agora + duracaoBloqueio;
    }

    public void RegistrarLoginOk() { TentativasFalhas = 0; BloqueadoAte = null; }
}
