using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TiendaVirtual.Api.Common.RateLimiting;
using TiendaVirtual.Api.Common.Results;
using TiendaVirtual.Api.Common.Web;

namespace TiendaVirtual.Api.Features.Auth;

[Route("api/auth")]
public class AuthController(AdminAuthService auth, IHostEnvironment environment) : ApiControllerBase
{
    private static readonly Error SetupNotAllowed = Error.Forbidden(
        "El primer administrador solo se puede crear en desarrollo y desde este mismo equipo (localhost). " +
        "En producción usa Admin:Email y Admin:Password en la configuración.");

    /// <summary>
    /// Crea el primer administrador si el panel no tiene usuarios. Solo en Development y desde localhost,
    /// para que en un servidor publicado nadie pueda adueñarse del panel.
    /// </summary>
    [HttpPost("setup"), EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<ActionResult<LoginResponse>> SetupFirstAdmin(FirstAdminRequest request, CancellationToken ct)
    {
        var isLocal = HttpContext.Connection.RemoteIpAddress is { } ip && IPAddress.IsLoopback(ip);
        if (!environment.IsDevelopment() || !isLocal) return Failure(SetupNotAllowed);
        return OkOrFailure(await auth.CreateFirstAdminAsync(request, ct));
    }

    [HttpPost("login"), EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct) =>
        OkOrFailure(await auth.LoginAsync(request, ct));

    /// <summary>Usuario conectado y sus permisos vigentes.</summary>
    [HttpGet("me"), Authorize]
    public async Task<ActionResult<MeDto>> Me(CancellationToken ct) =>
        OkOrFailure(await auth.GetMeAsync(CurrentUserId, ct));

    [HttpPost("change-password"), Authorize, EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<ActionResult<LoginResponse>> ChangePassword(ChangePasswordRequest request, CancellationToken ct) =>
        OkOrFailure(await auth.ChangePasswordAsync(CurrentUserId, request, ct));
}
