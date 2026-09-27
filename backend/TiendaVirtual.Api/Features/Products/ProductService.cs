using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Api.Common.Results;
using TiendaVirtual.Api.Common.Storage;
using TiendaVirtual.Api.Domain.Catalog;
using TiendaVirtual.Api.Infrastructure.Persistence;

namespace TiendaVirtual.Api.Features.Products;

/// <summary>Altas, cambios y bajas de productos desde el panel.</summary>
public class ProductService(AppDbContext db, ProductQueries queries, IFileStorage files)
{
    private static readonly Error CategoryMissing = Error.Validation("La categoría seleccionada no existe.");
    private static readonly Error InvalidImage =
        Error.Validation("La imagen debe ser una dirección http(s) o una imagen subida desde el panel.");
    private static readonly Error HasOrders =
        Error.Conflict("Este producto tiene pedidos registrados. Desactívalo para ocultarlo de la tienda.");

    public async Task<Result<ProductDto>> CreateAsync(ProductUpsert request, CancellationToken ct)
    {
        if (!await CategoryExistsAsync(request.CategoryId, ct)) return CategoryMissing;
        if (!IsAllowedImage(request.ImageUrl)) return InvalidImage;

        var product = Product.Create(request.ToDetails());
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        return await LoadAsync(product.Id, ct);
    }

    public async Task<Result<ProductDto>> UpdateAsync(int id, ProductUpsert request, CancellationToken ct)
    {
        var product = await db.Products.Include(p => p.Attributes).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (product is null) return Error.NotFound();
        if (!await CategoryExistsAsync(request.CategoryId, ct)) return CategoryMissing;
        if (!IsAllowedImage(request.ImageUrl)) return InvalidImage;

        var previousImage = product.ImageUrl;
        product.Update(request.ToDetails());
        await db.SaveChangesAsync(ct);

        if (previousImage != product.ImageUrl) await DeleteImageIfUnusedAsync(previousImage, ct);
        return await LoadAsync(id, ct);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var product = await db.Products.FindAsync([id], ct);
        if (product is null) return Error.NotFound();
        // Si ya se vendió, borrarlo rompería el historial: se pide desactivarlo.
        if (await db.OrderItems.AnyAsync(i => i.ProductId == id, ct)) return HasOrders;

        db.Products.Remove(product);
        await db.SaveChangesAsync(ct);
        await DeleteImageIfUnusedAsync(product.ImageUrl, ct);
        return Result.Success();
    }

    /// <summary>Sin imagen, una URL externa http(s) o un archivo subido a este almacén.</summary>
    private bool IsAllowedImage(string? url) =>
        string.IsNullOrWhiteSpace(url) || ProductImage.IsExternalUrl(url) || files.IsStored(url.Trim());

    /// <summary>Borra el archivo subido solo si ningún otro producto lo sigue usando.</summary>
    private async Task DeleteImageIfUnusedAsync(string? url, CancellationToken ct)
    {
        if (!files.IsStored(url)) return;
        if (await db.Products.AnyAsync(p => p.ImageUrl == url, ct)) return;
        files.TryDelete(url);
    }

    private Task<bool> CategoryExistsAsync(int categoryId, CancellationToken ct) =>
        db.Categories.AnyAsync(c => c.Id == categoryId, ct);

    private async Task<ProductDto> LoadAsync(int id, CancellationToken ct) =>
        await queries.FindAsync(id, includeInactive: true, ct)
        ?? throw new InvalidOperationException($"El producto {id} desapareció justo después de guardarlo.");
}
