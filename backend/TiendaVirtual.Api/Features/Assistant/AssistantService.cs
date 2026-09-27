using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Api.Common.Results;
using TiendaVirtual.Api.Features.Products;
using TiendaVirtual.Api.Infrastructure.Persistence;

namespace TiendaVirtual.Api.Features.Assistant;

/// <summary>
/// Caso de uso del asistente: arma el contexto con el catálogo en stock, consulta al modelo y
/// devuelve solo productos reales (valida los IDs que propone el modelo contra la BD).
/// </summary>
public class AssistantService(AppDbContext db, ProductQueries products, IChatModel model)
{
    /// <summary>Solo los últimos mensajes: controla costo y tamaño de contexto.</summary>
    public const int MaxHistoryMessages = 10;

    public async Task<Result<AssistantReply>> AskAsync(IReadOnlyList<ChatMessage> history, CancellationToken ct)
    {
        if (history.Count == 0 || history[^1].Role != "user")
            return Error.Validation("El último mensaje debe ser del usuario.");

        var prompt = AssistantPrompt.Build(await LoadCatalogAsync(ct));
        var conversation = history.TakeLast(MaxHistoryMessages)
            .Select(m => new ChatTurn(m.Role == "assistant" ? ChatRole.Assistant : ChatRole.User, m.Content))
            .ToList();

        var completion = await model.CompleteAsync(prompt, conversation, ct);
        if (!completion.IsSuccess) return completion.Error!;

        var reply = AssistantReplyParser.Parse(completion.Value);
        var recommended = await products.ListAvailableAsync(reply.ProductIds, ct);
        return new AssistantReply(reply.Answer, recommended);
    }

    private Task<List<CatalogEntry>> LoadCatalogAsync(CancellationToken ct) =>
        db.Products.AsNoTracking()
            .Where(p => p.Stock > 0 && p.IsActive)
            .Select(p => new CatalogEntry(p.Id, p.Name, p.Brand, p.Category!.Name, p.Price, p.Stock, p.Description,
                p.Attributes.Select(a => a.Name + ": " + a.Value).ToList()))
            .ToListAsync(ct);
}
