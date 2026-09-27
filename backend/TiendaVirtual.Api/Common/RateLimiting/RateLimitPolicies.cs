using System.Threading.RateLimiting;

namespace TiendaVirtual.Api.Common.RateLimiting;

/// <summary>
/// Políticas de límite de peticiones por IP. Úsalas con [EnableRateLimiting(RateLimitPolicies.X)].
/// Los límites se pueden ajustar por configuración: RateLimiting:login = 10, RateLimiting:assistant = 20…
/// </summary>
public static class RateLimitPolicies
{
    /// <summary>Cada llamada a Claude cuesta dinero.</summary>
    public const string Assistant = "assistant";
    /// <summary>Frena ataques de fuerza bruta al login.</summary>
    public const string Login = "login";
    /// <summary>Evita que alguien inunde un producto de calificaciones.</summary>
    public const string Reviews = "reviews";

    private const string SectionName = "RateLimiting";

    private static readonly (string Name, int DefaultPermitLimit, TimeSpan Window)[] Definitions =
    [
        (Assistant, 10, TimeSpan.FromMinutes(1)),
        (Login, 5, TimeSpan.FromMinutes(1)),
        (Reviews, 3, TimeSpan.FromMinutes(10))
    ];

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var overrides = configuration.GetSection(SectionName);
        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            foreach (var (name, defaultLimit, window) in Definitions)
            {
                var limit = overrides.GetValue(name, defaultLimit);
                options.AddPolicy(name, ctx => RateLimitPartition.GetFixedWindowLimiter(ClientIp(ctx),
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = limit, Window = window }));
            }
        });
    }

    private static string ClientIp(HttpContext ctx) => ctx.Connection.RemoteIpAddress?.ToString() ?? "anon";
}
