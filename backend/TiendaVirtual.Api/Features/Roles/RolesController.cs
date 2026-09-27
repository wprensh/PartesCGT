using Microsoft.AspNetCore.Mvc;
using TiendaVirtual.Api.Common.Security;
using TiendaVirtual.Api.Common.Web;
using TiendaVirtual.Api.Domain.Access;

namespace TiendaVirtual.Api.Features.Roles;

[Route("api/roles")]
[HasPermission(Permissions.UsersManage)]
public class RolesController(RoleService roles) : ApiControllerBase
{
    [HttpGet]
    public Task<List<RoleDto>> GetAll(CancellationToken ct) => roles.ListAsync(ct);

    /// <summary>Catálogo de permisos disponibles, para armar la pantalla de roles.</summary>
    [HttpGet("permissions")]
    public IReadOnlyList<PermissionDto> GetPermissions() => RoleService.PermissionCatalog;

    [HttpPost]
    public async Task<ActionResult<RoleDto>> Create(RoleUpsert request, CancellationToken ct)
    {
        var result = await roles.CreateAsync(request, ct);
        return result.IsSuccess ? Created($"/api/roles/{result.Value.Id}", result.Value) : Failure(result.Error!);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RoleDto>> Update(int id, RoleUpsert request, CancellationToken ct) =>
        OkOrFailure(await roles.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken ct) =>
        NoContentOrFailure(await roles.DeleteAsync(id, ct));
}
