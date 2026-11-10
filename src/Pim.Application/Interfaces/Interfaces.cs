using Pim.Application.Dtos;
using Pim.Domain.Entidades;
using Pim.Domain.Enums;

namespace Pim.Application.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(T entidade, CancellationToken ct = default);
    void Remove(T entidade);
}

public interface IUnitOfWork
{
    /// <summary>Persiste as alterações. Se informado, o usuário é repassado ao banco (set_config na transação) para a trigger de histórico.</summary>
    Task<int> SalvarAsync(int? usuarioId = null, CancellationToken ct = default);
}

public interface IUsuarioRepository : IRepository<Usuario>
{
    Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> EmailExisteAsync(string email, int? ignorarId, CancellationToken ct = default);
    Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken ct = default);
}

public enum ResultadoRemocao { Excluido, Inativado }

public interface IColaboradorRepository : IRepository<Colaborador>
{
    Task<IReadOnlyList<Colaborador>> ListarAsync(bool apenasAtivos, CancellationToken ct = default);
    Task<Colaborador?> GetByUsuarioIdAsync(int usuarioId, CancellationToken ct = default);
    Task<bool> EmailExisteAsync(string email, int? ignorarId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<int, int>> MapaUsuarioColaboradorAsync(CancellationToken ct = default);
    /// <summary>Executa o DELETE (a trigger inativa o colaborador se houver tarefas abertas).</summary>
    Task<ResultadoRemocao> RemoverOuInativarAsync(int id, CancellationToken ct = default);
}

public interface ITarefaRepository : IRepository<Tarefa>
{
    Task<Tarefa?> ObterDetalhadaAsync(int id, CancellationToken ct = default);
    Task<(IReadOnlyList<Tarefa> Itens, int Total)> ListarAsync(TarefaFiltro filtro, int? forcarColaboradorId, CancellationToken ct = default);
    Task ConcluirAsync(int tarefaId, int? usuarioId, CancellationToken ct = default);
    Task ReatribuirAsync(int tarefaId, int? novoColaboradorId, int? usuarioId, CancellationToken ct = default);
    Task<IReadOnlyList<TarefaHistorico>> ListarHistoricoAsync(int tarefaId, CancellationToken ct = default);
    Task<IReadOnlyList<TarefaComentario>> ListarComentariosAsync(int tarefaId, CancellationToken ct = default);
    Task AdicionarComentarioAsync(TarefaComentario comentario, CancellationToken ct = default);
    /// <summary>Inclui tarefas excluídas logicamente (para o sync do mobile).</summary>
    Task<IReadOnlyList<Tarefa>> SyncAsync(DateTime? atualizadoDesde, int? forcarColaboradorId, CancellationToken ct = default);
}

public interface IEquipeRepository : IRepository<Equipe>
{
    Task<IReadOnlyList<Equipe>> ListarAsync(CancellationToken ct = default);
    Task<Equipe?> ObterComMembrosAsync(int id, CancellationToken ct = default);
    Task<bool> NomeExisteAsync(string nome, int? ignorarId, CancellationToken ct = default);
    Task<bool> PossuiTarefasAsync(int id, CancellationToken ct = default);
}

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    Task<RefreshToken?> GetByHashAsync(string hash, CancellationToken ct = default);
    Task RevogarTodosDoUsuarioAsync(int usuarioId, CancellationToken ct = default);
}

public interface IRelatorioRepository
{
    Task<IReadOnlyList<ResumoColaboradorResponse>> ResumoPorColaboradorAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TarefaAtrasadaResponse>> AtrasadasAsync(CancellationToken ct = default);
}

public interface IPasswordHasher
{
    string Hash(string senha);
    bool Verificar(string senha, string hash);
}

public interface ITokenService
{
    (string Token, DateTime ExpiraEm) GerarAccessToken(Usuario usuario, int? colaboradorId);
    (string Token, string Hash) GerarRefreshToken();
    string Hash(string token);
}

/// <summary>Usuário autenticado na requisição atual (implementado na camada Api).</summary>
public interface IUsuarioAtual
{
    int? Id { get; }
    string? Nome { get; }
    PerfilUsuario? Perfil { get; }
    int? ColaboradorId { get; }
    bool EhGestorOuAdmin { get; }
}

public class AuthOptions
{
    public int MaxTentativas { get; set; } = 5;
    public int BloqueioMinutos { get; set; } = 15;
    public int RefreshDias { get; set; } = 7;
}
