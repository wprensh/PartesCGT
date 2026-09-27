using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using TiendaVirtual.Api.Common.Storage;

namespace TiendaVirtual.Api.Infrastructure.Storage;

/// <summary>
/// Guarda archivos en un contenedor privado de Azure Blob Storage. Las URL son las mismas que en disco
/// (/api/files/...) y la API los sirve con <see cref="OpenReadAsync"/>: no hace falta acceso anónimo al contenedor.
/// </summary>
public sealed class AzureBlobFileStorage(BlobContainerClient container, IOptions<FileStorageOptions> options) : IFileStorage
{
    private static readonly FileExtensionContentTypeProvider ContentTypes = new();
    private readonly string _publicPath = options.Value.PublicPath;

    public async Task<string> SaveAsync(string folder, string extension, Stream content, CancellationToken ct)
    {
        var relativeName = StoredFileUrl.NewRelativeName(folder, extension);
        await container.GetBlobClient(relativeName).UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = ContentTypeOf(relativeName) },
            // Nunca sobrescribe: el nombre es un GUID nuevo.
            Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All }
        }, ct);
        return StoredFileUrl.ToUrl(_publicPath, relativeName);
    }

    public bool IsStored(string? url) => StoredFileUrl.TryGetRelativeName(_publicPath, url, out _);

    public bool TryDelete(string? url) =>
        StoredFileUrl.TryGetRelativeName(_publicPath, url, out var relativeName)
        && container.GetBlobClient(relativeName).DeleteIfExists().Value;

    /// <summary>Contenido y tipo del archivo, o null si el nombre no es válido o no existe.</summary>
    public async Task<(Stream Content, string ContentType)?> OpenReadAsync(string relativeName, CancellationToken ct)
    {
        if (!StoredFileUrl.IsRelativeName(relativeName)) return null;
        try
        {
            var download = await container.GetBlobClient(relativeName).DownloadStreamingAsync(cancellationToken: ct);
            return (download.Value.Content, ContentTypeOf(relativeName));
        }
        catch (RequestFailedException e) when (e.Status == StatusCodes.Status404NotFound)
        {
            return null;
        }
    }

    private static string ContentTypeOf(string name) =>
        ContentTypes.TryGetContentType(name, out var type) ? type : "application/octet-stream";
}
