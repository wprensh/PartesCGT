using TiendaVirtual.Api.Common.Results;

namespace TiendaVirtual.Api.Features.Assistant;

public enum ChatRole { User, Assistant }

public readonly record struct ChatTurn(ChatRole Role, string Content);

/// <summary>
/// Modelo de lenguaje que responde en texto. El caso de uso depende de esta abstracción y no de Claude:
/// se puede cambiar de proveedor o usar un doble en las pruebas sin tocar AssistantService.
/// </summary>
public interface IChatModel
{
    /// <returns>El texto de la respuesta, o <see cref="ErrorType.Unavailable"/> si el modelo no responde.</returns>
    Task<Result<string>> CompleteAsync(string systemPrompt, IReadOnlyList<ChatTurn> conversation, CancellationToken ct);
}
