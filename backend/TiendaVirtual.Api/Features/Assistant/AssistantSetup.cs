using Microsoft.Extensions.Options;
using TiendaVirtual.Api.Features.Assistant.Claude;

namespace TiendaVirtual.Api.Features.Assistant;

public static class AssistantSetup
{
    private static readonly Uri AnthropicBaseAddress = new("https://api.anthropic.com/");
    private const string AnthropicApiVersion = "2023-06-01";
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(60);

    public static IServiceCollection AddAssistant(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ClaudeOptions>(configuration.GetSection(ClaudeOptions.SectionName));

        // Para cambiar de proveedor basta con registrar otra implementación de IChatModel.
        services.AddHttpClient<IChatModel, ClaudeChatModel>((sp, http) =>
        {
            var options = sp.GetRequiredService<IOptions<ClaudeOptions>>().Value;
            http.BaseAddress = AnthropicBaseAddress;
            http.Timeout = RequestTimeout;
            http.DefaultRequestHeaders.Add("anthropic-version", AnthropicApiVersion);
            if (!string.IsNullOrWhiteSpace(options.ApiKey))
                http.DefaultRequestHeaders.Add("x-api-key", options.ApiKey);
        });

        services.AddScoped<AssistantService>();
        return services;
    }
}
