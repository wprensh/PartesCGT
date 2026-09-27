using Microsoft.AspNetCore.Mvc;
using TiendaVirtual.Api.Common.Security;
using TiendaVirtual.Api.Common.Web;
using TiendaVirtual.Api.Domain.Access;

namespace TiendaVirtual.Api.Features.Products;

[Route("api/products")]
public class ProductsController(ProductQueries queries, ProductService products, ProductImageService images) : ApiControllerBase
{
    // ---------- Tienda ----------

    [HttpGet]
    public Task<List<ProductDto>> GetAll([FromQuery] int? categoryId, [FromQuery] string? q, CancellationToken ct) =>
        queries.ListAsync(new ProductFilter(categoryId, q), includeInactive: false, ct);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken ct)
    {
        var product = await queries.FindAsync(id, includeInactive: false, ct);
        if (product is null) return NotFound();
        return product;
    }

    // ---------- Panel ----------

    /// <summary>Incluye productos inactivos.</summary>
    [HttpGet("admin"), HasPermission(Permissions.ProductsView)]
    public Task<List<ProductDto>> GetAllAdmin([FromQuery] int? categoryId, [FromQuery] string? q, CancellationToken ct) =>
        queries.ListAsync(new ProductFilter(categoryId, q), includeInactive: true, ct);

    [HttpPost, HasPermission(Permissions.ProductsManage)]
    public async Task<ActionResult<ProductDto>> Create(ProductUpsert request, CancellationToken ct)
    {
        var result = await products.CreateAsync(request, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : Failure(result.Error!);
    }

    [HttpPut("{id:int}"), HasPermission(Permissions.ProductsManage)]
    public async Task<ActionResult<ProductDto>> Update(int id, ProductUpsert request, CancellationToken ct) =>
        OkOrFailure(await products.UpdateAsync(id, request, ct));

    /// <summary>Sube una foto (JPG, PNG o WebP, máx. 2 MB). Devuelve la URL para guardar en el producto.</summary>
    [HttpPost("images"), HasPermission(Permissions.ProductsManage), RequestSizeLimit(ProductImageService.MaxRequestBytes)]
    public async Task<ActionResult<ImageUploadDto>> UploadImage(IFormFile file, CancellationToken ct)
    {
        await using var content = file.OpenReadStream();
        var result = await images.UploadAsync(content, file.Length, ct);
        return result.IsSuccess ? Created(result.Value.Url, result.Value) : Failure(result.Error!);
    }

    [HttpDelete("{id:int}"), HasPermission(Permissions.ProductsManage)]
    public async Task<ActionResult> Delete(int id, CancellationToken ct) =>
        NoContentOrFailure(await products.DeleteAsync(id, ct));
}
