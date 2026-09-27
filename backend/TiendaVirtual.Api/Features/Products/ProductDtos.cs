using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using TiendaVirtual.Api.Domain.Catalog;

namespace TiendaVirtual.Api.Features.Products;

public record ProductAttributeDto(
    [Required, StringLength(60, MinimumLength = 1)] string Name,
    [Required, StringLength(80, MinimumLength = 1)] string Value);

/// <remarks>Rating es el promedio de reseñas (null si no tiene).</remarks>
public record ProductDto(
    int Id, string Name, string Description, string Brand, string? ImageUrl,
    int CategoryId, string Category, int? ParentCategoryId, string? ParentCategory,
    decimal Price, int Stock, bool IsActive,
    List<ProductAttributeDto> Attributes, double? Rating, int ReviewCount);

public record ProductUpsert(
    [Required, StringLength(150, MinimumLength = 3)] string Name,
    [Required, StringLength(1000, MinimumLength = 10)] string Description,
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una categoría.")] int CategoryId,
    [Range(typeof(decimal), "0", "100000000")] decimal Price,
    [Range(0, 100000)] int Stock,
    bool IsActive = true,
    [StringLength(60)] string? Brand = null,
    [MaxLength(Product.MaxAttributes)] List<ProductAttributeDto>? Attributes = null,
    [StringLength(ProductImage.MaxUrlLength)] string? ImageUrl = null)
{
    public ProductDetails ToDetails() => new(
        Name, Description, Brand, CategoryId, Price, Stock, IsActive,
        (Attributes ?? []).Select(a => (a.Name, a.Value)), ImageUrl);
}

/// <summary>Resultado de subir una imagen: la URL que se guarda luego en ProductUpsert.ImageUrl.</summary>
public record ImageUploadDto(string Url);

/// <summary>Filtros del listado. Una categoría padre incluye los productos de sus subcategorías.</summary>
public record ProductFilter(int? CategoryId, string? Search);

public static class ProductMapping
{
    // Expresión traducible por EF: proyecta directo a SQL, sin cargar entidades.
    public static readonly Expression<Func<Product, ProductDto>> ToDto = p =>
        new ProductDto(p.Id, p.Name, p.Description, p.Brand, p.ImageUrl,
            p.CategoryId, p.Category!.Name, p.Category.ParentId, p.Category.Parent!.Name,
            p.Price, p.Stock, p.IsActive,
            p.Attributes.OrderBy(a => a.Id).Select(a => new ProductAttributeDto(a.Name, a.Value)).ToList(),
            p.Reviews.Average(r => (double?)r.Rating), p.Reviews.Count);
}
