using Microsoft.Extensions.DependencyInjection;
using OrderClientApp.Application.Abstractions.Auth;
using OrderClientApp.Application.Abstractions.Orders;
using OrderClientApp.Infrastructure.Auth;
using OrderClientApp.Infrastructure.Orders;

namespace OrderClientApp.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("Database path is required.", nameof(databasePath));
        }

        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        services.AddSingleton(new SqliteOptions { DatabasePath = databasePath });
        services.AddSingleton<SqliteConnectionFactory>();
        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IUserRepository, SqliteUserRepository>();
        services.AddScoped<IAuthDatabaseInitializer, SqliteAuthDatabaseInitializer>();
        services.AddScoped<IOrderRepository, SqliteOrderRepository>();
        services.AddScoped<IOrderTemplateRepository, SqliteOrderTemplateRepository>();
        services.AddScoped<IOrderNumberSequenceRepository, SqliteOrderNumberSequenceRepository>();
        services.AddScoped<IOrderDatabaseInitializer, SqliteOrderDatabaseInitializer>();

        return services;
    }
}
