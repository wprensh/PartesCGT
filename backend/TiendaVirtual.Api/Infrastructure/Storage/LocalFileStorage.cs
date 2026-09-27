using Microsoft.Extensions.Options;
using TiendaVirtual.Api.Common.Storage;

namespace TiendaVirtual.Api.Infrastructure.Storage;

/// <summary>Guarda archivos en disco con nombres aleatorios (ver <see cref="StoredFileUrl"/>).</summary>
public sealed class LocalFileStorage(IOptions<FileStorageOptions> options, IHostEnvironment environment) : IFileStorage
{
    private readonly FileStorageOptions _options = options.Value;
    private readonly string _root = options.Value.ResolveRoot(environment.ContentRootPath);

    public async Task<string> SaveAsync(string folder, string extension, Stream content, CancellationToken ct)
    {
        var relativeName = StoredFileUrl.NewRelativeName(folder, extension);
        var path = ToPath(relativeName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write,
            FileShare.None, bufferSize: 81920, useAsync: true);
        await content.CopyToAsync(file, ct);
        return StoredFileUrl.ToUrl(_options.PublicPath, relativeName);
    }

    public bool IsStored(string? url) => StoredFileUrl.TryGetRelativeName(_options.PublicPath, url, out _);

    public bool TryDelete(string? url)
    {
        if (!StoredFileUrl.TryGetRelativeName(_options.PublicPath, url, out var relativeName)) return false;
        var path = ToPath(relativeName);
        if (!File.Exists(path)) return false;
        File.Delete(path);
        return true;
    }

    private string ToPath(string relativeName) => Path.Combine(_root, relativeName.Replace('/', Path.DirectorySeparatorChar));
}
