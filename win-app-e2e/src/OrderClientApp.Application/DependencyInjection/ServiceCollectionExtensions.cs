using Microsoft.Extensions.DependencyInjection;
using OrderClientApp.Application.Abstractions.Analytics;
using OrderClientApp.Application.Abstractions.Auth;
using OrderClientApp.Application.Abstractions.Orders;
using OrderClientApp.Application.Abstractions.Products;
using OrderClientApp.Application.Abstractions.Suppliers;
using OrderClientApp.Application.Services.Analytics;
using OrderClientApp.Application.Services.Auth;
using OrderClientApp.Application.Services.Orders;
using OrderClientApp.Application.Services.Products;
using OrderClientApp.Application.Services.Suppliers;

namespace OrderClientApp.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        return services;
    }
}
