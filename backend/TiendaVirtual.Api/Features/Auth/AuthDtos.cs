using System.ComponentModel.DataAnnotations;
using TiendaVirtual.Api.Domain.Access;

namespace TiendaVirtual.Api.Features.Auth;

public record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);

public record LoginResponse(string Token, DateTime ExpiresAt);

/// <summary>Quién está conectado y qué puede hacer. El frontend lo usa para mostrar u ocultar secciones.</summary>
public record MeDto(int Id, string Email, string FullName, string Role, IReadOnlyCollection<string> Permissions);

/// <summary>Crea el primer administrador del panel (solo si no hay ningún usuario).</summary>
public record FirstAdminRequest(
    [Required, EmailAddress, StringLength(AdminUser.MaxEmailLength)] string Email,
    [Required, StringLength(AdminUser.MaxNameLength, MinimumLength = 3)] string FullName,
    [Required, StringLength(PasswordPolicy.MaxLength)] string Password);

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, StringLength(PasswordPolicy.MaxLength)] string NewPassword);

/// <summary>
/// Primer administrador. Solo se usa si la tabla de usuarios está vacía (ver AdminBootstrapper);
/// después los usuarios se administran desde el panel.
/// </summary>
public class AdminOptions
{
    public const string SectionName = "Admin";

    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string FullName { get; set; } = "Administrador";
}

public class JwtOptions
{
    public const string SectionName = "Jwt";
    /// <summary>HMAC-SHA256 necesita al menos 256 bits.</summary>
    public const int MinKeyBytes = 32;

    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "TiendaVirtual";
    public int ExpiresHours { get; set; } = 8;
}
