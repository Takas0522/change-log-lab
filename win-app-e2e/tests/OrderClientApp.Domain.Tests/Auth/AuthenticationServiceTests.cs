using OrderClientApp.Application.Abstractions.Auth;
using OrderClientApp.Application.Services.Auth;
using OrderClientApp.Domain.Auth;
using OrderClientApp.Infrastructure.Auth;

namespace OrderClientApp.Domain.Tests.Auth;

public class AuthenticationServiceTests
{
    [Fact]
    public async Task LoginAsync_ReturnsSuccess_WhenCredentialsAreValid()
    {
        var hasher = new Pbkdf2PasswordHasher();
        var hash = hasher.HashPassword("Test#12345");
        var user = new User(
            Guid.NewGuid(),
            "general.user",
            hash.Hash,
            hash.Salt,
            UserRole.General,
            isActive: true,
            failedLoginCount: 0,
            lockedUntilUtc: null,
            createdAtUtc: DateTimeOffset.UtcNow);
        var repository = new InMemoryUserRepository(user);
        var service = new AuthenticationService(repository, hasher);

        var result = await service.LoginAsync("general.user", "Test#12345");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.User);
        Assert.Equal(UserRole.General, result.User!.Role);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_ReturnsValidationError_WhenUsernameIsEmpty()
    {
        var service = new AuthenticationService(new InMemoryUserRepository(), new Pbkdf2PasswordHasher());

        var result = await service.LoginAsync("", "x");

        Assert.False(result.IsSuccess);
        Assert.Equal("ユーザー名を入力してください。", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_LocksAccount_WhenPasswordIsIncorrectFiveTimes()
    {
        var hasher = new Pbkdf2PasswordHasher();
        var hash = hasher.HashPassword("Correct#123");
        var user = new User(
            Guid.NewGuid(),
            "approver.user",
            hash.Hash,
            hash.Salt,
            UserRole.Approver,
            isActive: true,
            failedLoginCount: 0,
            lockedUntilUtc: null,
            createdAtUtc: DateTimeOffset.UtcNow);
        var repository = new InMemoryUserRepository(user);
        var service = new AuthenticationService(repository, hasher);

        for (var i = 0; i < 4; i++)
        {
            var failure = await service.LoginAsync("approver.user", "wrong");
            Assert.False(failure.IsSuccess);
            Assert.Equal("ユーザー名またはパスワードが正しくありません。", failure.ErrorMessage);
        }

        var locked = await service.LoginAsync("approver.user", "wrong");

        Assert.False(locked.IsSuccess);
        Assert.Equal("アカウントが一時ロックされています。管理者へ連絡してください。", locked.ErrorMessage);
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        private readonly Dictionary<string, User> _users;

        public InMemoryUserRepository(params User[] users)
        {
            _users = users.ToDictionary(x => x.Username, StringComparer.Ordinal);
        }

        public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            _users.TryGetValue(username, out var user);
            return Task.FromResult(user);
        }

        public Task<int> CountAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_users.Count);

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            _users[user.Username] = user;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            _users[user.Username] = user;
            return Task.CompletedTask;
        }
    }
}
