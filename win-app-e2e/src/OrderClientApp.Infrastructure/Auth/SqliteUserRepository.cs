using Microsoft.Data.Sqlite;
using OrderClientApp.Application.Abstractions.Auth;
using OrderClientApp.Domain.Auth;

namespace OrderClientApp.Infrastructure.Auth;

public sealed class SqliteUserRepository : IUserRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteUserRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Username, PasswordHash, PasswordSalt, Role, IsActive, FailedLoginCount, LockedUntilUtc, CreatedAtUtc
            FROM Users
            WHERE Username = @username
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@username", username);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var lockedUntilRaw = reader.GetValue(7);
        DateTimeOffset? lockedUntilUtc = lockedUntilRaw is DBNull
            ? null
            : DateTimeOffset.Parse((string)lockedUntilRaw);

        return new User(
            Guid.Parse(reader.GetString(0)),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            Enum.Parse<UserRole>(reader.GetString(4)),
            reader.GetInt64(5) == 1,
            Convert.ToInt32(reader.GetInt64(6)),
            lockedUntilUtc,
            DateTimeOffset.Parse(reader.GetString(8)));
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM Users;";
        var result = await command.ExecuteScalarAsync(cancellationToken);

        return Convert.ToInt32(result);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Users
            (Id, Username, PasswordHash, PasswordSalt, Role, IsActive, FailedLoginCount, LockedUntilUtc, CreatedAtUtc)
            VALUES
            (@id, @username, @passwordHash, @passwordSalt, @role, @isActive, @failedLoginCount, @lockedUntilUtc, @createdAtUtc);
            """;
        AddUserParameters(command, user);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Users
            SET
                PasswordHash = @passwordHash,
                PasswordSalt = @passwordSalt,
                Role = @role,
                IsActive = @isActive,
                FailedLoginCount = @failedLoginCount,
                LockedUntilUtc = @lockedUntilUtc
            WHERE Id = @id;
            """;
        AddUserParameters(command, user);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddUserParameters(SqliteCommand command, User user)
    {
        command.Parameters.AddWithValue("@id", user.Id.ToString());
        command.Parameters.AddWithValue("@username", user.Username);
        command.Parameters.AddWithValue("@passwordHash", user.PasswordHash);
        command.Parameters.AddWithValue("@passwordSalt", user.PasswordSalt);
        command.Parameters.AddWithValue("@role", user.Role.ToString());
        command.Parameters.AddWithValue("@isActive", user.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("@failedLoginCount", user.FailedLoginCount);
        command.Parameters.AddWithValue(
            "@lockedUntilUtc",
            user.LockedUntilUtc?.ToUniversalTime().ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@createdAtUtc", user.CreatedAtUtc.ToUniversalTime().ToString("O"));
    }
}
