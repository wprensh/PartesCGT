using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using TiendaVirtual.Api.Domain.Orders;

namespace TiendaVirtual.Api.Features.Orders;

public record CartLine([Range(1, int.MaxValue)] int ProductId, [Range(1, 99)] int Quantity);

public record CreateOrderRequest(
    [Required, StringLength(120)] string CustomerName,
    [Required, EmailAddress, StringLength(254)] string CustomerEmail,
    [Required, MinLength(1)] List<CartLine> Items);

public record OrderItemDto(int Id, int OrderId, int ProductId, string ProductName, decimal UnitPrice, int Quantity);

public record OrderDto(int Id, string CustomerName, string CustomerEmail, DateTime CreatedAt, decimal Total, List<OrderItemDto> Items);

public static class OrderMapping
{
    public static readonly Expression<Func<Order, OrderDto>> ToDto = o =>
        new OrderDto(o.Id, o.CustomerName, o.CustomerEmail, o.CreatedAt, o.Total,
            o.Items.OrderBy(i => i.Id)
                .Select(i => new OrderItemDto(i.Id, i.OrderId, i.ProductId, i.ProductName, i.UnitPrice, i.Quantity))
                .ToList());

    private static readonly Func<Order, OrderDto> Compiled = ToDto.Compile();

    public static OrderDto From(Order order) => Compiled(order);
}
