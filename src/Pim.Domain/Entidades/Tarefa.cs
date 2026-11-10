using Pim.Domain.Enums;
using Pim.Domain.Excecoes;

namespace Pim.Domain.Entidades;

public class Tarefa
{
    public int Id { get; private set; }
    public string Titulo { get; private set; } = string.Empty;
    public string Descricao { get; private set; } = string.Empty;
    public StatusTarefa Status { get; private set; } = StatusTarefa.Pendente;
    public PrioridadeTarefa Prioridade { get; private set; } = PrioridadeTarefa.Media;
    public DateTime? DataPrazo { get; private set; }
    public int? ColaboradorId { get; private set; }
    public int? EquipeId { get; private set; }
    public bool Excluida { get; private set; }
    public DateTime DataCriacao { get; private set; }
    public DateTime DataAtualizacao { get; private set; }

    public Colaborador? Colaborador { get; private set; }
    public Equipe? Equipe { get; private set; }
    public List<TarefaComentario> Comentarios { get; private set; } = new();
    public List<TarefaHistorico> Historico { get; private set; } = new();

    private Tarefa() { } // EF Core

    public static Tarefa Criar(string titulo, string? descricao = null,
        PrioridadeTarefa prioridade = PrioridadeTarefa.Media, DateTime? dataPrazo = null,
        int? colaboradorId = null, int? equipeId = null)
    {
        var t = new Tarefa { DataCriacao = DateTime.UtcNow, DataAtualizacao = DateTime.UtcNow };
        t.Atualizar(titulo, descricao, prioridade, dataPrazo);
        t.ColaboradorId = colaboradorId;
        t.EquipeId = equipeId;
        return t;
    }

    public void Atualizar(string titulo, string? descricao, PrioridadeTarefa prioridade, DateTime? dataPrazo)
    {
        GarantirNaoExcluida();
        if (string.IsNullOrWhiteSpace(titulo)) throw new RegraDeNegocioException("O título da tarefa é obrigatório.");
        if (titulo.Trim().Length > 200) throw new RegraDeNegocioException("O título deve ter no máximo 200 caracteres.");
        if ((descricao?.Length ?? 0) > 2000) throw new RegraDeNegocioException("A descrição deve ter no máximo 2000 caracteres.");
        if (!Enum.IsDefined(prioridade)) throw new RegraDeNegocioException("Prioridade inválida.");
        Titulo = titulo.Trim();
        Descricao = descricao?.Trim() ?? string.Empty;
        Prioridade = prioridade;
        DataPrazo = dataPrazo;
    }

    public void AlterarStatus(StatusTarefa novo)
    {
        GarantirNaoExcluida();
        if (!Enum.IsDefined(novo)) throw new RegraDeNegocioException("Status inválido.");
        Status = novo;
    }

    /// <summary>Conclui a tarefa. Não permite concluir tarefa excluída ou já concluída.</summary>
    public void Concluir()
    {
        GarantirNaoExcluida("Não é possível concluir uma tarefa excluída.");
        if (Status == StatusTarefa.Concluida) throw new RegraDeNegocioException("A tarefa já está concluída.");
        Status = StatusTarefa.Concluida;
    }

    public void Reatribuir(int? colaboradorId)
    {
        GarantirNaoExcluida("Não é possível reatribuir uma tarefa excluída.");
        ColaboradorId = colaboradorId;
    }

    public void DefinirEquipe(int? equipeId)
    {
        GarantirNaoExcluida();
        EquipeId = equipeId;
    }

    /// <summary>Remoção lógica (necessária para a sincronização incremental do mobile).</summary>
    public void Excluir() => Excluida = true;

    public bool EstaAtrasada(DateTime? agoraUtc = null) =>
        !Excluida && Status != StatusTarefa.Concluida && DataPrazo.HasValue && DataPrazo.Value < (agoraUtc ?? DateTime.UtcNow);

    public bool PertenceA(int colaboradorId) => ColaboradorId == colaboradorId;

    private void GarantirNaoExcluida(string? msg = null)
    {
        if (Excluida) throw new RegraDeNegocioException(msg ?? "Não é possível alterar uma tarefa excluída.");
    }
}
