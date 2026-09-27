using System.Text.Json;

namespace TiendaVirtual.Api.Features.Assistant;

public record ParsedReply(string Answer, List<int> ProductIds);

/// <summary>Lee la respuesta del modelo ({"answer", "productIds"}), tolerando markdown y texto suelto.</summary>
public static class AssistantReplyParser
{
    public const int MaxRecommendedProducts = 4;

    public static ParsedReply Parse(string raw)
    {
        var clean = raw.Replace("```json", "").Replace("```", "").Trim();
        try
        {
            using var doc = JsonDocument.Parse(clean);
            var answer = doc.RootElement.GetProperty("answer").GetString() ?? "";
            var ids = doc.RootElement.TryGetProperty("productIds", out var array) && array.ValueKind == JsonValueKind.Array
                ? array.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.Number && e.TryGetInt32(out _))
                    .Select(e => e.GetInt32())
                    .Distinct()
                    .Take(MaxRecommendedProducts)
                    .ToList()
                : [];
            return new ParsedReply(answer, ids);
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            // Si el modelo no devolvió JSON, se muestra el texto tal cual y sin productos.
            return new ParsedReply(clean, []);
        }
    }
}
