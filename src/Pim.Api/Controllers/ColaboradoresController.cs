using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pim.Application.Dtos;
using Pim.Application.Services;

namespace Pim.Api.Controllers;

[Route("api/v1/colaboradores")]
[Authorize(Roles = Papeis.Gestores)]
public class ColaboradoresController : BaseApiController
{
    private readonly ColaboradorService _svc;
    public ColaboradoresController(ColaboradorService svc) => _svc = svc;

    /// <summary>Lista colaboradores (use apenasAtivos=true para ocultar inativos).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ColaboradorResponse>>> Listar([FromQuery] bool apenasAtivos, CancellationToken ct) =>
        Ok(await _svc.ListarAsync(apenasAtivos, ct));

    /// <summary>Obtém um colaborador pelo id.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ColaboradorResponse>> Obter(int id, CancellationToken ct) => Ok(await _svc.ObterAsync(id, ct));

    /// <summary>Cadastra um colaborador. Exemplo: <c>{ "nome": "Maria", "email": "maria@empresa.com", "cargo": "Analista" }</c></summary>
    [HttpPost]
    [ProducesResponseType(typeof(ColaboradorResponse), 201)]
    public async Task<ActionResult<ColaboradorResponse>> Criar(ColaboradorRequest req, CancellationToken ct)
    {
        var r = await _svc.CriarAsync(req, ct);
        return CreatedAtAction(nameof(Obter), new { id = r.Id }, r);
    }

    /// <summary>Atualiza os dados de um colaborador.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ColaboradorResponse>> Atualizar(int id, ColaboradorRequest req, CancellationToken ct) =>
        Ok(await _svc.AtualizarAsync(id, req, ct));

    /// <summary>Exclui o colaborador; se houver tarefas abertas, apenas o inativa (resposta 200 com aviso).</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(RemocaoColaboradorResponse), 200)]
    public async Task<IActionResult> Remover(int id, CancellationToken ct)
    {
        var r = await _svc.RemoverAsync(id, ct);
        return r.Inativado ? Ok(r) : NoContent();
    }
}
