using OrderClientApp.Application.Abstractions.Settings;
using OrderClientApp.Infrastructure.Auth;

namespace OrderClientApp.Infrastructure.Settings;

public sealed class SqliteAppSettingsRepository : IAppSettingsRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteAppSettingsRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AppSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT CompanyName, CompanyAddress, Theme, UpdatedAtUtc
            FROM AppSettings
            WHERE Id = 1
            LIMIT 1;
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new AppSettingsDto("自社", "-", 0m, "Light", DateTimeOffset.UnixEpoch);
        }

        return new AppSettingsDto(
            reader.GetString(0),
            reader.GetString(1),
            ApprovalThreshold: 0m,
            reader.GetString(2),
            DateTimeOffset.Parse(reader.GetString(3)));
    }

    public async Task<AppSettingsDto> UpsertAsync(
        string companyName,
        string companyAddress,
        string theme,
        DateTimeOffset updatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO AppSettings(Id, CompanyName, CompanyAddress, Theme, UpdatedAtUtc)
            VALUES(1, @companyName, @companyAddress, @theme, @updatedAtUtc)
            ON CONFLICT(Id) DO UPDATE SET
                CompanyName = excluded.CompanyName,
                CompanyAddress = excluded.CompanyAddress,
                Theme = excluded.Theme,
                UpdatedAtUtc = excluded.UpdatedAtUtc;
            """;
        command.Parameters.AddWithValue("@companyName", companyName);
        command.Parameters.AddWithValue("@companyAddress", companyAddress);
        command.Parameters.AddWithValue("@theme", theme);
        command.Parameters.AddWithValue("@updatedAtUtc", updatedAtUtc.ToUniversalTime().ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);

        return new AppSettingsDto(companyName, companyAddress, 0m, theme, updatedAtUtc.ToUniversalTime());
    }
}
