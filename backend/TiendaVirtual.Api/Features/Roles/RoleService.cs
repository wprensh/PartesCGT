using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Api.Common.Results;
using TiendaVirtual.Api.Domain.Access;
using TiendaVirtual.Api.Infrastructure.Persistence;

namespace TiendaVirtual.Api.Features.Roles;

public class RoleService(AppDbContext db)
{
    public static IReadOnlyList<PermissionDto> PermissionCatalog { get; } =
        Permissions.All.Select(p => new PermissionDto(p.Code, p.Module, p.Label, p.Implies)).ToList();

    public async Task<List<RoleDto>> ListAsync(CancellationToken ct)
    {
        var roles = await db.Roles.AsNoTracking().Include(r => r.Permissions).OrderBy(r => r.Id).ToListAsync(ct);
        var userCounts = await db.AdminUsers.GroupBy(u => u.RoleId).Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        return roles.Select(r => ToDto(r, userCounts.GetValueOrDefault(r.Id))).ToList();
    }

    public async Task<Result<RoleDto>> CreateAsync(RoleUpsert request, CancellationToken ct)
    {
        var role = new Role { Name = "" };
        if (role.Rename(request.Name, request.Description) is { } invalidName) return invalidName;
        if (await NameTakenAsync(role.Name, null, ct)) return NameConflict(role.Name);
        if (role.SetPermissions(request.Permissions) is { } invalidPermissions) return invalidPermissions;

        db.Roles.Add(role);
        await db.SaveChangesAsync(ct);
        return ToDto(role, userCount: 0);
    }

    public async Task<Result<RoleDto>> UpdateAsync(int id, RoleUpsert request, CancellationToken ct)
    {
        var role = await db.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == id, ct);
        if (role is null) return Error.NotFound();

        if (role.Rename(request.Name, request.Description) is { } invalidName) return invalidName;
        if (await NameTakenAsync(role.Name, id, ct)) return NameConflict(role.Name);

        var couldManageUsers = role.Can(Permissions.UsersManage);
        if (!role.IsSystem && role.SetPermissions(request.Permissions) is { } invalidPermissions) return invalidPermissions;
        if (couldManageUsers && !role.Can(Permissions.UsersManage) && !await AnotherManagerExistsAsync(id, ct))
            return AccessRules.LastManager;

        // Los permisos se leen de la base en cada petición: aplican de inmediato sin cerrar sesiones.
        await db.SaveChangesAsync(ct);

        return ToDto(role, await db.AdminUsers.CountAsync(u => u.RoleId == id, ct));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (role is null) return Error.NotFound();
        if (role.ValidateDeletion(await db.AdminUsers.CountAsync(u => u.RoleId == id, ct)) is { } error) return error;

        db.Roles.Remove(role);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    /// <summary>¿Hay un usuario activo, con otro rol, que pueda administrar usuarios?</summary>
    private async Task<bool> AnotherManagerExistsAsync(int excludedRoleId, CancellationToken ct)
    {
        var managerRoles = (await db.Roles.AsNoTracking().Include(r => r.Permissions).ToListAsync(ct))
            .Where(r => r.Id != excludedRoleId && r.Can(Permissions.UsersManage))
            .Select(r => r.Id)
            .ToList();
        return await db.AdminUsers.AnyAsync(u => u.IsActive && managerRoles.Contains(u.RoleId), ct);
    }

    private Task<bool> NameTakenAsync(string name, int? exceptId, CancellationToken ct)
    {
        var lower = name.ToLowerInvariant();
        return db.Roles.AnyAsync(r => r.Name.ToLower() == lower && r.Id != exceptId, ct);
    }

    private static Error NameConflict(string name) => Error.Conflict($"Ya existe un rol llamado \"{name}\".");

    private static RoleDto ToDto(Role role, int userCount) => new(
        role.Id, role.Name, role.Description, role.IsSystem, userCount,
        role.Permissions.Select(p => p.Permission).Order(StringComparer.Ordinal).ToList(),
        role.EffectivePermissions().Order(StringComparer.Ordinal).ToList());
}
