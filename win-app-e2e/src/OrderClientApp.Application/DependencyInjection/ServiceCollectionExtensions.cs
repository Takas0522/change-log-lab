using Microsoft.Extensions.DependencyInjection;
using OrderClientApp.Application.Abstractions.Auth;
using OrderClientApp.Application.Abstractions.Orders;
using OrderClientApp.Application.Services.Auth;
using OrderClientApp.Application.Services.Orders;

namespace OrderClientApp.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<IOrderService, OrderService>();
        return services;
    }
}
