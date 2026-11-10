using Microsoft.EntityFrameworkCore;
using Pim.Domain.Entidades;

namespace Pim.Infrastructure.Persistencia;

// Linhas retornadas pelas stored procedures de relatório (sem chave, não mapeadas para tabela)
public class ResumoColaboradorRow
{
    public int ColaboradorId { get; set; }
    public string ColaboradorNome { get; set; } = "";
    public int Total { get; set; }
    public int Pendentes { get; set; }
    public int EmAndamento { get; set; }
    public int Concluidas { get; set; }
}

public class TarefaAtrasadaRow
{
    public int Id { get; set; }
    public string Titulo { get; set; } = "";
    public string Status { get; set; } = "";
    public string Prioridade { get; set; } = "";
    public DateTime DataPrazo { get; set; }
    public int DiasAtraso { get; set; }
    public int? ColaboradorId { get; set; }
    public string? ColaboradorNome { get; set; }
    public int? EquipeId { get; set; }
    public string? EquipeNome { get; set; }
}

public class PimDbContext : DbContext
{
    public PimDbContext(DbContextOptions<PimDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Colaborador> Colaboradores => Set<Colaborador>();
    public DbSet<Equipe> Equipes => Set<Equipe>();
    public DbSet<EquipeColaborador> EquipeColaboradores => Set<EquipeColaborador>();
    public DbSet<Tarefa> Tarefas => Set<Tarefa>();
    public DbSet<TarefaComentario> TarefaComentarios => Set<TarefaComentario>();
    public DbSet<TarefaHistorico> TarefaHistorico => Set<TarefaHistorico>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ResumoColaboradorRow> ResumoColaboradores => Set<ResumoColaboradorRow>();
    public DbSet<TarefaAtrasadaRow> TarefasAtrasadasRows => Set<TarefaAtrasadaRow>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.HasDefaultSchema("pim");
        mb.ApplyConfigurationsFromAssembly(typeof(PimDbContext).Assembly);
        mb.Entity<ResumoColaboradorRow>().HasNoKey().ToView(null);
        mb.Entity<TarefaAtrasadaRow>().HasNoKey().ToView(null);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder cb)
    {
        // Datas sempre em UTC: o Npgsql só grava DateTime Kind=Utc em timestamptz
        cb.Properties<DateTime>().HaveConversion<UtcConverter>().HaveColumnType("timestamptz");
        cb.Properties<DateTime?>().HaveConversion<UtcNullableConverter>().HaveColumnType("timestamptz");
    }

    // Nos testes (InMemory) não há trigger: simula a atualização de DataAtualizacao.
    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        if (!Database.IsRelational())
        {
            foreach (var e in ChangeTracker.Entries<Tarefa>().Where(x => x.State == EntityState.Modified))
                e.Property(nameof(Tarefa.DataAtualizacao)).CurrentValue = DateTime.UtcNow;
        }
        return base.SaveChangesAsync(ct);
    }
}

public class UtcConverter : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>
{
    public UtcConverter() : base(
        v => v.Kind == DateTimeKind.Utc ? v : (v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : DateTime.SpecifyKind(v, DateTimeKind.Utc)),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc)) { }
}

public class UtcNullableConverter : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime?, DateTime?>
{
    public UtcNullableConverter() : base(
        v => v.HasValue ? (v.Value.Kind == DateTimeKind.Utc ? v : (v.Value.Kind == DateTimeKind.Local ? v.Value.ToUniversalTime() : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc))) : v,
        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v) { }
}
