using Pim.Domain.Entidades;
using Pim.Domain.Enums;
using Pim.Domain.Excecoes;

namespace Pim.Tests;

public class TarefaDominioTests
{
    [Fact]
    public void Concluir_MudaStatusParaConcluida()
    {
        var t = Tarefa.Criar("Fechar inventário");
        t.Concluir();
        Assert.Equal(StatusTarefa.Concluida, t.Status);
    }

    [Fact]
    public void Concluir_TarefaExcluida_Lanca()
    {
        var t = Tarefa.Criar("X");
        t.Excluir();
        Assert.Throws<RegraDeNegocioException>(() => t.Concluir());
    }

    [Fact]
    public void Concluir_TarefaJaConcluida_Lanca()
    {
        var t = Tarefa.Criar("X");
        t.Concluir();
        Assert.Throws<RegraDeNegocioException>(() => t.Concluir());
    }

    [Fact]
    public void AlterarStatus_TarefaExcluida_Lanca()
    {
        var t = Tarefa.Criar("X");
        t.Excluir();
        Assert.Throws<RegraDeNegocioException>(() => t.AlterarStatus(StatusTarefa.EmAndamento));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Criar_TituloVazio_Lanca(string titulo) => Assert.Throws<RegraDeNegocioException>(() => Tarefa.Criar(titulo));

    [Fact]
    public void EstaAtrasada_ConsideraPrazoEStatus()
    {
        var t = Tarefa.Criar("X", dataPrazo: DateTime.UtcNow.AddDays(-2));
        Assert.True(t.EstaAtrasada());
        t.Concluir();
        Assert.False(t.EstaAtrasada());
    }

    [Fact]
    public void Usuario_BloqueiaAposMaxTentativas()
    {
        var u = Usuario.Criar("A", "a@a.com", "h", PerfilUsuario.Colaborador);
        var agora = DateTime.UtcNow;
        for (var i = 0; i < 5; i++) u.RegistrarFalhaLogin(5, TimeSpan.FromMinutes(15), agora);
        Assert.True(u.EstaBloqueado(agora));
        Assert.False(u.EstaBloqueado(agora.AddMinutes(16)));
    }
}
