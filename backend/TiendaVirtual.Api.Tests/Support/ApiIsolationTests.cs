using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TiendaVirtual.Api.Infrastructure.Persistence;

namespace TiendaVirtual.Api.Tests.Support;

/// <summary>
/// Protege la base real: las pruebas de integración deben usar SQLite en un archivo temporal.
/// Si esta prueba falla, NO ejecutes las demás contra una base de verdad.
/// </summary>
public sealed class ApiIsolationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public void La_API_de_pruebas_usa_SQLite_temporal_y_no_la_base_real()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.True(db.Database.IsSqlite(), "Las pruebas de integración están usando otro proveedor (¿SQL Server real?).");
        Assert.Contains(factory.DatabasePath, db.Database.GetConnectionString());
    }
}
