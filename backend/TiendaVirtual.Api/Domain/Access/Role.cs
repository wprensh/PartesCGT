using TiendaVirtual.Api.Common.Results;

namespace TiendaVirtual.Api.Domain.Access;

/// <summary>Rol del panel con su lista de permisos. El rol de sistema (Administrador) siempre los tiene todos.</summary>
public class Role
{
    public const int AdministratorId = 1;
    public const int MaxNameLength = 60;

    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    /// <summary>Rol protegido: no se borra y tiene todos los permisos (también los que se agreguen en el futuro).</summary>
    public bool IsSystem { get; set; }
    public List<RolePermission> Permissions { get; set; } = [];
    public List<AdminUser> Users { get; set; } = [];

    /// <summary>Permisos que realmente aplica el rol, con los implícitos incluidos.</summary>
    public IReadOnlySet<string> EffectivePermissions() =>
        IsSystem
            ? Access.Permissions.Expand(Access.Permissions.All.Select(p => p.Code))
            : Access.Permissions.Expand(Permissions.Select(p => p.Permission));

    public bool Can(string permission) => EffectivePermissions().Contains(permission);

    public Error? Rename(string name, string? description)
    {
        var trimmed = name.Trim();
        if (trimmed.Length is < 3 or > MaxNameLength) return Error.Validation($"El nombre del rol debe tener entre 3 y {MaxNameLength} caracteres.");
        Name = trimmed;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        return null;
    }

    /// <summary>Reemplaza los permisos. Guarda solo los explícitos; los implícitos se calculan al leer.</summary>
    public Error? SetPermissions(IEnumerable<string> codes)
    {
        var requested = codes.Select(c => c.Trim()).Distinct(StringComparer.Ordinal).ToList();
        if (IsSystem) return Error.Validation("El rol Administrador siempre tiene todos los permisos; no se puede cambiar.");
        if (requested.FirstOrDefault(c => !Access.Permissions.Exists(c)) is { } unknown)
            return Error.Validation($"El permiso \"{unknown}\" no existe.");

        Permissions.RemoveAll(p => !requested.Contains(p.Permission));
        foreach (var code in requested.Where(c => Permissions.All(p => p.Permission != c)))
            Permissions.Add(new RolePermission { RoleId = Id, Permission = code });
        return null;
    }

    public Error? ValidateDeletion(int userCount)
    {
        if (IsSystem) return Error.Validation("El rol Administrador no se puede eliminar.");
        if (userCount > 0) return Error.Conflict($"El rol tiene {userCount} usuario(s). Asígnales otro rol antes de eliminarlo.");
        return null;
    }
}

public class RolePermission
{
    public int RoleId { get; set; }
    public required string Permission { get; set; }
}
