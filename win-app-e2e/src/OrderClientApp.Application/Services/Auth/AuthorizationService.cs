using OrderClientApp.Application.Abstractions.Auth;
using OrderClientApp.Domain.Auth;

namespace OrderClientApp.Application.Services.Auth;

public sealed class AuthorizationService : IAuthorizationService
{
    public bool HasRoleOrAbove(UserRole actualRole, UserRole requiredRole)
        => actualRole >= requiredRole;

    public bool CanAccess(AuthenticatedUser user, UserRole requiredRole)
    {
        ArgumentNullException.ThrowIfNull(user);
        return HasRoleOrAbove(user.Role, requiredRole);
    }
}
