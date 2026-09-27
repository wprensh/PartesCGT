using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TiendaVirtual.Api.Infrastructure.Persistence;

namespace TiendaVirtual.Api.Tests.Support;

/// <summary>
/// La API completa en memoria (pipeline, auth, validación, rate limiting) sobre una base SQLite temporal propia.
/// No usa user-secrets, no llama a Claude y NUNCA toca la base real: el DbContext se reemplaza explícitamente
/// (ver <see cref="ApiIsolationTests"/>).
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Ajustes extra de una variante (se aplican después de los de base).</summary>
    protected virtual IEnumerable<KeyValuePair<string, string?>> ExtraSettings => [];

    public const string AdminEmail = "admin@tienda.local";
    /// <summary>Cumple la política (10+ caracteres, letras y números): el primer admin se crea con ella.</summary>
    public const string AdminPassword = "clave-de-prueba-2026";

    public string DatabasePath { get; } = Path.Combine(Path.GetTempPath(), $"tienda-test-{Guid.NewGuid():N}.db");
    private readonly string _filesPath = Path.Combine(Path.GetTempPath(), $"tienda-test-files-{Guid.NewGuid():N}");

    private string ConnectionString => $"Data Source={DatabasePath};Pooling=False";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = ConnectionString,
            ["Database:Provider"] = "Sqlite",
            // Siempre en disco temporal: nunca el Azure Blob real aunque haya variables de entorno.
            ["Storage:Provider"] = "Local",
            ["Storage:RootPath"] = _filesPath,
            ["ReverseProxy:Enabled"] = "false",
            // Las pruebas inician sesión muchas veces seguidas desde la misma "IP".
            ["RateLimiting:login"] = "1000",
            ["Admin:Email"] = AdminEmail,
            ["Admin:Password"] = AdminPassword,
            ["Jwt:Key"] = new string('k', 48),
            ["Claude:ApiKey"] = ""
        }).AddInMemoryCollection(ExtraSettings));

        // Segunda barrera: se quita la configuración de base de la app y se registra solo SQLite temporal.
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(ConnectionString));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (File.Exists(DatabasePath)) File.Delete(DatabasePath);
        if (Directory.Exists(_filesPath)) Directory.Delete(_filesPath, recursive: true);
    }
}
