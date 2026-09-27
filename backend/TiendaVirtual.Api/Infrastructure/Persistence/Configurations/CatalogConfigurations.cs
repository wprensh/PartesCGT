using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TiendaVirtual.Api.Domain.Catalog;

namespace TiendaVirtual.Api.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> e)
    {
        e.Property(c => c.Name).HasMaxLength(80);
        e.HasIndex(c => c.Name).IsUnique();
        // Una categoría con subcategorías no se puede borrar (también lo valida CategoryHierarchy).
        e.HasOne(c => c.Parent).WithMany(c => c.Children)
         .HasForeignKey(c => c.ParentId).OnDelete(DeleteBehavior.Restrict);
        e.HasData(SeedData.Categories);
    }
}

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> e)
    {
        e.Property(p => p.Name).HasMaxLength(150);
        e.Property(p => p.Description).HasMaxLength(1000);
        e.Property(p => p.Brand).HasMaxLength(60);
        e.Property(p => p.ImageUrl).HasMaxLength(ProductImage.MaxUrlLength);
        e.HasIndex(p => p.Brand);
        // Una categoría con productos no se puede borrar (también lo valida CategoryHierarchy).
        e.HasOne(p => p.Category).WithMany(c => c.Products)
         .HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
        e.Property(p => p.Price).HasPrecision(MoneyPrecision.Digits, MoneyPrecision.Decimals);
        e.HasMany(p => p.Attributes).WithOne().HasForeignKey(a => a.ProductId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(p => p.Reviews).WithOne(r => r.Product).HasForeignKey(r => r.ProductId).OnDelete(DeleteBehavior.Cascade);
        e.HasData(SeedData.Products);
    }
}

internal sealed class ProductAttributeConfiguration : IEntityTypeConfiguration<ProductAttribute>
{
    public void Configure(EntityTypeBuilder<ProductAttribute> e)
    {
        e.Property(a => a.Name).HasMaxLength(60);
        e.Property(a => a.Value).HasMaxLength(80);
        e.HasData(SeedData.Attributes);
    }
}

internal sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    // Las reseñas de ejemplo NO van en HasData (se sembrarían en todos los entornos): ver DatabaseInitializer.
    public void Configure(EntityTypeBuilder<Review> e)
    {
        e.Property(r => r.Author).HasMaxLength(80);
        e.Property(r => r.Comment).HasMaxLength(1000);
    }
}
