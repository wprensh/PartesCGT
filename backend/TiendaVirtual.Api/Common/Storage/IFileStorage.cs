namespace TiendaVirtual.Api.Common.Storage;

/// <summary>
/// Almacén de archivos públicos (imágenes de productos). Hoy en disco (LocalFileStorage);
/// para Azure Blob o S3 basta otra implementación, sin tocar a quien lo usa.
/// </summary>
public interface IFileStorage
{
    /// <summary>Guarda el contenido con un nombre aleatorio y devuelve su URL pública relativa.</summary>
    /// <param name="folder">Carpeta lógica en minúsculas, p. ej. "products".</param>
    /// <param name="extension">Extensión sin punto, p. ej. "webp".</param>
    Task<string> SaveAsync(string folder, string extension, Stream content, CancellationToken ct);

    /// <summary>Si la URL corresponde a un archivo guardado por este almacén.</summary>
    bool IsStored(string? url);

    /// <summary>Borra el archivo si la URL es de este almacén y existe. Las URL externas se ignoran.</summary>
    bool TryDelete(string? url);
}
