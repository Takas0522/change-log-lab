using Microsoft.Data.Sqlite;
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
                ExpectedReceivingDateUtc TEXT NULL,
                RejectionReason TEXT NULL,
                Note TEXT NULL,
                DeliveryNoteNumber TEXT NULL,
                DeliveryNoteDateUtc TEXT NULL,
                InvoiceNumber TEXT NULL,
                InvoiceDateUtc TEXT NULL,
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
                ReceivedQuantity INTEGER NOT NULL DEFAULT 0,
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

            CREATE TABLE IF NOT EXISTS InventoryStocks
            (
                ProductCode TEXT NOT NULL PRIMARY KEY,
                ProductName TEXT NOT NULL,
                Quantity INTEGER NOT NULL,
                ReorderPoint INTEGER NOT NULL DEFAULT 0,
                UpdatedAtUtc TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS BudgetSettings
            (
                Id INTEGER NOT NULL PRIMARY KEY CHECK (Id = 1),
                ApprovalThreshold REAL NOT NULL DEFAULT 0,
                MonthlyLimit REAL NULL,
                YearlyLimit REAL NULL,
                UpdatedAtUtc TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Products
            (
                Id TEXT NOT NULL PRIMARY KEY,
                ProductCode TEXT NOT NULL UNIQUE,
                Name TEXT NOT NULL,
                UnitPriceExcludingTax REAL NOT NULL,
                Unit TEXT NOT NULL,
                Notes TEXT NULL,
                Category TEXT NOT NULL,
                ReorderPoint INTEGER NOT NULL DEFAULT 0,
                PreferredSupplierId TEXT NULL,
                CreatedAtUtc TEXT NOT NULL,
                UpdatedAtUtc TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Suppliers
            (
                Id TEXT NOT NULL PRIMARY KEY,
                CompanyName TEXT NOT NULL UNIQUE,
                ContactName TEXT NULL,
                ContactEmail TEXT NULL,
                ContactPhone TEXT NULL,
                Notes TEXT NULL,
                CreatedAtUtc TEXT NOT NULL,
                UpdatedAtUtc TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS ProductSupplierPrices
            (
                ProductId TEXT NOT NULL,
                SupplierId TEXT NOT NULL,
                UnitPriceExcludingTax REAL NOT NULL,
                UpdatedAtUtc TEXT NOT NULL,
                PRIMARY KEY (ProductId, SupplierId),
                FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE,
                FOREIGN KEY (SupplierId) REFERENCES Suppliers(Id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_Orders_OrderedAtUtc ON Orders(OrderedAtUtc);
            CREATE INDEX IF NOT EXISTS IX_Orders_Status ON Orders(Status);
            CREATE INDEX IF NOT EXISTS IX_OrderLineItems_OrderId ON OrderLineItems(OrderId);
            CREATE INDEX IF NOT EXISTS IX_OrderTemplateLineItems_TemplateId ON OrderTemplateLineItems(TemplateId);
            CREATE INDEX IF NOT EXISTS IX_InventoryStocks_ReorderPoint ON InventoryStocks(ReorderPoint, Quantity);
            CREATE INDEX IF NOT EXISTS IX_Products_Code ON Products(ProductCode);
            CREATE INDEX IF NOT EXISTS IX_Products_Category ON Products(Category);
            CREATE INDEX IF NOT EXISTS IX_Products_PreferredSupplierId ON Products(PreferredSupplierId);
            CREATE INDEX IF NOT EXISTS IX_ProductSupplierPrices_SupplierId ON ProductSupplierPrices(SupplierId);
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);

        await EnsureColumnAsync(connection, "Orders", "ExpectedReceivingDateUtc", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync(connection, "Orders", "RejectionReason", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync(connection, "Orders", "DeliveryNoteNumber", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync(connection, "Orders", "DeliveryNoteDateUtc", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync(connection, "Orders", "InvoiceNumber", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync(connection, "Orders", "InvoiceDateUtc", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync(connection, "OrderLineItems", "ReceivedQuantity", "INTEGER NOT NULL DEFAULT 0", cancellationToken);
        await EnsureColumnAsync(connection, "Products", "Category", "TEXT NOT NULL DEFAULT ''", cancellationToken);
        await EnsureColumnAsync(connection, "Products", "ReorderPoint", "INTEGER NOT NULL DEFAULT 0", cancellationToken);
        await EnsureColumnAsync(connection, "Products", "PreferredSupplierId", "TEXT NULL", cancellationToken);

        await using var seedCommand = connection.CreateCommand();
        seedCommand.CommandText = """
            INSERT INTO BudgetSettings(Id, ApprovalThreshold, MonthlyLimit, YearlyLimit, UpdatedAtUtc)
            SELECT 1, 0, NULL, NULL, @updatedAtUtc
            WHERE NOT EXISTS (SELECT 1 FROM BudgetSettings WHERE Id = 1);
            """;
        seedCommand.Parameters.AddWithValue("@updatedAtUtc", DateTimeOffset.UtcNow.ToString("O"));
        await seedCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task EnsureColumnAsync(
        SqliteConnection connection,
        string tableName,
        string columnName,
        string columnDefinition,
        CancellationToken cancellationToken)
    {
        await using var pragma = connection.CreateCommand();
        pragma.CommandText = $"PRAGMA table_info({tableName});";
        var exists = false;
        await using (var reader = await pragma.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    exists = true;
                    break;
                }
            }
        }

        if (exists)
        {
            return;
        }

        await using var alter = connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};";
        await alter.ExecuteNonQueryAsync(cancellationToken);
    }
}
