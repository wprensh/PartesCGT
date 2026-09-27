using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Api.Common.Results;
using TiendaVirtual.Api.Domain.Suppliers;
using TiendaVirtual.Api.Infrastructure.Persistence;

namespace TiendaVirtual.Api.Features.Suppliers;

public class SupplierService(AppDbContext db)
{
    private static readonly Expression<Func<Supplier, SupplierDto>> ToDto = s => new SupplierDto(
        s.Id, s.Name, s.TaxId, s.ContactName, s.Email, s.Phone, s.Address, s.City, s.Notes, s.IsActive, s.Products.Count);

    public Task<List<SupplierDto>> ListAsync(CancellationToken ct) =>
        db.Suppliers.AsNoTracking().OrderBy(s => s.Name).Select(ToDto).ToListAsync(ct);

    public async Task<Result<SupplierDto>> FindAsync(int id, CancellationToken ct) =>
        await db.Suppliers.AsNoTracking().Where(s => s.Id == id).Select(ToDto).FirstOrDefaultAsync(ct) is { } supplier
            ? supplier
            : Error.NotFound();

    public async Task<Result<List<SuppliedProductDto>>> ListProductsAsync(int id, CancellationToken ct)
    {
        if (!await db.Suppliers.AnyAsync(s => s.Id == id, ct)) return Error.NotFound();
        return await db.ProductSuppliers.AsNoTracking()
            .Where(ps => ps.SupplierId == id)
            .OrderBy(ps => ps.Product!.Name)
            .Select(ps => new SuppliedProductDto(ps.ProductId, ps.Product!.Name, ps.Product.Price, ps.Cost, ps.SupplierSku, ps.IsPreferred))
            .ToListAsync(ct);
    }

    public async Task<Result<SupplierDto>> CreateAsync(SupplierUpsert request, CancellationToken ct)
    {
        var supplier = Supplier.Create(request.ToDetails());
        if (await TaxIdTakenAsync(supplier.TaxId, null, ct)) return TaxIdConflict(supplier.TaxId);

        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync(ct);
        return await FindAsync(supplier.Id, ct);
    }

    public async Task<Result<SupplierDto>> UpdateAsync(int id, SupplierUpsert request, CancellationToken ct)
    {
        var supplier = await db.Suppliers.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (supplier is null) return Error.NotFound();

        supplier.Update(request.ToDetails());
        if (await TaxIdTakenAsync(supplier.TaxId, id, ct)) return TaxIdConflict(supplier.TaxId);

        await db.SaveChangesAsync(ct);
        return await FindAsync(id, ct);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var supplier = await db.Suppliers.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (supplier is null) return Error.NotFound();
        if (supplier.ValidateDeletion(await db.ProductSuppliers.CountAsync(ps => ps.SupplierId == id, ct)) is { } error) return error;

        db.Suppliers.Remove(supplier);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ---------- Proveedores de un producto ----------

    public async Task<Result<List<ProductSupplierDto>>> ListForProductAsync(int productId, CancellationToken ct)
    {
        if (!await db.Products.AnyAsync(p => p.Id == productId, ct)) return Error.NotFound();
        return await ProductSuppliersQuery(productId).ToListAsync(ct);
    }

    /// <summary>Reemplaza la lista completa de proveedores del producto (con costos y preferido).</summary>
    public async Task<Result<List<ProductSupplierDto>>> ReplaceForProductAsync(
        int productId, IReadOnlyCollection<ProductSupplierUpsert> request, CancellationToken ct)
    {
        if (!await db.Products.AnyAsync(p => p.Id == productId, ct)) return Error.NotFound();

        var offers = request.Select(r => r.ToOffer()).ToList();
        if (SupplierOffers.Validate(offers) is { } invalid) return invalid;

        var requestedIds = offers.Select(o => o.SupplierId).ToList();
        var current = await db.ProductSuppliers.Where(ps => ps.ProductId == productId).ToListAsync(ct);
        var currentIds = current.Select(ps => ps.SupplierId).ToHashSet();

        // Se pueden conservar proveedores que se desactivaron, pero no agregar uno inactivo o inexistente.
        var addable = await db.Suppliers.Where(s => requestedIds.Contains(s.Id) && s.IsActive).Select(s => s.Id).ToListAsync(ct);
        if (requestedIds.FirstOrDefault(id => !currentIds.Contains(id) && !addable.Contains(id)) is var bad and > 0)
            return Error.Validation($"El proveedor {bad} no existe o está inactivo.");

        db.ProductSuppliers.RemoveRange(current.Where(ps => !requestedIds.Contains(ps.SupplierId)));
        foreach (var offer in offers)
        {
            var link = current.FirstOrDefault(ps => ps.SupplierId == offer.SupplierId);
            if (link is null) db.ProductSuppliers.Add(link = new ProductSupplier { ProductId = productId, SupplierId = offer.SupplierId });
            link.Cost = offer.Cost;
            link.SupplierSku = offer.SupplierSku;
            link.IsPreferred = offer.IsPreferred;
        }
        await db.SaveChangesAsync(ct);
        return await ProductSuppliersQuery(productId).ToListAsync(ct);
    }

    private IQueryable<ProductSupplierDto> ProductSuppliersQuery(int productId) =>
        db.ProductSuppliers.AsNoTracking()
            .Where(ps => ps.ProductId == productId)
            .OrderByDescending(ps => ps.IsPreferred).ThenBy(ps => ps.Supplier!.Name)
            .Select(ps => new ProductSupplierDto(ps.SupplierId, ps.Supplier!.Name, ps.Supplier.IsActive, ps.Cost, ps.SupplierSku, ps.IsPreferred));

    private Task<bool> TaxIdTakenAsync(string taxId, int? exceptId, CancellationToken ct) =>
        db.Suppliers.AnyAsync(s => s.TaxId == taxId && s.Id != exceptId, ct);

    private static Error TaxIdConflict(string taxId) => Error.Conflict($"Ya existe un proveedor con el NIT {taxId}.");
}
