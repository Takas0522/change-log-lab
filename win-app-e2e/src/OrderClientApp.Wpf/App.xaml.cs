using System.Windows;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using OrderClientApp.Application.DependencyInjection;
using OrderClientApp.Application.Abstractions.Auth;
using OrderClientApp.Application.Abstractions.Orders;
using OrderClientApp.Infrastructure.DependencyInjection;

namespace OrderClientApp.Wpf;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);

        _serviceProvider = services.BuildServiceProvider();

        using var scope = _serviceProvider.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<IAuthDatabaseInitializer>();
        initializer.InitializeAsync().GetAwaiter().GetResult();
        var orderInitializer = scope.ServiceProvider.GetRequiredService<IOrderDatabaseInitializer>();
        orderInitializer.InitializeAsync().GetAwaiter().GetResult();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OrderClientApp",
            "order-client.db");

        services.AddApplicationServices();
        services.AddInfrastructure(dbPath);
        services.AddTransient<MainWindow>();
    }
}
