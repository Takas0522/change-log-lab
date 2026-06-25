using Microsoft.Extensions.DependencyInjection;
using OrderClientApp.Application.Abstractions.Auth;
using OrderClientApp.Application.Abstractions.Backup;
using OrderClientApp.Application.Abstractions.Operations;
using OrderClientApp.Application.Abstractions.Orders;
using OrderClientApp.Application.Abstractions.Settings;
using OrderClientApp.Application.DependencyInjection;
using OrderClientApp.Infrastructure.DependencyInjection;

namespace OrderClientApp.Domain.Tests.Ops;

public sealed class OpsSettingsIntegrationTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"order-client-ops-{Guid.NewGuid():N}.db");
    private readonly string _backupDir = Path.Combine(Path.GetTempPath(), $"order-client-ops-backup-{Guid.NewGuid():N}");

    [Fact]
    public async Task SaveSettings_AndManualBackup_WorkAsExpected()
    {
        Directory.CreateDirectory(_backupDir);
        using var provider = await BuildProviderAsync();
        using var scope = provider.CreateScope();

        var settingsService = scope.ServiceProvider.GetRequiredService<IAppSettingsService>();
        var operationLogService = scope.ServiceProvider.GetRequiredService<IOperationLogService>();
        var backupService = scope.ServiceProvider.GetRequiredService<IBackupService>();

        var saved = await settingsService.SaveAsync(new SaveAppSettingsRequest(
            "テスト商事",
            "東京都千代田区1-1-1",
            1500m,
            "Dark",
            "admin.user"));

        Assert.Equal("テスト商事", saved.CompanyName);
        Assert.Equal("Dark", saved.Theme);
        Assert.Equal(1500m, saved.ApprovalThreshold);

        var current = await settingsService.GetAsync();
        Assert.Equal("テスト商事", current.CompanyName);
        Assert.Equal("東京都千代田区1-1-1", current.CompanyAddress);
        Assert.Equal("Dark", current.Theme);
        Assert.Equal(1500m, current.ApprovalThreshold);

        var logs = await operationLogService.QueryAsync(new OperationLogQuery(
            Keyword: "設定を更新しました",
            Category: "Settings",
            FromUtc: null,
            ToUtc: null,
            Limit: 50));
        Assert.NotEmpty(logs);

        var backupPath = await backupService.CreateManualBackupAsync(_backupDir);
        Assert.True(File.Exists(backupPath));
        Assert.StartsWith(Path.Combine(_backupDir, "order-client-backup-"), backupPath, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (File.Exists(_databasePath))
        {
            try
            {
                File.Delete(_databasePath);
            }
            catch (IOException)
            {
                // ignore cleanup race.
            }
        }

        if (Directory.Exists(_backupDir))
        {
            try
            {
                Directory.Delete(_backupDir, recursive: true);
            }
            catch (IOException)
            {
                // ignore cleanup race.
            }
        }
    }

    private async Task<ServiceProvider> BuildProviderAsync()
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
