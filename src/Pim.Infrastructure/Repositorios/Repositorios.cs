using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Pim.Application.Dtos;
using Pim.Application.Excecoes;
using Pim.Application.Interfaces;
using Pim.Domain.Entidades;
using Pim.Domain.Enums;
using Pim.Domain.Excecoes;
using Pim.Infrastructure.Persistencia;

namespace Pim.Infrastructure.Repositorios;

public class UnitOfWork : IUnitOfWork
{
    private readonly PimDbContext _db;
    public UnitOfWork(PimDbContext db) => _db = db;

    public async Task<int> SalvarAsync(int? usuarioId = null, CancellationToken ct = default)
    {
        if (!_db.Database.IsRelational() || usuarioId is null) return await _db.SaveChangesAsync(ct);

        // Mantém a mesma conexão para o SESSION_CONTEXT ser visto pela trigger de histórico.
        await _db.Database.OpenConnectionAsync(ct);
        try
        {
            await _db.Database.ExecuteSqlInterpolatedAsync($"EXEC sys.sp_set_session_context @key=N'UsuarioId', @value={usuarioId}", ct);
            return await _db.SaveChangesAsync(ct);
        }
        finally
        {
            try { await _db.Database.ExecuteSqlRawAsync("EXEC sys.sp_set_session_context @key=N'UsuarioId', @value=NULL", CancellationToken.None); }
            catch { /* conexão pode já ter falhado */ }
            await _db.Database.CloseConnectionAsync();
        }
    }
}

public class RepositorioBase<T> : IRepository<T> where T : class
{
    protected readonly PimDbContext Db;
    public RepositorioBase(PimDbContext db) => Db = db;
    public virtual async Task<T?> GetByIdAsync(int id, CancellationToken ct = default) => await Db.Set<T>().FindAsync(new object[] { id }, ct);
    public virtual async Task AddAsync(T entidade, CancellationToken ct = default) => await Db.Set<T>().AddAsync(entidade, ct);
    public virtual void Remove(T entidade) => Db.Set<T>().Remove(entidade);
}

public class UsuarioRepository : RepositorioBase<Usuario>, IUsuarioRepository
{
    public UsuarioRepository(PimDbContext db) : base(db) { }
    public Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        Db.Usuarios.FirstOrDefaultAsync(u => u.Email == email, ct);
    public Task<bool> EmailExisteAsync(string email, int? ignorarId, CancellationToken ct = default) =>
        Db.Usuarios.AnyAsync(u => u.Email == email && (ignorarId == null || u.Id != ignorarId), ct);
    public async Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken ct = default) =>
        await Db.Usuarios.AsNoTracking().OrderBy(u => u.Nome).ToListAsync(ct);
}

public class ColaboradorRepository : RepositorioBase<Colaborador>, IColaboradorRepository
{
    public ColaboradorRepository(PimDbContext db) : base(db) { }

    public async Task<IReadOnlyList<Colaborador>> ListarAsync(bool apenasAtivos, CancellationToken ct = default) =>
        await Db.Colaboradores.AsNoTracking().Where(c => !apenasAtivos || c.Ativo).OrderBy(c => c.Nome).ToListAsync(ct);

    public Task<Colaborador?> GetByUsuarioIdAsync(int usuarioId, CancellationToken ct = default) =>
        Db.Colaboradores.FirstOrDefaultAsync(c => c.UsuarioId == usuarioId, ct);

    public Task<bool> EmailExisteAsync(string email, int? ignorarId, CancellationToken ct = default) =>
        Db.Colaboradores.AnyAsync(c => c.Email == email && (ignorarId == null || c.Id != ignorarId), ct);

    public async Task<IReadOnlyDictionary<int, int>> MapaUsuarioColaboradorAsync(CancellationToken ct = default) =>
        await Db.Colaboradores.AsNoTracking().Where(c => c.UsuarioId != null)
            .ToDictionaryAsync(c => c.UsuarioId!.Value, c => c.Id, ct);

    public async Task<ResultadoRemocao> RemoverOuInativarAsync(int id, CancellationToken ct = default)
    {
        if (!Db.Database.IsRelational())
        {
            var c = await Db.Colaboradores.FindAsync(new object[] { id }, ct);
            if (c == null) return ResultadoRemocao.Excluido;
            var abertas = await Db.Tarefas.AnyAsync(t => t.ColaboradorId == id && !t.Excluida && t.Status != StatusTarefa.Concluida, ct);
            if (abertas) { c.Inativar(); await Db.SaveChangesAsync(ct); return ResultadoRemocao.Inativado; }
            Db.Colaboradores.Remove(c); await Db.SaveChangesAsync(ct); return ResultadoRemocao.Excluido;
        }

        // DELETE direto: a trigger INSTEAD OF DELETE decide entre excluir e inativar (EF Remove() falharia por 0 linhas afetadas).
        await Db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM dbo.Colaboradores WHERE Id = {id}", ct);
        Db.ChangeTracker.Clear();
        var existe = await Db.Colaboradores.AsNoTracking().AnyAsync(c => c.Id == id, ct);
        return existe ? ResultadoRemocao.Inativado : ResultadoRemocao.Excluido;
    }
}

public class EquipeRepository : RepositorioBase<Equipe>, IEquipeRepository
{
    public EquipeRepository(PimDbContext db) : base(db) { }

    private IQueryable<Equipe> ComMembros => Db.Equipes.Include(e => e.Membros).ThenInclude(m => m.Colaborador);

    public async Task<IReadOnlyList<Equipe>> ListarAsync(CancellationToken ct = default) =>
        await ComMembros.AsNoTracking().OrderBy(e => e.Nome).ToListAsync(ct);
    public Task<Equipe?> ObterComMembrosAsync(int id, CancellationToken ct = default) => ComMembros.FirstOrDefaultAsync(e => e.Id == id, ct);
    public Task<bool> NomeExisteAsync(string nome, int? ignorarId, CancellationToken ct = default) =>
        Db.Equipes.AnyAsync(e => e.Nome == nome && (ignorarId == null || e.Id != ignorarId), ct);
    public Task<bool> PossuiTarefasAsync(int id, CancellationToken ct = default) => Db.Tarefas.AnyAsync(t => t.EquipeId == id, ct);
}

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly PimDbContext _db;
    public RefreshTokenRepository(PimDbContext db) => _db = db;
    public async Task AddAsync(RefreshToken token, CancellationToken ct = default) => await _db.RefreshTokens.AddAsync(token, ct);
    public Task<RefreshToken?> GetByHashAsync(string hash, CancellationToken ct = default) =>
        _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
    public async Task RevogarTodosDoUsuarioAsync(int usuarioId, CancellationToken ct = default)
    {
        var ativos = await _db.RefreshTokens.Where(t => t.UsuarioId == usuarioId && t.DataRevogacao == null).ToListAsync(ct);
        foreach (var t in ativos) t.Revogar();
    }
}

public class TarefaRepository : RepositorioBase<Tarefa>, ITarefaRepository
{
    public TarefaRepository(PimDbContext db) : base(db) { }

    private IQueryable<Tarefa> Detalhada => Db.Tarefas.Include(t => t.Colaborador).Include(t => t.Equipe);

    public Task<Tarefa?> ObterDetalhadaAsync(int id, CancellationToken ct = default) => Detalhada.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<(IReadOnlyList<Tarefa> Itens, int Total)> ListarAsync(TarefaFiltro f, int? forcarColaboradorId, CancellationToken ct = default)
    {
        var q = Detalhada.AsNoTracking().Where(t => !t.Excluida);
        if (forcarColaboradorId.HasValue) q = q.Where(t => t.ColaboradorId == forcarColaboradorId);
        else if (f.ColaboradorId.HasValue) q = q.Where(t => t.ColaboradorId == f.ColaboradorId);
        if (f.Status.HasValue) q = q.Where(t => t.Status == f.Status);
        if (f.Prioridade.HasValue) q = q.Where(t => t.Prioridade == f.Prioridade);
        if (!string.IsNullOrWhiteSpace(f.Busca))
        {
            var b = f.Busca.Trim();
            q = q.Where(t => t.Titulo.Contains(b) || t.Descricao.Contains(b));
        }
        var total = await q.CountAsync(ct);
        var itens = await q.OrderByDescending(t => t.DataAtualizacao).ThenByDescending(t => t.Id)
            .Skip((f.Page - 1) * f.PageSize).Take(f.PageSize).ToListAsync(ct);
        return (itens, total);
    }

    public async Task ConcluirAsync(int tarefaId, int? usuarioId, CancellationToken ct = default)
    {
        if (!Db.Database.IsRelational())
        {
            var t = await Db.Tarefas.FindAsync(new object[] { tarefaId }, ct) ?? throw new NotFoundException("Tarefa não encontrada.");
            t.Concluir();
            await Db.SaveChangesAsync(ct);
            return;
        }
        try { await Db.Database.ExecuteSqlInterpolatedAsync($"EXEC dbo.sp_ConcluirTarefa @TarefaId={tarefaId}, @UsuarioId={usuarioId}", ct); }
        catch (SqlException ex) { throw TraduzirErroProcedure(ex); }
        Db.ChangeTracker.Clear();   // a procedure alterou a linha fora do rastreamento do EF
    }

    public async Task ReatribuirAsync(int tarefaId, int? novoColaboradorId, int? usuarioId, CancellationToken ct = default)
    {
        if (!Db.Database.IsRelational())
        {
            var t = await Db.Tarefas.FindAsync(new object[] { tarefaId }, ct) ?? throw new NotFoundException("Tarefa não encontrada.");
            t.Reatribuir(novoColaboradorId);
            await Db.SaveChangesAsync(ct);
            return;
        }
        try { await Db.Database.ExecuteSqlInterpolatedAsync($"EXEC dbo.sp_ReatribuirTarefa @TarefaId={tarefaId}, @NovoColaboradorId={novoColaboradorId}, @UsuarioId={usuarioId}", ct); }
        catch (SqlException ex) { throw TraduzirErroProcedure(ex); }
        Db.ChangeTracker.Clear();
    }

    public async Task<IReadOnlyList<TarefaHistorico>> ListarHistoricoAsync(int tarefaId, CancellationToken ct = default) =>
        await Db.TarefaHistorico.AsNoTracking().Where(h => h.TarefaId == tarefaId)
            .OrderBy(h => h.DataAlteracao).ThenBy(h => h.Id).ToListAsync(ct);

    public async Task<IReadOnlyList<TarefaComentario>> ListarComentariosAsync(int tarefaId, CancellationToken ct = default) =>
        await Db.TarefaComentarios.Include(c => c.Usuario).AsNoTracking().Where(c => c.TarefaId == tarefaId)
            .OrderBy(c => c.DataCriacao).ThenBy(c => c.Id).ToListAsync(ct);

    public async Task AdicionarComentarioAsync(TarefaComentario comentario, CancellationToken ct = default) =>
        await Db.TarefaComentarios.AddAsync(comentario, ct);

    public async Task<IReadOnlyList<Tarefa>> SyncAsync(DateTime? desde, int? forcarColaboradorId, CancellationToken ct = default)
    {
        var q = Detalhada.AsNoTracking().AsQueryable();   // inclui excluídas
        if (desde.HasValue) q = q.Where(t => t.DataAtualizacao > desde.Value);
        if (forcarColaboradorId.HasValue) q = q.Where(t => t.ColaboradorId == forcarColaboradorId);
        return await q.OrderBy(t => t.DataAtualizacao).ThenBy(t => t.Id).ToListAsync(ct);
    }

    // 50001 não encontrada | 50002 excluída | 50003 já concluída | 50004 colaborador inválido
    private static Exception TraduzirErroProcedure(SqlException ex) => ex.Number switch
    {
        50001 => new NotFoundException(ex.Message),
        50002 => new RegraDeNegocioException(ex.Message),
        50003 => new ConflictException(ex.Message),
        50004 => new RegraDeNegocioException(ex.Message),
        _ => ex
    };
}

public class RelatorioRepository : IRelatorioRepository
{
    private readonly PimDbContext _db;
    public RelatorioRepository(PimDbContext db) => _db = db;

    public async Task<IReadOnlyList<ResumoColaboradorResponse>> ResumoPorColaboradorAsync(CancellationToken ct = default)
    {
        if (!_db.Database.IsRelational())
        {
            var cols = await _db.Colaboradores.AsNoTracking().Where(c => c.Ativo).OrderBy(c => c.Nome).ToListAsync(ct);
            var tarefas = await _db.Tarefas.AsNoTracking().Where(t => !t.Excluida).ToListAsync(ct);
            return cols.Select(c =>
            {
                var ts = tarefas.Where(t => t.ColaboradorId == c.Id).ToList();
                return new ResumoColaboradorResponse(c.Id, c.Nome, ts.Count, ts.Count(t => t.Status == StatusTarefa.Pendente),
                    ts.Count(t => t.Status == StatusTarefa.EmAndamento), ts.Count(t => t.Status == StatusTarefa.Concluida));
            }).ToList();
        }
        var rows = await _db.ResumoColaboradores.FromSqlRaw("EXEC dbo.sp_ResumoTarefasPorColaborador").AsNoTracking().ToListAsync(ct);
        return rows.Select(r => new ResumoColaboradorResponse(r.ColaboradorId, r.ColaboradorNome, r.Total, r.Pendentes, r.EmAndamento, r.Concluidas)).ToList();
    }

    public async Task<IReadOnlyList<TarefaAtrasadaResponse>> AtrasadasAsync(CancellationToken ct = default)
    {
        if (!_db.Database.IsRelational())
        {
            var agora = DateTime.UtcNow;
            var lista = await _db.Tarefas.Include(t => t.Colaborador).Include(t => t.Equipe).AsNoTracking()
                .Where(t => !t.Excluida && t.Status != StatusTarefa.Concluida && t.DataPrazo != null && t.DataPrazo < agora)
                .OrderBy(t => t.DataPrazo).ToListAsync(ct);
            return lista.Select(t => new TarefaAtrasadaResponse(t.Id, t.Titulo, t.Status.ToString(), t.Prioridade.ToString(),
                t.DataPrazo!.Value, (int)(agora - t.DataPrazo.Value).TotalDays, t.ColaboradorId, t.Colaborador?.Nome, t.EquipeId, t.Equipe?.Nome)).ToList();
        }
        var rows = await _db.TarefasAtrasadasRows.FromSqlRaw("EXEC dbo.sp_TarefasAtrasadas").AsNoTracking().ToListAsync(ct);
        return rows.Select(r => new TarefaAtrasadaResponse(r.Id, r.Titulo, r.Status, r.Prioridade, r.DataPrazo, r.DiasAtraso,
            r.ColaboradorId, r.ColaboradorNome, r.EquipeId, r.EquipeNome)).ToList();
    }
}
