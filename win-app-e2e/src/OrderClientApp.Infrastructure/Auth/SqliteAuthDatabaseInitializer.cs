using OrderClientApp.Application.Abstractions.Auth;
using OrderClientApp.Domain.Auth;

namespace OrderClientApp.Infrastructure.Auth;

public sealed class SqliteAuthDatabaseInitializer : IAuthDatabaseInitializer
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public SqliteAuthDatabaseInitializer(
        SqliteConnectionFactory connectionFactory,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)
    {
        _connectionFactory = connectionFactory;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Users
            (
                Id TEXT NOT NULL PRIMARY KEY,
                Username TEXT NOT NULL UNIQUE,
                PasswordHash TEXT NOT NULL,
                PasswordSalt TEXT NOT NULL,
                Role TEXT NOT NULL,
                IsActive INTEGER NOT NULL,
                FailedLoginCount INTEGER NOT NULL DEFAULT 0,
                LockedUntilUtc TEXT NULL,
                CreatedAtUtc TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_Users_Username ON Users (Username);
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);

        if (await _userRepository.CountAsync(cancellationToken) > 0)
        {
            return;
        }

        await SeedUsersAsync(cancellationToken);
    }

    private async Task SeedUsersAsync(CancellationToken cancellationToken)
    {
        var nowUtc = DateTimeOffset.UtcNow;

        await _userRepository.AddAsync(CreateSeedUser("general.user", "General#2026", UserRole.General, nowUtc), cancellationToken);
        await _userRepository.AddAsync(CreateSeedUser("approver.user", "Approver#2026", UserRole.Approver, nowUtc), cancellationToken);
        await _userRepository.AddAsync(CreateSeedUser("admin.user", "Admin#2026", UserRole.Admin, nowUtc), cancellationToken);
    }

    private User CreateSeedUser(string username, string password, UserRole role, DateTimeOffset nowUtc)
    {
        var passwordHash = _passwordHasher.HashPassword(password);
        return new User(
            Guid.NewGuid(),
            username,
            passwordHash.Hash,
            passwordHash.Salt,
            role,
            isActive: true,
            failedLoginCount: 0,
            lockedUntilUtc: null,
            createdAtUtc: nowUtc);
    }
}
