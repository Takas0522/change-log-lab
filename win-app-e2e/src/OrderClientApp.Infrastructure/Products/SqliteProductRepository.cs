using Microsoft.Data.Sqlite;
using OrderClientApp.Application.Abstractions.Products;
using OrderClientApp.Domain.Products;
using OrderClientApp.Infrastructure.Auth;

namespace OrderClientApp.Infrastructure.Products;

public sealed class SqliteProductRepository : IProductRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteProductRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Products
            (Id, ProductCode, Name, UnitPriceExcludingTax, Unit, Notes, Category, ReorderPoint, PreferredSupplierId, CreatedAtUtc, UpdatedAtUtc)
            VALUES
            (@id, @productCode, @name, @unitPriceExcludingTax, @unit, @notes, @category, @reorderPoint, @preferredSupplierId, @createdAtUtc, @updatedAtUtc);
            """;
        AddParameters(command, product);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Products
            SET ProductCode = @productCode,
                Name = @name,
                UnitPriceExcludingTax = @unitPriceExcludingTax,
                Unit = @unit,
                Notes = @notes,
                Category = @category,
                ReorderPoint = @reorderPoint,
                PreferredSupplierId = @preferredSupplierId,
                UpdatedAtUtc = @updatedAtUtc
            WHERE Id = @id;
            """;
        AddParameters(command, product);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Products WHERE Id = @id;";
        command.Parameters.AddWithValue("@id", productId.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<Product?> GetByIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, ProductCode, Name, UnitPriceExcludingTax, Unit, Notes, Category, ReorderPoint, PreferredSupplierId, CreatedAtUtc, UpdatedAtUtc
            FROM Products
            WHERE Id = @id
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@id", productId.ToString());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<Product?> GetByCodeAsync(string productCode, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, ProductCode, Name, UnitPriceExcludingTax, Unit, Notes, Category, ReorderPoint, PreferredSupplierId, CreatedAtUtc, UpdatedAtUtc
            FROM Products
            WHERE ProductCode = @productCode
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@productCode", productCode.Trim());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<IReadOnlyCollection<Product>> ListAsync(ProductFilter filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, ProductCode, Name, UnitPriceExcludingTax, Unit, Notes, Category, ReorderPoint, PreferredSupplierId, CreatedAtUtc, UpdatedAtUtc
            FROM Products
            WHERE (@productCode IS NULL OR ProductCode LIKE @productCodeLike)
              AND (@name IS NULL OR Name LIKE @nameLike)
              AND (@category IS NULL OR Category = @category)
            ORDER BY ProductCode;
            """;
        AddFilter(command, filter);

        var products = new List<Product>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            products.Add(Map(reader));
        }

        return products;
    }

    public async Task<bool> ExistsByCodeAsync(string productCode, Guid? excludingProductId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT EXISTS(
                SELECT 1
                FROM Products
                WHERE ProductCode = @productCode
                  AND (@excludingProductId IS NULL OR Id != @excludingProductId)
            );
            """;
        command.Parameters.AddWithValue("@productCode", productCode.Trim());
        command.Parameters.AddWithValue("@excludingProductId", excludingProductId?.ToString() ?? (object)DBNull.Value);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
    }

    public async Task SetPreferredSupplierAsync(Guid productId, Guid? supplierId, DateTimeOffset nowUtc, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Products
            SET PreferredSupplierId = @preferredSupplierId,
                UpdatedAtUtc = @updatedAtUtc
            WHERE Id = @productId;
            """;
        command.Parameters.AddWithValue("@productId", productId.ToString());
        command.Parameters.AddWithValue("@preferredSupplierId", supplierId?.ToString() ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@updatedAtUtc", nowUtc.ToUniversalTime().ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddFilter(SqliteCommand command, ProductFilter filter)
    {
        var productCode = string.IsNullOrWhiteSpace(filter.ProductCode) ? null : filter.ProductCode.Trim();
        var name = string.IsNullOrWhiteSpace(filter.Name) ? null : filter.Name.Trim();
        var category = string.IsNullOrWhiteSpace(filter.Category) ? null : filter.Category.Trim();

        command.Parameters.AddWithValue("@productCode", productCode ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@productCodeLike", productCode is null ? (object)DBNull.Value : $"%{productCode}%");
        command.Parameters.AddWithValue("@name", name ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@nameLike", name is null ? (object)DBNull.Value : $"%{name}%");
        command.Parameters.AddWithValue("@category", category ?? (object)DBNull.Value);
    }

    private static void AddParameters(SqliteCommand command, Product product)
    {
        command.Parameters.AddWithValue("@id", product.Id.ToString());
        command.Parameters.AddWithValue("@productCode", product.ProductCode);
        command.Parameters.AddWithValue("@name", product.Name);
        command.Parameters.AddWithValue("@unitPriceExcludingTax", product.UnitPriceExcludingTax);
        command.Parameters.AddWithValue("@unit", product.Unit);
        command.Parameters.AddWithValue("@notes", product.Notes ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@category", product.Category);
        command.Parameters.AddWithValue("@reorderPoint", product.ReorderPoint);
        command.Parameters.AddWithValue("@preferredSupplierId", product.PreferredSupplierId?.ToString() ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@createdAtUtc", product.CreatedAtUtc.ToUniversalTime().ToString("O"));
        command.Parameters.AddWithValue("@updatedAtUtc", product.UpdatedAtUtc.ToUniversalTime().ToString("O"));
    }

    private static Product Map(SqliteDataReader reader)
        => new(
            Guid.Parse(reader.GetString(0)),
            reader.GetString(1),
            reader.GetString(2),
            Convert.ToDecimal(reader.GetDouble(3)),
            reader.GetString(4),
            reader.GetValue(5) is DBNull ? null : reader.GetString(5),
            reader.GetString(6),
            Convert.ToInt32(reader.GetInt64(7)),
            reader.GetValue(8) is DBNull ? null : Guid.Parse(reader.GetString(8)),
            DateTimeOffset.Parse(reader.GetString(9)),
            DateTimeOffset.Parse(reader.GetString(10)));
}
