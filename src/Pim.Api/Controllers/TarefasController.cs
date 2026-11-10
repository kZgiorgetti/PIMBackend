using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pim.Application.Dtos;
using Pim.Application.Services;

namespace Pim.Api.Controllers;

[Route("api/v1/tarefas")]
public class TarefasController : BaseApiController
{
    private readonly TarefaService _svc;
    public TarefasController(TarefaService svc) => _svc = svc;

    /// <summary>Lista tarefas com paginação e filtros. Colaboradores veem apenas as próprias tarefas.</summary>
    /// <remarks>Exemplo: <c>GET /api/v1/tarefas?status=Pendente&amp;busca=relatório&amp;page=1&amp;pageSize=20</c> (pageSize máximo: 100).</remarks>
    [HttpGet]
    public async Task<ActionResult<PaginaResponse<TarefaResponse>>> Listar([FromQuery] TarefaFiltro filtro, CancellationToken ct) =>
        Ok(await _svc.ListarAsync(filtro, ct));

    /// <summary>Obtém uma tarefa (colaborador só acessa as suas).</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TarefaResponse>> Obter(int id, CancellationToken ct) => Ok(await _svc.ObterAsync(id, ct));

    /// <summary>Cria uma tarefa (Admin/Gestor).</summary>
    /// <remarks>Exemplo: <c>{ "titulo": "Fechar inventário", "prioridade": "Alta", "dataPrazo": "2026-12-01T00:00:00Z", "colaboradorId": 1 }</c></remarks>
    [HttpPost]
    [ProducesResponseType(typeof(TarefaResponse), 201)]
    public async Task<ActionResult<TarefaResponse>> Criar(CriarTarefaRequest req, CancellationToken ct)
    {
        var r = await _svc.CriarAsync(req, ct);
        return CreatedAtAction(nameof(Obter), new { id = r.Id }, r);
    }

    /// <summary>Atualiza os dados de uma tarefa (Admin/Gestor).</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<TarefaResponse>> Atualizar(int id, AtualizarTarefaRequest req, CancellationToken ct) =>
        Ok(await _svc.AtualizarAsync(id, req, ct));

    /// <summary>Exclusão lógica da tarefa (Admin/Gestor).</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Excluir(int id, CancellationToken ct) { await _svc.ExcluirAsync(id, ct); return NoContent(); }

    /// <summary>Altera o status. Colaborador só altera as próprias tarefas. Gera histórico via trigger.</summary>
    /// <remarks>Exemplo: <c>{ "status": "EmAndamento" }</c></remarks>
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<TarefaResponse>> AlterarStatus(int id, AlterarStatusRequest req, CancellationToken ct) =>
        Ok(await _svc.AlterarStatusAsync(id, req, ct));

    /// <summary>Conclui a tarefa usando a procedure sp_ConcluirTarefa.</summary>
    [HttpPost("{id:int}/concluir")]
    public async Task<ActionResult<TarefaResponse>> Concluir(int id, CancellationToken ct) => Ok(await _svc.ConcluirAsync(id, ct));

    /// <summary>Reatribui a tarefa a outro colaborador (procedure sp_ReatribuirTarefa). Admin/Gestor.</summary>
    [HttpPost("{id:int}/reatribuir")]
    public async Task<ActionResult<TarefaResponse>> Reatribuir(int id, ReatribuirTarefaRequest req, CancellationToken ct) =>
        Ok(await _svc.ReatribuirAsync(id, req, ct));

    /// <summary>Histórico de mudanças de status da tarefa.</summary>
    [HttpGet("{id:int}/historico")]
    public async Task<ActionResult<IReadOnlyList<HistoricoResponse>>> Historico(int id, CancellationToken ct) => Ok(await _svc.HistoricoAsync(id, ct));

    /// <summary>Lista os comentários da tarefa.</summary>
    [HttpGet("{id:int}/comentarios")]
    public async Task<ActionResult<IReadOnlyList<ComentarioResponse>>> Comentarios(int id, CancellationToken ct) => Ok(await _svc.ComentariosAsync(id, ct));

    /// <summary>Adiciona um comentário à tarefa.</summary>
    [HttpPost("{id:int}/comentarios")]
    [ProducesResponseType(typeof(ComentarioResponse), 201)]
    public async Task<ActionResult<ComentarioResponse>> Comentar(int id, ComentarioRequest req, CancellationToken ct)
    {
        var r = await _svc.ComentarAsync(id, req, ct);
        return Created($"/api/v1/tarefas/{id}/comentarios", r);
    }
}
