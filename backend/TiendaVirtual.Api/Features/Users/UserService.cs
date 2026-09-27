using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Api.Common.Results;
using TiendaVirtual.Api.Domain.Access;
using TiendaVirtual.Api.Infrastructure.Persistence;

namespace TiendaVirtual.Api.Features.Users;

/// <summary>
/// Usuarios del panel. Protecciones: nadie cambia su propio rol ni se desactiva o elimina a sí mismo,
/// y siempre queda al menos un usuario activo que puede administrar usuarios (no hay forma de quedarse fuera).
/// </summary>
public class UserService(AppDbContext db, IPasswordHasher<AdminUser> hasher, TimeProvider clock)
{
    private static readonly Error RoleMissing = Error.Validation("El rol seleccionado no existe.");

    private static readonly Expression<Func<AdminUser, UserDto>> ToDto = u =>
        new UserDto(u.Id, u.Email, u.FullName, u.RoleId, u.Role!.Name, u.IsActive, u.CreatedAt, u.LastLoginAt);

    public Task<List<UserDto>> ListAsync(CancellationToken ct) =>
        db.AdminUsers.AsNoTracking().OrderBy(u => u.FullName).Select(ToDto).ToListAsync(ct);

    public async Task<Result<UserDto>> CreateAsync(UserCreate request, CancellationToken ct)
    {
        var email = AdminUser.NormalizeEmail(request.Email);
        if (await db.AdminUsers.AnyAsync(u => u.Email == email, ct)) return Error.Conflict($"Ya existe un usuario con el correo {email}.");
        if (!await db.Roles.AnyAsync(r => r.Id == request.RoleId, ct)) return RoleMissing;
        if (PasswordPolicy.Validate(request.Password) is { } weak) return weak;

        var user = new AdminUser
        {
            Email = email, FullName = request.FullName.Trim(), PasswordHash = "", RoleId = request.RoleId,
            CreatedAt = clock.GetUtcNow().UtcDateTime
        };
        user.PasswordHash = hasher.HashPassword(user, request.Password);
        db.AdminUsers.Add(user);
        await db.SaveChangesAsync(ct);
        return await LoadAsync(user.Id, ct);
    }

    public async Task<Result<UserDto>> UpdateAsync(int actingUserId, int id, UserUpdate request, CancellationToken ct)
    {
        var user = await db.AdminUsers.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return Error.NotFound();

        var changesAccess = user.RoleId != request.RoleId || user.IsActive != request.IsActive;
        if (AccessRules.ValidateSelfChange(actingUserId, id, changesAccess) is { } self) return self;
        if (!await db.Roles.AnyAsync(r => r.Id == request.RoleId, ct)) return RoleMissing;
        if (changesAccess && !await CanManageAfterAsync(id, request.IsActive ? request.RoleId : null, ct)) return AccessRules.LastManager;

        user.FullName = request.FullName.Trim();
        user.RoleId = request.RoleId;
        user.IsActive = request.IsActive;
        if (changesAccess) user.RotateSecurityStamp();   // sus sesiones abiertas dejan de valer
        await db.SaveChangesAsync(ct);
        return await LoadAsync(id, ct);
    }

    /// <summary>Un administrador fija una contraseña nueva (p. ej. si el usuario la olvidó). Cierra sus sesiones.</summary>
    public async Task<Result> ResetPasswordAsync(int id, PasswordReset request, CancellationToken ct)
    {
        var user = await db.AdminUsers.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return Error.NotFound();
        if (PasswordPolicy.Validate(request.NewPassword) is { } weak) return weak;

        user.PasswordHash = hasher.HashPassword(user, request.NewPassword);
        user.RotateSecurityStamp();
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int actingUserId, int id, CancellationToken ct)
    {
        var user = await db.AdminUsers.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return Error.NotFound();
        if (AccessRules.ValidateSelfChange(actingUserId, id, changesRoleOrDisables: true) is { } self) return self;
        if (!await CanManageAfterAsync(id, newRoleId: null, ct)) return AccessRules.LastManager;

        db.AdminUsers.Remove(user);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    /// <summary>
    /// ¿Seguirá habiendo alguien activo con "users.manage" si el usuario <paramref name="userId"/> pasa a
    /// <paramref name="newRoleId"/> (null = desactivado o eliminado)?
    /// </summary>
    private async Task<bool> CanManageAfterAsync(int userId, int? newRoleId, CancellationToken ct)
    {
        var managerRoleIds = await RolesThatCanManageUsersAsync(ct);
        if (newRoleId is { } roleId && managerRoleIds.Contains(roleId)) return true;
        return await db.AdminUsers.AnyAsync(u => u.Id != userId && u.IsActive && managerRoleIds.Contains(u.RoleId), ct);
    }

    private async Task<List<int>> RolesThatCanManageUsersAsync(CancellationToken ct) =>
        (await db.Roles.AsNoTracking().Include(r => r.Permissions).ToListAsync(ct))
            .Where(r => r.Can(Permissions.UsersManage))
            .Select(r => r.Id)
            .ToList();

    private Task<UserDto> LoadAsync(int id, CancellationToken ct) =>
        db.AdminUsers.AsNoTracking().Where(u => u.Id == id).Select(ToDto).FirstAsync(ct);
}
