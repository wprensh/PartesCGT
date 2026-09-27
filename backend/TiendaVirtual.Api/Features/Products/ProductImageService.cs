using TiendaVirtual.Api.Common.Results;
using TiendaVirtual.Api.Common.Storage;

namespace TiendaVirtual.Api.Features.Products;

/// <summary>Detecta el formato de una imagen por sus primeros bytes (no por la extensión, que el cliente puede falsear).</summary>
public static class ImageFormat
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <returns>"jpg", "png" o "webp"; null si no es ninguno de los formatos admitidos.</returns>
    public static string? DetectExtension(ReadOnlySpan<byte> header)
    {
        if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) return "jpg";
        if (header.StartsWith(Png)) return "png";
        if (header.Length >= 12 && header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8)) return "webp";
        return null;
    }
}

/// <summary>Sube imágenes de producto al almacén de archivos.</summary>
public class ProductImageService(IFileStorage storage)
{
    public const long MaxBytes = 2 * 1024 * 1024;
    /// <summary>Límite de la petición HTTP: la imagen más el sobrecosto del multipart.</summary>
    public const long MaxRequestBytes = MaxBytes + 64 * 1024;
    private const string Folder = "products";

    public async Task<Result<ImageUploadDto>> UploadAsync(Stream content, long length, CancellationToken ct)
    {
        if (length <= 0) return Error.Validation("El archivo está vacío.");
        if (length > MaxBytes) return Error.Validation("La imagen supera los 2 MB. Redúcela o usa formato WebP.");

        // Máximo 2 MB: se lee a memoria para validar el contenido antes de escribir nada en disco.
        using var buffer = new MemoryStream(capacity: (int)length);
        await content.CopyToAsync(buffer, ct);
        if (buffer.Length > MaxBytes) return Error.Validation("La imagen supera los 2 MB. Redúcela o usa formato WebP.");

        var extension = ImageFormat.DetectExtension(buffer.GetBuffer().AsSpan(0, (int)Math.Min(buffer.Length, 16)));
        if (extension is null) return Error.Validation("Formato no admitido. Usa una imagen JPG, PNG o WebP.");

        buffer.Position = 0;
        return new ImageUploadDto(await storage.SaveAsync(Folder, extension, buffer, ct));
    }
}
