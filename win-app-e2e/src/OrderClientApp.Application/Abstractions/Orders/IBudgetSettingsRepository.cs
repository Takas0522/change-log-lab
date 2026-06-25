namespace OrderClientApp.Application.Abstractions.Orders;

public interface IBudgetSettingsRepository
{
    Task<BudgetSettingsDto> GetAsync(CancellationToken cancellationToken = default);

    Task<BudgetSettingsDto> UpsertAsync(
        decimal approvalThreshold,
        decimal? monthlyLimit,
        decimal? yearlyLimit,
        DateTimeOffset updatedAtUtc,
        CancellationToken cancellationToken = default);
}
