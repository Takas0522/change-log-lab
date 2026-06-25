using Microsoft.Data.Sqlite;
using OrderClientApp.Application.Abstractions.Orders;
using OrderClientApp.Infrastructure.Auth;

namespace OrderClientApp.Infrastructure.Orders;

public sealed class SqliteOrderNumberSequenceRepository : IOrderNumberSequenceRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteOrderNumberSequenceRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> GetNextSequenceAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        await using var upsert = connection.CreateCommand();
        upsert.Transaction = transaction;
        upsert.CommandText = """
            INSERT INTO OrderSequences (Name, CurrentValue)
            VALUES ('Order', 0)
            ON CONFLICT(Name) DO NOTHING;
            """;
        await upsert.ExecuteNonQueryAsync(cancellationToken);

        await using var read = connection.CreateCommand();
        read.Transaction = transaction;
        read.CommandText = "SELECT CurrentValue FROM OrderSequences WHERE Name = 'Order' LIMIT 1;";
        var currentValue = Convert.ToInt32(await read.ExecuteScalarAsync(cancellationToken));

        var nextValue = currentValue + 1;
        await using var update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText = "UPDATE OrderSequences SET CurrentValue = @nextValue WHERE Name = 'Order';";
        update.Parameters.AddWithValue("@nextValue", nextValue);
        await update.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return nextValue;
    }
}
