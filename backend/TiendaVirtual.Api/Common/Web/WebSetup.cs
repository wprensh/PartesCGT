using Microsoft.AspNetCore.HttpOverrides;

namespace TiendaVirtual.Api.Common.Web;

/// <summary>Configuración HTTP común: controladores, OpenAPI, ProblemDetails, CORS y el orden del pipeline.</summary>
public static class WebSetup
{
    private const string SpaCorsPolicy = "spa";
    public const string HealthPath = "/api/health";

    public static IServiceCollection AddApiCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddOpenApi();
        services.AddProblemDetails();
        services.AddHealthChecks();
        services.AddSingleton(TimeProvider.System);

        var origins = configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
        services.AddCors(o => o.AddPolicy(SpaCorsPolicy, p => p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));
        return services;
    }

    /// <summary>
    /// Detrás de un proxy (Render, Azure…) todas las peticiones llegan desde la IP del proxy. Con ReverseProxy:Enabled
    /// se toma la IP del cliente de X-Forwarded-For, para que el límite de intentos sea por cliente y no global.
    /// Solo el último salto (el que agrega el proxy): un cliente no puede falsear su IP enviando la cabecera.
    /// Debe ser el primer middleware. Apagado por defecto: sin proxy, cualquiera podría enviar esa cabecera.
    /// </summary>
    public static WebApplication UseReverseProxyHeaders(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("ReverseProxy:Enabled")) return app;

        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            // Saltos de proxy a descontar de X-Forwarded-For; 1 = el proxy que tiene delante la API.
            ForwardLimit = app.Configuration.GetValue("ReverseProxy:ForwardLimit", 1)
        };
        // Las IP de los proxies de Render no son fijas: se confía en el salto inmediato.
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
        app.UseForwardedHeaders(options);
        return app;
    }

    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment()) app.MapOpenApi();

        app.UseExceptionHandler();
        app.UseCors(SpaCorsPolicy);
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        // Solo indica que el proceso responde: no consulta la base, para no despertarla en cada chequeo.
        app.MapHealthChecks(HealthPath);
        return app;
    }
}
