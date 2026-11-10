using Pim.Domain.Excecoes;

namespace Pim.Domain.Entidades;

public class Colaborador
{
    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? Cargo { get; private set; }
    public bool Ativo { get; private set; } = true;
    public int? UsuarioId { get; private set; }
    public DateTime DataCriacao { get; private set; }
    public DateTime DataAtualizacao { get; private set; }

    public Usuario? Usuario { get; private set; }
    public List<EquipeColaborador> Equipes { get; private set; } = new();

    private Colaborador() { } // EF Core

    public static Colaborador Criar(string nome, string? email = null, string? cargo = null, int? usuarioId = null)
    {
        var c = new Colaborador { UsuarioId = usuarioId, DataCriacao = DateTime.UtcNow, DataAtualizacao = DateTime.UtcNow };
        c.Atualizar(nome, email, cargo);
        return c;
    }

    public void Atualizar(string nome, string? email, string? cargo)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new RegraDeNegocioException("O nome do colaborador é obrigatório.");
        if (nome.Trim().Length > 150) throw new RegraDeNegocioException("O nome deve ter no máximo 150 caracteres.");
        if (!string.IsNullOrWhiteSpace(email) && !email.Contains('@')) throw new RegraDeNegocioException("E-mail inválido.");
        Nome = nome.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        Cargo = string.IsNullOrWhiteSpace(cargo) ? null : cargo.Trim();
    }

    public void VincularUsuario(int usuarioId) => UsuarioId = usuarioId;
    public void Ativar() => Ativo = true;
    public void Inativar() => Ativo = false;
}
