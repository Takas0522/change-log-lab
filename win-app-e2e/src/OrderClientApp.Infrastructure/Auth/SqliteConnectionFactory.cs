using Microsoft.Data.Sqlite;

namespace OrderClientApp.Infrastructure.Auth;

public sealed class SqliteConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(SqliteOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.DatabasePath))
        {
            throw new ArgumentException("Database path is required.", nameof(options));
        }

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = options.DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();
    }

    public SqliteConnection CreateConnection()
        => new(_connectionString);
}
