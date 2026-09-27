using TiendaVirtual.Api.Common.Results;
using TiendaVirtual.Api.Common.Storage;
using TiendaVirtual.Api.Features.Assistant;

namespace TiendaVirtual.Api.Tests.Support;

/// <summary>Modelo de chat falso: devuelve una respuesta fija y guarda lo que recibió.</summary>
public sealed class FakeChatModel(Result<string> reply) : IChatModel
{
    public string? LastSystemPrompt { get; private set; }
    public IReadOnlyList<ChatTurn> LastConversation { get; private set; } = [];

    public Task<Result<string>> CompleteAsync(string systemPrompt, IReadOnlyList<ChatTurn> conversation, CancellationToken ct)
    {
        LastSystemPrompt = systemPrompt;
        LastConversation = conversation;
        return Task.FromResult(reply);
    }
}

/// <summary>Almacén de archivos en memoria: registra lo guardado y lo borrado.</summary>
public sealed class FakeFileStorage : IFileStorage
{
    public const string Prefix = "/api/files/";
    public HashSet<string> Stored { get; } = [];
    public List<string> Deleted { get; } = [];

    public Task<string> SaveAsync(string folder, string extension, Stream content, CancellationToken ct)
    {
        var url = $"{Prefix}{folder}/{Guid.NewGuid():N}.{extension}";
        Stored.Add(url);
        return Task.FromResult(url);
    }

    public bool IsStored(string? url) => url is not null && Stored.Contains(url);

    public bool TryDelete(string? url)
    {
        if (url is null || !Stored.Remove(url)) return false;
        Deleted.Add(url);
        return true;
    }
}

/// <summary>Reloj fijo para que las fechas sean predecibles.</summary>
public sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public static readonly FixedClock Default = new(new DateTimeOffset(2026, 9, 24, 12, 0, 0, TimeSpan.Zero));

    public override DateTimeOffset GetUtcNow() => now;
}
