using Microsoft.AspNetCore.Mvc;

namespace Pim.Api.Middlewares;

/// <summary>Escreve erros no formato ProblemDetails (RFC 7807) e mantém a chave "error" usada pelo front.</summary>
public static class ProblemaHttp
{
    private static readonly Dictionary<int, string> Titulos = new()
    {
        [400] = "Requisição inválida", [401] = "Não autenticado", [403] = "Acesso negado", [404] = "Não encontrado",
        [409] = "Conflito", [422] = "Regra de negócio violada", [429] = "Muitas requisições", [500] = "Erro interno"
    };

    public static async Task EscreverAsync(HttpContext ctx, int status, string detalhe, IReadOnlyDictionary<string, string[]>? erros = null)
    {
        if (ctx.Response.HasStarted) return;
        var pd = new ProblemDetails
        {
            Status = status,
            Title = Titulos.TryGetValue(status, out var t) ? t : "Erro",
            Detail = detalhe,
            Type = $"https://httpstatuses.io/{status}",
            Instance = ctx.Request.Path
        };
        pd.Extensions["error"] = detalhe;      // compatibilidade com o front
        pd.Extensions["traceId"] = ctx.TraceIdentifier;
        if (erros != null) pd.Extensions["errors"] = erros;

        ctx.Response.Clear();
        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(pd, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
    }
}
