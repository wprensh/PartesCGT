using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TiendaVirtual.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Categories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CustomerEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Brand = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Stock = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductAttributes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAttributes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductAttributes_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Author = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reviews_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Name", "ParentId" },
                values: new object[,]
                {
                    { 1, "Almacenamiento", null },
                    { 2, "Memoria", null },
                    { 3, "Procesadores", null },
                    { 4, "Gráficas", null },
                    { 5, "Energía", null },
                    { 6, "Tarjetas madre", null },
                    { 7, "Accesorios", null },
                    { 8, "Periféricos", null },
                    { 9, "SSD NVMe", 1 },
                    { 10, "SSD SATA", 1 },
                    { 11, "RAM escritorio", 2 },
                    { 12, "RAM portátil", 2 }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Brand", "CategoryId", "Description", "IsActive", "Name", "Price", "Stock" },
                values: new object[,]
                {
                    { 5, "AMD", 3, "6 núcleos y 12 hilos, socket AM4. Excelente relación precio/rendimiento para gaming y oficina.", true, "Procesador AMD Ryzen 5 5600", 520000m, 6 },
                    { 6, "MSI", 4, "Juegos en 1080p con DLSS 3 y bajo consumo (115 W). Requiere fuente de 550 W.", true, "Tarjeta gráfica RTX 4060 8 GB", 1450000m, 4 },
                    { 7, "Cooler Master", 5, "Fuente ATX certificada, suficiente para equipos gamer de gama media.", true, "Fuente de poder 650 W 80+ Bronze", 275000m, 10 },
                    { 8, "ASUS", 6, "Micro-ATX con PCIe 4.0 y dos ranuras M.2. Pareja natural del Ryzen 5 5600.", true, "Board B550M AM4", 480000m, 5 },
                    { 9, "Arctic", 7, "Para mantenimiento de procesadores y tarjetas gráficas. Rinde varias aplicaciones.", true, "Pasta térmica Arctic MX-4 4 g", 32000m, 40 },
                    { 10, "Redragon", 8, "Formato TKL, switches rojos, retroiluminación. Resistente para uso diario.", true, "Teclado mecánico Redragon Kumara", 165000m, 12 }
                });

            migrationBuilder.InsertData(
                table: "ProductAttributes",
                columns: new[] { "Id", "Name", "ProductId", "Value" },
                values: new object[,]
                {
                    { 15, "Socket", 5, "AM4" },
                    { 16, "Núcleos", 5, "6" },
                    { 17, "Memoria", 6, "8 GB" },
                    { 18, "Consumo", 6, "115 W" },
                    { 19, "Potencia", 7, "650 W" },
                    { 20, "Certificación", 7, "80+ Bronze" },
                    { 21, "Formato", 7, "ATX" },
                    { 22, "Socket", 8, "AM4" },
                    { 23, "Formato", 8, "Micro-ATX" },
                    { 24, "Peso", 9, "4 g" },
                    { 25, "Formato", 10, "TKL" },
                    { 26, "Color", 10, "Negro" },
                    { 27, "Conexión", 10, "USB" }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Brand", "CategoryId", "Description", "IsActive", "Name", "Price", "Stock" },
                values: new object[,]
                {
                    { 1, "Kingston", 9, "Unidad M.2 2280 PCIe 4.0, hasta 3500 MB/s de lectura. Ideal para revivir un equipo lento.", true, "SSD NVMe 1 TB Kingston NV2", 245000m, 14 },
                    { 2, "Crucial", 10, "Disco de 2,5\" para portátiles y torres que no tienen ranura M.2.", true, "SSD SATA 480 GB Crucial BX500", 135000m, 20 },
                    { 3, "Kingston", 11, "Módulo DIMM para escritorio. Compatible con la mayoría de placas Intel y AMD recientes.", true, "Memoria RAM DDR4 16 GB 3200 MHz", 189000m, 18 },
                    { 4, "Crucial", 12, "Módulo para portátil, 3200 MHz. La mejora más barata para un portátil que se queda corto.", true, "Memoria RAM SODIMM DDR4 8 GB", 99000m, 25 }
                });

            migrationBuilder.InsertData(
                table: "ProductAttributes",
                columns: new[] { "Id", "Name", "ProductId", "Value" },
                values: new object[,]
                {
                    { 1, "Capacidad", 1, "1 TB" },
                    { 2, "Interfaz", 1, "PCIe 4.0 NVMe" },
                    { 3, "Formato", 1, "M.2 2280" },
                    { 4, "Capacidad", 2, "480 GB" },
                    { 5, "Interfaz", 2, "SATA III" },
                    { 6, "Formato", 2, "2,5\"" },
                    { 7, "Capacidad", 3, "16 GB" },
                    { 8, "Tipo", 3, "DDR4" },
                    { 9, "Velocidad", 3, "3200 MHz" },
                    { 10, "Formato", 3, "DIMM" },
                    { 11, "Capacidad", 4, "8 GB" },
                    { 12, "Tipo", 4, "DDR4" },
                    { 13, "Velocidad", 4, "3200 MHz" },
                    { 14, "Formato", 4, "SODIMM" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentId",
                table: "Categories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributes_ProductId",
                table: "ProductAttributes",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Brand",
                table: "Products",
                column: "Brand");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ProductId",
                table: "Reviews",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "ProductAttributes");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
