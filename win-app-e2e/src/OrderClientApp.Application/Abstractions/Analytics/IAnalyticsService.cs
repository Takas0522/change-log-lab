namespace OrderClientApp.Application.Abstractions.Analytics;

public interface IAnalyticsService
{
    Task<AnalyticsDashboardDto> GetDashboardAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        int topProducts = 10,
        CancellationToken cancellationToken = default);
}
