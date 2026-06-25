using OrderClientApp.Domain.Auth;

namespace OrderClientApp.Application.Abstractions.Auth;

public interface IAuthorizationService
{
    bool HasRoleOrAbove(UserRole actualRole, UserRole requiredRole);

    bool CanAccess(AuthenticatedUser user, UserRole requiredRole);
}
