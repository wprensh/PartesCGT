using System.ComponentModel.DataAnnotations;
using TiendaVirtual.Api.Domain.Access;

namespace TiendaVirtual.Api.Features.Users;

public record UserDto(
    int Id, string Email, string FullName, int RoleId, string Role, bool IsActive, DateTime CreatedAt, DateTime? LastLoginAt);

public record UserCreate(
    [Required, EmailAddress, StringLength(AdminUser.MaxEmailLength)] string Email,
    [Required, StringLength(AdminUser.MaxNameLength, MinimumLength = 3)] string FullName,
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un rol.")] int RoleId,
    [Required, StringLength(PasswordPolicy.MaxLength)] string Password);

public record UserUpdate(
    [Required, StringLength(AdminUser.MaxNameLength, MinimumLength = 3)] string FullName,
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un rol.")] int RoleId,
    bool IsActive);

public record PasswordReset([Required, StringLength(PasswordPolicy.MaxLength)] string NewPassword);
