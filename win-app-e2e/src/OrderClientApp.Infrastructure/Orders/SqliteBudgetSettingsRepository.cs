using OrderClientApp.Application.Abstractions.Orders;
using OrderClientApp.Infrastructure.Auth;

namespace OrderClientApp.Infrastructure.Orders;

public sealed class SqliteBudgetSettingsRepository : IBudgetSettingsRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteBudgetSettingsRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<BudgetSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ApprovalThreshold, MonthlyLimit, YearlyLimit, UpdatedAtUtc
            FROM BudgetSettings
            WHERE Id = 1
            LIMIT 1;
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new BudgetSettingsDto(0m, null, null, DateTimeOffset.UnixEpoch);
        }

        return new BudgetSettingsDto(
            Convert.ToDecimal(reader.GetDouble(0)),
            reader.IsDBNull(1) ? null : Convert.ToDecimal(reader.GetDouble(1)),
            reader.IsDBNull(2) ? null : Convert.ToDecimal(reader.GetDouble(2)),
            DateTimeOffset.Parse(reader.GetString(3)));
    }

    public async Task<BudgetSettingsDto> UpsertAsync(
        decimal approvalThreshold,
        decimal? monthlyLimit,
        decimal? yearlyLimit,
        DateTimeOffset updatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO BudgetSettings(Id, ApprovalThreshold, MonthlyLimit, YearlyLimit, UpdatedAtUtc)
            VALUES(1, @approvalThreshold, @monthlyLimit, @yearlyLimit, @updatedAtUtc)
            ON CONFLICT(Id) DO UPDATE SET
                ApprovalThreshold = excluded.ApprovalThreshold,
                MonthlyLimit = excluded.MonthlyLimit,
                YearlyLimit = excluded.YearlyLimit,
                UpdatedAtUtc = excluded.UpdatedAtUtc;
            """;
        command.Parameters.AddWithValue("@approvalThreshold", approvalThreshold);
        command.Parameters.AddWithValue("@monthlyLimit", monthlyLimit ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@yearlyLimit", yearlyLimit ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@updatedAtUtc", updatedAtUtc.ToUniversalTime().ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);

        return new BudgetSettingsDto(approvalThreshold, monthlyLimit, yearlyLimit, updatedAtUtc.ToUniversalTime());
    }
}
