using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Api.Common.Results;
using TiendaVirtual.Api.Domain.Catalog;
using TiendaVirtual.Api.Infrastructure.Persistence;

namespace TiendaVirtual.Api.Features.Categories;

public class CategoryService(AppDbContext db)
{
    public async Task<List<CategoryDto>> ListAsync(CancellationToken ct)
    {
        var rows = await db.Categories.AsNoTracking()
            .Select(c => new CategoryCountRow(c.Id, c.Name, c.ParentId, c.Products.Count(p => p.IsActive), c.Products.Count))
            .ToListAsync(ct);
        return CategoryCounts.Aggregate(rows);
    }

    public async Task<Result<CategoryDto>> CreateAsync(CategoryUpsert request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        if (await ValidateAsync(null, name, request.ParentId, ct) is { } error) return error;

        var category = new Category { Name = name, ParentId = request.ParentId };
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
        return new CategoryDto(category.Id, category.Name, category.ParentId, ActiveProducts: 0, TotalProducts: 0);
    }

    public async Task<Result> UpdateAsync(int id, CategoryUpsert request, CancellationToken ct)
    {
        var category = await db.Categories.FindAsync([id], ct);
        if (category is null) return Error.NotFound();

        var name = request.Name.Trim();
        if (await ValidateAsync(id, name, request.ParentId, ct) is { } error) return error;

        category.Name = name;
        category.ParentId = request.ParentId;
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var category = await db.Categories.FindAsync([id], ct);
        if (category is null) return Error.NotFound();

        var subcategories = await db.Categories.CountAsync(c => c.ParentId == id, ct);
        var products = await db.Products.CountAsync(p => p.CategoryId == id, ct);
        if (CategoryHierarchy.ValidateDeletion(subcategories, products) is { } error) return error;

        db.Categories.Remove(category);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    /// <summary>Nombre único (sin distinguir mayúsculas) y jerarquía válida.</summary>
    private async Task<Error?> ValidateAsync(int? categoryId, string name, int? parentId, CancellationToken ct)
    {
        var lower = name.ToLowerInvariant();
        if (await db.Categories.AnyAsync(c => c.Name.ToLower() == lower && c.Id != categoryId, ct))
            return Error.Conflict($"Ya existe una categoría llamada \"{name}\".");

        if (parentId is null) return null;
        var parent = parentId == categoryId
            ? null
            : await db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == parentId, ct);
        var hasChildren = categoryId is not null && await db.Categories.AnyAsync(c => c.ParentId == categoryId, ct);
        return CategoryHierarchy.ValidateParent(categoryId, parentId, parent, hasChildren);
    }
}
