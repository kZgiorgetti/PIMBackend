using System.Reflection;
using Pim.Application.Dtos;
using Pim.Application.Excecoes;
using Pim.Application.Interfaces;
using Pim.Domain.Entidades;
using Pim.Domain.Enums;

namespace Pim.Tests.Fakes;

public static class Rf
{
    /// <summary>Define o Id (setter privado) como o banco faria.</summary>
    public static T ComId<T>(this T obj, int id) where T : class
    {
        typeof(T).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance)!.SetValue(obj, id);
        return obj;
    }
}

public class FakeUow : IUnitOfWork
{
    public int Salvamentos { get; private set; }
    public int? UltimoUsuario { get; private set; }
    public Task<int> SalvarAsync(int? usuarioId = null, CancellationToken ct = default) { Salvamentos++; UltimoUsuario = usuarioId; return Task.FromResult(1); }
}

public class FakeUsuarioAtual : IUsuarioAtual
{
    public int? Id { get; set; } = 1;
    public string? Nome { get; set; } = "Teste";
    public PerfilUsuario? Perfil { get; set; }
    public int? ColaboradorId { get; set; }
    public bool EhGestorOuAdmin => Perfil is PerfilUsuario.Admin or PerfilUsuario.Gestor;
}

public class FakeTarefaRepository : ITarefaRepository
{
    public List<Tarefa> Itens { get; } = new();
    public List<TarefaComentario> Comentarios { get; } = new();
    private int _seq = 100;

    public Task<Tarefa?> GetByIdAsync(int id, CancellationToken ct = default) => Task.FromResult(Itens.FirstOrDefault(t => t.Id == id));
    public Task AddAsync(Tarefa t, CancellationToken ct = default) { if (t.Id == 0) t.ComId(++_seq); Itens.Add(t); return Task.CompletedTask; }
    public void Remove(Tarefa t) => Itens.Remove(t);
    public Task<Tarefa?> ObterDetalhadaAsync(int id, CancellationToken ct = default) => GetByIdAsync(id, ct);

    public Task<(IReadOnlyList<Tarefa> Itens, int Total)> ListarAsync(TarefaFiltro f, int? forcar, CancellationToken ct = default)
    {
        var q = Itens.Where(t => !t.Excluida);
        if (forcar.HasValue) q = q.Where(t => t.ColaboradorId == forcar);
        else if (f.ColaboradorId.HasValue) q = q.Where(t => t.ColaboradorId == f.ColaboradorId);
        if (f.Status.HasValue) q = q.Where(t => t.Status == f.Status);
        var lista = q.ToList();
        return Task.FromResult(((IReadOnlyList<Tarefa>)lista, lista.Count));
    }

    public Task ConcluirAsync(int id, int? usuarioId, CancellationToken ct = default) { Itens.First(t => t.Id == id).Concluir(); return Task.CompletedTask; }
    public Task ReatribuirAsync(int id, int? novo, int? usuarioId, CancellationToken ct = default) { Itens.First(t => t.Id == id).Reatribuir(novo); return Task.CompletedTask; }
    public Task<IReadOnlyList<TarefaHistorico>> ListarHistoricoAsync(int id, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<TarefaHistorico>>(new List<TarefaHistorico>());
    public Task<IReadOnlyList<TarefaComentario>> ListarComentariosAsync(int id, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<TarefaComentario>>(Comentarios.Where(c => c.TarefaId == id).ToList());
    public Task AdicionarComentarioAsync(TarefaComentario c, CancellationToken ct = default) { Comentarios.Add(c); return Task.CompletedTask; }
    public Task<IReadOnlyList<Tarefa>> SyncAsync(DateTime? desde, int? forcar, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Tarefa>>(Itens.Where(t => !forcar.HasValue || t.ColaboradorId == forcar).ToList());
}

public class FakeColaboradorRepository : IColaboradorRepository
{
    public List<Colaborador> Itens { get; } = new();
    public Task<Colaborador?> GetByIdAsync(int id, CancellationToken ct = default) => Task.FromResult(Itens.FirstOrDefault(c => c.Id == id));
    public Task AddAsync(Colaborador c, CancellationToken ct = default) { if (c.Id == 0) c.ComId(Itens.Count + 1); Itens.Add(c); return Task.CompletedTask; }
    public void Remove(Colaborador c) => Itens.Remove(c);
    public Task<IReadOnlyList<Colaborador>> ListarAsync(bool apenasAtivos, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Colaborador>>(Itens.Where(c => !apenasAtivos || c.Ativo).ToList());
    public Task<Colaborador?> GetByUsuarioIdAsync(int uid, CancellationToken ct = default) => Task.FromResult(Itens.FirstOrDefault(c => c.UsuarioId == uid));
    public Task<bool> EmailExisteAsync(string email, int? ignorar, CancellationToken ct = default) => Task.FromResult(Itens.Any(c => c.Email == email && c.Id != ignorar));
    public Task<IReadOnlyDictionary<int, int>> MapaUsuarioColaboradorAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyDictionary<int, int>>(Itens.Where(c => c.UsuarioId.HasValue).ToDictionary(c => c.UsuarioId!.Value, c => c.Id));
    public Task<ResultadoRemocao> RemoverOuInativarAsync(int id, CancellationToken ct = default) => Task.FromResult(ResultadoRemocao.Excluido);
}

public class FakeEquipeRepository : IEquipeRepository
{
    public Task<Equipe?> GetByIdAsync(int id, CancellationToken ct = default) => Task.FromResult<Equipe?>(null);
    public Task AddAsync(Equipe e, CancellationToken ct = default) => Task.CompletedTask;
    public void Remove(Equipe e) { }
    public Task<IReadOnlyList<Equipe>> ListarAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Equipe>>(new List<Equipe>());
    public Task<Equipe?> ObterComMembrosAsync(int id, CancellationToken ct = default) => Task.FromResult<Equipe?>(null);
    public Task<bool> NomeExisteAsync(string nome, int? ignorar, CancellationToken ct = default) => Task.FromResult(false);
    public Task<bool> PossuiTarefasAsync(int id, CancellationToken ct = default) => Task.FromResult(false);
}

public class FakeUsuarioRepository : IUsuarioRepository
{
    public List<Usuario> Itens { get; } = new();
    public Task<Usuario?> GetByIdAsync(int id, CancellationToken ct = default) => Task.FromResult(Itens.FirstOrDefault(u => u.Id == id));
    public Task AddAsync(Usuario u, CancellationToken ct = default) { if (u.Id == 0) u.ComId(Itens.Count + 1); Itens.Add(u); return Task.CompletedTask; }
    public void Remove(Usuario u) => Itens.Remove(u);
    public Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct = default) => Task.FromResult(Itens.FirstOrDefault(u => u.Email == email));
    public Task<bool> EmailExisteAsync(string email, int? ignorar, CancellationToken ct = default) => Task.FromResult(Itens.Any(u => u.Email == email && u.Id != ignorar));
    public Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Usuario>>(Itens.ToList());
}

public class FakeRefreshTokenRepository : IRefreshTokenRepository
{
    public List<RefreshToken> Itens { get; } = new();
    public Task AddAsync(RefreshToken t, CancellationToken ct = default) { Itens.Add(t); return Task.CompletedTask; }
    public Task<RefreshToken?> GetByHashAsync(string hash, CancellationToken ct = default) => Task.FromResult(Itens.FirstOrDefault(t => t.TokenHash == hash));
    public Task RevogarTodosDoUsuarioAsync(int uid, CancellationToken ct = default) { foreach (var t in Itens.Where(t => t.UsuarioId == uid)) t.Revogar(); return Task.CompletedTask; }
}

public class FakeHasher : IPasswordHasher
{
    public string Hash(string senha) => "hash:" + senha;
    public bool Verificar(string senha, string hash) => hash == "hash:" + senha;
}

public class FakeTokenService : ITokenService
{
    private int _n;
    public (string Token, DateTime ExpiraEm) GerarAccessToken(Usuario u, int? colaboradorId) => ($"access-{u.Id}-{colaboradorId}", DateTime.UtcNow.AddMinutes(15));
    public (string Token, string Hash) GerarRefreshToken() { var t = $"refresh-{++_n}"; return (t, Hash(t)); }
    public string Hash(string token) => "h(" + token + ")";
}
