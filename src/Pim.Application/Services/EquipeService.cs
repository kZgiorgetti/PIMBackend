using Pim.Application.Dtos;
using Pim.Application.Excecoes;
using Pim.Application.Interfaces;
using Pim.Application.Mapeamentos;
using Pim.Domain.Entidades;

namespace Pim.Application.Services;

public class EquipeService
{
    private readonly IEquipeRepository _equipes;
    private readonly IColaboradorRepository _colaboradores;
    private readonly IUnitOfWork _uow;
    private readonly IUsuarioAtual _atual;

    public EquipeService(IEquipeRepository equipes, IColaboradorRepository colaboradores, IUnitOfWork uow, IUsuarioAtual atual)
    { _equipes = equipes; _colaboradores = colaboradores; _uow = uow; _atual = atual; }

    public async Task<IReadOnlyList<EquipeResponse>> ListarAsync(CancellationToken ct = default) =>
        (await _equipes.ListarAsync(ct)).Select(e => e.ParaResponse()).ToList();

    public async Task<EquipeResponse> ObterAsync(int id, CancellationToken ct = default) => (await Carregar(id, ct)).ParaResponse();

    public async Task<EquipeResponse> CriarAsync(EquipeRequest r, CancellationToken ct = default)
    {
        if (await _equipes.NomeExisteAsync(r.Nome.Trim(), null, ct)) throw new ConflictException("Já existe uma equipe com este nome.");
        var e = Equipe.Criar(r.Nome, r.Descricao);
        await _equipes.AddAsync(e, ct);
        await _uow.SalvarAsync(_atual.Id, ct);
        return e.ParaResponse();
    }

    public async Task<EquipeResponse> AtualizarAsync(int id, EquipeRequest r, CancellationToken ct = default)
    {
        var e = await Carregar(id, ct);
        if (await _equipes.NomeExisteAsync(r.Nome.Trim(), id, ct)) throw new ConflictException("Já existe uma equipe com este nome.");
        e.Atualizar(r.Nome, r.Descricao);
        await _uow.SalvarAsync(_atual.Id, ct);
        return e.ParaResponse();
    }

    public async Task ExcluirAsync(int id, CancellationToken ct = default)
    {
        var e = await Carregar(id, ct);
        if (await _equipes.PossuiTarefasAsync(id, ct)) throw new ConflictException("A equipe possui tarefas vinculadas e não pode ser excluída.");
        _equipes.Remove(e);
        await _uow.SalvarAsync(_atual.Id, ct);
    }

    public async Task<EquipeResponse> AdicionarColaboradorAsync(int equipeId, int colaboradorId, CancellationToken ct = default)
    {
        var e = await Carregar(equipeId, ct);
        _ = await _colaboradores.GetByIdAsync(colaboradorId, ct) ?? throw new NotFoundException("Colaborador não encontrado.");
        if (e.Membros.Any(m => m.ColaboradorId == colaboradorId)) throw new ConflictException("O colaborador já faz parte da equipe.");
        e.AdicionarColaborador(colaboradorId);
        await _uow.SalvarAsync(_atual.Id, ct);
        return (await Carregar(equipeId, ct)).ParaResponse();
    }

    public async Task RemoverColaboradorAsync(int equipeId, int colaboradorId, CancellationToken ct = default)
    {
        var e = await Carregar(equipeId, ct);
        if (e.Membros.All(m => m.ColaboradorId != colaboradorId)) throw new NotFoundException("O colaborador não faz parte da equipe.");
        e.RemoverColaborador(colaboradorId);
        await _uow.SalvarAsync(_atual.Id, ct);
    }

    private async Task<Equipe> Carregar(int id, CancellationToken ct) =>
        await _equipes.ObterComMembrosAsync(id, ct) ?? throw new NotFoundException("Equipe não encontrada.");
}
