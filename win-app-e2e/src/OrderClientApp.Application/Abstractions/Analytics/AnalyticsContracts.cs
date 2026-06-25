namespace OrderClientApp.Application.Abstractions.Analytics;

public sealed record MonthlyOrderAggregateDto(
    string Month,
    int OrderCount,
    decimal AmountExcludingTax,
    decimal AmountIncludingTax);

public sealed record ProductOrderAggregateDto(
    string ProductCode,
    string ProductName,
    int OrderCount,
    int TotalQuantity,
    decimal AmountExcludingTax);

public sealed record ChartPointDto(
    string Label,
    decimal Value);

public sealed record AnalyticsDashboardDto(
    IReadOnlyCollection<MonthlyOrderAggregateDto> Monthly,
    IReadOnlyCollection<ProductOrderAggregateDto> ProductWise,
    IReadOnlyCollection<ChartPointDto> MonthlyAmountLine,
    IReadOnlyCollection<ChartPointDto> ProductAmountBar);
