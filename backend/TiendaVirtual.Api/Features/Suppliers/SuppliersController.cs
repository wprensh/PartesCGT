using Microsoft.AspNetCore.Mvc;
using TiendaVirtual.Api.Common.Security;
using TiendaVirtual.Api.Common.Web;
using TiendaVirtual.Api.Domain.Access;

namespace TiendaVirtual.Api.Features.Suppliers;

[Route("api/suppliers")]
[HasPermission(Permissions.SuppliersView)]
public class SuppliersController(SupplierService suppliers) : ApiControllerBase
{
    [HttpGet]
    public Task<List<SupplierDto>> GetAll(CancellationToken ct) => suppliers.ListAsync(ct);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SupplierDto>> GetById(int id, CancellationToken ct) =>
        OkOrFailure(await suppliers.FindAsync(id, ct));

    /// <summary>Productos que surte, con su costo.</summary>
    [HttpGet("{id:int}/products")]
    public async Task<ActionResult<List<SuppliedProductDto>>> GetProducts(int id, CancellationToken ct) =>
        OkOrFailure(await suppliers.ListProductsAsync(id, ct));

    [HttpPost, HasPermission(Permissions.SuppliersManage)]
    public async Task<ActionResult<SupplierDto>> Create(SupplierUpsert request, CancellationToken ct)
    {
        var result = await suppliers.CreateAsync(request, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : Failure(result.Error!);
    }

    [HttpPut("{id:int}"), HasPermission(Permissions.SuppliersManage)]
    public async Task<ActionResult<SupplierDto>> Update(int id, SupplierUpsert request, CancellationToken ct) =>
        OkOrFailure(await suppliers.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}"), HasPermission(Permissions.SuppliersManage)]
    public async Task<ActionResult> Delete(int id, CancellationToken ct) =>
        NoContentOrFailure(await suppliers.DeleteAsync(id, ct));
}

/// <summary>Proveedores de un producto. Aparte del producto porque los costos son información de Compras.</summary>
[Route("api/products/{productId:int}/suppliers")]
[HasPermission(Permissions.SuppliersView)]
public class ProductSuppliersController(SupplierService suppliers) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ProductSupplierDto>>> GetAll(int productId, CancellationToken ct) =>
        OkOrFailure(await suppliers.ListForProductAsync(productId, ct));

    [HttpPut, HasPermission(Permissions.SuppliersManage)]
    public async Task<ActionResult<List<ProductSupplierDto>>> Replace(
        int productId, [FromBody] List<ProductSupplierUpsert> request, CancellationToken ct) =>
        OkOrFailure(await suppliers.ReplaceForProductAsync(productId, request, ct));
}
