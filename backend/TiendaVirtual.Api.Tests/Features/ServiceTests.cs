using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Api.Common.Results;
using TiendaVirtual.Api.Features.Assistant;
using TiendaVirtual.Api.Features.Categories;
using TiendaVirtual.Api.Features.Orders;
using TiendaVirtual.Api.Features.Products;
using TiendaVirtual.Api.Features.Reviews;
using TiendaVirtual.Api.Tests.Support;

namespace TiendaVirtual.Api.Tests.Features;

public sealed class ProductServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();
    public void Dispose() => _database.Dispose();

    private readonly FakeFileStorage _files = new();

    private ProductService Service()
    {
        var db = _database.CreateContext();
        return new ProductService(db, new ProductQueries(db), _files);
    }

    private static ProductUpsert AnUpsert(int categoryId = SeedIds.SsdNvme, List<ProductAttributeDto>? attributes = null, string? imageUrl = null) =>
        new("Monitor 24", "Monitor IPS de 24 pulgadas", categoryId, 500_000, 3, Brand: " LG ", Attributes: attributes, ImageUrl: imageUrl);

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:image/png;base64,AAAA")]
    [InlineData("/etc/passwd")]
    [InlineData("/api/files/products/no-subida.png")]
    public async Task Rechaza_imagenes_que_no_son_http_ni_subidas(string url) =>
        Assert.Equal(ErrorType.Validation, (await Service().CreateAsync(AnUpsert(imageUrl: url), default)).Error!.Type);

    [Fact]
    public async Task Acepta_url_externa_y_la_guarda_sin_espacios()
    {
        var created = await Service().CreateAsync(AnUpsert(imageUrl: "  https://cdn.ejemplo.co/ssd.webp  "), default);
        Assert.Equal("https://cdn.ejemplo.co/ssd.webp", created.Value.ImageUrl);
    }

    [Fact]
    public async Task Al_cambiar_la_imagen_se_borra_la_subida_anterior()
    {
        var old = await _files.SaveAsync("products", "png", Stream.Null, default);
        var replacement = await _files.SaveAsync("products", "webp", Stream.Null, default);
        await Service().UpdateAsync(SeedIds.NvmeSsd, AnUpsert(imageUrl: old), default);

        var updated = await Service().UpdateAsync(SeedIds.NvmeSsd, AnUpsert(imageUrl: replacement), default);

        Assert.Equal(replacement, updated.Value.ImageUrl);
        Assert.Equal([old], _files.Deleted);
    }

    [Fact]
    public async Task No_borra_una_imagen_que_usa_otro_producto()
    {
        var shared = await _files.SaveAsync("products", "png", Stream.Null, default);
        await Service().UpdateAsync(SeedIds.NvmeSsd, AnUpsert(imageUrl: shared), default);
        await Service().UpdateAsync(SeedIds.SataSsd, AnUpsert(imageUrl: shared), default);

        await Service().UpdateAsync(SeedIds.NvmeSsd, AnUpsert(imageUrl: null), default);

        Assert.Empty(_files.Deleted);
        Assert.True(await Service().DeleteAsync(SeedIds.SataSsd, default) is { IsSuccess: true });
        Assert.Equal([shared], _files.Deleted);
    }

    [Fact]
    public async Task Crear_con_categoria_inexistente_es_error_de_validacion()
    {
        var result = await Service().CreateAsync(AnUpsert(categoryId: 999), default);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
    }

    [Fact]
    public async Task Editar_reemplaza_los_atributos_y_devuelve_la_categoria_padre()
    {
        var result = await Service().UpdateAsync(SeedIds.NvmeSsd, AnUpsert(attributes: [new("Tamaño", "24\"")]), default);

        var product = result.Value;
        Assert.Equal("LG", product.Brand);
        Assert.Equal("Almacenamiento", product.ParentCategory);
        Assert.Equal([new ProductAttributeDto("Tamaño", "24\"")], product.Attributes);
    }

    [Fact]
    public async Task Editar_un_producto_inexistente_es_NotFound() =>
        Assert.Equal(ErrorType.NotFound, (await Service().UpdateAsync(999, AnUpsert(), default)).Error!.Type);

    [Fact]
    public async Task No_se_borra_un_producto_con_pedidos()
    {
        await using (var db = _database.CreateContext())
        {
            await new OrderService(db, FixedClock.Default).PlaceAsync(
                new CreateOrderRequest("Ana", "ana@test.co", [new CartLine(SeedIds.ThermalPaste, 1)]), default);
        }

        Assert.Equal(ErrorType.Conflict, (await Service().DeleteAsync(SeedIds.ThermalPaste, default)).Error!.Type);
        Assert.True((await Service().DeleteAsync(SeedIds.SataSsd, default)).IsSuccess);
    }

    [Fact]
    public async Task Filtrar_por_categoria_padre_incluye_sus_subcategorias()
    {
        await using var db = _database.CreateContext();
        var products = await new ProductQueries(db).ListAsync(new ProductFilter(SeedIds.Storage, null), false, default);
        Assert.Equal([SeedIds.NvmeSsd, SeedIds.SataSsd], products.Select(p => p.Id).Order());
    }
}

public sealed class OrderServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();
    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Registrar_un_pedido_descuenta_el_stock_en_la_base()
    {
        await using (var db = _database.CreateContext())
        {
            var result = await new OrderService(db, FixedClock.Default).PlaceAsync(
                new CreateOrderRequest("Ana", "ana@test.co", [new CartLine(SeedIds.Rtx4060, 3)]), default);
            Assert.Equal(1_450_000m * 3, result.Value.Total);
            Assert.Equal(FixedClock.Default.GetUtcNow().UtcDateTime, result.Value.CreatedAt);
        }

        await using var check = _database.CreateContext();
        Assert.Equal(1, (await check.Products.SingleAsync(p => p.Id == SeedIds.Rtx4060)).Stock);
    }

    [Fact]
    public async Task Un_pedido_rechazado_no_cambia_el_stock()
    {
        await using (var db = _database.CreateContext())
        {
            var result = await new OrderService(db, FixedClock.Default).PlaceAsync(
                new CreateOrderRequest("Ana", "ana@test.co", [new CartLine(SeedIds.ThermalPaste, 1), new CartLine(SeedIds.Rtx4060, 5)]), default);
            Assert.Equal(ErrorType.Conflict, result.Error!.Type);
        }

        await using var check = _database.CreateContext();
        Assert.Equal(40, (await check.Products.SingleAsync(p => p.Id == SeedIds.ThermalPaste)).Stock);
    }
}

public sealed class CategoryServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();
    public void Dispose() => _database.Dispose();

    private CategoryService Service() => new(_database.CreateContext());

    [Fact]
    public async Task El_nombre_es_unico_sin_distinguir_mayusculas() =>
        Assert.Equal(ErrorType.Conflict, (await Service().CreateAsync(new CategoryUpsert(" MEMORIA "), default)).Error!.Type);

    [Fact]
    public async Task No_permite_un_tercer_nivel() =>
        Assert.Equal("Solo se permite un nivel de subcategorías.",
            (await Service().CreateAsync(new CategoryUpsert("Nivel 3", SeedIds.SsdNvme), default)).Error!.Message);

    [Fact]
    public async Task No_borra_una_categoria_con_subcategorias() =>
        Assert.Equal(ErrorType.Conflict, (await Service().DeleteAsync(SeedIds.Storage, default)).Error!.Type);
}

public sealed class ReviewServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();
    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Una_resena_nueva_usa_el_reloj_y_aparece_primera()
    {
        await using var db = _database.CreateContext();
        var service = new ReviewService(db, new ProductQueries(db), FixedClock.Default);

        var created = await service.AddAsync(SeedIds.NvmeSsd, new ReviewCreate(5, " Ana ", " Excelente "), default);
        var list = await service.ListAsync(SeedIds.NvmeSsd, default);

        Assert.Equal(new ReviewDto(created.Value.Id, 5, "Ana", "Excelente", FixedClock.Default.GetUtcNow().UtcDateTime), created.Value);
        Assert.Equal(created.Value.Id, list.Value[0].Id);
    }

    [Fact]
    public async Task No_acepta_resenas_de_productos_inexistentes()
    {
        await using var db = _database.CreateContext();
        var service = new ReviewService(db, new ProductQueries(db), FixedClock.Default);
        Assert.Equal(ErrorType.NotFound, (await service.AddAsync(999, new ReviewCreate(5, "Ana", null), default)).Error!.Type);
    }
}

public sealed class AssistantServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();
    public void Dispose() => _database.Dispose();

    private (AssistantService Service, FakeChatModel Model) Create(Result<string> reply)
    {
        var db = _database.CreateContext();
        var model = new FakeChatModel(reply);
        return (new AssistantService(db, new ProductQueries(db), model), model);
    }

    private static List<ChatMessage> Conversation(int messages) =>
        Enumerable.Range(0, messages).Select(i => new ChatMessage(i % 2 == 0 ? "user" : "assistant", $"m{i}")).ToList();

    [Fact]
    public async Task Solo_devuelve_productos_reales_con_stock()
    {
        await using (var db = _database.CreateContext())
        {
            (await db.Products.SingleAsync(p => p.Id == SeedIds.Rtx4060)).Stock = 0;
            await db.SaveChangesAsync();
        }
        var (service, _) = Create($$"""{"answer": "Mira estos", "productIds": [{{SeedIds.NvmeSsd}}, {{SeedIds.Rtx4060}}, 999]}""");

        var reply = (await service.AskAsync(Conversation(1), default)).Value;

        Assert.Equal("Mira estos", reply.Answer);
        Assert.Equal([SeedIds.NvmeSsd], reply.Products.Select(p => p.Id));
    }

    [Fact]
    public async Task Envia_al_modelo_el_catalogo_y_solo_los_ultimos_mensajes()
    {
        var (service, model) = Create("""{"answer": "ok"}""");

        await service.AskAsync(Conversation(13), default);

        Assert.Equal(AssistantService.MaxHistoryMessages, model.LastConversation.Count);
        Assert.Equal("m12", model.LastConversation[^1].Content);
        Assert.Contains("SSD NVMe 1 TB Kingston NV2", model.LastSystemPrompt);
    }

    [Fact]
    public async Task Propaga_el_error_si_el_modelo_no_esta_disponible()
    {
        var (service, _) = Create(Error.Unavailable("Sin conexión"));
        Assert.Equal(ErrorType.Unavailable, (await service.AskAsync(Conversation(1), default)).Error!.Type);
    }

    [Fact]
    public async Task El_ultimo_mensaje_debe_ser_del_usuario()
    {
        var (service, model) = Create("""{"answer": "ok"}""");
        Assert.Equal(ErrorType.Validation, (await service.AskAsync(Conversation(2), default)).Error!.Type);
        Assert.Null(model.LastSystemPrompt);
    }
}
