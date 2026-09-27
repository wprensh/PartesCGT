using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Api.Infrastructure.Persistence;

namespace TiendaVirtual.Api.Tests.Support;

/// <summary>
/// SQLite en memoria con el mismo modelo y datos semilla que la app (10 productos, 12 categorías).
/// Se usa SQLite real y no el proveedor InMemory de EF para que las consultas se traduzcan igual que en producción.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public TestDatabase()
    {
        _connection.Open();
        using var db = CreateContext();
        db.Database.EnsureCreated();
    }

    /// <summary>Un contexto nuevo por operación, como en una petición HTTP real.</summary>
    public AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}
