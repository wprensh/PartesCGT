using System.Text.RegularExpressions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using TiendaVirtual.Api.Common.Storage;

namespace TiendaVirtual.Api.Infrastructure.Storage;

public class FileStorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Carpeta en disco, relativa a la raíz del proyecto. No va al repositorio (.gitignore).</summary>
    public string RootPath { get; set; } = "App_Data/files";

    /// <summary>Ruta pública. Bajo /api para que el proxy del frontend y el despliegue la traten igual que la API.</summary>
    public string PublicPath { get; set; } = "/api/files";

    public string ResolveRoot(string contentRoot) => Path.GetFullPath(Path.Combine(contentRoot, RootPath));
}

/// <summary>
/// Guarda archivos en disco con nombres aleatorios. Solo acepta URL con el formato exacto que genera
/// (carpeta/guid.extensión), así que no se puede usar para leer ni borrar otros archivos del servidor.
/// </summary>
public sealed partial class LocalFileStorage(IOptions<FileStorageOptions> options, IHostEnvironment environment) : IFileStorage
{
    private readonly FileStorageOptions _options = options.Value;
    private readonly string _root = options.Value.ResolveRoot(environment.ContentRootPath);

    public async Task<string> SaveAsync(string folder, string extension, Stream content, CancellationToken ct)
    {
        if (!SafeSegment().IsMatch(folder) || !SafeSegment().IsMatch(extension))
            throw new ArgumentException("Carpeta o extensión con caracteres no permitidos.");

        var fileName = $"{Guid.NewGuid():N}.{extension}";
        var directory = Path.Combine(_root, folder);
        Directory.CreateDirectory(directory);

        await using var file = new FileStream(Path.Combine(directory, fileName), FileMode.CreateNew, FileAccess.Write,
            FileShare.None, bufferSize: 81920, useAsync: true);
        await content.CopyToAsync(file, ct);
        return $"{_options.PublicPath}/{folder}/{fileName}";
    }

    public bool IsStored(string? url) => TryResolvePath(url, out _);

    public bool TryDelete(string? url)
    {
        if (!TryResolvePath(url, out var path) || !File.Exists(path)) return false;
        File.Delete(path);
        return true;
    }

    private bool TryResolvePath(string? url, out string path)
    {
        path = "";
        var prefix = _options.PublicPath + "/";
        if (url is null || !url.StartsWith(prefix, StringComparison.Ordinal)) return false;

        var relative = url[prefix.Length..];
        if (!StoredFile().IsMatch(relative)) return false;

        path = Path.Combine(_root, relative.Replace('/', Path.DirectorySeparatorChar));
        return true;
    }

    [GeneratedRegex("^[a-z0-9-]+$")]
    private static partial Regex SafeSegment();

    [GeneratedRegex("^[a-z0-9-]+/[a-f0-9]{32}\\.[a-z0-9]+$")]
    private static partial Regex StoredFile();
}

public static class StorageSetup
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromDays(365);

    public static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));
        return services.AddSingleton<IFileStorage, LocalFileStorage>();
    }

    /// <summary>Sirve los archivos guardados. Los nombres son únicos, así que se pueden cachear para siempre.</summary>
    public static WebApplication UseFileStorage(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<FileStorageOptions>>().Value;
        var root = options.ResolveRoot(app.Environment.ContentRootPath);
        Directory.CreateDirectory(root);

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(root),
            RequestPath = options.PublicPath,
            OnPrepareResponse = ctx =>
            {
                ctx.Context.Response.Headers.XContentTypeOptions = "nosniff";
                ctx.Context.Response.Headers.CacheControl = $"public, max-age={(int)CacheDuration.TotalSeconds}, immutable";
            }
        });
        return app;
    }
}
