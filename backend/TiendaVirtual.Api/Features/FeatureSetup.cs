using TiendaVirtual.Api.Features.Categories;
using TiendaVirtual.Api.Features.Orders;
using TiendaVirtual.Api.Features.Products;
using TiendaVirtual.Api.Features.Reviews;
using TiendaVirtual.Api.Features.Roles;
using TiendaVirtual.Api.Features.Suppliers;
using TiendaVirtual.Api.Features.Users;

namespace TiendaVirtual.Api.Features;

/// <summary>Servicios de las features sin configuración propia. Assistant y Auth tienen su propio *Setup.</summary>
public static class FeatureSetup
{
    public static IServiceCollection AddCatalogFeatures(this IServiceCollection services) => services
        .AddScoped<ProductQueries>()
        .AddScoped<ProductService>()
        .AddScoped<ProductImageService>()
        .AddScoped<ReviewService>()
        .AddScoped<CategoryService>()
        .AddScoped<SupplierService>();

    public static IServiceCollection AddOrderFeatures(this IServiceCollection services) => services
        .AddScoped<OrderService>();

    public static IServiceCollection AddAccessFeatures(this IServiceCollection services) => services
        .AddScoped<UserService>()
        .AddScoped<RoleService>();
}
