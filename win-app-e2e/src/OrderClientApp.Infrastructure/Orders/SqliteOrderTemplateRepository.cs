using Microsoft.Data.Sqlite;
using OrderClientApp.Application.Abstractions.Orders;
using OrderClientApp.Domain.Orders;
using OrderClientApp.Infrastructure.Auth;

namespace OrderClientApp.Infrastructure.Orders;

public sealed class SqliteOrderTemplateRepository : IOrderTemplateRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteOrderTemplateRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(OrderTemplate template, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(template);

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO OrderTemplates
            (Id, CreatedByUserId, TemplateName, Note, TaxRate, CreatedAtUtc)
            VALUES
            (@id, @createdByUserId, @templateName, @note, @taxRate, @createdAtUtc);
            """;
        command.Parameters.AddWithValue("@id", template.Id.ToString());
        command.Parameters.AddWithValue("@createdByUserId", template.CreatedByUserId.ToString());
        command.Parameters.AddWithValue("@templateName", template.TemplateName);
        command.Parameters.AddWithValue("@note", template.Note ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@taxRate", template.TaxRate);
        command.Parameters.AddWithValue("@createdAtUtc", template.CreatedAtUtc.ToUniversalTime().ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);

        foreach (var lineItem in template.LineItems)
        {
            await using var lineCommand = connection.CreateCommand();
            lineCommand.Transaction = transaction;
            lineCommand.CommandText = """
                INSERT INTO OrderTemplateLineItems
                (Id, TemplateId, ProductCode, ProductName, Quantity, UnitPriceExcludingTax)
                VALUES
                (@id, @templateId, @productCode, @productName, @quantity, @unitPriceExcludingTax);
                """;
            lineCommand.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
            lineCommand.Parameters.AddWithValue("@templateId", template.Id.ToString());
            lineCommand.Parameters.AddWithValue("@productCode", lineItem.ProductCode);
            lineCommand.Parameters.AddWithValue("@productName", lineItem.ProductName);
            lineCommand.Parameters.AddWithValue("@quantity", lineItem.Quantity);
            lineCommand.Parameters.AddWithValue("@unitPriceExcludingTax", lineItem.UnitPriceExcludingTax);
            await lineCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<OrderTemplate>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, CreatedByUserId, TemplateName, Note, TaxRate, CreatedAtUtc
            FROM OrderTemplates
            ORDER BY TemplateName;
            """;

        var templates = new List<OrderTemplate>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            templates.Add(await MapTemplateAsync(connection, reader, cancellationToken));
        }

        return templates;
    }

    public async Task<OrderTemplate?> GetByIdAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, CreatedByUserId, TemplateName, Note, TaxRate, CreatedAtUtc
            FROM OrderTemplates
            WHERE Id = @id
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@id", templateId.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return await MapTemplateAsync(connection, reader, cancellationToken);
    }

    private static async Task<OrderTemplate> MapTemplateAsync(
        Microsoft.Data.Sqlite.SqliteConnection connection,
        Microsoft.Data.Sqlite.SqliteDataReader reader,
        CancellationToken cancellationToken)
    {
        var templateId = Guid.Parse(reader.GetString(0));
        var lineItems = await GetLineItemsAsync(connection, templateId, cancellationToken);

        return new OrderTemplate(
            templateId,
            Guid.Parse(reader.GetString(1)),
            reader.GetString(2),
            reader.GetValue(3) is DBNull ? null : reader.GetString(3),
            Convert.ToDecimal(reader.GetDouble(4)),
            lineItems,
            DateTimeOffset.Parse(reader.GetString(5)));
    }

    private static async Task<IReadOnlyCollection<OrderTemplateLineItem>> GetLineItemsAsync(
        Microsoft.Data.Sqlite.SqliteConnection connection,
        Guid templateId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ProductCode, ProductName, Quantity, UnitPriceExcludingTax
            FROM OrderTemplateLineItems
            WHERE TemplateId = @templateId
            ORDER BY ProductCode;
            """;
        command.Parameters.AddWithValue("@templateId", templateId.ToString());

        var lineItems = new List<OrderTemplateLineItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            lineItems.Add(new OrderTemplateLineItem(
                reader.GetString(0),
                reader.GetString(1),
                Convert.ToInt32(reader.GetInt64(2)),
                Convert.ToDecimal(reader.GetDouble(3))));
        }

        return lineItems;
    }
}
