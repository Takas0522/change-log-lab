using Microsoft.Data.Sqlite;
using OrderClientApp.Application.Abstractions.Orders;
using OrderClientApp.Domain.Orders;
using OrderClientApp.Infrastructure.Auth;

namespace OrderClientApp.Infrastructure.Orders;

public sealed class SqliteOrderRepository : IOrderRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteOrderRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        await using var orderCommand = connection.CreateCommand();
        orderCommand.Transaction = transaction;
        orderCommand.CommandText = """
            INSERT INTO Orders
            (
                Id, OrderNumber, CreatedByUserId, SupplierName, OrderedAtUtc, Status,
                ExpectedReceivingDateUtc, RejectionReason, Note, DeliveryNoteNumber, DeliveryNoteDateUtc, InvoiceNumber, InvoiceDateUtc,
                TaxRate, IsDeleted, DeletedAtUtc, CreatedAtUtc, UpdatedAtUtc
            )
            VALUES
            (
                @id, @orderNumber, @createdByUserId, @supplierName, @orderedAtUtc, @status,
                @expectedReceivingDateUtc, @rejectionReason, @note, @deliveryNoteNumber, @deliveryNoteDateUtc, @invoiceNumber, @invoiceDateUtc,
                @taxRate, @isDeleted, @deletedAtUtc, @createdAtUtc, @updatedAtUtc
            );
            """;
        AddOrderParameters(orderCommand, order);
        await orderCommand.ExecuteNonQueryAsync(cancellationToken);

        foreach (var lineItem in order.LineItems)
        {
            await InsertLineItemAsync(connection, transaction, order.Id, lineItem, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        await using var orderCommand = connection.CreateCommand();
        orderCommand.Transaction = transaction;
        orderCommand.CommandText = """
            UPDATE Orders
            SET
                SupplierName = @supplierName,
                OrderedAtUtc = @orderedAtUtc,
                Status = @status,
                ExpectedReceivingDateUtc = @expectedReceivingDateUtc,
                RejectionReason = @rejectionReason,
                Note = @note,
                DeliveryNoteNumber = @deliveryNoteNumber,
                DeliveryNoteDateUtc = @deliveryNoteDateUtc,
                InvoiceNumber = @invoiceNumber,
                InvoiceDateUtc = @invoiceDateUtc,
                TaxRate = @taxRate,
                IsDeleted = @isDeleted,
                DeletedAtUtc = @deletedAtUtc,
                UpdatedAtUtc = @updatedAtUtc
            WHERE Id = @id;
            """;
        AddOrderParameters(orderCommand, order);
        await orderCommand.ExecuteNonQueryAsync(cancellationToken);

        await using var deleteLineItems = connection.CreateCommand();
        deleteLineItems.Transaction = transaction;
        deleteLineItems.CommandText = "DELETE FROM OrderLineItems WHERE OrderId = @orderId;";
        deleteLineItems.Parameters.AddWithValue("@orderId", order.Id.ToString());
        await deleteLineItems.ExecuteNonQueryAsync(cancellationToken);

        foreach (var lineItem in order.LineItems)
        {
            await InsertLineItemAsync(connection, transaction, order.Id, lineItem, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<Order?> GetByIdAsync(Guid orderId, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var orderCommand = connection.CreateCommand();
        orderCommand.CommandText = """
            SELECT Id, OrderNumber, CreatedByUserId, SupplierName, OrderedAtUtc, Status,
                   ExpectedReceivingDateUtc, RejectionReason, Note, DeliveryNoteNumber, DeliveryNoteDateUtc, InvoiceNumber, InvoiceDateUtc,
                   TaxRate, IsDeleted, DeletedAtUtc, CreatedAtUtc, UpdatedAtUtc
            FROM Orders
            WHERE Id = @id
              AND (@includeDeleted = 1 OR IsDeleted = 0)
            LIMIT 1;
            """;
        orderCommand.Parameters.AddWithValue("@id", orderId.ToString());
        orderCommand.Parameters.AddWithValue("@includeDeleted", includeDeleted ? 1 : 0);

        await using var reader = await orderCommand.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var order = await MapOrderAsync(connection, reader, cancellationToken);
        return order;
    }

    public async Task<IReadOnlyCollection<Order>> ListAsync(OrderListQuery query, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var whereClause = BuildWhereClause(query);
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT Id, OrderNumber, CreatedByUserId, SupplierName, OrderedAtUtc, Status,
                   ExpectedReceivingDateUtc, RejectionReason, Note, DeliveryNoteNumber, DeliveryNoteDateUtc, InvoiceNumber, InvoiceDateUtc,
                   TaxRate, IsDeleted, DeletedAtUtc, CreatedAtUtc, UpdatedAtUtc
            FROM Orders
            {whereClause}
            ORDER BY OrderedAtUtc DESC, CreatedAtUtc DESC
            LIMIT @limit OFFSET @offset;
            """;
        AddFilterParameters(command, query);
        command.Parameters.AddWithValue("@limit", query.PageSize);
        command.Parameters.AddWithValue("@offset", (query.PageNumber - 1) * query.PageSize);

        var orders = new List<Order>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            orders.Add(await MapOrderAsync(connection, reader, cancellationToken));
        }

        return orders;
    }

    public async Task<int> CountAsync(OrderListQuery query, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var whereClause = BuildWhereClause(query);
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(1) FROM Orders {whereClause};";
        AddFilterParameters(command, query);

        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task SoftDeleteAsync(Guid orderId, DateTimeOffset deletedAtUtc, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Orders
            SET IsDeleted = 1,
                DeletedAtUtc = @deletedAtUtc,
                UpdatedAtUtc = @updatedAtUtc,
                Status = @status
            WHERE Id = @id;
            """;
        command.Parameters.AddWithValue("@id", orderId.ToString());
        command.Parameters.AddWithValue("@deletedAtUtc", deletedAtUtc.ToUniversalTime().ToString("O"));
        command.Parameters.AddWithValue("@updatedAtUtc", deletedAtUtc.ToUniversalTime().ToString("O"));
        command.Parameters.AddWithValue("@status", (int)OrderStatus.Canceled);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<decimal> SumAmountIncludingTaxAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                COALESCE(SUM(
                    (SELECT COALESCE(SUM(li.Quantity * li.UnitPriceExcludingTax), 0)
                     FROM OrderLineItems li
                     WHERE li.OrderId = o.Id
                    ) * (1 + o.TaxRate)
                ), 0)
            FROM Orders o
            WHERE o.IsDeleted = 0
              AND o.Status != @canceled
              AND o.OrderedAtUtc >= @fromUtc
              AND o.OrderedAtUtc <= @toUtc;
            """;
        command.Parameters.AddWithValue("@canceled", (int)OrderStatus.Canceled);
        command.Parameters.AddWithValue("@fromUtc", fromUtc.ToUniversalTime().ToString("O"));
        command.Parameters.AddWithValue("@toUtc", toUtc.ToUniversalTime().ToString("O"));
        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return scalar is null or DBNull ? 0m : Convert.ToDecimal(scalar);
    }

    private static async Task InsertLineItemAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid orderId,
        OrderLineItem lineItem,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO OrderLineItems
            (Id, OrderId, ProductCode, ProductName, Quantity, ReceivedQuantity, UnitPriceExcludingTax)
            VALUES
            (@id, @orderId, @productCode, @productName, @quantity, @receivedQuantity, @unitPriceExcludingTax);
            """;
        command.Parameters.AddWithValue("@id", lineItem.Id.ToString());
        command.Parameters.AddWithValue("@orderId", orderId.ToString());
        command.Parameters.AddWithValue("@productCode", lineItem.ProductCode);
        command.Parameters.AddWithValue("@productName", lineItem.ProductName);
        command.Parameters.AddWithValue("@quantity", lineItem.Quantity);
        command.Parameters.AddWithValue("@receivedQuantity", lineItem.ReceivedQuantity);
        command.Parameters.AddWithValue("@unitPriceExcludingTax", lineItem.UnitPriceExcludingTax);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddOrderParameters(SqliteCommand command, Order order)
    {
        command.Parameters.AddWithValue("@id", order.Id.ToString());
        command.Parameters.AddWithValue("@orderNumber", order.OrderNumber.Value);
        command.Parameters.AddWithValue("@createdByUserId", order.CreatedByUserId.ToString());
        command.Parameters.AddWithValue("@supplierName", order.SupplierName);
        command.Parameters.AddWithValue("@orderedAtUtc", order.OrderedAtUtc.ToUniversalTime().ToString("O"));
        command.Parameters.AddWithValue("@status", (int)order.Status);
        command.Parameters.AddWithValue("@expectedReceivingDateUtc", order.ExpectedReceivingDateUtc?.ToUniversalTime().ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@rejectionReason", order.RejectionReason ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@note", order.Note ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@deliveryNoteNumber", order.DeliveryNoteNumber ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@deliveryNoteDateUtc", order.DeliveryNoteDateUtc?.ToUniversalTime().ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@invoiceNumber", order.InvoiceNumber ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@invoiceDateUtc", order.InvoiceDateUtc?.ToUniversalTime().ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@taxRate", order.TaxRate);
        command.Parameters.AddWithValue("@isDeleted", order.IsDeleted ? 1 : 0);
        command.Parameters.AddWithValue("@deletedAtUtc", order.DeletedAtUtc?.ToUniversalTime().ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@createdAtUtc", order.CreatedAtUtc.ToUniversalTime().ToString("O"));
        command.Parameters.AddWithValue("@updatedAtUtc", order.UpdatedAtUtc.ToUniversalTime().ToString("O"));
    }

    private static string BuildWhereClause(OrderListQuery query)
    {
        var conditions = new List<string>();
        if (!query.IncludeDeleted)
        {
            conditions.Add("IsDeleted = 0");
        }

        if (query.Status.HasValue)
        {
            conditions.Add("Status = @status");
        }

        if (query.DateFromUtc.HasValue)
        {
            conditions.Add("OrderedAtUtc >= @dateFromUtc");
        }

        if (query.DateToUtc.HasValue)
        {
            conditions.Add("OrderedAtUtc <= @dateToUtc");
        }

        return conditions.Count == 0
            ? string.Empty
            : $"WHERE {string.Join(" AND ", conditions)}";
    }

    private static void AddFilterParameters(SqliteCommand command, OrderListQuery query)
    {
        if (query.Status.HasValue)
        {
            command.Parameters.AddWithValue("@status", (int)query.Status.Value);
        }

        if (query.DateFromUtc.HasValue)
        {
            command.Parameters.AddWithValue("@dateFromUtc", query.DateFromUtc.Value.ToUniversalTime().ToString("O"));
        }

        if (query.DateToUtc.HasValue)
        {
            command.Parameters.AddWithValue("@dateToUtc", query.DateToUtc.Value.ToUniversalTime().ToString("O"));
        }
    }

    private static async Task<Order> MapOrderAsync(SqliteConnection connection, SqliteDataReader reader, CancellationToken cancellationToken)
    {
        var orderId = Guid.Parse(reader.GetString(0));
        var lineItems = await GetLineItemsAsync(connection, orderId, cancellationToken);

        var expectedReceivingDateRaw = reader.GetValue(6);
        DateTimeOffset? expectedReceivingDateUtc = expectedReceivingDateRaw is DBNull
            ? null
            : DateTimeOffset.Parse((string)expectedReceivingDateRaw);

        var deliveryNoteDateRaw = reader.GetValue(10);
        DateTimeOffset? deliveryNoteDateUtc = deliveryNoteDateRaw is DBNull
            ? null
            : DateTimeOffset.Parse((string)deliveryNoteDateRaw);

        var invoiceDateRaw = reader.GetValue(12);
        DateTimeOffset? invoiceDateUtc = invoiceDateRaw is DBNull
            ? null
            : DateTimeOffset.Parse((string)invoiceDateRaw);

        var deletedAtRaw = reader.GetValue(15);
        DateTimeOffset? deletedAtUtc = deletedAtRaw is DBNull
            ? null
            : DateTimeOffset.Parse((string)deletedAtRaw);

        return new Order(
            orderId,
            OrderNumber.Parse(reader.GetString(1)),
            Guid.Parse(reader.GetString(2)),
            reader.GetString(3),
            DateTimeOffset.Parse(reader.GetString(4)),
            (OrderStatus)Convert.ToInt32(reader.GetInt64(5)),
            expectedReceivingDateUtc,
            reader.GetValue(7) is DBNull ? null : reader.GetString(7),
            reader.GetValue(8) is DBNull ? null : reader.GetString(8),
            reader.GetValue(9) is DBNull ? null : reader.GetString(9),
            deliveryNoteDateUtc,
            reader.GetValue(11) is DBNull ? null : reader.GetString(11),
            invoiceDateUtc,
            Convert.ToDecimal(reader.GetDouble(13)),
            lineItems,
            reader.GetInt64(14) == 1,
            deletedAtUtc,
            DateTimeOffset.Parse(reader.GetString(16)),
            DateTimeOffset.Parse(reader.GetString(17)));
    }

    private static async Task<IReadOnlyCollection<OrderLineItem>> GetLineItemsAsync(SqliteConnection connection, Guid orderId, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, ProductCode, ProductName, Quantity, ReceivedQuantity, UnitPriceExcludingTax
            FROM OrderLineItems
            WHERE OrderId = @orderId
            ORDER BY ProductCode;
            """;
        command.Parameters.AddWithValue("@orderId", orderId.ToString());

        var lineItems = new List<OrderLineItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            lineItems.Add(new OrderLineItem(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                reader.GetString(2),
                Convert.ToInt32(reader.GetInt64(3)),
                Convert.ToDecimal(reader.GetDouble(5)),
                Convert.ToInt32(reader.GetInt64(4))));
        }

        return lineItems;
    }
}
