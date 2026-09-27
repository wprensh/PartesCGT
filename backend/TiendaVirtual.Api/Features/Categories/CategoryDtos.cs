using System.ComponentModel.DataAnnotations;

namespace TiendaVirtual.Api.Features.Categories;

/// <remarks>Los conteos de una categoría padre incluyen los de sus subcategorías.</remarks>
public record CategoryDto(int Id, string Name, int? ParentId, int ActiveProducts, int TotalProducts);

public record CategoryUpsert([Required, StringLength(80, MinimumLength = 2)] string Name, int? ParentId = null);

/// <summary>Conteos directos de una categoría (sin sumar subcategorías), tal como salen de la BD.</summary>
public record CategoryCountRow(int Id, string Name, int? ParentId, int ActiveProducts, int TotalProducts);

public static class CategoryCounts
{
    /// <summary>Suma a cada categoría padre los productos de sus subcategorías (un solo nivel) y ordena por nombre.</summary>
    public static List<CategoryDto> Aggregate(IReadOnlyCollection<CategoryCountRow> rows) =>
        rows.Select(row =>
            {
                var children = rows.Where(c => c.ParentId == row.Id).ToList();
                return new CategoryDto(row.Id, row.Name, row.ParentId,
                    row.ActiveProducts + children.Sum(c => c.ActiveProducts),
                    row.TotalProducts + children.Sum(c => c.TotalProducts));
            })
            .OrderBy(c => c.Name)
            .ToList();
}
