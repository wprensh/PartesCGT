using System.Text.Json;
using Microsoft.Extensions.Options;
using TiendaVirtual.Api.Common.Results;

namespace TiendaVirtual.Api.Features.Assistant.Claude;

public class ClaudeOptions
{
    public const string SectionName = "Claude";

    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "claude-haiku-4-5-20251001";
    public int MaxTokens { get; set; } = 1024;
}

/// <summary>
/// Implementación de <see cref="IChatModel"/> con la Messages API de Anthropic.
/// La API key vive solo en el servidor; el frontend nunca la ve.
/// </summary>
public partial class ClaudeChatModel(HttpClient http, IOptions<ClaudeOptions> options, ILogger<ClaudeChatModel> logger) : IChatModel
{
    private static readonly Error MissingApiKey = Error.Unavailable("Falta configurar Claude:ApiKey (usa dotnet user-secrets).");
    private static readonly Error NotAvailable = Error.Unavailable("El asistente no está disponible en este momento.");

    private readonly ClaudeOptions _options = options.Value;

    public async Task<Result<string>> CompleteAsync(string systemPrompt, IReadOnlyList<ChatTurn> conversation, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey)) return MissingApiKey;

        var body = new
        {
            model = _options.Model,
            max_tokens = _options.MaxTokens,
            system = systemPrompt,
            messages = conversation.Select(t => new { role = t.Role == ChatRole.Assistant ? "assistant" : "user", content = t.Content })
        };

        try
        {
            using var response = await http.PostAsJsonAsync("v1/messages", body, ct);
            if (!response.IsSuccessStatusCode)
            {
                LogApiError(logger, (int)response.StatusCode, await response.Content.ReadAsStringAsync(ct));
                return NotAvailable;
            }

            var payload = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            return ExtractText(payload);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or KeyNotFoundException or InvalidOperationException
                                   || (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            // Red caída, timeout o respuesta ilegible: el cliente ve un mensaje genérico; el detalle queda en el log.
            LogRequestFailed(logger, ex);
            return NotAvailable;
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Claude API {Status}: {Error}")]
    private static partial void LogApiError(ILogger logger, int status, string error);

    [LoggerMessage(Level = LogLevel.Error, Message = "No se pudo consultar a Claude")]
    private static partial void LogRequestFailed(ILogger logger, Exception exception);

    /// <summary>Une los bloques de texto de la respuesta (content[].type == "text").</summary>
    private static string ExtractText(JsonElement payload) =>
        string.Concat(payload.GetProperty("content").EnumerateArray()
            .Where(block => block.GetProperty("type").GetString() == "text")
            .Select(block => block.GetProperty("text").GetString()));
}
