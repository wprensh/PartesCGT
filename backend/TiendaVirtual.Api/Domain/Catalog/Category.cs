using TiendaVirtual.Api.Common.Results;

namespace TiendaVirtual.Api.Domain.Catalog;

public class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }
    /// <summary>Si tiene valor, es una subcategoría. Solo se permite un nivel.</summary>
    public int? ParentId { get; set; }
    public Category? Parent { get; set; }
    public List<Category> Children { get; set; } = [];
    public List<Product> Products { get; set; } = [];

    public bool IsSubcategory => ParentId is not null;
}

/// <summary>Reglas de la jerarquía de categorías: un solo nivel de subcategorías.</summary>
public static class CategoryHierarchy
{
    /// <param name="categoryId">La categoría que se edita (null si se está creando).</param>
    /// <param name="parentId">El padre pedido (null = categoría principal).</param>
    /// <param name="parent">El padre cargado de la BD (null si no existe).</param>
    /// <param name="categoryHasChildren">Si la categoría que se edita ya tiene subcategorías.</param>
    public static Error? ValidateParent(int? categoryId, int? parentId, Category? parent, bool categoryHasChildren)
    {
        if (parentId is null) return null;
        if (parentId == categoryId) return Error.Validation("Una categoría no puede ser subcategoría de sí misma.");
        if (parent is null) return Error.Validation("La categoría padre no existe.");
        if (parent.IsSubcategory) return Error.Validation("Solo se permite un nivel de subcategorías.");
        if (categoryHasChildren) return Error.Validation("Esta categoría tiene subcategorías; no puede ser subcategoría de otra.");
        return null;
    }

    /// <summary>Una categoría solo se puede borrar vacía: sin subcategorías ni productos.</summary>
    public static Error? ValidateDeletion(int subcategories, int products)
    {
        if (subcategories > 0)
            return Error.Conflict($"La categoría tiene {subcategories} subcategoría(s). Elimínalas o muévelas antes.");
        if (products > 0)
            return Error.Conflict($"La categoría tiene {products} producto(s). Muévelos a otra categoría antes de eliminarla.");
        return null;
    }
}
