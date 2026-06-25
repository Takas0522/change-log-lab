using OrderClientApp.Application.Abstractions.Analytics;
using OrderClientApp.Infrastructure.Auth;

namespace OrderClientApp.Infrastructure.Analytics;

public sealed class SqliteAnalyticsRepository : IAnalyticsRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteAnalyticsRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyCollection<MonthlyOrderAggregateDto>> GetMonthlyAggregatesAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT strftime('%Y-%m', OrderedAtUtc) AS Month,
                   COUNT(1) AS OrderCount,
                   SUM((SELECT COALESCE(SUM(li.Quantity * li.UnitPriceExcludingTax), 0)
                        FROM OrderLineItems li
                        WHERE li.OrderId = o.Id)) AS AmountExcludingTax,
                   SUM((SELECT COALESCE(SUM(li.Quantity * li.UnitPriceExcludingTax), 0)
                        FROM OrderLineItems li
                        WHERE li.OrderId = o.Id) * (1 + o.TaxRate)) AS AmountIncludingTax
            FROM Orders o
            WHERE o.IsDeleted = 0
              AND o.OrderedAtUtc >= @fromUtc
              AND o.OrderedAtUtc <= @toUtc
            GROUP BY strftime('%Y-%m', OrderedAtUtc)
            ORDER BY Month;
            """;
        command.Parameters.AddWithValue("@fromUtc", fromUtc.ToUniversalTime().ToString("O"));
        command.Parameters.AddWithValue("@toUtc", toUtc.ToUniversalTime().ToString("O"));

        var aggregates = new List<MonthlyOrderAggregateDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            aggregates.Add(new MonthlyOrderAggregateDto(
                reader.GetString(0),
                Convert.ToInt32(reader.GetInt64(1)),
                Convert.ToDecimal(reader.GetDouble(2)),
                decimal.Round(Convert.ToDecimal(reader.GetDouble(3)), 2, MidpointRounding.AwayFromZero)));
        }

        return aggregates;
    }

    public async Task<IReadOnlyCollection<ProductOrderAggregateDto>> GetProductAggregatesAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        int top,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT li.ProductCode,
                   li.ProductName,
                   COUNT(DISTINCT o.Id) AS OrderCount,
                   SUM(li.Quantity) AS TotalQuantity,
                   SUM(li.Quantity * li.UnitPriceExcludingTax) AS AmountExcludingTax
            FROM OrderLineItems li
            INNER JOIN Orders o ON o.Id = li.OrderId
            WHERE o.IsDeleted = 0
              AND o.OrderedAtUtc >= @fromUtc
              AND o.OrderedAtUtc <= @toUtc
            GROUP BY li.ProductCode, li.ProductName
            ORDER BY AmountExcludingTax DESC
            LIMIT @top;
            """;
        command.Parameters.AddWithValue("@fromUtc", fromUtc.ToUniversalTime().ToString("O"));
        command.Parameters.AddWithValue("@toUtc", toUtc.ToUniversalTime().ToString("O"));
        command.Parameters.AddWithValue("@top", top <= 0 ? 10 : top);

        var aggregates = new List<ProductOrderAggregateDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            aggregates.Add(new ProductOrderAggregateDto(
                reader.GetString(0),
                reader.GetString(1),
                Convert.ToInt32(reader.GetInt64(2)),
                Convert.ToInt32(reader.GetInt64(3)),
                Convert.ToDecimal(reader.GetDouble(4))));
        }

        return aggregates;
    }
}
