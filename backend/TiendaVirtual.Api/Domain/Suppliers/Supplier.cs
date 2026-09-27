using TiendaVirtual.Api.Common.Results;
using TiendaVirtual.Api.Domain.Catalog;

namespace TiendaVirtual.Api.Domain.Suppliers;

public sealed record SupplierDetails(
    string Name, string TaxId, string? ContactName, string? Email, string? Phone,
    string? Address, string? City, string? Notes, bool IsActive);

public class Supplier
{
    public int Id { get; set; }
    /// <summary>Razón social.</summary>
    public required string Name { get; set; }
    /// <summary>NIT o documento. Único.</summary>
    public required string TaxId { get; set; }
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Notes { get; set; }
    /// <summary>Un proveedor inactivo no se ofrece al asignar proveedores a productos.</summary>
    public bool IsActive { get; set; } = true;
    public List<ProductSupplier> Products { get; set; } = [];

    public static Supplier Create(SupplierDetails details)
    {
        var supplier = new Supplier { Name = "", TaxId = "" };
        supplier.Update(details);
        return supplier;
    }

    public void Update(SupplierDetails d)
    {
        Name = d.Name.Trim();
        TaxId = NormalizeTaxId(d.TaxId);
        ContactName = Clean(d.ContactName);
        Email = Clean(d.Email)?.ToLowerInvariant();
        Phone = Clean(d.Phone);
        Address = Clean(d.Address);
        City = Clean(d.City);
        Notes = Clean(d.Notes);
        IsActive = d.IsActive;
    }

    /// <summary>Sin espacios y en mayúsculas, para que "900.123.456-7" y "900.123.456-7 " sean el mismo NIT.</summary>
    public static string NormalizeTaxId(string taxId) => string.Concat(taxId.Where(c => !char.IsWhiteSpace(c))).ToUpperInvariant();

    public Error? ValidateDeletion(int productCount) =>
        productCount > 0
            ? Error.Conflict($"El proveedor surte {productCount} producto(s). Quítalo de esos productos o desactívalo.")
            : null;

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>Un proveedor que surte un producto, con su costo de compra y su propio código.</summary>
public class ProductSupplier
{
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public decimal Cost { get; set; }
    /// <summary>Código del producto en el catálogo del proveedor.</summary>
    public string? SupplierSku { get; set; }
    /// <summary>El proveedor al que se le compra primero. Máximo uno por producto.</summary>
    public bool IsPreferred { get; set; }
}

public readonly record struct SupplierOffer(int SupplierId, decimal Cost, string? SupplierSku, bool IsPreferred);

public static class SupplierOffers
{
    /// <summary>Reglas de la lista de proveedores de un producto.</summary>
    public static Error? Validate(IReadOnlyCollection<SupplierOffer> offers)
    {
        if (offers.GroupBy(o => o.SupplierId).Any(g => g.Count() > 1))
            return Error.Validation("Un proveedor aparece más de una vez en el mismo producto.");
        if (offers.Any(o => o.Cost < 0)) return Error.Validation("El costo no puede ser negativo.");
        if (offers.Count(o => o.IsPreferred) > 1) return Error.Validation("Solo puede haber un proveedor preferido por producto.");
        return null;
    }
}
