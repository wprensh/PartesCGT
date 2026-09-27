using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

// Copia los datos de la base SQLite anterior (tienda-v3.db) a SQL Server, conservando los IDs.
//
//   dotnet run -- <ruta a tienda-v3.db> "<cadena de conexión SQL Server>" [--simular]
//
// Requisitos: la base destino ya debe tener el esquema (la API aplica las migraciones al arrancar, o
// `dotnet ef database update`) y solo los datos semilla. Todo ocurre en una transacción: si algo falla,
// no queda nada a medias. Con --simular hace la copia completa, la verifica y la deshace.

if (args.Length < 2)
{
    Console.Error.WriteLine("Uso: dotnet run -- <ruta a tienda-v3.db> \"<cadena SQL Server>\" [--simular]");
    return 2;
}

var sqlitePath = Path.GetFullPath(args[0]);
var sqlServer = args[1];
var simulate = args.Contains("--simular");
if (!File.Exists(sqlitePath))
{
    Console.Error.WriteLine($"No existe {sqlitePath}");
    return 2;
}

// Orden: primero los padres, luego los hijos (llaves foráneas). Categories: principales antes que subcategorías.
Table[] tables =
[
    new("Categories", ["Id", "Name", "ParentId"], OrderBy: "ParentId IS NOT NULL, Id"),
    new("Products", ["Id", "Name", "Description", "Brand", "CategoryId", "Price", "Stock", "IsActive"], Money: ["Price"]),
    new("ProductAttributes", ["Id", "ProductId", "Name", "Value"]),
    new("Reviews", ["Id", "ProductId", "Rating", "Author", "Comment", "CreatedAt"], Dates: ["CreatedAt"]),
    new("Orders", ["Id", "CustomerName", "CustomerEmail", "CreatedAt", "Total"], Money: ["Total"], Dates: ["CreatedAt"]),
    new("OrderItems", ["Id", "OrderId", "ProductId", "ProductName", "UnitPrice", "Quantity"], Money: ["UnitPrice"])
];

await using var source = new SqliteConnection($"Data Source={sqlitePath};Mode=ReadOnly");
await using var target = new SqlConnection(sqlServer);
await source.OpenAsync();
await target.OpenAsync();
Console.WriteLine($"Origen:  {sqlitePath}");
Console.WriteLine($"Destino: {target.DataSource} / {target.Database}{(simulate ? "   (SIMULACIÓN: se deshace al final)" : "")}\n");

if (await EnsureTargetIsFreshAsync(target) is { } problem)
{
    Console.Error.WriteLine($"No se copió nada: {problem}");
    return 1;
}

await using var transaction = (SqlTransaction)await target.BeginTransactionAsync();
try
{
    // El destino solo tiene los datos semilla de la migración: se reemplazan por los de la base anterior,
    // que los incluyen (con los cambios que se hayan hecho desde el panel).
    foreach (var table in tables.Reverse())
    {
        if (table.Name == "Categories") await ExecAsync(target, transaction, "DELETE FROM [Categories] WHERE [ParentId] IS NOT NULL");
        await ExecAsync(target, transaction, $"DELETE FROM [{table.Name}]");
    }

    foreach (var table in tables) await CopyAsync(source, target, transaction, table);

    var ok = await VerifyAsync(source, target, transaction, tables);
    if (!ok || simulate)
    {
        await transaction.RollbackAsync();
        Console.WriteLine(ok ? "\nSimulación correcta. No se guardó nada." : "\nLa verificación falló. No se guardó nada.");
        return ok ? 0 : 1;
    }

    await transaction.CommitAsync();
    Console.WriteLine("\nDatos copiados y verificados.");
    return 0;
}
catch
{
    await transaction.RollbackAsync();
    throw;
}

static async Task<string?> EnsureTargetIsFreshAsync(SqlConnection target)
{
    if (await ScalarAsync<int>(target, null, "SELECT COUNT(*) FROM sys.tables WHERE name = '__EFMigrationsHistory'") == 0)
        return "la base destino no tiene el esquema. Arranca la API una vez (aplica las migraciones) o usa `dotnet ef database update`.";

    var orders = await ScalarAsync<int>(target, null, "SELECT COUNT(*) FROM [Orders]");
    var reviews = await ScalarAsync<int>(target, null, "SELECT COUNT(*) FROM [Reviews]");
    var extraProducts = await ScalarAsync<int>(target, null, "SELECT COUNT(*) FROM [Products] WHERE [Id] > 10");
    var extraCategories = await ScalarAsync<int>(target, null, "SELECT COUNT(*) FROM [Categories] WHERE [Id] > 12");
    return orders + reviews + extraProducts + extraCategories == 0
        ? null
        : $"la base destino ya tiene datos propios ({orders} pedidos, {reviews} reseñas, {extraProducts} productos y "
          + $"{extraCategories} categorías fuera de la semilla). Para no pisarlos, esta herramienta solo copia a una base recién creada.";
}

static async Task CopyAsync(SqliteConnection source, SqlConnection target, SqlTransaction transaction, Table table)
{
    var columns = string.Join(", ", table.Columns.Select(c => $"[{c}]"));
    var parameters = string.Join(", ", table.Columns.Select(c => $"@{c}"));

    await using var read = source.CreateCommand();
    read.CommandText = $"SELECT {string.Join(", ", table.Columns)} FROM \"{table.Name}\" ORDER BY {table.OrderBy}";
    await using var reader = await read.ExecuteReaderAsync();

    await ExecAsync(target, transaction, $"SET IDENTITY_INSERT [{table.Name}] ON");
    var copied = 0;
    while (await reader.ReadAsync())
    {
        await using var insert = new SqlCommand($"INSERT INTO [{table.Name}] ({columns}) VALUES ({parameters})", target, transaction);
        for (var i = 0; i < table.Columns.Length; i++)
            insert.Parameters.AddWithValue($"@{table.Columns[i]}", ReadValue(reader, i, table, table.Columns[i]));
        await insert.ExecuteNonQueryAsync();
        copied++;
    }
    await ExecAsync(target, transaction, $"SET IDENTITY_INSERT [{table.Name}] OFF");

    await ResetIdentityAsync(target, transaction, table.Name);
    Console.WriteLine($"  {table.Name,-18} {copied,5} filas");
}

/// <summary>
/// Deja el contador de Ids para que el próximo sea MAX(Id) + 1, o 1 si la tabla está vacía.
/// Ojo: en una tabla donde nunca se insertó nada, RESEED n hace que el próximo Id sea n (no n + 1),
/// así que en ese caso no se toca: SQL Server ya empieza en 1.
/// </summary>
static async Task ResetIdentityAsync(SqlConnection target, SqlTransaction transaction, string table)
{
    var maxId = await ScalarAsync<int>(target, transaction, $"SELECT ISNULL(MAX([Id]), 0) FROM [{table}]");
    var everHadRows = await ScalarAsync<int>(target, transaction,
        $"SELECT CASE WHEN last_value IS NULL THEN 0 ELSE 1 END FROM sys.identity_columns WHERE object_id = OBJECT_ID('{table}')") == 1;
    if (maxId > 0 || everHadRows)
        await ExecAsync(target, transaction, $"DBCC CHECKIDENT ('[{table}]', RESEED, {maxId}) WITH NO_INFOMSGS");
}

static object ReadValue(SqliteDataReader reader, int i, Table table, string column)
{
    if (reader.IsDBNull(i)) return DBNull.Value;
    // SQLite guardaba el dinero como REAL (double); en SQL Server es decimal(18,2).
    if (table.Money.Contains(column)) return Math.Round(Convert.ToDecimal(reader.GetDouble(i), CultureInfo.InvariantCulture), 2);
    if (table.Dates.Contains(column)) return reader.GetDateTime(i);
    return reader.GetValue(i) switch
    {
        long n when column is "IsActive" => n != 0,
        long n => checked((int)n),
        var value => value
    };
}

static async Task<bool> VerifyAsync(SqliteConnection source, SqlConnection target, SqlTransaction transaction, Table[] tables)
{
    Console.WriteLine("\nVerificación (origen = destino):");
    var checks = tables.Select(t => ($"{t.Name}: filas", $"SELECT COUNT(*) FROM {t.Name}")).Concat(
    [
        ("Products: suma de stock", "SELECT SUM(Stock) FROM Products"),
        ("Products: suma de precios", "SELECT ROUND(SUM(Price), 2) FROM Products"),
        ("Products: activos", "SELECT SUM(CASE WHEN IsActive <> 0 THEN 1 ELSE 0 END) FROM Products"),
        ("Orders: suma de totales", "SELECT ROUND(SUM(Total), 2) FROM Orders"),
        ("OrderItems: unidades vendidas", "SELECT SUM(Quantity) FROM OrderItems"),
        ("Reviews: suma de calificaciones", "SELECT SUM(Rating) FROM Reviews"),
        ("Categories: subcategorías", "SELECT COUNT(ParentId) FROM Categories")
    ]);

    var allEqual = true;
    foreach (var (label, sql) in checks)
    {
        var expected = Normalize(await new SqliteCommand(sql, source).ExecuteScalarAsync());
        var actual = Normalize(await new SqlCommand(sql, target, transaction).ExecuteScalarAsync());
        var equal = expected == actual;
        allEqual &= equal;
        Console.WriteLine($"  {(equal ? "OK " : "DIF")} {label,-32} {expected,14} {(equal ? "" : $"≠ {actual}")}");
    }

    // El próximo Id que asignará SQL Server debe ser MAX(Id) + 1 (o 1 si la tabla está vacía).
    foreach (var table in tables)
    {
        var expected = await ScalarAsync<int>(target, transaction, $"SELECT ISNULL(MAX([Id]), 0) + 1 FROM [{table.Name}]");
        var next = await ScalarAsync<int>(target, transaction,
            "SELECT CAST(CASE WHEN last_value IS NULL THEN seed_value ELSE CAST(last_value AS bigint) + CAST(increment_value AS bigint) END AS int) " +
            $"FROM sys.identity_columns WHERE object_id = OBJECT_ID('{table.Name}')");
        var equal = expected == next;
        allEqual &= equal;
        Console.WriteLine($"  {(equal ? "OK " : "DIF")} {$"{table.Name}: próximo Id",-32} {expected,14} {(equal ? "" : $"≠ {next}")}");
    }
    return allEqual;

    static string Normalize(object? value) => value is null or DBNull
        ? "0"
        : Convert.ToDecimal(value, CultureInfo.InvariantCulture).ToString("0.##", CultureInfo.InvariantCulture);
}

static async Task ExecAsync(SqlConnection connection, SqlTransaction transaction, string sql)
{
    await using var command = new SqlCommand(sql, connection, transaction);
    await command.ExecuteNonQueryAsync();
}

static async Task<T> ScalarAsync<T>(SqlConnection connection, SqlTransaction? transaction, string sql)
{
    await using var command = new SqlCommand(sql, connection, transaction);
    return (T)Convert.ChangeType((await command.ExecuteScalarAsync())!, typeof(T), CultureInfo.InvariantCulture);
}

internal sealed record Table(string Name, string[] Columns, string OrderBy = "Id", string[]? Money = null, string[]? Dates = null)
{
    public string[] Money { get; } = Money ?? [];
    public string[] Dates { get; } = Dates ?? [];
}
