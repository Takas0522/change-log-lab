namespace OrderClientApp.Domain.Auth;

public sealed class User
{
    public User(
        Guid id,
        string username,
        string passwordHash,
        string passwordSalt,
        UserRole role,
        bool isActive,
        int failedLoginCount,
        DateTimeOffset? lockedUntilUtc,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        Username = string.IsNullOrWhiteSpace(username)
            ? throw new ArgumentException("Username is required.", nameof(username))
            : username;
        PasswordHash = string.IsNullOrWhiteSpace(passwordHash)
            ? throw new ArgumentException("Password hash is required.", nameof(passwordHash))
            : passwordHash;
        PasswordSalt = string.IsNullOrWhiteSpace(passwordSalt)
            ? throw new ArgumentException("Password salt is required.", nameof(passwordSalt))
            : passwordSalt;
        Role = role;
        IsActive = isActive;
        FailedLoginCount = failedLoginCount < 0
            ? throw new ArgumentOutOfRangeException(nameof(failedLoginCount))
            : failedLoginCount;
        LockedUntilUtc = lockedUntilUtc;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }

    public string Username { get; }

    public string PasswordHash { get; }

    public string PasswordSalt { get; }

    public UserRole Role { get; }

    public bool IsActive { get; }

    public int FailedLoginCount { get; private set; }

    public DateTimeOffset? LockedUntilUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public bool IsLocked(DateTimeOffset nowUtc)
        => LockedUntilUtc is { } lockedUntilUtc && lockedUntilUtc > nowUtc;

    public void RegisterFailedLogin(int maxFailedAttempts, TimeSpan lockoutDuration, DateTimeOffset nowUtc)
    {
        if (maxFailedAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxFailedAttempts));
        }

        if (lockoutDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(lockoutDuration));
        }

        FailedLoginCount++;
        if (FailedLoginCount >= maxFailedAttempts)
        {
            LockedUntilUtc = nowUtc.Add(lockoutDuration);
            FailedLoginCount = 0;
        }
    }

    public void ResetFailedLogin()
    {
        FailedLoginCount = 0;
        LockedUntilUtc = null;
    }
}
