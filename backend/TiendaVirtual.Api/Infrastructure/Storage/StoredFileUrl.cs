using System.Text.RegularExpressions;

namespace TiendaVirtual.Api.Infrastructure.Storage;

/// <summary>
/// Formato único de las URL de archivos guardados: {PublicPath}/{carpeta}/{guid}.{extensión}.
/// Lo comparten todos los almacenes, así que cambiar de disco a Azure Blob no cambia las URL guardadas en la base.
/// Solo se reconoce ese formato exacto: no se puede usar para leer ni borrar otros archivos.
/// </summary>
public static partial class StoredFileUrl
{
    /// <summary>Nombre relativo nuevo (carpeta/guid.extensión) para un archivo a guardar.</summary>
    public static string NewRelativeName(string folder, string extension)
    {
        if (!SafeSegment().IsMatch(folder) || !SafeSegment().IsMatch(extension))
            throw new ArgumentException("Carpeta o extensión con caracteres no permitidos.");
        return $"{folder}/{Guid.NewGuid():N}.{extension}";
    }

    public static string ToUrl(string publicPath, string relativeName) => $"{publicPath}/{relativeName}";

    /// <summary>Extrae "carpeta/guid.ext" si la URL la generó este almacén.</summary>
    public static bool TryGetRelativeName(string publicPath, string? url, out string relativeName)
    {
        relativeName = "";
        var prefix = publicPath + "/";
        if (url is null || !url.StartsWith(prefix, StringComparison.Ordinal)) return false;

        var relative = url[prefix.Length..];
        if (!IsRelativeName(relative)) return false;
        relativeName = relative;
        return true;
    }

    public static bool IsRelativeName(string relativeName) => StoredFile().IsMatch(relativeName);

    [GeneratedRegex("^[a-z0-9-]+$")]
    private static partial Regex SafeSegment();

    [GeneratedRegex("^[a-z0-9-]+/[a-f0-9]{32}\\.[a-z0-9]+$")]
    private static partial Regex StoredFile();
}
