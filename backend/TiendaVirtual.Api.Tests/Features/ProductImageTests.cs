using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using TiendaVirtual.Api.Common.Results;
using TiendaVirtual.Api.Domain.Catalog;
using TiendaVirtual.Api.Features.Products;
using TiendaVirtual.Api.Infrastructure.Storage;
using TiendaVirtual.Api.Tests.Support;

namespace TiendaVirtual.Api.Tests.Features;

/// <summary>Cabeceras mínimas de cada formato, para pruebas.</summary>
public static class TestImages
{
    public static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D];
    public static readonly byte[] Jpg = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10];
    public static readonly byte[] Webp = [.."RIFF"u8, 0x24, 0, 0, 0, .."WEBP"u8, .."VP8 "u8];
    public static readonly byte[] Html = "<html><script>alert(1)</script>"u8.ToArray();
}

public class ImageFormatTests
{
    [Fact]
    public void Detecta_por_contenido_los_formatos_admitidos()
    {
        Assert.Equal("png", ImageFormat.DetectExtension(TestImages.Png));
        Assert.Equal("jpg", ImageFormat.DetectExtension(TestImages.Jpg));
        Assert.Equal("webp", ImageFormat.DetectExtension(TestImages.Webp));
    }

    [Fact]
    public void Rechaza_lo_que_no_es_imagen_aunque_se_llame_foto_png()
    {
        Assert.Null(ImageFormat.DetectExtension(TestImages.Html));
        Assert.Null(ImageFormat.DetectExtension([]));
    }

    [Theory]
    [InlineData("https://cdn.ejemplo.co/a.webp", true)]
    [InlineData("http://ejemplo.co/a.png", true)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("ftp://ejemplo.co/a.png", false)]
    [InlineData("/api/files/products/a.png", false)]
    public void Solo_son_externas_las_URL_http_absolutas(string url, bool expected) =>
        Assert.Equal(expected, ProductImage.IsExternalUrl(url));
}

public class ProductImageServiceTests
{
    private readonly FakeFileStorage _files = new();
    private ProductImageService Service() => new(_files);

    private Task<Result<ImageUploadDto>> Upload(byte[] bytes) => Service().UploadAsync(new MemoryStream(bytes), bytes.Length, default);

    [Fact]
    public async Task Guarda_una_imagen_valida_con_su_extension_real()
    {
        var result = await Upload(TestImages.Webp);
        Assert.EndsWith(".webp", result.Value.Url);
        Assert.Contains(result.Value.Url, _files.Stored);
    }

    [Fact]
    public async Task Rechaza_archivos_que_no_son_imagen_sin_guardar_nada()
    {
        Assert.Equal(ErrorType.Validation, (await Upload(TestImages.Html)).Error!.Type);
        Assert.Empty(_files.Stored);
    }

    [Fact]
    public async Task Rechaza_archivos_vacios_o_de_mas_de_2_MB()
    {
        Assert.Equal(ErrorType.Validation, (await Upload([])).Error!.Type);
        var big = new byte[ProductImageService.MaxBytes + 1];
        TestImages.Png.CopyTo(big, 0);
        Assert.Contains("2 MB", (await Upload(big)).Error!.Message);
    }
}

public sealed class LocalFileStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"tienda-files-{Guid.NewGuid():N}");
    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true); }

    private LocalFileStorage Storage() =>
        new(Options.Create(new FileStorageOptions { RootPath = _root }), new FakeEnvironment());

    [Fact]
    public async Task Guarda_con_nombre_aleatorio_y_luego_lo_borra()
    {
        var storage = Storage();
        var url = await storage.SaveAsync("products", "png", new MemoryStream(TestImages.Png), default);

        Assert.Matches("^/api/files/products/[a-f0-9]{32}\\.png$", url);
        Assert.True(storage.IsStored(url));
        Assert.True(storage.TryDelete(url));
        Assert.False(storage.TryDelete(url));
    }

    [Theory]
    [InlineData("/api/files/../appsettings.json")]
    [InlineData("/api/files/products/..\\..\\secreto.txt")]
    [InlineData("/api/files/products/nombre-elegido.png")]
    [InlineData("https://cdn.ejemplo.co/products/0123456789abcdef0123456789abcdef.png")]
    public void No_reconoce_ni_borra_rutas_que_no_genero(string url)
    {
        Assert.False(Storage().IsStored(url));
        Assert.False(Storage().TryDelete(url));
    }

    private sealed class FakeEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
