namespace OrderClientApp.Application.Abstractions.Analytics;

public interface IAnalyticsRepository
{
    Task<IReadOnlyCollection<MonthlyOrderAggregateDto>> GetMonthlyAggregatesAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ProductOrderAggregateDto>> GetProductAggregatesAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        int top,
        CancellationToken cancellationToken = default);
}
