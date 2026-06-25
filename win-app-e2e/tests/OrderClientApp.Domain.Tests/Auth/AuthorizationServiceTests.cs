using OrderClientApp.Application.Services.Auth;
using OrderClientApp.Domain.Auth;

namespace OrderClientApp.Domain.Tests.Auth;

public class AuthorizationServiceTests
{
    private readonly AuthorizationService _service = new();

    [Theory]
    [InlineData(UserRole.General, UserRole.General, true)]
    [InlineData(UserRole.General, UserRole.Approver, false)]
    [InlineData(UserRole.Approver, UserRole.General, true)]
    [InlineData(UserRole.Admin, UserRole.Approver, true)]
    [InlineData(UserRole.Approver, UserRole.Admin, false)]
    public void HasRoleOrAbove_WorksAsExpected(UserRole actual, UserRole required, bool expected)
    {
        var result = _service.HasRoleOrAbove(actual, required);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void CanAccess_ReturnsTrueForAdmin()
    {
        var user = new AuthenticatedUser(Guid.NewGuid(), "admin.user", UserRole.Admin);

        var result = _service.CanAccess(user, UserRole.Admin);

        Assert.True(result);
    }
}
