using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Api.Infrastructure.Persistence;

namespace TiendaVirtual.Api.Tests.Architecture;

public class MigrationTests
{
    [Fact]
    public void Las_migraciones_estan_al_dia_con_el_modelo()
    {
        // No se conecta: solo compara el modelo actual con el último snapshot de Migrations/.
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=sin-conexion;Database=sin-conexion").Options);

        Assert.False(db.Database.HasPendingModelChanges(),
            "Cambiaste el modelo sin crear la migración. Ejecuta en backend/TiendaVirtual.Api: " +
            "dotnet tool run dotnet-ef migrations add <NombreDelCambio> -o Infrastructure/Persistence/Migrations");
    }
}
