using TiendaVirtual.Api.Common.Results;

namespace TiendaVirtual.Api.Domain.Access;

/// <summary>Usuario del panel de administración (no es un cliente de la tienda).</summary>
public class AdminUser
{
    public const int MaxEmailLength = 254;
    public const int MaxNameLength = 120;

    public int Id { get; set; }
    public required string Email { get; set; }
    public required string FullName { get; set; }
    /// <summary>Hash PBKDF2 (formato de ASP.NET Identity). Nunca la contraseña.</summary>
    public required string PasswordHash { get; set; }
    public bool IsActive { get; set; } = true;
    public int RoleId { get; set; }
    public Role? Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    /// <summary>Cambia al cambiar contraseña, rol o estado: invalida de inmediato las sesiones abiertas.</summary>
    public string SecurityStamp { get; set; } = NewStamp();

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public void RotateSecurityStamp() => SecurityStamp = NewStamp();

    private static string NewStamp() => Guid.NewGuid().ToString("N");
}

public static class PasswordPolicy
{
    public const int MinLength = 10;
    public const int MaxLength = 128;

    public static Error? Validate(string password)
    {
        if (password.Length < MinLength) return Error.Validation($"La contraseña debe tener al menos {MinLength} caracteres.");
        if (password.Length > MaxLength) return Error.Validation($"La contraseña no puede superar {MaxLength} caracteres.");
        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            return Error.Validation("La contraseña debe combinar letras y números.");
        return null;
    }
}

/// <summary>Reglas que evitan quedarse sin acceso al panel.</summary>
public static class AccessRules
{
    public static readonly Error LastManager =
        Error.Conflict("Debe quedar al menos un usuario activo que pueda administrar usuarios y roles.");

    public static Error? ValidateSelfChange(int actingUserId, int targetUserId, bool changesRoleOrDisables) =>
        actingUserId == targetUserId && changesRoleOrDisables
            ? Error.Validation("No puedes cambiar tu propio rol, desactivarte ni eliminarte. Pídeselo a otro administrador.")
            : null;
}
