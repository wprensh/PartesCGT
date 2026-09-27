using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Api.Domain.Catalog;
using TiendaVirtual.Api.Infrastructure.Persistence;

namespace TiendaVirtual.Api.Features.Products;

/// <summary>Lecturas del catálogo (sin tracking, proyectadas a DTO). La usan productos, reseñas y el asistente.</summary>
public class ProductQueries(AppDbContext db)
{
    public Task<List<ProductDto>> ListAsync(ProductFilter filter, bool includeInactive, CancellationToken ct) =>
        Visible(includeInactive)
            .ApplyFilter(filter)
            .OrderBy(p => p.Category!.Name).ThenBy(p => p.Name)
            .Select(ProductMapping.ToDto)
            .ToListAsync(ct);

    public Task<ProductDto?> FindAsync(int id, bool includeInactive, CancellationToken ct) =>
        Visible(includeInactive).Where(p => p.Id == id).Select(ProductMapping.ToDto).FirstOrDefaultAsync(ct);

    public Task<bool> IsPublishedAsync(int id, CancellationToken ct) =>
        db.Products.AnyAsync(p => p.Id == id && p.IsActive, ct);

    /// <summary>Productos que se pueden recomendar: activos y con stock.</summary>
    public Task<List<ProductDto>> ListAvailableAsync(IReadOnlyCollection<int> ids, CancellationToken ct) =>
        ids.Count == 0
            ? Task.FromResult(new List<ProductDto>())
            : db.Products.AsNoTracking()
                .Where(p => ids.Contains(p.Id) && p.Stock > 0 && p.IsActive)
                .Select(ProductMapping.ToDto)
                .ToListAsync(ct);

    private IQueryable<Product> Visible(bool includeInactive)
    {
        var query = db.Products.AsNoTracking();
        return includeInactive ? query : query.Where(p => p.IsActive);
    }
}

internal static class ProductQueryFilters
{
    public static IQueryable<Product> ApplyFilter(this IQueryable<Product> query, ProductFilter filter)
    {
        if (filter.CategoryId is int categoryId && categoryId > 0)
            query = query.Where(p => p.CategoryId == categoryId || p.Category!.ParentId == categoryId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLowerInvariant();
            query = query.Where(p => p.Name.ToLower().Contains(term)
                                     || p.Description.ToLower().Contains(term)
                                     || p.Brand.ToLower().Contains(term));
        }
        return query;
    }
}
