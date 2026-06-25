using OrderClientApp.Application.Abstractions.Auth;
using OrderClientApp.Domain.Auth;

namespace OrderClientApp.Application.Services.Auth;

public sealed class AuthenticationService : IAuthenticationService
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly TimeProvider _timeProvider;

    public AuthenticationService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        TimeProvider? timeProvider = null)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<AuthenticationResult> LoginAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return AuthenticationResult.Failure("ユーザー名を入力してください。");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return AuthenticationResult.Failure("パスワードを入力してください。");
        }

        var normalizedUsername = username.Trim();
        var user = await _userRepository.GetByUsernameAsync(normalizedUsername, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return AuthenticationResult.Failure("ユーザー名またはパスワードが正しくありません。");
        }

        var nowUtc = _timeProvider.GetUtcNow();
        if (user.IsLocked(nowUtc))
        {
            return AuthenticationResult.Failure("アカウントが一時ロックされています。管理者へ連絡してください。");
        }

        if (!_passwordHasher.VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
        {
            user.RegisterFailedLogin(MaxFailedAttempts, LockoutDuration, nowUtc);
            await _userRepository.UpdateAsync(user, cancellationToken);

            if (user.IsLocked(nowUtc))
            {
                return AuthenticationResult.Failure("アカウントが一時ロックされています。管理者へ連絡してください。");
            }

            return AuthenticationResult.Failure("ユーザー名またはパスワードが正しくありません。");
        }

        user.ResetFailedLogin();
        await _userRepository.UpdateAsync(user, cancellationToken);

        return AuthenticationResult.Success(new AuthenticatedUser(user.Id, user.Username, user.Role));
    }
}
