namespace TiendaVirtual.Api.Domain.Access;

/// <summary>
/// Catálogo de permisos del panel. Es fijo (lo define el código); los roles eligen cuáles tienen.
/// Regla: "gestionar" un módulo incluye "ver" ese módulo.
/// </summary>
public static class Permissions
{
    public const string CategoriesView = "categories.view";
    public const string CategoriesManage = "categories.manage";
    public const string ProductsView = "products.view";
    public const string ProductsManage = "products.manage";
    public const string SuppliersView = "suppliers.view";
    public const string SuppliersManage = "suppliers.manage";
    public const string UsersManage = "users.manage";

    public sealed record Definition(string Code, string Module, string Label, string? Implies = null);

    public static readonly IReadOnlyList<Definition> All =
    [
        new(CategoriesView, "Categorías", "Ver"),
        new(CategoriesManage, "Categorías", "Crear, editar y eliminar", Implies: CategoriesView),
        new(ProductsView, "Productos", "Ver (incluye ocultos)"),
        new(ProductsManage, "Productos", "Crear, editar y eliminar", Implies: ProductsView),
        new(SuppliersView, "Proveedores", "Ver (incluye costos)"),
        new(SuppliersManage, "Proveedores", "Crear, editar, eliminar y asignar a productos", Implies: SuppliersView),
        new(UsersManage, "Usuarios y roles", "Administrar usuarios, roles y permisos")
    ];

    private static readonly Dictionary<string, Definition> ByCode = All.ToDictionary(p => p.Code);

    public static bool Exists(string code) => ByCode.ContainsKey(code);

    /// <summary>Agrega los permisos implícitos (gestionar ⇒ ver) y quita repetidos.</summary>
    public static IReadOnlySet<string> Expand(IEnumerable<string> codes)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var code in codes)
        {
            if (!ByCode.TryGetValue(code, out var definition)) continue;
            result.Add(code);
            if (definition.Implies is { } implied) result.Add(implied);
        }
        return result;
    }
}
