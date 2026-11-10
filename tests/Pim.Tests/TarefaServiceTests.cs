using Pim.Application.Dtos;
using Pim.Application.Excecoes;
using Pim.Application.Services;
using Pim.Domain.Entidades;
using Pim.Domain.Enums;
using Pim.Tests.Fakes;

namespace Pim.Tests;

public class TarefaServiceTests
{
    private readonly FakeTarefaRepository _tarefas = new();
    private readonly FakeColaboradorRepository _colabs = new();
    private readonly FakeUow _uow = new();
    private readonly FakeUsuarioAtual _atual = new();
    private readonly TarefaService _svc;

    public TarefaServiceTests()
    {
        _colabs.Itens.Add(Colaborador.Criar("Ana").ComId(1));
        _colabs.Itens.Add(Colaborador.Criar("Bruno").ComId(2));
        _tarefas.Itens.Add(Tarefa.Criar("Da Ana", colaboradorId: 1).ComId(10));
        _tarefas.Itens.Add(Tarefa.Criar("Do Bruno", colaboradorId: 2).ComId(11));
        _svc = new TarefaService(_tarefas, _colabs, new FakeEquipeRepository(), _uow, _atual);
    }

    private void ComoColaborador(int colaboradorId) { _atual.Perfil = PerfilUsuario.Colaborador; _atual.ColaboradorId = colaboradorId; _atual.Id = 50 + colaboradorId; }
    private void ComoGestor() { _atual.Perfil = PerfilUsuario.Gestor; _atual.ColaboradorId = null; }

    [Fact]
    public async Task Colaborador_ListaApenasAsPropriasTarefas()
    {
        ComoColaborador(1);
        var r = await _svc.ListarAsync(new TarefaFiltro());
        Assert.Single(r.Itens);
        Assert.Equal("Da Ana", r.Itens[0].Titulo);
    }

    [Fact]
    public async Task Colaborador_NaoAcessaTarefaDeOutro()
    {
        ComoColaborador(1);
        await Assert.ThrowsAsync<ForbiddenException>(() => _svc.ObterAsync(11));
        await Assert.ThrowsAsync<ForbiddenException>(() => _svc.AlterarStatusAsync(11, new AlterarStatusRequest(StatusTarefa.Concluida)));
        await Assert.ThrowsAsync<ForbiddenException>(() => _svc.ComentarAsync(11, new ComentarioRequest("oi")));
    }

    [Fact]
    public async Task Colaborador_AlteraStatusDaPropriaTarefa_ERegistraUsuario()
    {
        ComoColaborador(1);
        var r = await _svc.AlterarStatusAsync(10, new AlterarStatusRequest(StatusTarefa.EmAndamento));
        Assert.Equal(StatusTarefa.EmAndamento, r.Status);
        Assert.Equal(51, _uow.UltimoUsuario);   // repassado para a trigger de histórico
    }

    [Fact]
    public async Task Colaborador_NaoPodeCriarNemExcluirNemReatribuir()
    {
        ComoColaborador(1);
        await Assert.ThrowsAsync<ForbiddenException>(() => _svc.CriarAsync(new CriarTarefaRequest("Nova")));
        await Assert.ThrowsAsync<ForbiddenException>(() => _svc.ExcluirAsync(10));
        await Assert.ThrowsAsync<ForbiddenException>(() => _svc.ReatribuirAsync(10, new ReatribuirTarefaRequest(2)));
    }

    [Fact]
    public async Task ColaboradorSemVinculo_NaoVeNada()
    {
        _atual.Perfil = PerfilUsuario.Colaborador; _atual.ColaboradorId = null;
        var r = await _svc.ListarAsync(new TarefaFiltro());
        Assert.Empty(r.Itens);
    }

    [Fact]
    public async Task Gestor_VeTodasECria()
    {
        ComoGestor();
        Assert.Equal(2, (await _svc.ListarAsync(new TarefaFiltro())).Total);
        var nova = await _svc.CriarAsync(new CriarTarefaRequest("Nova", colaboradorId: 2));
        Assert.Equal("Nova", nova.Titulo);
    }

    [Fact]
    public async Task Criar_ColaboradorInexistente_LancaValidacao()
    {
        ComoGestor();
        await Assert.ThrowsAsync<ValidationException>(() => _svc.CriarAsync(new CriarTarefaRequest("Nova", colaboradorId: 99)));
    }

    [Fact]
    public async Task Concluir_TarefaJaConcluida_LancaConflito()
    {
        ComoColaborador(1);
        await _svc.ConcluirAsync(10);
        await Assert.ThrowsAsync<ConflictException>(() => _svc.ConcluirAsync(10));
    }

    [Fact]
    public async Task TarefaExcluida_NaoEncontrada()
    {
        ComoGestor();
        await _svc.ExcluirAsync(10);
        await Assert.ThrowsAsync<NotFoundException>(() => _svc.ObterAsync(10));
    }
}
