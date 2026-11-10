using Pim.Application.Dtos;
using Pim.Application.Excecoes;
using Pim.Application.Interfaces;
using Pim.Application.Mapeamentos;
using Pim.Domain.Entidades;
using Pim.Domain.Enums;

namespace Pim.Application.Services;

public class TarefaService
{
    private readonly ITarefaRepository _tarefas;
    private readonly IColaboradorRepository _colaboradores;
    private readonly IEquipeRepository _equipes;
    private readonly IUnitOfWork _uow;
    private readonly IUsuarioAtual _atual;

    public TarefaService(ITarefaRepository tarefas, IColaboradorRepository colaboradores, IEquipeRepository equipes,
        IUnitOfWork uow, IUsuarioAtual atual)
    { _tarefas = tarefas; _colaboradores = colaboradores; _equipes = equipes; _uow = uow; _atual = atual; }

    // ---------- consultas ----------
    public async Task<PaginaResponse<TarefaResponse>> ListarAsync(TarefaFiltro filtro, CancellationToken ct = default)
    {
        var (itens, total) = await _tarefas.ListarAsync(filtro, ColaboradorForcado(), ct);
        return new PaginaResponse<TarefaResponse>(itens.Select(t => t.ParaResponse()).ToList(), total, filtro.Page, filtro.PageSize);
    }

    public async Task<TarefaResponse> ObterAsync(int id, CancellationToken ct = default)
    {
        var t = await Carregar(id, ct);
        GarantirAcesso(t);
        return t.ParaResponse();
    }

    public async Task<IReadOnlyList<HistoricoResponse>> HistoricoAsync(int id, CancellationToken ct = default)
    {
        GarantirAcesso(await Carregar(id, ct));
        return (await _tarefas.ListarHistoricoAsync(id, ct)).Select(h => h.ParaResponse()).ToList();
    }

    public async Task<IReadOnlyList<ComentarioResponse>> ComentariosAsync(int id, CancellationToken ct = default)
    {
        GarantirAcesso(await Carregar(id, ct));
        return (await _tarefas.ListarComentariosAsync(id, ct)).Select(c => c.ParaResponse()).ToList();
    }

    public async Task<SyncResponse> SyncAsync(DateTime? atualizadoDesde, CancellationToken ct = default)
    {
        var desde = atualizadoDesde.HasValue ? NormalizarUtc(atualizadoDesde.Value) : (DateTime?)null;
        var servidorEm = DateTime.UtcNow;
        var itens = await _tarefas.SyncAsync(desde, ColaboradorForcado(), ct);
        return new SyncResponse(itens.Select(t => t.ParaResponse()).ToList(), servidorEm);
    }

    // ---------- comandos (gestor/admin) ----------
    public async Task<TarefaResponse> CriarAsync(CriarTarefaRequest r, CancellationToken ct = default)
    {
        ExigirGestor();
        await ValidarReferencias(r.ColaboradorId, r.EquipeId, ct);
        var t = Tarefa.Criar(r.Titulo, r.Descricao, r.Prioridade, r.DataPrazo, r.ColaboradorId, r.EquipeId);
        await _tarefas.AddAsync(t, ct);
        await _uow.SalvarAsync(_atual.Id, ct);
        return (await Carregar(t.Id, ct)).ParaResponse();
    }

    public async Task<TarefaResponse> AtualizarAsync(int id, AtualizarTarefaRequest r, CancellationToken ct = default)
    {
        ExigirGestor();
        var t = await Carregar(id, ct);
        await ValidarReferencias(r.ColaboradorId, r.EquipeId, ct);
        t.Atualizar(r.Titulo, r.Descricao, r.Prioridade, r.DataPrazo);
        t.DefinirEquipe(r.EquipeId);
        if (t.ColaboradorId != r.ColaboradorId) t.Reatribuir(r.ColaboradorId);
        await _uow.SalvarAsync(_atual.Id, ct);
        return (await Carregar(id, ct)).ParaResponse();
    }

    public async Task ExcluirAsync(int id, CancellationToken ct = default)
    {
        ExigirGestor();
        var t = await Carregar(id, ct);
        t.Excluir();
        await _uow.SalvarAsync(_atual.Id, ct);
    }

    public async Task<TarefaResponse> ReatribuirAsync(int id, ReatribuirTarefaRequest r, CancellationToken ct = default)
    {
        ExigirGestor();
        await Carregar(id, ct);
        await ValidarReferencias(r.ColaboradorId, null, ct);
        await _tarefas.ReatribuirAsync(id, r.ColaboradorId, _atual.Id, ct);
        return (await Carregar(id, ct)).ParaResponse();
    }

    // ---------- comandos (dono ou gestor) ----------
    public async Task<TarefaResponse> AlterarStatusAsync(int id, AlterarStatusRequest r, CancellationToken ct = default)
    {
        var t = await Carregar(id, ct);
        GarantirAcesso(t);
        t.AlterarStatus(r.Status);
        await _uow.SalvarAsync(_atual.Id, ct);   // a trigger grava o histórico
        return t.ParaResponse();
    }

    public async Task<TarefaResponse> ConcluirAsync(int id, CancellationToken ct = default)
    {
        var t = await Carregar(id, ct);
        GarantirAcesso(t);
        if (t.Status == StatusTarefa.Concluida) throw new ConflictException("A tarefa já está concluída.");
        await _tarefas.ConcluirAsync(id, _atual.Id, ct);   // sp_ConcluirTarefa
        return (await Carregar(id, ct)).ParaResponse();
    }

    public async Task<ComentarioResponse> ComentarAsync(int id, ComentarioRequest r, CancellationToken ct = default)
    {
        GarantirAcesso(await Carregar(id, ct));
        var usuarioId = _atual.Id ?? throw new UnauthorizedException("Não autenticado.");
        var c = TarefaComentario.Criar(id, usuarioId, r.Texto);
        await _tarefas.AdicionarComentarioAsync(c, ct);
        await _uow.SalvarAsync(usuarioId, ct);
        return new ComentarioResponse(c.Id, id, usuarioId, _atual.Nome, c.Texto, c.DataCriacao);
    }

    // ---------- autorização por posse ----------
    private int? ColaboradorForcado() => _atual.EhGestorOuAdmin ? null : (_atual.ColaboradorId ?? -1);

    private void ExigirGestor()
    {
        if (!_atual.EhGestorOuAdmin) throw new ForbiddenException("Você não tem permissão para esta operação.");
    }

    private void GarantirAcesso(Tarefa t)
    {
        if (_atual.EhGestorOuAdmin) return;
        if (_atual.ColaboradorId.HasValue && t.PertenceA(_atual.ColaboradorId.Value)) return;
        throw new ForbiddenException("Você só pode acessar as suas próprias tarefas.");
    }

    private async Task<Tarefa> Carregar(int id, CancellationToken ct)
    {
        var t = await _tarefas.ObterDetalhadaAsync(id, ct);
        if (t == null || t.Excluida) throw new NotFoundException("Tarefa não encontrada.");
        return t;
    }

    private async Task ValidarReferencias(int? colaboradorId, int? equipeId, CancellationToken ct)
    {
        if (colaboradorId.HasValue)
        {
            var c = await _colaboradores.GetByIdAsync(colaboradorId.Value, ct);
            if (c == null) throw new ValidationException("colaboradorId", "O colaborador informado não existe.");
            if (!c.Ativo) throw new ValidationException("colaboradorId", "O colaborador informado está inativo.");
        }
        if (equipeId.HasValue && await _equipes.GetByIdAsync(equipeId.Value, ct) == null)
            throw new ValidationException("equipeId", "A equipe informada não existe.");
    }

    private static DateTime NormalizarUtc(DateTime d) =>
        d.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(d, DateTimeKind.Utc) : d.ToUniversalTime();
}
