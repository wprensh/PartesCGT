using TiendaVirtual.Api.Domain.Catalog;
using TiendaVirtual.Api.Features.Categories;

namespace TiendaVirtual.Api.Tests.Domain;

public class ProductTests
{
    private static Product AProduct(int stock = 5, bool active = true) =>
        new() { Name = "SSD", Description = "Disco", Stock = stock, IsActive = active };

    [Fact]
    public void ReplaceAttributes_recorta_ignora_vacios_y_quita_repetidos_sin_importar_mayusculas()
    {
        var product = AProduct();

        product.ReplaceAttributes([(" Capacidad ", " 1 TB "), ("capacidad", "1 tb"), ("Formato", "  "), ("", "M.2"), ("Formato", "M.2")]);

        Assert.Equal([("Capacidad", "1 TB"), ("Formato", "M.2")], product.Attributes.Select(a => (a.Name, a.Value)));
    }

    [Fact]
    public void Update_normaliza_textos_y_marca_vacia()
    {
        var product = Product.Create(new ProductDetails("  SSD  ", "  Disco rápido  ", null, 1, 100, 3, true, []));

        Assert.Equal("SSD", product.Name);
        Assert.Equal("Disco rápido", product.Description);
        Assert.Equal("", product.Brand);
    }

    [Theory]
    [InlineData(5, true, 5, true)]
    [InlineData(5, true, 6, false)]
    [InlineData(5, false, 1, false)]
    [InlineData(5, true, 0, false)]
    public void CanSell_exige_activo_cantidad_positiva_y_stock(int stock, bool active, int quantity, bool expected) =>
        Assert.Equal(expected, AProduct(stock, active).CanSell(quantity));

    [Fact]
    public void RemoveStock_protege_contra_stock_negativo()
    {
        var product = AProduct(stock: 2);
        Assert.Throws<InvalidOperationException>(() => product.RemoveStock(3));
        Assert.Equal(2, product.Stock);
    }
}

public class ReviewTests
{
    [Fact]
    public void Create_recorta_autor_y_convierte_comentario_vacio_en_null()
    {
        var review = Review.Create(1, 4, "  Ana  ", "   ", DateTime.UnixEpoch);

        Assert.Equal("Ana", review.Author);
        Assert.Null(review.Comment);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Create_rechaza_calificaciones_fuera_de_rango(int rating) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Review.Create(1, rating, "Ana", null, DateTime.UnixEpoch));
}

public class CategoryHierarchyTests
{
    private static readonly Category Main = new() { Id = 1, Name = "Almacenamiento" };
    private static readonly Category Sub = new() { Id = 9, Name = "SSD NVMe", ParentId = 1 };

    [Fact]
    public void Una_categoria_principal_siempre_es_valida() =>
        Assert.Null(CategoryHierarchy.ValidateParent(5, parentId: null, parent: null, categoryHasChildren: true));

    [Fact]
    public void Una_subcategoria_de_una_principal_es_valida() =>
        Assert.Null(CategoryHierarchy.ValidateParent(null, 1, Main, categoryHasChildren: false));

    [Theory]
    [InlineData(1, 1, false, "Una categoría no puede ser subcategoría de sí misma.")]
    [InlineData(5, 99, false, "La categoría padre no existe.")]
    [InlineData(5, 9, false, "Solo se permite un nivel de subcategorías.")]
    [InlineData(5, 1, true, "Esta categoría tiene subcategorías; no puede ser subcategoría de otra.")]
    public void Rechaza_jerarquias_invalidas(int categoryId, int parentId, bool hasChildren, string message)
    {
        var parent = parentId switch { 1 => Main, 9 => Sub, _ => null };
        Assert.Equal(message, CategoryHierarchy.ValidateParent(categoryId, parentId, parent, hasChildren)?.Message);
    }

    [Fact]
    public void Solo_se_borra_una_categoria_vacia()
    {
        Assert.Null(CategoryHierarchy.ValidateDeletion(0, 0));
        Assert.Contains("2 subcategoría(s)", CategoryHierarchy.ValidateDeletion(2, 5)!.Message);
        Assert.Contains("3 producto(s)", CategoryHierarchy.ValidateDeletion(0, 3)!.Message);
    }

    [Fact]
    public void Los_conteos_del_padre_suman_los_de_sus_subcategorias()
    {
        var counts = CategoryCounts.Aggregate([
            new CategoryCountRow(1, "Almacenamiento", null, ActiveProducts: 1, TotalProducts: 2),
            new CategoryCountRow(9, "SSD NVMe", 1, 3, 4),
            new CategoryCountRow(10, "SSD SATA", 1, 0, 1),
            new CategoryCountRow(2, "Accesorios", null, 5, 5)
        ]);

        Assert.Equal(["Accesorios", "Almacenamiento", "SSD NVMe", "SSD SATA"], counts.Select(c => c.Name));
        var storage = counts.Single(c => c.Id == 1);
        Assert.Equal((4, 7), (storage.ActiveProducts, storage.TotalProducts));
    }
}
