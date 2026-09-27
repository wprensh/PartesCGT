using TiendaVirtual.Api.Domain.Catalog;

namespace TiendaVirtual.Api.Infrastructure.Persistence;

public static class SeedData
{
    public static readonly Category[] Categories =
    [
        new() { Id = 1, Name = "Almacenamiento" },
        new() { Id = 2, Name = "Memoria" },
        new() { Id = 3, Name = "Procesadores" },
        new() { Id = 4, Name = "Gráficas" },
        new() { Id = 5, Name = "Energía" },
        new() { Id = 6, Name = "Tarjetas madre" },
        new() { Id = 7, Name = "Accesorios" },
        new() { Id = 8, Name = "Periféricos" },
        // Subcategorías
        new() { Id = 9, Name = "SSD NVMe", ParentId = 1 },
        new() { Id = 10, Name = "SSD SATA", ParentId = 1 },
        new() { Id = 11, Name = "RAM escritorio", ParentId = 2 },
        new() { Id = 12, Name = "RAM portátil", ParentId = 2 }
    ];

    public static readonly Product[] Products =
    [
        new() { Id = 1, Name = "SSD NVMe 1 TB Kingston NV2", Brand = "Kingston", CategoryId = 9, Price = 245000, Stock = 14,
                Description = "Unidad M.2 2280 PCIe 4.0, hasta 3500 MB/s de lectura. Ideal para revivir un equipo lento." },
        new() { Id = 2, Name = "SSD SATA 480 GB Crucial BX500", Brand = "Crucial", CategoryId = 10, Price = 135000, Stock = 20,
                Description = "Disco de 2,5\" para portátiles y torres que no tienen ranura M.2." },
        new() { Id = 3, Name = "Memoria RAM DDR4 16 GB 3200 MHz", Brand = "Kingston", CategoryId = 11, Price = 189000, Stock = 18,
                Description = "Módulo DIMM para escritorio. Compatible con la mayoría de placas Intel y AMD recientes." },
        new() { Id = 4, Name = "Memoria RAM SODIMM DDR4 8 GB", Brand = "Crucial", CategoryId = 12, Price = 99000, Stock = 25,
                Description = "Módulo para portátil, 3200 MHz. La mejora más barata para un portátil que se queda corto." },
        new() { Id = 5, Name = "Procesador AMD Ryzen 5 5600", Brand = "AMD", CategoryId = 3, Price = 520000, Stock = 6,
                Description = "6 núcleos y 12 hilos, socket AM4. Excelente relación precio/rendimiento para gaming y oficina." },
        new() { Id = 6, Name = "Tarjeta gráfica RTX 4060 8 GB", Brand = "MSI", CategoryId = 4, Price = 1450000, Stock = 4,
                Description = "Juegos en 1080p con DLSS 3 y bajo consumo (115 W). Requiere fuente de 550 W." },
        new() { Id = 7, Name = "Fuente de poder 650 W 80+ Bronze", Brand = "Cooler Master", CategoryId = 5, Price = 275000, Stock = 10,
                Description = "Fuente ATX certificada, suficiente para equipos gamer de gama media." },
        new() { Id = 8, Name = "Board B550M AM4", Brand = "ASUS", CategoryId = 6, Price = 480000, Stock = 5,
                Description = "Micro-ATX con PCIe 4.0 y dos ranuras M.2. Pareja natural del Ryzen 5 5600." },
        new() { Id = 9, Name = "Pasta térmica Arctic MX-4 4 g", Brand = "Arctic", CategoryId = 7, Price = 32000, Stock = 40,
                Description = "Para mantenimiento de procesadores y tarjetas gráficas. Rinde varias aplicaciones." },
        new() { Id = 10, Name = "Teclado mecánico Redragon Kumara", Brand = "Redragon", CategoryId = 8, Price = 165000, Stock = 12,
                Description = "Formato TKL, switches rojos, retroiluminación. Resistente para uso diario." }
    ];

    private static int _attrId;
    private static ProductAttribute A(int productId, string name, string value) =>
        new() { Id = ++_attrId, ProductId = productId, Name = name, Value = value };

    public static readonly ProductAttribute[] Attributes =
    [
        A(1, "Capacidad", "1 TB"), A(1, "Interfaz", "PCIe 4.0 NVMe"), A(1, "Formato", "M.2 2280"),
        A(2, "Capacidad", "480 GB"), A(2, "Interfaz", "SATA III"), A(2, "Formato", "2,5\""),
        A(3, "Capacidad", "16 GB"), A(3, "Tipo", "DDR4"), A(3, "Velocidad", "3200 MHz"), A(3, "Formato", "DIMM"),
        A(4, "Capacidad", "8 GB"), A(4, "Tipo", "DDR4"), A(4, "Velocidad", "3200 MHz"), A(4, "Formato", "SODIMM"),
        A(5, "Socket", "AM4"), A(5, "Núcleos", "6"),
        A(6, "Memoria", "8 GB"), A(6, "Consumo", "115 W"),
        A(7, "Potencia", "650 W"), A(7, "Certificación", "80+ Bronze"), A(7, "Formato", "ATX"),
        A(8, "Socket", "AM4"), A(8, "Formato", "Micro-ATX"),
        A(9, "Peso", "4 g"),
        A(10, "Formato", "TKL"), A(10, "Color", "Negro"), A(10, "Conexión", "USB")
    ];

    /// <summary>
    /// Reseñas de EJEMPLO para probar el filtro por calificación en desarrollo.
    /// PersistenceSetup.InitializeDatabaseAsync solo las siembra con ASPNETCORE_ENVIRONMENT=Development; nunca deben llegar a producción.
    /// </summary>
    public static List<Review> DemoReviews()
    {
        (int product, int[] ratings)[] data =
        [
            (1, [5, 5, 4]), (2, [4, 4, 3]), (3, [5, 4]), (4, [4]), (5, [5, 5, 5, 4]),
            (6, [5, 4, 4]), (7, [3, 4]), (8, [4, 5]), (9, [5]), (10, [4, 3, 4])
        ];
        return data.SelectMany(d => d.ratings.Select((r, i) => new Review
        {
            ProductId = d.product, Rating = r, Author = $"Reseña de ejemplo {i + 1}",
            Comment = "Dato de ejemplo para desarrollo.", CreatedAt = DateTime.UtcNow.AddDays(-7 * (i + 1))
        })).ToList();
    }
}
