using TiendaVirtual.Api.Common.Results;
using TiendaVirtual.Api.Domain.Catalog;

namespace TiendaVirtual.Api.Domain.Orders;

/// <summary>Una línea del carrito tal como la pide el cliente (puede repetir productos).</summary>
public readonly record struct OrderLineRequest(int ProductId, int Quantity);

public class Order
{
    public int Id { get; set; }
    public required string CustomerName { get; set; }
    public required string CustomerEmail { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public decimal Total { get; set; }
    public List<OrderItem> Items { get; set; } = [];

    /// <summary>
    /// Arma el pedido con los precios del catálogo (nunca los del cliente) y descuenta el stock.
    /// Valida todas las líneas antes de tocar el stock: si una falla, no cambia nada.
    /// </summary>
    /// <param name="products">Productos del catálogo por Id (los que no estén se consideran no disponibles).</param>
    public static Result<Order> Place(
        string customerName, string customerEmail, IEnumerable<OrderLineRequest> lines,
        IReadOnlyDictionary<int, Product> products, DateTime createdAt)
    {
        // El mismo producto en varias líneas se suma (el orden de la primera aparición se conserva).
        var merged = lines.GroupBy(l => l.ProductId)
            .Select(g => new OrderLineRequest(g.Key, g.Sum(l => l.Quantity)))
            .ToList();

        foreach (var line in merged)
        {
            if (!products.TryGetValue(line.ProductId, out var product) || !product.IsActive)
                return Error.Conflict($"El producto {line.ProductId} ya no está disponible.");
            if (!product.CanSell(line.Quantity))
                return Error.Conflict($"Solo quedan {product.Stock} unidades de {product.Name}.");
        }

        var order = new Order { CustomerName = customerName.Trim(), CustomerEmail = customerEmail.Trim(), CreatedAt = createdAt };
        foreach (var line in merged)
        {
            var product = products[line.ProductId];
            product.RemoveStock(line.Quantity);
            order.Items.Add(new OrderItem
            {
                ProductId = product.Id, ProductName = product.Name, UnitPrice = product.Price, Quantity = line.Quantity
            });
        }
        order.Total = order.Items.Sum(i => i.Subtotal);
        return order;
    }
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    /// <summary>Copia del nombre al momento de la compra: el historial no cambia si se edita el producto.</summary>
    public required string ProductName { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }

    public decimal Subtotal => UnitPrice * Quantity;
}
