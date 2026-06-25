using OrderClientApp.Domain.Auth;

namespace OrderClientApp.Application.Abstractions.Auth;

public interface IAuthenticationService
{
    Task<AuthenticationResult> LoginAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default);
}
