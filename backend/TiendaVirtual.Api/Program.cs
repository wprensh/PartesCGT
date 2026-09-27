using TiendaVirtual.Api.Common.RateLimiting;
using TiendaVirtual.Api.Common.Web;
using TiendaVirtual.Api.Features;
using TiendaVirtual.Api.Features.Assistant;
using TiendaVirtual.Api.Features.Auth;
using TiendaVirtual.Api.Infrastructure.Persistence;
using TiendaVirtual.Api.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApiCore(builder.Configuration)
    .AddPersistence()
    .AddFileStorage(builder.Configuration)
    .AddAdminAuth(builder.Configuration, builder.Environment)
    .AddApiRateLimiting(builder.Configuration)
    .AddCatalogFeatures()
    .AddOrderFeatures()
    .AddAccessFeatures()
    .AddAssistant(builder.Configuration);

var app = builder.Build();

await app.InitializeDatabaseAsync();
await app.EnsureInitialAdminAsync();
app.UseFileStorage();
app.UseApiPipeline();
app.Run();

/// <summary>Visible para las pruebas de integración (WebApplicationFactory).</summary>
public partial class Program;
