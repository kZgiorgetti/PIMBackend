using Pim.Domain.Enums;

namespace Pim.Domain.Entidades;

/// <summary>Somente leitura para a aplicação: as linhas são gravadas pela trigger trg_Tarefas_Historico.</summary>
public class TarefaHistorico
{
    public int Id { get; private set; }
    public int TarefaId { get; private set; }
    public StatusTarefa StatusAnterior { get; private set; }
    public StatusTarefa StatusNovo { get; private set; }
    public int? UsuarioId { get; private set; }
    public DateTime DataAlteracao { get; private set; }

    private TarefaHistorico() { } // EF Core
}
