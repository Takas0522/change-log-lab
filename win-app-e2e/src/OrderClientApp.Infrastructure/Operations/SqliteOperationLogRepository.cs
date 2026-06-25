using OrderClientApp.Application.Abstractions.Operations;
using OrderClientApp.Infrastructure.Auth;

namespace OrderClientApp.Infrastructure.Operations;

public sealed class SqliteOperationLogRepository : IOperationLogRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteOperationLogRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(OperationLogEntryDto entry, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO OperationLogs(Id, Category, Action, Actor, Description, CreatedAtUtc)
            VALUES(@id, @category, @action, @actor, @description, @createdAtUtc);
            """;
        command.Parameters.AddWithValue("@id", entry.Id.ToString());
        command.Parameters.AddWithValue("@category", entry.Category);
        command.Parameters.AddWithValue("@action", entry.Action);
        command.Parameters.AddWithValue("@actor", entry.Actor ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@description", entry.Description);
        command.Parameters.AddWithValue("@createdAtUtc", entry.CreatedAtUtc.ToUniversalTime().ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<OperationLogEntryDto>> QueryAsync(OperationLogQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var limit = Math.Clamp(query.Limit, 1, 500);

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Category, Action, Actor, Description, CreatedAtUtc
            FROM OperationLogs
            WHERE (@keyword IS NULL OR Category LIKE @keywordLike OR Action LIKE @keywordLike OR Description LIKE @keywordLike OR IFNULL(Actor, '') LIKE @keywordLike)
              AND (@category IS NULL OR Category = @category)
              AND (@fromUtc IS NULL OR CreatedAtUtc >= @fromUtc)
              AND (@toUtc IS NULL OR CreatedAtUtc <= @toUtc)
            ORDER BY CreatedAtUtc DESC
            LIMIT @limit;
            """;
        var keyword = string.IsNullOrWhiteSpace(query.Keyword) ? null : query.Keyword.Trim();
        command.Parameters.AddWithValue("@keyword", keyword ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@keywordLike", keyword is null ? (object)DBNull.Value : $"%{keyword}%");
        command.Parameters.AddWithValue("@category", string.IsNullOrWhiteSpace(query.Category) ? (object)DBNull.Value : query.Category.Trim());
        command.Parameters.AddWithValue("@fromUtc", query.FromUtc?.ToUniversalTime().ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@toUtc", query.ToUtc?.ToUniversalTime().ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@limit", limit);

        var entries = new List<OperationLogEntryDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            entries.Add(new OperationLogEntryDto(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.GetString(4),
                DateTimeOffset.Parse(reader.GetString(5))));
        }

        return entries;
    }
}
