using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pim.Domain.Entidades;

namespace Pim.Infrastructure.Persistencia.Configuracoes;

public class UsuarioConfig : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("Usuarios", t => t.HasTrigger("trg_Usuarios_DataAtualizacao"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Nome).HasMaxLength(150).IsRequired();
        b.Property(x => x.Email).HasMaxLength(200).IsRequired();
        b.HasIndex(x => x.Email).IsUnique();
        b.Property(x => x.SenhaHash).HasMaxLength(200).IsRequired();
        b.Property(x => x.Perfil).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.DataAtualizacao).ValueGeneratedOnAddOrUpdate();
    }
}

public class RefreshTokenConfig : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("RefreshTokens");
        b.HasKey(x => x.Id);
        b.Property(x => x.TokenHash).HasMaxLength(64).IsFixedLength().IsRequired();
        b.Property(x => x.SubstituidoPorHash).HasMaxLength(64).IsFixedLength();
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.NoAction);
    }
}

public class ColaboradorConfig : IEntityTypeConfiguration<Colaborador>
{
    public void Configure(EntityTypeBuilder<Colaborador> b)
    {
        b.ToTable("Colaboradores", t =>
        {
            t.HasTrigger("trg_Colaboradores_DataAtualizacao");
            t.HasTrigger("trg_Colaboradores_BloqueiaExclusaoComTarefas");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Nome).HasMaxLength(150).IsRequired();
        b.Property(x => x.Email).HasMaxLength(200);
        b.Property(x => x.Cargo).HasMaxLength(100);
        b.Property(x => x.DataAtualizacao).ValueGeneratedOnAddOrUpdate();
        b.HasIndex(x => x.UsuarioId).IsUnique().HasFilter("[UsuarioId] IS NOT NULL").HasDatabaseName("UX_Colaboradores_UsuarioId");
        b.HasIndex(x => x.Email).IsUnique().HasFilter("[Email] IS NOT NULL").HasDatabaseName("UX_Colaboradores_Email");
        b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.NoAction);
    }
}

public class EquipeConfig : IEntityTypeConfiguration<Equipe>
{
    public void Configure(EntityTypeBuilder<Equipe> b)
    {
        b.ToTable("Equipes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Nome).HasMaxLength(100).IsRequired();
        b.Property(x => x.Descricao).HasMaxLength(500);
        b.HasIndex(x => x.Nome).IsUnique();
    }
}

public class EquipeColaboradorConfig : IEntityTypeConfiguration<EquipeColaborador>
{
    public void Configure(EntityTypeBuilder<EquipeColaborador> b)
    {
        b.ToTable("EquipeColaboradores");
        b.HasKey(x => new { x.EquipeId, x.ColaboradorId });
        b.HasOne<Equipe>().WithMany(e => e.Membros).HasForeignKey(x => x.EquipeId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Colaborador).WithMany(c => c.Equipes).HasForeignKey(x => x.ColaboradorId).OnDelete(DeleteBehavior.NoAction);
    }
}

public class TarefaConfig : IEntityTypeConfiguration<Tarefa>
{
    public void Configure(EntityTypeBuilder<Tarefa> b)
    {
        b.ToTable("Tarefas", t =>
        {
            t.HasTrigger("trg_Tarefas_Historico");
            t.HasTrigger("trg_Tarefas_DataAtualizacao");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Titulo).HasMaxLength(200).IsRequired();
        b.Property(x => x.Descricao).HasMaxLength(2000).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Prioridade).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.DataAtualizacao).ValueGeneratedOnAddOrUpdate();
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.ColaboradorId);
        b.HasIndex(x => x.DataAtualizacao);
        b.HasOne(x => x.Colaborador).WithMany().HasForeignKey(x => x.ColaboradorId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne(x => x.Equipe).WithMany().HasForeignKey(x => x.EquipeId).OnDelete(DeleteBehavior.NoAction);
        b.HasMany(x => x.Comentarios).WithOne().HasForeignKey(c => c.TarefaId).OnDelete(DeleteBehavior.NoAction);
        b.HasMany(x => x.Historico).WithOne().HasForeignKey(h => h.TarefaId).OnDelete(DeleteBehavior.NoAction);
    }
}

public class TarefaComentarioConfig : IEntityTypeConfiguration<TarefaComentario>
{
    public void Configure(EntityTypeBuilder<TarefaComentario> b)
    {
        b.ToTable("TarefaComentarios");
        b.HasKey(x => x.Id);
        b.Property(x => x.Texto).HasMaxLength(1000).IsRequired();
        b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.NoAction);
    }
}

public class TarefaHistoricoConfig : IEntityTypeConfiguration<TarefaHistorico>
{
    public void Configure(EntityTypeBuilder<TarefaHistorico> b)
    {
        b.ToTable("TarefaHistorico");
        b.HasKey(x => x.Id);
        b.Property(x => x.StatusAnterior).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.StatusNovo).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.DataAlteracao).ValueGeneratedOnAdd();
    }
}
