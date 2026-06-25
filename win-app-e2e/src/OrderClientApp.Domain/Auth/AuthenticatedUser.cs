namespace OrderClientApp.Domain.Auth;

public sealed record AuthenticatedUser(
    Guid UserId,
    string Username,
    UserRole Role);
