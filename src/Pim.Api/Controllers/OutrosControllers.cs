using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pim.Application.Dtos;
using Pim.Application.Services;

namespace Pim.Api.Controllers;

[Route("api/v1/equipes")]
[Authorize(Roles = Papeis.Gestores)]
public class EquipesController : BaseApiController
{
    private readonly EquipeService _svc;
    public EquipesController(EquipeService svc) => _svc = svc;

    /// <summary>Lista as equipes com seus membros.</summary>
    [HttpGet] public async Task<ActionResult<IReadOnlyList<EquipeResponse>>> Listar(CancellationToken ct) => Ok(await _svc.ListarAsync(ct));

    /// <summary>Obtém uma equipe.</summary>
    [HttpGet("{id:int}")] public async Task<ActionResult<EquipeResponse>> Obter(int id, CancellationToken ct) => Ok(await _svc.ObterAsync(id, ct));

    /// <summary>Cria uma equipe.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(EquipeResponse), 201)]
    public async Task<ActionResult<EquipeResponse>> Criar(EquipeRequest req, CancellationToken ct)
    {
        var r = await _svc.CriarAsync(req, ct);
        return CreatedAtAction(nameof(Obter), new { id = r.Id }, r);
    }

    /// <summary>Atualiza nome e descrição da equipe.</summary>
    [HttpPut("{id:int}")] public async Task<ActionResult<EquipeResponse>> Atualizar(int id, EquipeRequest req, CancellationToken ct) => Ok(await _svc.AtualizarAsync(id, req, ct));

    /// <summary>Exclui a equipe (não permitido se houver tarefas vinculadas).</summary>
    [HttpDelete("{id:int}")] public async Task<IActionResult> Excluir(int id, CancellationToken ct) { await _svc.ExcluirAsync(id, ct); return NoContent(); }

    /// <summary>Adiciona um colaborador à equipe.</summary>
    [HttpPost("{id:int}/colaboradores/{colaboradorId:int}")]
    public async Task<ActionResult<EquipeResponse>> AdicionarColaborador(int id, int colaboradorId, CancellationToken ct) =>
        Ok(await _svc.AdicionarColaboradorAsync(id, colaboradorId, ct));

    /// <summary>Remove um colaborador da equipe.</summary>
    [HttpDelete("{id:int}/colaboradores/{colaboradorId:int}")]
    public async Task<IActionResult> RemoverColaborador(int id, int colaboradorId, CancellationToken ct) { await _svc.RemoverColaboradorAsync(id, colaboradorId, ct); return NoContent(); }
}

[Route("api/v1/usuarios")]
[Authorize(Roles = Papeis.Admin)]
public class UsuariosController : BaseApiController
{
    private readonly UsuarioService _svc;
    public UsuariosController(UsuarioService svc) => _svc = svc;

    /// <summary>Lista usuários (somente Admin).</summary>
    [HttpGet] public async Task<ActionResult<IReadOnlyList<UsuarioResponse>>> Listar(CancellationToken ct) => Ok(await _svc.ListarAsync(ct));

    /// <summary>Obtém um usuário.</summary>
    [HttpGet("{id:int}")] public async Task<ActionResult<UsuarioResponse>> Obter(int id, CancellationToken ct) => Ok(await _svc.ObterAsync(id, ct));

    /// <summary>Cria um usuário. Para o perfil Colaborador, um colaborador vinculado é criado automaticamente.</summary>
    /// <remarks>Exemplo: <c>{ "nome": "Ana", "email": "ana@pim.local", "senha": "Senha@123", "perfil": "Colaborador" }</c></remarks>
    [HttpPost]
    [ProducesResponseType(typeof(UsuarioResponse), 201)]
    public async Task<ActionResult<UsuarioResponse>> Criar(CriarUsuarioRequest req, CancellationToken ct)
    {
        var r = await _svc.CriarAsync(req, ct);
        return CreatedAtAction(nameof(Obter), new { id = r.Id }, r);
    }

    /// <summary>Atualiza nome, e-mail e perfil.</summary>
    [HttpPut("{id:int}")] public async Task<ActionResult<UsuarioResponse>> Atualizar(int id, AtualizarUsuarioRequest req, CancellationToken ct) => Ok(await _svc.AtualizarAsync(id, req, ct));

    /// <summary>Redefine a senha do usuário.</summary>
    [HttpPost("{id:int}/redefinir-senha")]
    public async Task<IActionResult> RedefinirSenha(int id, RedefinirSenhaRequest req, CancellationToken ct) { await _svc.RedefinirSenhaAsync(id, req, ct); return NoContent(); }

    /// <summary>Ativa o usuário.</summary>
    [HttpPost("{id:int}/ativar")] public async Task<ActionResult<UsuarioResponse>> Ativar(int id, CancellationToken ct) => Ok(await _svc.AlterarAtivoAsync(id, true, ct));

    /// <summary>Inativa o usuário (impede novos logins).</summary>
    [HttpPost("{id:int}/inativar")] public async Task<ActionResult<UsuarioResponse>> Inativar(int id, CancellationToken ct) => Ok(await _svc.AlterarAtivoAsync(id, false, ct));

    /// <summary>Equivale a inativar (usuários não são apagados para preservar o histórico).</summary>
    [HttpDelete("{id:int}")] public async Task<IActionResult> Excluir(int id, CancellationToken ct) { await _svc.AlterarAtivoAsync(id, false, ct); return NoContent(); }
}

[Route("api/v1/relatorios")]
[Authorize(Roles = Papeis.Gestores)]
public class RelatoriosController : BaseApiController
{
    private readonly RelatorioService _svc;
    public RelatoriosController(RelatorioService svc) => _svc = svc;

    /// <summary>Total de tarefas por status para cada colaborador (sp_ResumoTarefasPorColaborador).</summary>
    [HttpGet("resumo-por-colaborador")]
    public async Task<ActionResult<IReadOnlyList<ResumoColaboradorResponse>>> Resumo(CancellationToken ct) => Ok(await _svc.ResumoPorColaboradorAsync(ct));

    /// <summary>Tarefas com prazo vencido e não concluídas (sp_TarefasAtrasadas).</summary>
    [HttpGet("atrasadas")]
    public async Task<ActionResult<IReadOnlyList<TarefaAtrasadaResponse>>> Atrasadas(CancellationToken ct) => Ok(await _svc.AtrasadasAsync(ct));
}

[Route("api/v1/sync")]
public class SyncController : BaseApiController
{
    private readonly TarefaService _svc;
    public SyncController(TarefaService svc) => _svc = svc;

    /// <summary>Sincronização incremental para o mobile: tarefas alteradas desde o instante informado, incluindo as excluídas (flag excluida).</summary>
    /// <remarks>Exemplo: <c>GET /api/v1/sync/tarefas?atualizadoDesde=2026-11-01T00:00:00Z</c>. Guarde <c>servidorEm</c> para a próxima chamada.</remarks>
    [HttpGet("tarefas")]
    public async Task<ActionResult<SyncResponse>> Tarefas([FromQuery] DateTime? atualizadoDesde, CancellationToken ct) =>
        Ok(await _svc.SyncAsync(atualizadoDesde, ct));
}
