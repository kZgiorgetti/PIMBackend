using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Pim.Api.Middlewares;

/// <summary>Valida automaticamente cada argumento da action com o IValidator&lt;T&gt; correspondente (se existir).</summary>
public class ValidacaoFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
    {
        var erros = new Dictionary<string, string[]>();

        foreach (var (chave, entrada) in ctx.ModelState)
            if (entrada.Errors.Count > 0)
                erros[chave] = entrada.Errors.Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Valor inválido." : e.ErrorMessage).ToArray();

        if (erros.Count == 0)
        {
            foreach (var arg in ctx.ActionArguments.Values)
            {
                if (arg is null) continue;
                var tipo = typeof(IValidator<>).MakeGenericType(arg.GetType());
                if (ctx.HttpContext.RequestServices.GetService(tipo) is not IValidator validador) continue;
                var r = await validador.ValidateAsync(new ValidationContext<object>(arg), ctx.HttpContext.RequestAborted);
                foreach (var g in r.Errors.GroupBy(e => e.PropertyName))
                    erros[g.Key] = g.Select(e => e.ErrorMessage).ToArray();
            }
        }

        if (erros.Count > 0) throw new Application.Excecoes.ValidationException(erros);
        await next();
    }
}
