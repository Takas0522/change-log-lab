using OrderClientApp.Application.Abstractions.Orders;
using OrderClientApp.Infrastructure.Auth;

namespace OrderClientApp.Infrastructure.Orders;

public sealed class SqliteInventoryRepository : IInventoryRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteInventoryRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task IncreaseStockAsync(string productCode, string productName, int quantity, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(productCode))
        {
            throw new ArgumentException("Product code is required.", nameof(productCode));
        }

        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new ArgumentException("Product name is required.", nameof(productName));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO InventoryStocks(ProductCode, ProductName, Quantity, ReorderPoint, UpdatedAtUtc)
            VALUES(@productCode, @productName, @quantity, 0, @updatedAtUtc)
            ON CONFLICT(ProductCode) DO UPDATE SET
                ProductName = excluded.ProductName,
                Quantity = InventoryStocks.Quantity + excluded.Quantity,
                UpdatedAtUtc = excluded.UpdatedAtUtc;
            """;
        command.Parameters.AddWithValue("@productCode", productCode.Trim());
        command.Parameters.AddWithValue("@productName", productName.Trim());
        command.Parameters.AddWithValue("@quantity", quantity);
        command.Parameters.AddWithValue("@updatedAtUtc", DateTimeOffset.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<InventoryAlertDto>> ListAlertsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ProductCode, ProductName, Quantity, ReorderPoint
            FROM InventoryStocks
            WHERE ReorderPoint > 0
              AND Quantity <= ReorderPoint
            ORDER BY ProductCode;
            """;

        var result = new List<InventoryAlertDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new InventoryAlertDto(
                reader.GetString(0),
                reader.GetString(1),
                Convert.ToInt32(reader.GetInt64(2)),
                Convert.ToInt32(reader.GetInt64(3))));
        }

        return result;
    }

    public async Task<int> GetQuantityAsync(string productCode, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Quantity
            FROM InventoryStocks
            WHERE ProductCode = @productCode
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@productCode", productCode);
        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return scalar is null or DBNull ? 0 : Convert.ToInt32(scalar);
    }
}
