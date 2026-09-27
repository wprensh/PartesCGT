namespace TiendaVirtual.Api.Tests.Support;

/// <summary>IDs de los datos semilla (Infrastructure/Persistence/SeedData.cs) que usan las pruebas.</summary>
public static class SeedIds
{
    public const int Storage = 1;          // Almacenamiento (padre de SSD NVMe y SSD SATA)
    public const int Processors = 3;       // Procesadores (sin subcategorías)
    public const int SsdNvme = 9;          // Subcategoría de Almacenamiento

    public const int NvmeSsd = 1;          // SSD NVMe 1 TB Kingston NV2 — $245.000, stock 14
    public const int SataSsd = 2;          // SSD SATA 480 GB Crucial BX500 — $135.000, stock 20
    public const int Rtx4060 = 6;          // Tarjeta gráfica RTX 4060 8 GB — $1.450.000, stock 4
    public const int ThermalPaste = 9;     // Pasta térmica Arctic MX-4 — $32.000, stock 40
}
