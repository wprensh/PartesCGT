using System.Net;
using System.Net.Http.Json;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using TiendaVirtual.Api.Common.Web;
using TiendaVirtual.Api.Features.Auth;
using TiendaVirtual.Api.Infrastructure.Storage;
using TiendaVirtual.Api.Tests.Support;

namespace TiendaVirtual.Api.Tests.Api;

/// <summary>Detrás del proxy de Render: login limitado a 1 por minuto y ReverseProxy:Enabled.</summary>
public sealed class BehindProxyApiFactory : ApiFactory
{
    protected override IEnumerable<KeyValuePair<string, string?>> ExtraSettings =>
        [new("ReverseProxy:Enabled", "true"), new("RateLimiting:login", "1")];
}

/// <summary>Sin proxy (valor por defecto) y login limitado a 1 por minuto.</summary>
public sealed class NoProxyApiFactory : ApiFactory
{
    protected override IEnumerable<KeyValuePair<string, string?>> ExtraSettings => [new("RateLimiting:login", "1")];
}

internal static class LoginAttempts
{
    public static async Task<HttpStatusCode> From(HttpClient client, string ip)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new LoginRequest("nadie@tienda.local", "clave-incorrecta-1"))
        };
        request.Headers.Add("X-Forwarded-For", ip);
        return (await client.SendAsync(request)).StatusCode;
    }
}

public sealed class BehindProxyTests(BehindProxyApiFactory factory) : IClassFixture<BehindProxyApiFactory>
{
    [Fact]
    public async Task El_limite_de_intentos_es_por_cliente_y_no_global()
    {
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, await LoginAttempts.From(client, "203.0.113.10"));
        Assert.Equal(HttpStatusCode.TooManyRequests, await LoginAttempts.From(client, "203.0.113.10"));
        // Otro cliente detrás del mismo proxy no queda bloqueado por el primero.
        Assert.Equal(HttpStatusCode.Unauthorized, await LoginAttempts.From(client, "203.0.113.20"));
    }

    [Fact]
    public async Task El_chequeo_de_salud_responde_sin_autenticacion()
    {
        var response = await factory.CreateClient().GetAsync(WebSetup.HealthPath);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}

public sealed class NoProxyTests(NoProxyApiFactory factory) : IClassFixture<NoProxyApiFactory>
{
    [Fact]
    public async Task Sin_proxy_la_cabecera_X_Forwarded_For_no_permite_esquivar_el_limite()
    {
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, await LoginAttempts.From(client, "198.51.100.1"));
        Assert.Equal(HttpStatusCode.TooManyRequests, await LoginAttempts.From(client, "198.51.100.2"));
    }
}

public class StoredFileUrlTests
{
    [Fact]
    public void Genera_nombres_aleatorios_con_el_formato_reconocido()
    {
        var name = StoredFileUrl.NewRelativeName("products", "webp");

        Assert.Matches("^products/[a-f0-9]{32}\\.webp$", name);
        Assert.True(StoredFileUrl.TryGetRelativeName("/api/files", StoredFileUrl.ToUrl("/api/files", name), out var back));
        Assert.Equal(name, back);
    }

    [Theory]
    [InlineData("../secretos", "png")]
    [InlineData("products", "p/ng")]
    [InlineData("Products", "png")]
    public void Rechaza_carpetas_o_extensiones_peligrosas(string folder, string extension) =>
        Assert.Throws<ArgumentException>(() => StoredFileUrl.NewRelativeName(folder, extension));
}

public class AzureBlobFileStorageTests
{
    // Construir el cliente no conecta con Azure: estas pruebas no usan la red.
    private static AzureBlobFileStorage Storage() => new(
        new BlobContainerClient(new Uri("https://cuenta-de-prueba.blob.core.windows.net/product-images")),
        Options.Create(new FileStorageOptions()));

    [Theory]
    [InlineData("/api/files/../appsettings.json")]
    [InlineData("/api/files/products/nombre-elegido.png")]
    [InlineData("https://cuenta-de-prueba.blob.core.windows.net/product-images/products/0123456789abcdef0123456789abcdef.png")]
    [InlineData(null)]
    public void No_reconoce_ni_borra_URL_que_no_genero(string? url)
    {
        Assert.False(Storage().IsStored(url));
        Assert.False(Storage().TryDelete(url));
    }

    [Fact]
    public void Reconoce_sus_propias_URL()
    {
        Assert.True(Storage().IsStored($"/api/files/products/{Guid.NewGuid():N}.jpg"));
    }

    [Fact]
    public async Task No_descarga_nombres_con_otro_formato()
    {
        Assert.Null(await Storage().OpenReadAsync("../appsettings.json", default));
    }

    [Fact]
    public void Sin_cadena_de_conexion_falla_con_un_mensaje_claro()
    {
        var options = new FileStorageOptions { Provider = FileStorageProvider.AzureBlob };
        var error = Assert.Throws<InvalidOperationException>(() => StorageSetup.CreateBlobContainer(options));
        Assert.Contains("Storage:AzureBlob:ConnectionString", error.Message);
    }
}
