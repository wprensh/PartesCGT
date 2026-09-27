using TiendaVirtual.Api.Features.Assistant;

namespace TiendaVirtual.Api.Tests.Features;

public class AssistantReplyParserTests
{
    [Fact]
    public void Lee_respuesta_y_productos()
    {
        var reply = AssistantReplyParser.Parse("""{"answer": "Te sirve el SSD", "productIds": [1, 2]}""");

        Assert.Equal("Te sirve el SSD", reply.Answer);
        Assert.Equal([1, 2], reply.ProductIds);
    }

    [Fact]
    public void Tolera_bloques_de_markdown() =>
        Assert.Equal("Hola", AssistantReplyParser.Parse("```json\n{\"answer\": \"Hola\"}\n```").Answer);

    [Fact]
    public void Si_no_es_JSON_devuelve_el_texto_sin_productos()
    {
        var reply = AssistantReplyParser.Parse("  Lo siento, no entendí.  ");

        Assert.Equal("Lo siento, no entendí.", reply.Answer);
        Assert.Empty(reply.ProductIds);
    }

    [Fact]
    public void Descarta_ids_no_numericos_repetidos_y_limita_la_cantidad()
    {
        var reply = AssistantReplyParser.Parse("""{"answer": "x", "productIds": [1, "2", 1, 3.5, 4, 5, 6, 7]}""");

        Assert.Equal([1, 4, 5, 6], reply.ProductIds);
    }

    [Fact]
    public void ProductIds_que_no_es_lista_se_ignora() =>
        Assert.Empty(AssistantReplyParser.Parse("""{"answer": "x", "productIds": 3}""").ProductIds);
}
