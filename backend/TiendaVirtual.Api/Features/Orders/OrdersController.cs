using Microsoft.AspNetCore.Mvc;
using TiendaVirtual.Api.Common.Web;

namespace TiendaVirtual.Api.Features.Orders;

[Route("api/orders")]
public class OrdersController(OrderService orders) : ApiControllerBase
{
    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        var result = await orders.PlaceAsync(request, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : Failure(result.Error!);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> GetById(int id, CancellationToken ct) =>
        OkOrFailure(await orders.FindAsync(id, ct));
}
