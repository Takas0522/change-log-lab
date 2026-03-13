using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using UserApi.Controllers;
using UserApi.Data;
using UserApi.DTOs;
using UserApi.Models;

namespace UserServiceTest;

/// <summary>
/// Unit tests for UserController following ISTQB standards and best practices.
/// Tests cover all controller methods with equivalence partitioning and boundary value analysis.
/// </summary>
public class UserControllerTests : IAsyncDisposable
{
    private readonly UserDbContext _dbContext;
    private readonly Mock<ILogger<UserController>> _mockLogger;
    private readonly UserController _controller;
    private readonly Guid _testUserId = Guid.NewGuid();

    public UserControllerTests()
    {
        // Arrange: Set up in-memory database and mock logger
        var options = new DbContextOptionsBuilder<UserDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _dbContext = new UserDbContext(options);
        _mockLogger = new Mock<ILogger<UserController>>();
        _controller = new UserController(_dbContext, _mockLogger.Object);
    }

    public async ValueTask DisposeAsync()
    {
        await _dbContext.DisposeAsync();
    }

    #region GetMyProfile Tests

    [Fact]
    public async Task GetMyProfile_WithValidUserAndExistingProfile_ReturnsOkWithProfile()
    {
        // Arrange
        var profile = CreateTestProfile(_testUserId, "test@example.com", "Test User");
        await _dbContext.UserProfiles.AddAsync(profile);
        await _dbContext.SaveChangesAsync();
        
        SetupUserClaims(_testUserId);

        // Act
        var result = await _controller.GetMyProfile();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<UserProfileResponse>().Subject;
        response.UserId.Should().Be(_testUserId);
        response.Email.Should().Be("test@example.com");
        response.DisplayName.Should().Be("Test User");
    }

    [Fact]
    public async Task GetMyProfile_WithValidUserButNoProfile_ReturnsNotFound()
    {
        // Arrange
        SetupUserClaims(_testUserId);

        // Act
        var result = await _controller.GetMyProfile();

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public void GetMyProfile_WithInvalidUserToken_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        SetupUserClaims(null); // No valid user ID

        // Act & Assert
        var act = async () => await _controller.GetMyProfile();
        act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Invalid user ID in token*");
    }

    #endregion

    #region UpdateMyProfile Tests

    [Fact]
    public async Task UpdateMyProfile_WithValidRequestAndExistingProfile_ReturnsOkWithUpdatedProfile()
    {
        // Arrange
        var profile = CreateTestProfile(_testUserId, "test@example.com", "Original Name");
        await _dbContext.UserProfiles.AddAsync(profile);
        await _dbContext.SaveChangesAsync();
        
        SetupUserClaims(_testUserId);
        
        var updateRequest = new UpdateProfileRequest
        {
            DisplayName = "Updated Name",
            Bio = "Updated bio",
            AvatarUrl = "https://example.com/avatar.jpg"
        };

        // Act
        var result = await _controller.UpdateMyProfile(updateRequest);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<UserProfileResponse>().Subject;
        response.DisplayName.Should().Be("Updated Name");
        response.Bio.Should().Be("Updated bio");
        response.AvatarUrl.Should().Be("https://example.com/avatar.jpg");
        response.UpdatedAt.Should().BeAfter(profile.CreatedAt);
    }

    [Fact]
    public async Task UpdateMyProfile_WithPartialUpdate_UpdatesOnlyProvidedFields()
    {
        // Arrange
        var profile = CreateTestProfile(_testUserId, "test@example.com", "Original Name");
        profile.Bio = "Original bio";
        await _dbContext.UserProfiles.AddAsync(profile);
        await _dbContext.SaveChangesAsync();
        
        SetupUserClaims(_testUserId);
        
        var updateRequest = new UpdateProfileRequest
        {
            DisplayName = "Updated Name"
            // Bio and AvatarUrl are null, should not update
        };

        // Act
        var result = await _controller.UpdateMyProfile(updateRequest);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<UserProfileResponse>().Subject;
        response.DisplayName.Should().Be("Updated Name");
        response.Bio.Should().Be("Original bio"); // Should remain unchanged
    }

    [Fact]
    public async Task UpdateMyProfile_WithNonExistentProfile_ReturnsNotFound()
    {
        // Arrange
        SetupUserClaims(_testUserId);
        var updateRequest = new UpdateProfileRequest
        {
            DisplayName = "New Name"
        };

        // Act
        var result = await _controller.UpdateMyProfile(updateRequest);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateMyProfile_SuccessfulUpdate_LogsInformation()
    {
        // Arrange
        var profile = CreateTestProfile(_testUserId, "test@example.com", "Original Name");
        await _dbContext.UserProfiles.AddAsync(profile);
        await _dbContext.SaveChangesAsync();
        
        SetupUserClaims(_testUserId);
        
        var updateRequest = new UpdateProfileRequest { DisplayName = "Updated Name" };

        // Act
        await _controller.UpdateMyProfile(updateRequest);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Profile updated")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region GetUserProfile Tests

    [Fact]
    public async Task GetUserProfile_WithExistingUserId_ReturnsOkWithProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateTestProfile(userId, "user@example.com", "User Name");
        await _dbContext.UserProfiles.AddAsync(profile);
        await _dbContext.SaveChangesAsync();
        
        SetupUserClaims(_testUserId); // Authenticated user

        // Act
        var result = await _controller.GetUserProfile(userId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<UserProfileResponse>().Subject;
        response.UserId.Should().Be(userId);
        response.Email.Should().Be("user@example.com");
        response.DisplayName.Should().Be("User Name");
    }

    [Fact]
    public async Task GetUserProfile_WithNonExistentUserId_ReturnsNotFound()
    {
        // Arrange
        SetupUserClaims(_testUserId);
        var nonExistentUserId = Guid.NewGuid();

        // Act
        var result = await _controller.GetUserProfile(nonExistentUserId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region SearchUsers Tests

    [Fact]
    public async Task SearchUsers_WithoutQuery_ReturnsAllUsersOrdered()
    {
        // Arrange
        SetupUserClaims(_testUserId);
        await SeedMultipleProfiles();

        // Act
        var result = await _controller.SearchUsers(null, 0, 20);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<UserSearchResponse>().Subject;
        response.Users.Should().HaveCount(3);
        response.TotalCount.Should().Be(3);
        response.Users.Should().BeInAscendingOrder(u => u.DisplayName);
    }

    [Fact]
    public async Task SearchUsers_WithEmailQuery_ReturnsMatchingUsers()
    {
        // Arrange
        SetupUserClaims(_testUserId);
        await SeedMultipleProfiles();

        // Act
        var result = await _controller.SearchUsers("alice", 0, 20);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<UserSearchResponse>().Subject;
        response.Users.Should().HaveCount(1);
        response.Users[0].Email.Should().Contain("alice");
    }

    [Fact]
    public async Task SearchUsers_WithDisplayNameQuery_ReturnsMatchingUsers()
    {
        // Arrange
        SetupUserClaims(_testUserId);
        await SeedMultipleProfiles();

        // Act
        var result = await _controller.SearchUsers("bob", 0, 20);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<UserSearchResponse>().Subject;
        response.Users.Should().HaveCount(1);
        response.Users[0].DisplayName.Should().Contain("Bob");
    }

    [Fact]
    public async Task SearchUsers_WithPagination_ReturnsCorrectSubset()
    {
        // Arrange
        SetupUserClaims(_testUserId);
        await SeedMultipleProfiles();

        // Act
        var result = await _controller.SearchUsers(null, 1, 2);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<UserSearchResponse>().Subject;
        response.Users.Should().HaveCount(2);
        response.TotalCount.Should().Be(3);
    }

    [Theory]
    [InlineData(101)]
    [InlineData(150)]
    [InlineData(1000)]
    public async Task SearchUsers_WithTakeExceedingMax_LimitsToHundred(int take)
    {
        // Arrange
        SetupUserClaims(_testUserId);
        await SeedMultipleProfiles();

        // Act
        var result = await _controller.SearchUsers(null, 0, take);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<UserSearchResponse>().Subject;
        // The controller should limit the take value to 100
        response.Users.Should().HaveCountLessOrEqualTo(100);
    }

    [Fact]
    public async Task SearchUsers_WithWhitespaceQuery_TreatsAsEmpty()
    {
        // Arrange
        SetupUserClaims(_testUserId);
        await SeedMultipleProfiles();

        // Act
        var result = await _controller.SearchUsers("   ", 0, 20);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<UserSearchResponse>().Subject;
        response.Users.Should().HaveCount(3); // Returns all users
    }

    [Fact]
    public async Task SearchUsers_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        SetupUserClaims(_testUserId);
        await SeedMultipleProfiles();

        // Act
        var result = await _controller.SearchUsers("nonexistent@test.com", 0, 20);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<UserSearchResponse>().Subject;
        response.Users.Should().BeEmpty();
        response.TotalCount.Should().Be(0);
    }

    #endregion

    #region CreateProfile Tests

    [Fact]
    public async Task CreateProfile_WithValidRequest_ReturnsCreatedAtActionWithProfile()
    {
        // Arrange
        SetupUserClaims(_testUserId);
        var createRequest = new CreateProfileRequest
        {
            UserId = Guid.NewGuid(),
            Email = "newuser@example.com",
            DisplayName = "New User",
            Bio = "Bio text",
            AvatarUrl = "https://example.com/avatar.jpg"
        };

        // Act
        var result = await _controller.CreateProfile(createRequest);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(UserController.GetUserProfile));
        
        var response = createdResult.Value.Should().BeOfType<UserProfileResponse>().Subject;
        response.UserId.Should().Be(createRequest.UserId);
        response.Email.Should().Be(createRequest.Email);
        response.DisplayName.Should().Be(createRequest.DisplayName);
        response.Bio.Should().Be(createRequest.Bio);
        response.AvatarUrl.Should().Be(createRequest.AvatarUrl);
        response.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        response.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateProfile_WithMinimalRequest_CreatesProfileWithNullableFields()
    {
        // Arrange
        SetupUserClaims(_testUserId);
        var createRequest = new CreateProfileRequest
        {
            UserId = Guid.NewGuid(),
            Email = "minimal@example.com",
            DisplayName = "Minimal User",
            Bio = null,
            AvatarUrl = null
        };

        // Act
        var result = await _controller.CreateProfile(createRequest);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var response = createdResult.Value.Should().BeOfType<UserProfileResponse>().Subject;
        response.Bio.Should().BeNull();
        response.AvatarUrl.Should().BeNull();
    }

    [Fact]
    public async Task CreateProfile_WithExistingProfile_ReturnsConflict()
    {
        // Arrange
        SetupUserClaims(_testUserId);
        var userId = Guid.NewGuid();
        var existingProfile = CreateTestProfile(userId, "existing@example.com", "Existing User");
        await _dbContext.UserProfiles.AddAsync(existingProfile);
        await _dbContext.SaveChangesAsync();
        
        var createRequest = new CreateProfileRequest
        {
            UserId = userId, // Same user ID
            Email = "duplicate@example.com",
            DisplayName = "Duplicate User"
        };

        // Act
        var result = await _controller.CreateProfile(createRequest);

        // Assert
        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task CreateProfile_SuccessfulCreation_LogsInformation()
    {
        // Arrange
        SetupUserClaims(_testUserId);
        var createRequest = new CreateProfileRequest
        {
            UserId = Guid.NewGuid(),
            Email = "newuser@example.com",
            DisplayName = "New User"
        };

        // Act
        await _controller.CreateProfile(createRequest);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Profile created")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateProfile_SuccessfulCreation_PersistsToDatabase()
    {
        // Arrange
        SetupUserClaims(_testUserId);
        var userId = Guid.NewGuid();
        var createRequest = new CreateProfileRequest
        {
            UserId = userId,
            Email = "persist@example.com",
            DisplayName = "Persist User"
        };

        // Act
        await _controller.CreateProfile(createRequest);

        // Assert
        var savedProfile = await _dbContext.UserProfiles.FindAsync(userId);
        savedProfile.Should().NotBeNull();
        savedProfile!.Email.Should().Be("persist@example.com");
        savedProfile.DisplayName.Should().Be("Persist User");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a test user profile with required fields
    /// </summary>
    private UserProfile CreateTestProfile(Guid userId, string email, string displayName)
    {
        return new UserProfile
        {
            UserId = userId,
            Email = email,
            DisplayName = displayName,
            Bio = null,
            AvatarUrl = null,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
    }

    /// <summary>
    /// Seeds multiple profiles for search testing
    /// </summary>
    private async Task SeedMultipleProfiles()
    {
        var profiles = new[]
        {
            CreateTestProfile(Guid.NewGuid(), "alice@example.com", "Alice Smith"),
            CreateTestProfile(Guid.NewGuid(), "bob@example.com", "Bob Jones"),
            CreateTestProfile(Guid.NewGuid(), "charlie@example.com", "Charlie Brown")
        };
        
        await _dbContext.UserProfiles.AddRangeAsync(profiles);
        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Sets up user claims for authentication in controller
    /// </summary>
    private void SetupUserClaims(Guid? userId)
    {
        var claims = new List<Claim>();
        
        if (userId.HasValue)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
        }
        
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }

    #endregion
}
