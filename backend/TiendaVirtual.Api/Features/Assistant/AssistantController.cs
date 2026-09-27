using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TiendaVirtual.Api.Common.RateLimiting;
using TiendaVirtual.Api.Common.Web;

namespace TiendaVirtual.Api.Features.Assistant;

[Route("api/assistant")]
[EnableRateLimiting(RateLimitPolicies.Assistant)]
public class AssistantController(AssistantService assistant) : ApiControllerBase
{
    [HttpPost("chat")]
    public async Task<ActionResult<AssistantReply>> Chat(AssistantRequest request, CancellationToken ct) =>
        OkOrFailure(await assistant.AskAsync(request.Messages, ct));
}
