namespace TiendaVirtual.Api.Domain.Catalog;

/// <summary>Datos editables de un producto, ya validados en su forma (longitudes, rangos).</summary>
public sealed record ProductDetails(
    string Name,
    string Description,
    string? Brand,
    int CategoryId,
    decimal Price,
    int Stock,
    bool IsActive,
    IEnumerable<(string Name, string Value)> Attributes,
    string? ImageUrl = null);

/// <summary>Reglas de la imagen de un producto.</summary>
public static class ProductImage
{
    public const int MaxUrlLength = 500;

    /// <summary>Una URL absoluta http(s). Nada de javascript:, data: ni rutas relativas arbitrarias.</summary>
    public static bool IsExternalUrl(string url) =>
        Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
}

public class Product
{
    public const int MaxAttributes = 20;

    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    /// <summary>Vacía si no aplica (productos genéricos).</summary>
    public string Brand { get; set; } = "";
    /// <summary>URL de la foto: subida desde el panel (/api/files/...) o externa http(s). Null si no tiene.</summary>
    public string? ImageUrl { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    /// <summary>Los productos inactivos no aparecen en la tienda ni en el asistente.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>Datos filtrables: capacidad, formato, socket, peso…</summary>
    public List<ProductAttribute> Attributes { get; set; } = [];
    public List<Review> Reviews { get; set; } = [];

    public static Product Create(ProductDetails details)
    {
        var product = new Product { Name = details.Name, Description = details.Description };
        product.Update(details);
        return product;
    }

    public void Update(ProductDetails details)
    {
        Name = details.Name.Trim();
        Description = details.Description.Trim();
        Brand = details.Brand?.Trim() ?? "";
        ImageUrl = string.IsNullOrWhiteSpace(details.ImageUrl) ? null : details.ImageUrl.Trim();
        CategoryId = details.CategoryId;
        Price = details.Price;
        Stock = details.Stock;
        IsActive = details.IsActive;
        ReplaceAttributes(details.Attributes);
    }

    /// <summary>
    /// Reemplaza la lista completa (EF borra los que ya no vienen). Ignora los vacíos y los repetidos
    /// sin distinguir mayúsculas, para que el filtro de la tienda no muestre "16 GB" dos veces.
    /// </summary>
    public void ReplaceAttributes(IEnumerable<(string Name, string Value)> attributes)
    {
        Attributes.Clear();
        Attributes.AddRange(attributes
            .Select(a => (Name: a.Name.Trim(), Value: a.Value.Trim()))
            .Where(a => a.Name.Length > 0 && a.Value.Length > 0)
            .DistinctBy(a => (a.Name.ToLowerInvariant(), a.Value.ToLowerInvariant()))
            .Select(a => new ProductAttribute { Name = a.Name, Value = a.Value }));
    }

    public bool CanSell(int quantity) => IsActive && quantity > 0 && Stock >= quantity;

    public void RemoveStock(int quantity)
    {
        if (!CanSell(quantity))
            throw new InvalidOperationException($"No se pueden vender {quantity} unidades de {Name} (stock {Stock}).");
        Stock -= quantity;
    }
}

public class ProductAttribute
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public required string Name { get; set; }
    public required string Value { get; set; }
}
