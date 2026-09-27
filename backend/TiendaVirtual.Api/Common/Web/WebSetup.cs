namespace TiendaVirtual.Api.Common.Web;

/// <summary>Configuración HTTP común: controladores, OpenAPI, ProblemDetails, CORS y el orden del pipeline.</summary>
public static class WebSetup
{
    private const string SpaCorsPolicy = "spa";

    public static IServiceCollection AddApiCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddOpenApi();
        services.AddProblemDetails();
        services.AddSingleton(TimeProvider.System);

        var origins = configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
        services.AddCors(o => o.AddPolicy(SpaCorsPolicy, p => p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));
        return services;
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
        return app;
    }
}
