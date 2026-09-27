using Microsoft.AspNetCore.Mvc;
using TiendaVirtual.Api.Common.Security;
using TiendaVirtual.Api.Common.Web;
using TiendaVirtual.Api.Domain.Access;

namespace TiendaVirtual.Api.Features.Users;

[Route("api/users")]
[HasPermission(Permissions.UsersManage)]
public class UsersController(UserService users) : ApiControllerBase
{
    [HttpGet]
    public Task<List<UserDto>> GetAll(CancellationToken ct) => users.ListAsync(ct);

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(UserCreate request, CancellationToken ct)
    {
        var result = await users.CreateAsync(request, ct);
        return result.IsSuccess ? Created($"/api/users/{result.Value.Id}", result.Value) : Failure(result.Error!);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserDto>> Update(int id, UserUpdate request, CancellationToken ct) =>
        OkOrFailure(await users.UpdateAsync(CurrentUserId, id, request, ct));

    [HttpPost("{id:int}/password")]
    public async Task<ActionResult> ResetPassword(int id, PasswordReset request, CancellationToken ct) =>
        NoContentOrFailure(await users.ResetPasswordAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken ct) =>
        NoContentOrFailure(await users.DeleteAsync(CurrentUserId, id, ct));
}
