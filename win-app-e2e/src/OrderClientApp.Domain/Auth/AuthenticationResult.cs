namespace OrderClientApp.Domain.Auth;

public sealed record AuthenticationResult(
    bool IsSuccess,
    AuthenticatedUser? User,
    string? ErrorMessage)
{
    public static AuthenticationResult Success(AuthenticatedUser user)
        => new(true, user, null);

    public static AuthenticationResult Failure(string errorMessage)
        => new(false, null, errorMessage);
}
