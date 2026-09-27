using Microsoft.AspNetCore.Mvc;
using TiendaVirtual.Api.Common.Security;
using TiendaVirtual.Api.Common.Web;
using TiendaVirtual.Api.Domain.Access;

namespace TiendaVirtual.Api.Features.Categories;

[Route("api/categories")]
public class CategoriesController(CategoryService categories) : ApiControllerBase
{
    [HttpGet]
    public Task<List<CategoryDto>> GetAll(CancellationToken ct) => categories.ListAsync(ct);

    [HttpPost, HasPermission(Permissions.CategoriesManage)]
    public async Task<ActionResult<CategoryDto>> Create(CategoryUpsert request, CancellationToken ct)
    {
        var result = await categories.CreateAsync(request, ct);
        return result.IsSuccess
            ? Created($"/api/categories/{result.Value.Id}", result.Value)
            : Failure(result.Error!);
    }

    [HttpPut("{id:int}"), HasPermission(Permissions.CategoriesManage)]
    public async Task<ActionResult> Update(int id, CategoryUpsert request, CancellationToken ct) =>
        NoContentOrFailure(await categories.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}"), HasPermission(Permissions.CategoriesManage)]
    public async Task<ActionResult> Delete(int id, CancellationToken ct) =>
        NoContentOrFailure(await categories.DeleteAsync(id, ct));
}
