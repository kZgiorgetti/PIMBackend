using Pim.Domain.Excecoes;

namespace Pim.Domain.Entidades;

public class Equipe
{
    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string? Descricao { get; private set; }
    public List<EquipeColaborador> Membros { get; private set; } = new();

    private Equipe() { } // EF Core

    public static Equipe Criar(string nome, string? descricao = null)
    {
        var e = new Equipe();
        e.Atualizar(nome, descricao);
        return e;
    }

    public void Atualizar(string nome, string? descricao)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new RegraDeNegocioException("O nome da equipe é obrigatório.");
        if (nome.Trim().Length > 100) throw new RegraDeNegocioException("O nome deve ter no máximo 100 caracteres.");
        Nome = nome.Trim();
        Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim();
    }

    public void AdicionarColaborador(int colaboradorId)
    {
        if (Membros.Any(m => m.ColaboradorId == colaboradorId))
            throw new RegraDeNegocioException("O colaborador já faz parte da equipe.");
        Membros.Add(new EquipeColaborador(Id, colaboradorId));
    }

    public void RemoverColaborador(int colaboradorId)
    {
        var m = Membros.FirstOrDefault(x => x.ColaboradorId == colaboradorId)
                ?? throw new RegraDeNegocioException("O colaborador não faz parte da equipe.");
        Membros.Remove(m);
    }
}

public class EquipeColaborador
{
    public int EquipeId { get; private set; }
    public int ColaboradorId { get; private set; }
    public DateTime DataEntrada { get; private set; } = DateTime.UtcNow;
    public Colaborador? Colaborador { get; private set; }
    private EquipeColaborador() { } // EF Core
    public EquipeColaborador(int equipeId, int colaboradorId) { EquipeId = equipeId; ColaboradorId = colaboradorId; }
}
