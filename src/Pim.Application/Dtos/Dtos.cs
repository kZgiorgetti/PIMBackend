using Pim.Domain.Enums;

namespace Pim.Application.Dtos;

// ---------- Auth ----------
public record LoginRequest(string Email, string Senha);
public record RefreshRequest(string RefreshToken);
public record UsuarioResponse(int Id, string Nome, string Email, PerfilUsuario Perfil, bool Ativo, int? ColaboradorId);
public record TokenResponse(string AccessToken, string RefreshToken, DateTime ExpiraEm, UsuarioResponse Usuario);

// ---------- Usuários ----------
public record CriarUsuarioRequest(string Nome, string Email, string Senha, PerfilUsuario Perfil, int? ColaboradorId = null);
public record AtualizarUsuarioRequest(string Nome, string Email, PerfilUsuario Perfil);
public record RedefinirSenhaRequest(string NovaSenha);

// ---------- Colaboradores ----------
public record ColaboradorRequest(string Nome, string? Email = null, string? Cargo = null);
public record ColaboradorResponse(int Id, string Nome, string? Email, string? Cargo, bool Ativo, int? UsuarioId);
public record RemocaoColaboradorResponse(bool Inativado, string Mensagem);

// ---------- Equipes ----------
public record EquipeRequest(string Nome, string? Descricao = null);
public record EquipeResponse(int Id, string Nome, string? Descricao, IReadOnlyList<ColaboradorResponse> Membros);

// ---------- Tarefas ----------
public record CriarTarefaRequest(string Titulo, string? Descricao = null, PrioridadeTarefa Prioridade = PrioridadeTarefa.Media,
    DateTime? DataPrazo = null, int? ColaboradorId = null, int? EquipeId = null);
public record AtualizarTarefaRequest(string Titulo, string? Descricao = null, PrioridadeTarefa Prioridade = PrioridadeTarefa.Media,
    DateTime? DataPrazo = null, int? ColaboradorId = null, int? EquipeId = null);
public record AlterarStatusRequest(StatusTarefa Status);
public record ReatribuirTarefaRequest(int? ColaboradorId);
public record ComentarioRequest(string Texto);

public record TarefaResponse(int Id, string Titulo, string Descricao, StatusTarefa Status, PrioridadeTarefa Prioridade,
    DateTime? DataPrazo, int? ColaboradorId, string? ColaboradorNome, int? EquipeId, string? EquipeNome,
    bool Excluida, bool Atrasada, DateTime DataCriacao, DateTime DataAtualizacao);
public record ComentarioResponse(int Id, int TarefaId, int UsuarioId, string? UsuarioNome, string Texto, DateTime DataCriacao);
public record HistoricoResponse(int Id, int TarefaId, StatusTarefa StatusAnterior, StatusTarefa StatusNovo, int? UsuarioId, DateTime DataAlteracao);

public class TarefaFiltro
{
    public StatusTarefa? Status { get; set; }
    public int? ColaboradorId { get; set; }
    public PrioridadeTarefa? Prioridade { get; set; }
    public string? Busca { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public record PaginaResponse<T>(IReadOnlyList<T> Itens, int Total, int Page, int PageSize);
public record SyncResponse(IReadOnlyList<TarefaResponse> Tarefas, DateTime ServidorEm);

// ---------- Relatórios ----------
public record ResumoColaboradorResponse(int ColaboradorId, string ColaboradorNome, int Total, int Pendentes, int EmAndamento, int Concluidas);
public record TarefaAtrasadaResponse(int Id, string Titulo, string Status, string Prioridade, DateTime DataPrazo, int DiasAtraso,
    int? ColaboradorId, string? ColaboradorNome, int? EquipeId, string? EquipeNome);
