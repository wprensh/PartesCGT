namespace TiendaVirtual.Api.Infrastructure.Storage;

public enum FileStorageProvider
{
    /// <summary>Carpeta en disco. En Render se borra en cada despliegue salvo que sea un disco persistente.</summary>
    Local,
    /// <summary>Contenedor de Azure Blob Storage (privado); la API sirve los archivos.</summary>
    AzureBlob
}

public class FileStorageOptions
{
    public const string SectionName = "Storage";

    public FileStorageProvider Provider { get; set; } = FileStorageProvider.Local;

    /// <summary>Carpeta en disco (Local), relativa a la raíz del proyecto. No va al repositorio (.gitignore).</summary>
    public string RootPath { get; set; } = "App_Data/files";

    /// <summary>Ruta pública. Bajo /api para que el proxy del frontend y el despliegue la traten igual que la API.</summary>
    public string PublicPath { get; set; } = "/api/files";

    public AzureBlobOptions AzureBlob { get; set; } = new();

    public string ResolveRoot(string contentRoot) => Path.GetFullPath(Path.Combine(contentRoot, RootPath));
}

public class AzureBlobOptions
{
    /// <summary>Cadena de conexión de la cuenta de almacenamiento. Secreta: user-secrets o variable de entorno.</summary>
    public string ConnectionString { get; set; } = "";

    /// <summary>Contenedor (se crea privado si no existe). Minúsculas, números y guiones.</summary>
    public string Container { get; set; } = "product-images";
}
