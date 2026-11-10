using Pim.Application.Dtos;
using Pim.Application.Excecoes;
using Pim.Application.Interfaces;
using Pim.Application.Mapeamentos;
using Pim.Domain.Entidades;

namespace Pim.Application.Services;

public class ColaboradorService
{
    private readonly IColaboradorRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IUsuarioAtual _atual;

    public ColaboradorService(IColaboradorRepository repo, IUnitOfWork uow, IUsuarioAtual atual)
    { _repo = repo; _uow = uow; _atual = atual; }

    public async Task<IReadOnlyList<ColaboradorResponse>> ListarAsync(bool apenasAtivos = false, CancellationToken ct = default) =>
        (await _repo.ListarAsync(apenasAtivos, ct)).Select(c => c.ParaResponse()).ToList();

    public async Task<ColaboradorResponse> ObterAsync(int id, CancellationToken ct = default) => (await Carregar(id, ct)).ParaResponse();

    public async Task<ColaboradorResponse> CriarAsync(ColaboradorRequest r, CancellationToken ct = default)
    {
        await GarantirEmailLivre(r.Email, null, ct);
        var c = Colaborador.Criar(r.Nome, r.Email, r.Cargo);
        await _repo.AddAsync(c, ct);
        await _uow.SalvarAsync(_atual.Id, ct);
        return c.ParaResponse();
    }

    public async Task<ColaboradorResponse> AtualizarAsync(int id, ColaboradorRequest r, CancellationToken ct = default)
    {
        var c = await Carregar(id, ct);
        await GarantirEmailLivre(r.Email, id, ct);
        c.Atualizar(r.Nome, r.Email, r.Cargo);
        await _uow.SalvarAsync(_atual.Id, ct);
        return c.ParaResponse();
    }

    /// <summary>Exclui; se houver tarefas abertas, apenas inativa (regra aplicada pela trigger no banco).</summary>
    public async Task<RemocaoColaboradorResponse> RemoverAsync(int id, CancellationToken ct = default)
    {
        await Carregar(id, ct);
        var res = await _repo.RemoverOuInativarAsync(id, ct);
        return res == ResultadoRemocao.Inativado
            ? new RemocaoColaboradorResponse(true, "O colaborador possui tarefas abertas e foi inativado em vez de excluído.")
            : new RemocaoColaboradorResponse(false, "Colaborador excluído.");
    }

    private async Task<Colaborador> Carregar(int id, CancellationToken ct) =>
        await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Colaborador não encontrado.");

    private async Task GarantirEmailLivre(string? email, int? ignorarId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email)) return;
        if (await _repo.EmailExisteAsync(email.Trim().ToLowerInvariant(), ignorarId, ct))
            throw new ConflictException("Já existe um colaborador com este e-mail.");
    }
}
