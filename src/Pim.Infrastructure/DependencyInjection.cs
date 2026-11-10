using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pim.Application.Interfaces;
using Pim.Infrastructure.Persistencia;
using Pim.Infrastructure.Repositorios;
using Pim.Infrastructure.Seguranca;

namespace Pim.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        if (config["Database:Provider"] == "InMemory")
        {
            var nome = config["Database:InMemoryName"] ?? "pim";
            services.AddDbContext<PimDbContext>(o => o.UseInMemoryDatabase(nome));
        }
        else
        {
            var cs = config.GetConnectionString("Pim") ?? config["DB_CONNECTION"]
                ?? throw new InvalidOperationException("Defina a connection string 'ConnectionStrings:Pim'.");
            services.AddDbContext<PimDbContext>(o => o.UseSqlServer(cs));
        }

        services.AddSingleton(JwtOptions.De(config));
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, TokenService>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IColaboradorRepository, ColaboradorRepository>();
        services.AddScoped<IEquipeRepository, EquipeRepository>();
        services.AddScoped<ITarefaRepository, TarefaRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IRelatorioRepository, RelatorioRepository>();
        return services;
    }
}
