using System.ComponentModel.DataAnnotations;
using TiendaVirtual.Api.Domain.Access;

namespace TiendaVirtual.Api.Features.Roles;

/// <param name="Permissions">Los que el rol tiene marcados explícitamente.</param>
/// <param name="EffectivePermissions">Los que realmente aplica (incluye los implícitos: gestionar ⇒ ver).</param>
public record RoleDto(
    int Id, string Name, string? Description, bool IsSystem, int UserCount,
    IReadOnlyCollection<string> Permissions, IReadOnlyCollection<string> EffectivePermissions);

public record RoleUpsert(
    [Required, StringLength(Role.MaxNameLength, MinimumLength = 3)] string Name,
    [StringLength(200)] string? Description,
    [Required] List<string> Permissions);

public record PermissionDto(string Code, string Module, string Label, string? Implies);
