using OrderClientApp.Application.Abstractions.Orders;
using OrderClientApp.Infrastructure.Auth;

namespace OrderClientApp.Infrastructure.Orders;

public sealed class SqliteOrderDatabaseInitializer : IOrderDatabaseInitializer
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteOrderDatabaseInitializer(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Orders
            (
                Id TEXT NOT NULL PRIMARY KEY,
                OrderNumber TEXT NOT NULL UNIQUE,
                CreatedByUserId TEXT NOT NULL,
                SupplierName TEXT NOT NULL,
                OrderedAtUtc TEXT NOT NULL,
                Status INTEGER NOT NULL,
                Note TEXT NULL,
                TaxRate REAL NOT NULL,
                IsDeleted INTEGER NOT NULL DEFAULT 0,
                DeletedAtUtc TEXT NULL,
                CreatedAtUtc TEXT NOT NULL,
                UpdatedAtUtc TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS OrderLineItems
            (
                Id TEXT NOT NULL PRIMARY KEY,
                OrderId TEXT NOT NULL,
                ProductCode TEXT NOT NULL,
                ProductName TEXT NOT NULL,
                Quantity INTEGER NOT NULL,
                UnitPriceExcludingTax REAL NOT NULL,
                FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS OrderTemplates
            (
                Id TEXT NOT NULL PRIMARY KEY,
                CreatedByUserId TEXT NOT NULL,
                TemplateName TEXT NOT NULL UNIQUE,
                Note TEXT NULL,
                TaxRate REAL NOT NULL,
                CreatedAtUtc TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS OrderTemplateLineItems
            (
                Id TEXT NOT NULL PRIMARY KEY,
                TemplateId TEXT NOT NULL,
                ProductCode TEXT NOT NULL,
                ProductName TEXT NOT NULL,
                Quantity INTEGER NOT NULL,
                UnitPriceExcludingTax REAL NOT NULL,
                FOREIGN KEY (TemplateId) REFERENCES OrderTemplates(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS OrderSequences
            (
                Name TEXT NOT NULL PRIMARY KEY,
                CurrentValue INTEGER NOT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_Orders_OrderedAtUtc ON Orders(OrderedAtUtc);
            CREATE INDEX IF NOT EXISTS IX_Orders_Status ON Orders(Status);
            CREATE INDEX IF NOT EXISTS IX_OrderLineItems_OrderId ON OrderLineItems(OrderId);
            CREATE INDEX IF NOT EXISTS IX_OrderTemplateLineItems_TemplateId ON OrderTemplateLineItems(TemplateId);
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
