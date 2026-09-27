using Microsoft.EntityFrameworkCore;

namespace TiendaVirtual.Api.Infrastructure.Persistence;

public enum DatabaseProvider
{
    /// <summary>La base de la aplicación. El esquema se versiona con migraciones (Migrations/).</summary>
    SqlServer,
    /// <summary>Solo para pruebas automáticas: sin migraciones, el esquema se crea con EnsureCreated.</summary>
    Sqlite
}

public class DatabaseOptions
{
    public const string SectionName = "Database";

    public DatabaseProvider Provider { get; set; } = DatabaseProvider.SqlServer;
}

public static class PersistenceSetup
{
    private const string ConnectionStringName = "Default";

    /// <remarks>
    /// La configuración se lee al crear el contexto (no al registrar el servicio). Así las pruebas de integración
    /// pueden cambiar la base: si se leyera aquí, tomaría la de appsettings.json antes de que se aplique su
    /// configuración y escribirían en la base real (pasó una vez; ver ApiFactory).
    /// </remarks>
    public static IServiceCollection AddPersistence(this IServiceCollection services) =>
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var provider = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()?.Provider ?? DatabaseProvider.SqlServer;
            var connectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException($"Falta ConnectionStrings:{ConnectionStringName}.");

            _ = provider switch
            {
                DatabaseProvider.SqlServer => options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()),
                DatabaseProvider.Sqlite => options.UseSqlite(connectionString),
                _ => throw new InvalidOperationException($"Proveedor de base de datos no soportado: {provider}.")
            };
        });

    /// <summary>
    /// Aplica las migraciones pendientes (SQL Server) o crea el esquema (SQLite de pruebas).
    /// Solo en desarrollo siembra reseñas de ejemplo si no hay ninguna.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (db.Database.IsSqlServer()) await db.Database.MigrateAsync();
        else await db.Database.EnsureCreatedAsync();

        if (app.Environment.IsDevelopment() && !await db.Reviews.AnyAsync())
        {
            db.Reviews.AddRange(SeedData.DemoReviews());
            await db.SaveChangesAsync();
        }
    }
}
