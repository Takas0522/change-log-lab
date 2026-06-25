using Microsoft.Extensions.DependencyInjection;
using OrderClientApp.Application.Abstractions.Auth;
using OrderClientApp.Application.Abstractions.Backup;
using OrderClientApp.Application.Abstractions.Operations;
using OrderClientApp.Application.Abstractions.Orders;
using OrderClientApp.Application.Abstractions.Settings;
using OrderClientApp.Application.DependencyInjection;
using OrderClientApp.Infrastructure.DependencyInjection;

namespace OrderClientApp.Domain.Tests.Orders;

public sealed class OpsDistributionIntegrationTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"order-client-ops-test-{Guid.NewGuid():N}.db");
    private readonly string _backupDirectory = Path.Combine(Path.GetTempPath(), $"order-client-ops-backup-{Guid.NewGuid():N}");

    [Fact]
    public async Task AppSettings_SaveAndGet_PersistsCompanyAndThemeAndThreshold()
    {
        using var provider = await CreateProviderAsync();
        using var scope = provider.CreateScope();
        var settingsService = scope.ServiceProvider.GetRequiredService<IAppSettingsService>();

        await settingsService.SaveAsync(new SaveAppSettingsRequest("テスト株式会社", "東京都港区", 12345m, "Dark", "admin"));
        var saved = await settingsService.GetAsync();

        Assert.Equal("テスト株式会社", saved.CompanyName);
        Assert.Equal("東京都港区", saved.CompanyAddress);
        Assert.Equal(12345m, saved.ApprovalThreshold);
        Assert.Equal("Dark", saved.Theme);
    }

    [Fact]
    public async Task BackupService_CreatesBackupFile()
    {
        using var provider = await CreateProviderAsync();
        using var scope = provider.CreateScope();
        var backupService = scope.ServiceProvider.GetRequiredService<IBackupService>();
        Directory.CreateDirectory(_backupDirectory);

        var backupPath = await backupService.CreateManualBackupAsync(_backupDirectory);

        Assert.True(File.Exists(backupPath));
    }

    [Fact]
    public async Task OrderOperations_AreWrittenToOperationLogs()
    {
        using var provider = await CreateProviderAsync();
        using var scope = provider.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var logService = scope.ServiceProvider.GetRequiredService<IOperationLogService>();

        var created = await orderService.CreateAsync(new CreateOrderRequest(
            Guid.NewGuid(),
            "仕入先A",
            DateTimeOffset.UtcNow,
            null,
            "integration",
            null,
            null,
            null,
            null,
            0.1m,
            [new CreateOrderLineItemInput("P001", "商品A", 1, 200m)]));
        await orderService.SoftDeleteAsync(created.Id);

        var logs = await logService.QueryAsync(new OperationLogQuery(null, "Order", null, null, 50));
        Assert.Contains(logs, x => x.Action == "Create");
        Assert.Contains(logs, x => x.Action == "Delete");
    }

    public void Dispose()
    {
        if (File.Exists(_databasePath))
        {
            try { File.Delete(_databasePath); } catch (IOException) { }
        }

        if (Directory.Exists(_backupDirectory))
        {
            try { Directory.Delete(_backupDirectory, true); } catch (IOException) { }
        }
    }

    private async Task<ServiceProvider> CreateProviderAsync()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();
        services.AddInfrastructure(_databasePath);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var authInitializer = scope.ServiceProvider.GetRequiredService<IAuthDatabaseInitializer>();
        await authInitializer.InitializeAsync();
        var orderInitializer = scope.ServiceProvider.GetRequiredService<IOrderDatabaseInitializer>();
        await orderInitializer.InitializeAsync();
        return provider;
    }
}
