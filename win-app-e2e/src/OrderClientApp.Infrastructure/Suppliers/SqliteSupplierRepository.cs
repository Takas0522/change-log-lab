using Microsoft.Data.Sqlite;
using OrderClientApp.Application.Abstractions.Suppliers;
using OrderClientApp.Domain.Suppliers;
using OrderClientApp.Infrastructure.Auth;

namespace OrderClientApp.Infrastructure.Suppliers;

public sealed class SqliteSupplierRepository : ISupplierRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteSupplierRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(supplier);
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Suppliers
            (Id, CompanyName, ContactName, ContactEmail, ContactPhone, Notes, CreatedAtUtc, UpdatedAtUtc)
            VALUES
            (@id, @companyName, @contactName, @contactEmail, @contactPhone, @notes, @createdAtUtc, @updatedAtUtc);
            """;
        AddParameters(command, supplier);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(supplier);
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Suppliers
            SET CompanyName = @companyName,
                ContactName = @contactName,
                ContactEmail = @contactEmail,
                ContactPhone = @contactPhone,
                Notes = @notes,
                UpdatedAtUtc = @updatedAtUtc
            WHERE Id = @id;
            """;
        AddParameters(command, supplier);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Suppliers WHERE Id = @id;";
        command.Parameters.AddWithValue("@id", supplierId.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<Supplier?> GetByIdAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, CompanyName, ContactName, ContactEmail, ContactPhone, Notes, CreatedAtUtc, UpdatedAtUtc
            FROM Suppliers
            WHERE Id = @id
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@id", supplierId.ToString());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<IReadOnlyCollection<Supplier>> ListAsync(string? keyword, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, CompanyName, ContactName, ContactEmail, ContactPhone, Notes, CreatedAtUtc, UpdatedAtUtc
            FROM Suppliers
            WHERE (@keyword IS NULL OR CompanyName LIKE @keywordLike OR ContactName LIKE @keywordLike)
            ORDER BY CompanyName;
            """;
        var normalized = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();
        command.Parameters.AddWithValue("@keyword", normalized ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@keywordLike", normalized is null ? (object)DBNull.Value : $"%{normalized}%");

        var suppliers = new List<Supplier>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            suppliers.Add(Map(reader));
        }

        return suppliers;
    }

    public async Task<bool> ExistsAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS(SELECT 1 FROM Suppliers WHERE Id = @id);";
        command.Parameters.AddWithValue("@id", supplierId.ToString());
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
    }

    public async Task<bool> ExistsByCompanyNameAsync(string companyName, Guid? excludingSupplierId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT EXISTS(
                SELECT 1
                FROM Suppliers
                WHERE CompanyName = @companyName
                  AND (@excludingSupplierId IS NULL OR Id != @excludingSupplierId)
            );
            """;
        command.Parameters.AddWithValue("@companyName", companyName.Trim());
        command.Parameters.AddWithValue("@excludingSupplierId", excludingSupplierId?.ToString() ?? (object)DBNull.Value);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
    }

    public async Task SetProductSupplierPriceAsync(
        Guid productId,
        Guid supplierId,
        decimal unitPriceExcludingTax,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO ProductSupplierPrices(ProductId, SupplierId, UnitPriceExcludingTax, UpdatedAtUtc)
            VALUES(@productId, @supplierId, @unitPriceExcludingTax, @updatedAtUtc)
            ON CONFLICT(ProductId, SupplierId) DO UPDATE SET
                UnitPriceExcludingTax = excluded.UnitPriceExcludingTax,
                UpdatedAtUtc = excluded.UpdatedAtUtc;
            """;
        command.Parameters.AddWithValue("@productId", productId.ToString());
        command.Parameters.AddWithValue("@supplierId", supplierId.ToString());
        command.Parameters.AddWithValue("@unitPriceExcludingTax", unitPriceExcludingTax);
        command.Parameters.AddWithValue("@updatedAtUtc", nowUtc.ToUniversalTime().ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductSupplierPriceDto>> GetProductSupplierPricesAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ProductId, SupplierId, UnitPriceExcludingTax, UpdatedAtUtc
            FROM ProductSupplierPrices
            WHERE ProductId = @productId
            ORDER BY UnitPriceExcludingTax ASC;
            """;
        command.Parameters.AddWithValue("@productId", productId.ToString());
        var items = new List<ProductSupplierPriceDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new ProductSupplierPriceDto(
                Guid.Parse(reader.GetString(0)),
                Guid.Parse(reader.GetString(1)),
                Convert.ToDecimal(reader.GetDouble(2)),
                DateTimeOffset.Parse(reader.GetString(3))));
        }

        return items;
    }

    private static void AddParameters(SqliteCommand command, Supplier supplier)
    {
        command.Parameters.AddWithValue("@id", supplier.Id.ToString());
        command.Parameters.AddWithValue("@companyName", supplier.CompanyName);
        command.Parameters.AddWithValue("@contactName", supplier.ContactName ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@contactEmail", supplier.ContactEmail ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@contactPhone", supplier.ContactPhone ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@notes", supplier.Notes ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@createdAtUtc", supplier.CreatedAtUtc.ToUniversalTime().ToString("O"));
        command.Parameters.AddWithValue("@updatedAtUtc", supplier.UpdatedAtUtc.ToUniversalTime().ToString("O"));
    }

    private static Supplier Map(SqliteDataReader reader)
        => new(
            Guid.Parse(reader.GetString(0)),
            reader.GetString(1),
            reader.GetValue(2) is DBNull ? null : reader.GetString(2),
            reader.GetValue(3) is DBNull ? null : reader.GetString(3),
            reader.GetValue(4) is DBNull ? null : reader.GetString(4),
            reader.GetValue(5) is DBNull ? null : reader.GetString(5),
            DateTimeOffset.Parse(reader.GetString(6)),
            DateTimeOffset.Parse(reader.GetString(7)));
}
