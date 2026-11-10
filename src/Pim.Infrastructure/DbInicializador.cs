using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pim.Application.Interfaces;
using Pim.Domain.Entidades;
using Pim.Domain.Enums;
using Pim.Infrastructure.Persistencia;

namespace Pim.Infrastructure;

public static class DbInicializador
{
    /// <summary>Cria o Admin inicial (se não existir) a partir de ADMIN_EMAIL / ADMIN_PASSWORD.</summary>
    public static async Task InicializarAsync(PimDbContext db, IPasswordHasher hasher, string? email, string? senha, ILogger logger)
    {
        if (!db.Database.IsRelational()) await db.Database.EnsureCreatedAsync();

        if (await db.Usuarios.AnyAsync(u => u.Perfil == PerfilUsuario.Admin)) return;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
        {
            logger.LogWarning("Nenhum Admin existe e ADMIN_EMAIL/ADMIN_PASSWORD não foram definidos. Defina-os para criar o primeiro Admin.");
            return;
        }
        db.Usuarios.Add(Usuario.Criar("Administrador", email, hasher.Hash(senha), PerfilUsuario.Admin));
        await db.SaveChangesAsync();
        logger.LogInformation("Usuário Admin inicial criado ({Email}).", email);
    }
}
