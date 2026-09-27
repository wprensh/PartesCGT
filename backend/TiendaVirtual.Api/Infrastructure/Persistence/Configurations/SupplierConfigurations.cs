using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TiendaVirtual.Api.Domain.Suppliers;

namespace TiendaVirtual.Api.Infrastructure.Persistence.Configurations;

internal sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> e)
    {
        e.Property(s => s.Name).HasMaxLength(150);
        e.Property(s => s.TaxId).HasMaxLength(20);
        e.HasIndex(s => s.TaxId).IsUnique();
        e.Property(s => s.ContactName).HasMaxLength(120);
        e.Property(s => s.Email).HasMaxLength(254);
        e.Property(s => s.Phone).HasMaxLength(40);
        e.Property(s => s.Address).HasMaxLength(200);
        e.Property(s => s.City).HasMaxLength(80);
        e.Property(s => s.Notes).HasMaxLength(1000);
    }
}

internal sealed class ProductSupplierConfiguration : IEntityTypeConfiguration<ProductSupplier>
{
    public void Configure(EntityTypeBuilder<ProductSupplier> e)
    {
        e.HasKey(ps => new { ps.ProductId, ps.SupplierId });
        e.Property(ps => ps.Cost).HasPrecision(MoneyPrecision.Digits, MoneyPrecision.Decimals);
        e.Property(ps => ps.SupplierSku).HasMaxLength(60);
        // Al borrar un producto se van sus costos; un proveedor con productos no se borra (se desactiva).
        e.HasOne(ps => ps.Product).WithMany().HasForeignKey(ps => ps.ProductId).OnDelete(DeleteBehavior.Cascade);
        e.HasOne(ps => ps.Supplier).WithMany(s => s.Products).HasForeignKey(ps => ps.SupplierId).OnDelete(DeleteBehavior.Restrict);
    }
}
