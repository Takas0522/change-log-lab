using Xunit;
using Moq;
using MyApp.Services;
using MyApp.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyApp.Tests.Services
{
    /// <summary>
    /// Example unit tests for AuthorizationService demonstrating:
    /// - Decision table testing pattern (complex business rules)
    /// - State transition testing
    /// - Equivalence partitioning with business rules
    /// - Mock callback patterns
    /// - Exception testing with message validation
    /// </summary>
    public class AuthorizationServiceTests
    {
        private readonly Mock<IRoleRepository> _mockRoleRepository;
        private readonly Mock<IAuditLogger> _mockAuditLogger;
        private readonly AuthorizationService _service;

        public AuthorizationServiceTests()
        {
            _mockRoleRepository = new Mock<IRoleRepository>();
            _mockAuditLogger = new Mock<IAuditLogger>();
            
            _service = new AuthorizationService(
                _mockRoleRepository.Object,
                _mockAuditLogger.Object
            );
        }

        #region Decision Table Tests - Loan Approval Business Rules

        /*
        DECISION TABLE: Loan Approval Authorization
        
        Condition 1: Annual Income >= $50,000
        Condition 2: Credit Score >= 650
        Condition 3: Employment Duration >= 2 years
        Condition 4: No Recent Bankruptcies
        
        Expected Result Matrix:
        ─────────────────────────────────────────────────────────────────
        Income  | CreditScore | Employment | Bankruptcy | Authorized
        ─────────────────────────────────────────────────────────────────
        Yes     | Yes        | Yes        | No         | YES (Row 1)
        Yes     | Yes        | Yes        | Yes        | NO  (Row 2)
        Yes     | Yes        | No         | No         | NO  (Row 3)
        Yes     | No         | Yes        | No         | NO  (Row 4)
        No      | Yes        | Yes        | No         | NO  (Row 5)
        ─────────────────────────────────────────────────────────────────
        
        Implementation: One test per row (Decision Table Coverage)
        */

        [Theory]
        // Row 1: All conditions met → APPROVED
        [InlineData(60000, 700, 3, false, true)]
        
        // Row 2: All conditions except bankruptcy → DENIED (bankruptcy blocks approval)
        [InlineData(60000, 700, 3, true, false)]
        
        // Row 3: All conditions except employment → DENIED (need 2+ years)
        [InlineData(60000, 700, 1, false, false)]
        
        // Row 4: All conditions except credit → DENIED (need 650+)
        [InlineData(60000, 650, 3, false, true)]      // Boundary: exactly 650 (passes)
        [InlineData(60000, 649, 3, false, false)]     // Just below 650 (fails)
        
        // Row 5: All conditions except income → DENIED (need $50k+)
        [InlineData(50000, 700, 3, false, true)]      // Boundary: exactly 50k (passes)
        [InlineData(49999, 700, 3, false, false)]     // Just below 50k (fails)
        
        // Additional boundary tests
        [InlineData(100000, 800, 10, false, true)]    // Excellent all criteria
        [InlineData(50000, 650, 2, false, true)]      // Minimum all criteria
        public void AuthorizeLoad_WithVariousConditions_ReturnsExpectedApproval(
            decimal annualIncome,
            int creditScore,
            int employmentYearsMonths,
            bool hasRecentBankruptcy,
            bool expectedAuthorized)
        {
            // Arrange
            var applicant = new LoanApplicant
            {
                AnnualIncome = annualIncome,
                CreditScore = creditScore,
                EmploymentDurationYears = employmentYearsMonths,
                HasRecentBankruptcy = hasRecentBankruptcy
            };

            // Act
            var result = _service.AuthorizeLoan(applicant);

            // Assert
            Assert.Equal(expectedAuthorized, result.IsAuthorized);
            
            // Verify audit log was created
            _mockAuditLogger.Verify(
                l => l.LogDecision(It.IsAny<string>(), It.IsAny<bool>()),
                Times.Once
            );
        }

        #endregion

        #region State Transition Tests - Permission State Changes

        [Fact]
        public void GrantPermission_FromNoPermission_TransitionsToGranted()
        {
            // Arrange: User starts with no permission
            var userId = 1;
            var permission = "DELETE_USER";
            
            _mockRoleRepository
                .Setup(r => r.HasPermission(userId, permission))
                .Returns(false)
                .Verifiable();

            // Act: Grant permission
            _service.GrantPermission(userId, permission);

            // Assert: Verify transition occurred
            var hasPermissionAfter = _service.CheckPermission(userId, permission);
            Assert.True(hasPermissionAfter);
            
            // Verify repository was called
            _mockRoleRepository.Verify();
        }

        [Fact]
        public void RevokePermission_FromGranted_TransitionsToRevoked()
        {
            // Arrange: User has permission
            var userId = 1;
            var permission = "EDIT_REPORT";
            
            _mockRoleRepository
                .Setup(r => r.HasPermission(userId, permission))
                .Returns(true);
            
            _mockRoleRepository
                .Setup(r => r.RevokePermission(userId, permission))
                .Returns(true);

            // Act: Revoke permission
            var result = _service.RevokePermission(userId, permission);

            // Assert
            Assert.True(result);
            
            // Verify the state changed
            _mockRoleRepository.Verify(
                r => r.RevokePermission(userId, permission),
                Times.Once
            );
        }

        [Fact]
        public void RevokePermission_OfAdminRole_ThrowsUnauthorizedAccessException()
        {
            // Arrange: Attempting to revoke ADMIN permission
            var userId = 1;
            var adminPermission = "ADMIN";
            
            // Act & Assert: System should prevent revoking critical permissions
            var ex = Assert.Throws<UnauthorizedAccessException>(
                () => _service.RevokePermission(userId, adminPermission)
            );
            
            Assert.Contains("ADMIN permission", ex.Message);
        }

        #endregion

        #region Equivalence Partitioning - Role-Based Authorization

        [Theory]
        // Valid roles (should be authorized for standard operations)
        [InlineData("ADMIN", true)]
        [InlineData("MODERATOR", true)]
        [InlineData("USER", false)]
        
        // Invalid roles (should throw or deny)
        [InlineData("", false)]
        [InlineData("UNKNOWN_ROLE", false)]
        public void IsUserAuthorizedForOperation_WithVariousRoles_ReturnsExpected(
            string role, bool isAuthorizedForDelete)
        {
            // Arrange
            var user = new User { Id = 1, Role = role };
            
            _mockRoleRepository
                .Setup(r => r.GetRole(role))
                .Returns(new Role { Name = role, IsAdmin = role == "ADMIN" });

            // Act
            var result = _service.IsAuthorized(user, "DELETE_POST");

            // Assert
            if (role == "" || role == "UNKNOWN_ROLE")
            {
                Assert.False(result);
            }
            else
            {
                Assert.Equal(isAuthorizedForDelete, result);
            }
        }

        #endregion

        #region Boundary Value Testing - Authorization Levels

        [Theory]
        // Boundary: Age at which authorization changes
        [InlineData(17, false)]     // Under 18: NOT authorized
        [InlineData(18, true)]      // Exactly 18: Authorized
        [InlineData(19, true)]      // Over 18: Authorized
        
        // Boundary: Approval threshold
        [InlineData(99, false)]     // Just below 100: NOT authorized
        [InlineData(100, true)]     // Exactly 100: Authorized
        [InlineData(101, true)]     // Above 100: Authorized
        public void IsAuthorizedByAge_AtBoundaries_ReturnsCorrectly(int age, bool expected)
        {
            // Arrange
            var applicant = new Applicant { Age = age };

            // Act
            var result = _service.IsAuthorizedByAge(applicant);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Callback Testing - Audit Trail Verification

        [Fact]
        public void GrantPermission_WithCallback_CapturesAuditDetails()
        {
            // Arrange: Setup mock with callback to capture audit data
            string capturedAction = null;
            string capturedPermission = null;
            DateTime capturedTimestamp = DateTime.MinValue;
            
            _mockAuditLogger
                .Setup(l => l.LogAction(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
                .Callback<string, string, DateTime>((action, perm, time) =>
                {
                    capturedAction = action;
                    capturedPermission = perm;
                    capturedTimestamp = time;
                });

            // Act
            _service.GrantPermission(1, "VIEW_REPORTS");

            // Assert: Verify callback captured correct values
            Assert.Equal("GRANT", capturedAction);
            Assert.Equal("VIEW_REPORTS", capturedPermission);
            Assert.True(capturedTimestamp > DateTime.MinValue);
        }

        #endregion

        #region Complex Scenario Tests

        [Fact]
        public void AuthorizeAction_WithMultipleConditions_ChecksAllCriteria()
        {
            // Arrange: Create a user with specific context
            var user = new User { Id = 1, Role = "USER", DepartmentId = 5 };
            var action = "APPROVE_BUDGET";
            
            // Setup multiple conditions
            _mockRoleRepository
                .Setup(r => r.HasPermission(user.Id, action))
                .Returns(true);
            
            _mockRoleRepository
                .Setup(r => r.IsInDepartment(user.DepartmentId, 5))
                .Returns(true);
            
            _mockAuditLogger
                .Setup(l => l.LogDecision(It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(true);

            // Act
            var result = _service.AuthorizeAction(user, action);

            // Assert: Verify all checks passed
            Assert.True(result);
            
            // Verify all conditions were checked
            _mockRoleRepository.Verify(r => r.HasPermission(user.Id, action), Times.Once);
            _mockRoleRepository.Verify(r => r.IsInDepartment(user.DepartmentId, 5), Times.Once);
        }

        [Fact]
        public void AuthorizeAction_WhenOneConditionFails_DeniesAccess()
        {
            // Arrange: One condition will fail
            var user = new User { Id = 1, Role = "USER", DepartmentId = 5 };
            var action = "APPROVE_BUDGET";
            
            _mockRoleRepository
                .Setup(r => r.HasPermission(user.Id, action))
                .Returns(true);
            
            _mockRoleRepository
                .Setup(r => r.IsInDepartment(user.DepartmentId, 5))
                .Returns(false);  // This condition fails

            // Act
            var result = _service.AuthorizeAction(user, action);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Exception Testing with Details

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void CheckPermission_WithInvalidPermissionName_ThrowsArgumentException(string permissionName)
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(
                () => _service.CheckPermission(1, permissionName)
            );
            
            // Verify exception details
            Assert.Contains("permission", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("invalid", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void AuthorizeCriticalOperation_WithUnauthorizedUser_ThrowsUnauthorizedAccessException()
        {
            // Arrange: User without admin role
            var user = new User { Id = 1, Role = "USER" };
            
            _mockRoleRepository
                .Setup(r => r.IsAdmin(user.Id))
                .Returns(false);

            // Act & Assert
            var ex = Assert.Throws<UnauthorizedAccessException>(
                () => _service.AuthorizeCriticalOperation(user)
            );
            
            // Verify detailed exception message
            Assert.Contains("ADMIN", ex.Message);
            Assert.Contains(user.Id.ToString(), ex.Message);
        }

        #endregion
    }
}
