using OrderClientApp.Application.Abstractions.Backup;
using OrderClientApp.Infrastructure.Auth;

namespace OrderClientApp.Infrastructure.Backup;

public sealed class SqliteBackupService : IBackupService
{
    private readonly SqliteOptions _options;
    private readonly TimeProvider _timeProvider;

    public SqliteBackupService(SqliteOptions options, TimeProvider? timeProvider = null)
    {
        _options = options;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Task<string> CreateManualBackupAsync(string destinationDirectory, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(destinationDirectory))
        {
            throw new ArgumentException("Destination directory is required.", nameof(destinationDirectory));
        }

        if (!Directory.Exists(destinationDirectory))
        {
            throw new DirectoryNotFoundException(destinationDirectory);
        }

        if (!File.Exists(_options.DatabasePath))
        {
            throw new FileNotFoundException("Database file was not found.", _options.DatabasePath);
        }

        var now = _timeProvider.GetUtcNow().ToString("yyyyMMdd-HHmmss");
        var destinationPath = Path.Combine(destinationDirectory, $"order-client-backup-{now}.db");
        File.Copy(_options.DatabasePath, destinationPath, overwrite: false);
        return Task.FromResult(destinationPath);
    }
}
