using System.ComponentModel.DataAnnotations;
using TiendaVirtual.Api.Domain.Suppliers;

namespace TiendaVirtual.Api.Features.Suppliers;

public record SupplierDto(
    int Id, string Name, string TaxId, string? ContactName, string? Email, string? Phone,
    string? Address, string? City, string? Notes, bool IsActive, int ProductCount);

public record SupplierUpsert(
    [Required, StringLength(150, MinimumLength = 2)] string Name,
    [Required, StringLength(20, MinimumLength = 5)] string TaxId,
    [StringLength(120)] string? ContactName,
    [EmailAddress, StringLength(254)] string? Email,
    [StringLength(40)] string? Phone,
    [StringLength(200)] string? Address,
    [StringLength(80)] string? City,
    [StringLength(1000)] string? Notes,
    bool IsActive = true)
{
    public SupplierDetails ToDetails() => new(Name, TaxId, ContactName, Email, Phone, Address, City, Notes, IsActive);
}

/// <summary>Un proveedor de un producto, con su costo. Solo lo ven quienes tienen suppliers.view.</summary>
public record ProductSupplierDto(int SupplierId, string SupplierName, bool SupplierIsActive, decimal Cost, string? SupplierSku, bool IsPreferred);

/// <summary>Un producto que surte el proveedor (vista desde el proveedor).</summary>
public record SuppliedProductDto(int ProductId, string ProductName, decimal Price, decimal Cost, string? SupplierSku, bool IsPreferred);

public record ProductSupplierUpsert(
    [Range(1, int.MaxValue)] int SupplierId,
    [Range(typeof(decimal), "0", "100000000")] decimal Cost,
    [StringLength(60)] string? SupplierSku,
    bool IsPreferred)
{
    public SupplierOffer ToOffer() => new(SupplierId, Cost, string.IsNullOrWhiteSpace(SupplierSku) ? null : SupplierSku.Trim(), IsPreferred);
}
