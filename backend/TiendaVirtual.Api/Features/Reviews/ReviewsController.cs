using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TiendaVirtual.Api.Common.RateLimiting;
using TiendaVirtual.Api.Common.Web;

namespace TiendaVirtual.Api.Features.Reviews;

[Route("api/products/{productId:int}/reviews")]
public class ReviewsController(ReviewService reviews) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ReviewDto>>> GetAll(int productId, CancellationToken ct) =>
        OkOrFailure(await reviews.ListAsync(productId, ct));

    [HttpPost, EnableRateLimiting(RateLimitPolicies.Reviews)]
    public async Task<ActionResult<ReviewDto>> Add(int productId, ReviewCreate request, CancellationToken ct)
    {
        var result = await reviews.AddAsync(productId, request, ct);
        return result.IsSuccess
            ? Created($"/api/products/{productId}/reviews", result.Value)
            : Failure(result.Error!);
    }
}
