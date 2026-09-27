using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Api.Common.Results;
using TiendaVirtual.Api.Domain.Access;
using TiendaVirtual.Api.Infrastructure.Persistence;

namespace TiendaVirtual.Api.Features.Auth;

/// <summary>Inicio de sesión, perfil y cambio de contraseña de los usuarios del panel.</summary>
public class AdminAuthService(AppDbContext db, IPasswordHasher<AdminUser> hasher, ITokenIssuer tokens, TimeProvider clock)
{
    // Mismo mensaje si el correo no existe, la contraseña falla o el usuario está inactivo: no revela cuál fue.
    private static readonly Error InvalidCredentials = Error.Unauthorized("Correo o contraseña incorrectos.");

    /// <summary>Hash de relleno: si el correo no existe se verifica igual, para que el tiempo de respuesta no lo delate.</summary>
    private static readonly string DummyHash =
        new PasswordHasher<AdminUser>().HashPassword(new AdminUser { Email = "", FullName = "", PasswordHash = "" }, Guid.NewGuid().ToString());

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = AdminUser.NormalizeEmail(request.Email);
        var user = await db.AdminUsers.FirstOrDefaultAsync(u => u.Email == email, ct);

        var verification = hasher.VerifyHashedPassword(user!, user?.PasswordHash ?? DummyHash, request.Password);
        if (user is null || !user.IsActive || verification == PasswordVerificationResult.Failed) return InvalidCredentials;

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            user.PasswordHash = hasher.HashPassword(user, request.Password);
        user.LastLoginAt = clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);

        return tokens.Issue(user);
    }

    /// <summary>
    /// Crea el primer administrador y devuelve su sesión. Solo funciona con la tabla de usuarios vacía:
    /// en cuanto existe uno, los demás se crean desde el panel. Transacción serializable para que dos
    /// peticiones simultáneas no creen dos "primeros" administradores.
    /// </summary>
    public Task<Result<LoginResponse>> CreateFirstAdminAsync(FirstAdminRequest request, CancellationToken ct)
    {
        if (PasswordPolicy.Validate(request.Password) is { } weak) return Task.FromResult<Result<LoginResponse>>(weak);

        return db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            if (await db.AdminUsers.AnyAsync(ct))
                return (Result<LoginResponse>)Error.Conflict("El panel ya tiene usuarios. Los nuevos se crean desde el panel, en Usuarios.");

            var user = new AdminUser
            {
                Email = AdminUser.NormalizeEmail(request.Email),
                FullName = request.FullName.Trim(),
                PasswordHash = "",
                RoleId = Role.AdministratorId,
                CreatedAt = clock.GetUtcNow().UtcDateTime,
                LastLoginAt = clock.GetUtcNow().UtcDateTime
            };
            user.PasswordHash = hasher.HashPassword(user, request.Password);
            db.AdminUsers.Add(user);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return tokens.Issue(user);
        });
    }

    public async Task<Result<MeDto>> GetMeAsync(int userId, CancellationToken ct)
    {
        var user = await db.AdminUsers.AsNoTracking()
            .Include(u => u.Role).ThenInclude(r => r!.Permissions)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user?.Role is null) return Error.NotFound();

        return new MeDto(user.Id, user.Email, user.FullName, user.Role.Name,
            user.Role.EffectivePermissions().Order(StringComparer.Ordinal).ToList());
    }

    /// <summary>Cambia la contraseña y cierra las demás sesiones (rota el sello). Devuelve un token nuevo para esta.</summary>
    public async Task<Result<LoginResponse>> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken ct)
    {
        var user = await db.AdminUsers.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return Error.NotFound();
        if (hasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
            return Error.Validation("La contraseña actual no es correcta.");
        if (PasswordPolicy.Validate(request.NewPassword) is { } weak) return weak;

        user.PasswordHash = hasher.HashPassword(user, request.NewPassword);
        user.RotateSecurityStamp();
        await db.SaveChangesAsync(ct);
        return tokens.Issue(user);
    }
}
