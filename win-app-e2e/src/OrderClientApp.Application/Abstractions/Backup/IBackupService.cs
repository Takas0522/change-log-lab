namespace OrderClientApp.Application.Abstractions.Backup;

public interface IBackupService
{
    Task<string> CreateManualBackupAsync(string destinationDirectory, CancellationToken cancellationToken = default);
}
