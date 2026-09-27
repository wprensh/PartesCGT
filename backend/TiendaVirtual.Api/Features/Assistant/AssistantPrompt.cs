using System.Text.Json;

namespace TiendaVirtual.Api.Features.Assistant;

/// <summary>Producto tal como se le muestra al modelo: solo lo necesario para recomendar.</summary>
public record CatalogEntry(
    int Id, string Name, string Brand, string Category, decimal Price, int Stock, string Description, List<string> Attributes);

/// <summary>Instrucciones del asistente. Cambiar el tono o las reglas solo toca este archivo.</summary>
public static class AssistantPrompt
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static string Build(IEnumerable<CatalogEntry> catalog) => $$"""
        Eres el asistente de compras de una tienda de partes de computador en Cartagena, Colombia.
        Responde en español, de forma breve y práctica (máximo 4 frases).
        Solo puedes recomendar productos de este catálogo (precios en COP):
        {{JsonSerializer.Serialize(catalog, Json)}}

        Reglas:
        - Si algo no está en el catálogo, dilo y sugiere la alternativa más cercana que sí esté.
        - Menciona compatibilidades relevantes (socket, tipo de RAM, potencia de fuente).
        - No inventes precios, stock ni especificaciones.
        Responde ÚNICAMENTE con JSON válido, sin markdown, con esta forma:
        {"answer": "texto para el cliente", "productIds": [1, 2]}
        """;
}
