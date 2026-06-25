using OrderClientApp.Application.Abstractions.Analytics;

namespace OrderClientApp.Application.Services.Analytics;

public sealed class AnalyticsService : IAnalyticsService
{
    private readonly IAnalyticsRepository _analyticsRepository;

    public AnalyticsService(IAnalyticsRepository analyticsRepository)
    {
        _analyticsRepository = analyticsRepository;
    }

    public async Task<AnalyticsDashboardDto> GetDashboardAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        int topProducts = 10,
        CancellationToken cancellationToken = default)
    {
        if (fromUtc > toUtc)
        {
            throw new ArgumentException("From date must be earlier than to date.");
        }

        var monthly = await _analyticsRepository.GetMonthlyAggregatesAsync(fromUtc, toUtc, cancellationToken);
        var productWise = await _analyticsRepository.GetProductAggregatesAsync(fromUtc, toUtc, topProducts, cancellationToken);

        var monthlyLine = monthly
            .Select(x => new ChartPointDto(x.Month, x.AmountIncludingTax))
            .ToArray();
        var productBar = productWise
            .Select(x => new ChartPointDto($"{x.ProductCode}:{x.ProductName}", x.AmountExcludingTax))
            .ToArray();

        return new AnalyticsDashboardDto(monthly, productWise, monthlyLine, productBar);
    }
}
