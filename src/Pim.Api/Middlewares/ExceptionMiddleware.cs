using Microsoft.EntityFrameworkCore;
using Npgsql;
using Pim.Application.Excecoes;
using Pim.Domain.Excecoes;

namespace Pim.Api.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _log;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> log, IHostEnvironment env)
    { _next = next; _log = log; _env = env; }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try { await _next(ctx); }
        catch (OperationCanceledException) when (ctx.RequestAborted.IsCancellationRequested) { /* cliente desistiu */ }
        catch (Exception ex)
        {
            switch (ex)
            {
                case Application.Excecoes.ValidationException v: await ProblemaHttp.EscreverAsync(ctx, 400, v.Message, v.Erros); break;
                case NotFoundException: await ProblemaHttp.EscreverAsync(ctx, 404, ex.Message); break;
                case ConflictException: await ProblemaHttp.EscreverAsync(ctx, 409, ex.Message); break;
                case ForbiddenException: await ProblemaHttp.EscreverAsync(ctx, 403, ex.Message); break;
                case UnauthorizedException: await ProblemaHttp.EscreverAsync(ctx, 401, ex.Message); break;
                case RegraDeNegocioException: await ProblemaHttp.EscreverAsync(ctx, 422, ex.Message); break;
                case DbUpdateException { InnerException: PostgresException { SqlState: "23505" } }:
                    await ProblemaHttp.EscreverAsync(ctx, 409, "Já existe um registro com estes dados."); break;
                default:
                    _log.LogError(ex, "Erro não tratado em {Metodo} {Caminho}", ctx.Request.Method, ctx.Request.Path);
                    await ProblemaHttp.EscreverAsync(ctx, 500, _env.IsDevelopment() ? ex.ToString() : "Erro interno do servidor."); break;
            }
        }
    }
}
