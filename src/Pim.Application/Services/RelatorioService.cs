using Pim.Application.Dtos;
using Pim.Application.Interfaces;

namespace Pim.Application.Services;

public class RelatorioService
{
    private readonly IRelatorioRepository _repo;
    public RelatorioService(IRelatorioRepository repo) => _repo = repo;
    public Task<IReadOnlyList<ResumoColaboradorResponse>> ResumoPorColaboradorAsync(CancellationToken ct = default) => _repo.ResumoPorColaboradorAsync(ct);
    public Task<IReadOnlyList<TarefaAtrasadaResponse>> AtrasadasAsync(CancellationToken ct = default) => _repo.AtrasadasAsync(ct);
}
