using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TiendaVirtual.Api.Domain.Access;
using TiendaVirtual.Api.Features.Auth;
using TiendaVirtual.Api.Features.Roles;
using TiendaVirtual.Api.Features.Users;
using TiendaVirtual.Api.Features.Categories;
using TiendaVirtual.Api.Features.Orders;
using TiendaVirtual.Api.Features.Products;
using TiendaVirtual.Api.Tests.Features;
using TiendaVirtual.Api.Tests.Support;

namespace TiendaVirtual.Api.Tests.Api;

/// <summary>La API completa por HTTP: rutas, validación, autorización y formato de errores.</summary>
public sealed class ApiIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public ApiIntegrationTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task El_catalogo_publico_responde_con_los_datos_semilla()
    {
        var client = _factory.CreateClient();

        var products = await client.GetFromJsonAsync<List<ProductDto>>("/api/products");
        var categories = await client.GetFromJsonAsync<List<CategoryDto>>("/api/categories");

        Assert.Equal(10, products!.Count);
        Assert.Equal(2, categories!.Single(c => c.Name == "Almacenamiento").ActiveProducts);
    }

    [Fact]
    public async Task Un_producto_inexistente_devuelve_404_ProblemDetails()
    {
        var response = await _factory.CreateClient().GetAsync("/api/products/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Los_errores_de_negocio_llegan_como_ProblemDetails_con_detalle()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/orders",
            new CreateOrderRequest("Ana", "ana@test.co", [new CartLine(SeedIds.Rtx4060, 50)]));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemBody>();
        Assert.StartsWith("Solo quedan", problem!.Detail);
    }

    [Fact]
    public async Task La_validacion_de_entrada_responde_400()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/products/1/reviews", new { rating = 9, author = "A" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Crear_el_primer_admin_por_API_esta_cerrado_fuera_de_Development()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/setup",
            new FirstAdminRequest("intruso@tienda.local", "Intruso", "clave-intrusa-2026"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task El_panel_exige_token_y_funciona_tras_el_login()
    {
        var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/products/admin")).StatusCode);

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(ApiFactory.AdminEmail, ApiFactory.AdminPassword));
        var token = (await login.Content.ReadFromJsonAsync<LoginResponse>())!.Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/products/admin")).StatusCode);
    }

    [Fact]
    public async Task Subir_una_imagen_la_guarda_en_el_producto_y_se_sirve_de_forma_segura()
    {
        var client = await AdminClientAsync();

        var upload = await client.PostAsync("/api/products/images", ImageForm(TestImages.Png, "foto.png"));
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        var url = (await upload.Content.ReadFromJsonAsync<ImageUploadDto>())!.Url;

        var file = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, file.StatusCode);
        Assert.Equal("image/png", file.Content.Headers.ContentType?.MediaType);
        Assert.Contains("nosniff", file.Headers.GetValues("X-Content-Type-Options"));
        Assert.Equal(TestImages.Png, await file.Content.ReadAsByteArrayAsync());

        var product = await client.GetFromJsonAsync<ProductDto>($"/api/products/{SeedIds.ThermalPaste}");
        var saved = await client.PutAsJsonAsync($"/api/products/{SeedIds.ThermalPaste}", new ProductUpsert(
            product!.Name, product.Description, product.CategoryId, product.Price, product.Stock, product.IsActive,
            product.Brand, product.Attributes, ImageUrl: url));
        Assert.Equal(url, (await saved.Content.ReadFromJsonAsync<ProductDto>())!.ImageUrl);
    }

    [Fact]
    public async Task Un_archivo_que_no_es_imagen_se_rechaza_aunque_se_llame_png()
    {
        var client = await AdminClientAsync();
        var response = await client.PostAsync("/api/products/images", ImageForm(TestImages.Html, "foto.png"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Sin_sesion_no_se_pueden_subir_imagenes() =>
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await _factory.CreateClient().PostAsync("/api/products/images", ImageForm(TestImages.Png, "foto.png"))).StatusCode);

    [Fact]
    public async Task Cada_rol_solo_puede_lo_que_permiten_sus_permisos_y_se_aplica_al_instante()
    {
        var admin = await AdminClientAsync();
        var email = $"compras-{Guid.NewGuid():N}@tienda.local";
        var created = await admin.PostAsJsonAsync("/api/users", new UserCreate(email, "Compras Prueba", RoleId: 3, "clave-compras-2026"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var buyerId = (await created.Content.ReadFromJsonAsync<UserDto>())!.Id;

        var buyer = await ClientForAsync(email, "clave-compras-2026");
        var me = await buyer.GetFromJsonAsync<MeDto>("/api/auth/me");
        Assert.Equal("Compras", me!.Role);

        Assert.Equal(HttpStatusCode.OK, (await buyer.GetAsync("/api/suppliers")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await buyer.GetAsync("/api/products/admin")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await buyer.DeleteAsync($"/api/products/{SeedIds.SataSsd}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await buyer.GetAsync("/api/users")).StatusCode);

        // El administrador lo desactiva: su token deja de servir en la siguiente petición.
        await admin.PutAsJsonAsync($"/api/users/{buyerId}", new UserUpdate("Compras Prueba", RoleId: 3, IsActive: false));
        Assert.Equal(HttpStatusCode.Unauthorized, (await buyer.GetAsync("/api/suppliers")).StatusCode);
    }

    [Fact]
    public async Task Un_cambio_de_permisos_del_rol_aplica_sin_volver_a_entrar()
    {
        var admin = await AdminClientAsync();
        var role = await (await admin.PostAsJsonAsync("/api/roles", new RoleUpsert($"Temporal {Guid.NewGuid():N}"[..20], null, []))).Content.ReadFromJsonAsync<RoleDto>();
        var email = $"temp-{Guid.NewGuid():N}@tienda.local";
        await admin.PostAsJsonAsync("/api/users", new UserCreate(email, "Temporal", role!.Id, "clave-temporal-2026"));
        var user = await ClientForAsync(email, "clave-temporal-2026");

        Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/api/suppliers")).StatusCode);
        await admin.PutAsJsonAsync($"/api/roles/{role.Id}", new RoleUpsert(role.Name, null, [Permissions.SuppliersView]));
        Assert.Equal(HttpStatusCode.OK, (await user.GetAsync("/api/suppliers")).StatusCode);
    }

    private Task<HttpClient> AdminClientAsync() => ClientForAsync(ApiFactory.AdminEmail, ApiFactory.AdminPassword);

    private async Task<HttpClient> ClientForAsync(string email, string password)
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<LoginResponse>())!.Token);
        return client;
    }

    private static MultipartFormDataContent ImageForm(byte[] bytes, string fileName) =>
        new() { { new ByteArrayContent(bytes), "file", fileName } };

    [Fact]
    public async Task Sin_API_key_el_asistente_responde_503_sin_llamar_a_Claude()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/assistant/chat",
            new { messages = new[] { new { role = "user", content = "hola" } } });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    private sealed record ProblemBody(string? Title, int Status, string? Detail);
}
