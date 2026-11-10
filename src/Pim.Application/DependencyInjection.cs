using System.Globalization;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Pim.Application.Services;

namespace Pim.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("pt-BR");
        services.AddValidatorsFromAssemblyContaining<AuthService>();
        services.AddScoped<AuthService>();
        services.AddScoped<UsuarioService>();
        services.AddScoped<ColaboradorService>();
        services.AddScoped<EquipeService>();
        services.AddScoped<TarefaService>();
        services.AddScoped<RelatorioService>();
        return services;
    }
}
