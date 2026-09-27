using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TiendaVirtual.Api.Domain.Orders;

namespace TiendaVirtual.Api.Infrastructure.Persistence.Configurations;

/// <summary>Importes en pesos: decimal(18,2). Un solo lugar para cambiarlo.</summary>
internal static class MoneyPrecision
{
    public const int Digits = 18;
    public const int Decimals = 2;
}

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> e)
    {
        e.Property(o => o.Total).HasPrecision(MoneyPrecision.Digits, MoneyPrecision.Decimals);
        e.Property(o => o.CustomerName).HasMaxLength(120);
        e.Property(o => o.CustomerEmail).HasMaxLength(254);
    }
}

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> e)
    {
        e.Property(i => i.UnitPrice).HasPrecision(MoneyPrecision.Digits, MoneyPrecision.Decimals);
        // Mismo largo que Product.Name, del que es copia.
        e.Property(i => i.ProductName).HasMaxLength(150);
    }
}
