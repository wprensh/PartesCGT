using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TiendaVirtual.Api.Domain.Access;
using TiendaVirtual.Api.Infrastructure.Persistence;

namespace TiendaVirtual.Api.Features.Auth;

/// <summary>
/// Crea el primer administrador desde la configuración (Admin:Email / Admin:Password, idealmente user-secrets)
/// solo si todavía no hay ningún usuario. Luego se ignora: los usuarios se gestionan desde el panel.
/// </summary>
public static partial class AdminBootstrapper
{
    public static async Task EnsureInitialAdminAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<AppDbContext>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(AdminBootstrapper));

        if (await db.AdminUsers.AnyAsync()) return;

        var admin = services.GetRequiredService<IOptions<AdminOptions>>().Value;
        if (string.IsNullOrWhiteSpace(admin.Email) || string.IsNullOrWhiteSpace(admin.Password))
        {
            LogNoAdmin(logger);
            return;
        }
        if (PasswordPolicy.Validate(admin.Password) is { } weak)
        {
            LogWeakPassword(logger, weak.Message ?? "");
            return;
        }

        var user = new AdminUser
        {
            Email = AdminUser.NormalizeEmail(admin.Email),
            FullName = admin.FullName.Trim(),
            PasswordHash = "",
            RoleId = Role.AdministratorId,
            CreatedAt = services.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime
        };
        user.PasswordHash = services.GetRequiredService<IPasswordHasher<AdminUser>>().HashPassword(user, admin.Password);
        db.AdminUsers.Add(user);
        await db.SaveChangesAsync();
        LogCreated(logger, user.Email);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message =
        "No hay usuarios del panel. Configura el primero con: dotnet user-secrets set \"Admin:Email\" \"...\" y \"Admin:Password\" \"...\"")]
    private static partial void LogNoAdmin(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No se creó el primer administrador: {Reason}")]
    private static partial void LogWeakPassword(ILogger logger, string reason);

    [LoggerMessage(Level = LogLevel.Information, Message = "Primer administrador creado: {Email}")]
    private static partial void LogCreated(ILogger logger, string email);
}
