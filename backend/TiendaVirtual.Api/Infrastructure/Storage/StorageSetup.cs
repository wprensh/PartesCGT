using Azure.Storage.Blobs;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using TiendaVirtual.Api.Common.Storage;

namespace TiendaVirtual.Api.Infrastructure.Storage;

/// <summary>Elige el almacén según Storage:Provider (Local o AzureBlob) y publica los archivos en Storage:PublicPath.</summary>
public static class StorageSetup
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromDays(365);

    /// <remarks>
    /// El proveedor se elige al resolver el servicio, no al registrarlo: así las pruebas (WebApplicationFactory)
    /// pueden sobrescribir Storage:* y nunca terminan usando el almacén real.
    /// </remarks>
    public static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));
        services.AddSingleton<LocalFileStorage>();
        services.AddSingleton<AzureBlobFileStorage>();
        services.AddSingleton(sp => CreateBlobContainer(sp.GetRequiredService<IOptions<FileStorageOptions>>().Value));
        return services.AddSingleton<IFileStorage>(sp =>
            sp.GetRequiredService<IOptions<FileStorageOptions>>().Value.Provider == FileStorageProvider.AzureBlob
                ? sp.GetRequiredService<AzureBlobFileStorage>()
                : sp.GetRequiredService<LocalFileStorage>());
    }

    public static BlobContainerClient CreateBlobContainer(FileStorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.AzureBlob.ConnectionString))
            throw new InvalidOperationException(
                "Storage:Provider es AzureBlob pero falta Storage:AzureBlob:ConnectionString (user-secrets o variable de entorno).");
        return new BlobContainerClient(options.AzureBlob.ConnectionString, options.AzureBlob.Container);
    }

    /// <summary>Sirve los archivos guardados. Los nombres son únicos, así que se pueden cachear para siempre.</summary>
    public static WebApplication UseFileStorage(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<FileStorageOptions>>().Value;
        return options.Provider == FileStorageProvider.Local ? ServeFromDisk(app, options) : ServeFromBlob(app, options);
    }

    private static WebApplication ServeFromDisk(WebApplication app, FileStorageOptions options)
    {
        var root = options.ResolveRoot(app.Environment.ContentRootPath);
        Directory.CreateDirectory(root);

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(root),
            RequestPath = options.PublicPath,
            OnPrepareResponse = ctx => AddCacheHeaders(ctx.Context.Response)
        });
        return app;
    }

    private static WebApplication ServeFromBlob(WebApplication app, FileStorageOptions options)
    {
        // Falla al arrancar (y no en la primera subida) si la cuenta o la cadena de conexión están mal.
        app.Services.GetRequiredService<BlobContainerClient>().CreateIfNotExists();

        app.MapGet($"{options.PublicPath}/{{folder}}/{{file}}",
            async (string folder, string file, AzureBlobFileStorage storage, HttpResponse response, CancellationToken ct) =>
            {
                if (await storage.OpenReadAsync($"{folder}/{file}", ct) is not { } found) return Results.NotFound();
                AddCacheHeaders(response);
                return Results.Stream(found.Content, found.ContentType);
            });
        return app;
    }

    private static void AddCacheHeaders(HttpResponse response)
    {
        response.Headers.XContentTypeOptions = "nosniff";
        response.Headers.CacheControl = $"public, max-age={(int)CacheDuration.TotalSeconds}, immutable";
    }
}
