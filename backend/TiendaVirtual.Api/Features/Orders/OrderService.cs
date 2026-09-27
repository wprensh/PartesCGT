using System.Data;
using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Api.Common.Results;
using TiendaVirtual.Api.Domain.Orders;
using TiendaVirtual.Api.Infrastructure.Persistence;

namespace TiendaVirtual.Api.Features.Orders;

public class OrderService(AppDbContext db, TimeProvider clock)
{
    /// <summary>
    /// Registra el pedido y descuenta el stock en una sola transacción. Las reglas viven en Order.Place.
    /// Serializable: dos pedidos simultáneos no pueden vender la misma última unidad (uno espera al otro;
    /// si SQL Server elige uno como víctima de deadlock, la estrategia de reintentos lo repite).
    /// </summary>
    public Task<Result<OrderDto>> PlaceAsync(CreateOrderRequest request, CancellationToken ct)
    {
        var lines = request.Items.Select(i => new OrderLineRequest(i.ProductId, i.Quantity)).ToList();
        var ids = lines.Select(l => l.ProductId).Distinct().ToList();

        return db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            // En un reintento se descarta lo que el intento fallido dejó en memoria (stock ya descontado).
            db.ChangeTracker.Clear();

            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var products = await db.Products.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

            var placed = Order.Place(request.CustomerName, request.CustomerEmail, lines, products, clock.GetUtcNow().UtcDateTime);
            if (!placed.IsSuccess) return (Result<OrderDto>)placed.Error!;

            db.Orders.Add(placed.Value);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return OrderMapping.From(placed.Value);
        });
    }

    public async Task<Result<OrderDto>> FindAsync(int id, CancellationToken ct) =>
        await db.Orders.AsNoTracking().Where(o => o.Id == id).Select(OrderMapping.ToDto).FirstOrDefaultAsync(ct) is { } order
            ? order
            : Error.NotFound();
}
