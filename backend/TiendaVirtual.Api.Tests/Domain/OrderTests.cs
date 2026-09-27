using TiendaVirtual.Api.Common.Results;
using TiendaVirtual.Api.Domain.Catalog;
using TiendaVirtual.Api.Domain.Orders;

namespace TiendaVirtual.Api.Tests.Domain;

public class OrderTests
{
    private static readonly DateTime Now = new(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc);

    private static Product AProduct(int id, decimal price, int stock, bool active = true) =>
        new() { Id = id, Name = $"Producto {id}", Description = "Descripción", Price = price, Stock = stock, IsActive = active };

    private static Result<Order> Place(Dictionary<int, Product> catalog, params OrderLineRequest[] lines) =>
        Order.Place(" Ana ", " ana@test.co ", lines, catalog, Now);

    [Fact]
    public void Usa_los_precios_del_catalogo_y_suma_el_total()
    {
        var catalog = new Dictionary<int, Product> { [1] = AProduct(1, 1000, 10), [2] = AProduct(2, 250, 10) };

        var order = Place(catalog, new(1, 2), new(2, 3)).Value;

        Assert.Equal(2 * 1000 + 3 * 250, order.Total);
        Assert.Equal([1000m, 250m], order.Items.Select(i => i.UnitPrice));
        Assert.Equal("Ana", order.CustomerName);
        Assert.Equal("ana@test.co", order.CustomerEmail);
        Assert.Equal(Now, order.CreatedAt);
    }

    [Fact]
    public void Suma_las_lineas_repetidas_del_mismo_producto()
    {
        var catalog = new Dictionary<int, Product> { [1] = AProduct(1, 100, 10) };

        var order = Place(catalog, new(1, 1), new(1, 2)).Value;

        var item = Assert.Single(order.Items);
        Assert.Equal(3, item.Quantity);
        Assert.Equal(7, catalog[1].Stock);
    }

    [Fact]
    public void Rechaza_productos_inexistentes_o_inactivos()
    {
        var catalog = new Dictionary<int, Product> { [1] = AProduct(1, 100, 10, active: false) };

        Assert.Equal("El producto 1 ya no está disponible.", Place(catalog, new OrderLineRequest(1, 1)).Error!.Message);
        Assert.Equal(ErrorType.Conflict, Place(catalog, new OrderLineRequest(99, 1)).Error!.Type);
    }

    [Fact]
    public void Si_una_linea_no_tiene_stock_no_descuenta_ninguna()
    {
        var catalog = new Dictionary<int, Product> { [1] = AProduct(1, 100, 10), [2] = AProduct(2, 100, 1) };

        var result = Place(catalog, new(1, 5), new(2, 3));

        Assert.Equal("Solo quedan 1 unidades de Producto 2.", result.Error!.Message);
        Assert.Equal(10, catalog[1].Stock);
        Assert.Equal(1, catalog[2].Stock);
    }
}
