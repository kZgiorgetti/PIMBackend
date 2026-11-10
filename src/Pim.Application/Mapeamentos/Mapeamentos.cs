using Pim.Application.Dtos;
using Pim.Domain.Entidades;

namespace Pim.Application.Mapeamentos;

public static class Mapeamentos
{
    public static TarefaResponse ParaResponse(this Tarefa t) => new(t.Id, t.Titulo, t.Descricao, t.Status, t.Prioridade,
        t.DataPrazo, t.ColaboradorId, t.Colaborador?.Nome, t.EquipeId, t.Equipe?.Nome, t.Excluida, t.EstaAtrasada(),
        t.DataCriacao, t.DataAtualizacao);

    public static ColaboradorResponse ParaResponse(this Colaborador c) => new(c.Id, c.Nome, c.Email, c.Cargo, c.Ativo, c.UsuarioId);

    public static UsuarioResponse ParaResponse(this Usuario u, int? colaboradorId) =>
        new(u.Id, u.Nome, u.Email, u.Perfil, u.Ativo, colaboradorId);

    public static EquipeResponse ParaResponse(this Equipe e) => new(e.Id, e.Nome, e.Descricao,
        e.Membros.Where(m => m.Colaborador != null).Select(m => m.Colaborador!.ParaResponse()).OrderBy(m => m.Nome).ToList());

    public static ComentarioResponse ParaResponse(this TarefaComentario c) =>
        new(c.Id, c.TarefaId, c.UsuarioId, c.Usuario?.Nome, c.Texto, c.DataCriacao);

    public static HistoricoResponse ParaResponse(this TarefaHistorico h) =>
        new(h.Id, h.TarefaId, h.StatusAnterior, h.StatusNovo, h.UsuarioId, h.DataAlteracao);
}
