namespace OrderClientApp.Application.Abstractions.Settings;

public interface IAppSettingsRepository
{
    Task<AppSettingsDto> GetAsync(CancellationToken cancellationToken = default);

    Task<AppSettingsDto> UpsertAsync(
        string companyName,
        string companyAddress,
        string theme,
        DateTimeOffset updatedAtUtc,
        CancellationToken cancellationToken = default);
}
