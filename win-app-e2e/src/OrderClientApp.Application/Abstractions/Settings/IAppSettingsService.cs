namespace OrderClientApp.Application.Abstractions.Settings;

public interface IAppSettingsService
{
    Task<AppSettingsDto> GetAsync(CancellationToken cancellationToken = default);

    Task<AppSettingsDto> SaveAsync(SaveAppSettingsRequest request, CancellationToken cancellationToken = default);
}
