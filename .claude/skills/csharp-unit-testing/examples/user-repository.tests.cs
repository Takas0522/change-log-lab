using Xunit;
using Moq;
using MyApp.Data;
using MyApp.Models;
using MyApp.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyApp.Tests.Services
{
    /// <summary>
    /// Example unit tests for UserRepository demonstrating:
    /// - Dependency injection and mocking
    /// - Test doubles (Mock, Stub)
    /// - Verification of method calls
    /// - Testing both success and error paths
    /// - Async operations testing
    /// </summary>
    public class UserRepositoryTests
    {
        private readonly Mock<IUserDataContext> _mockContext;
        private readonly UserRepository _repository;

        public UserRepositoryTests()
        {
            // Arrange: Create mock dependency
            _mockContext = new Mock<IUserDataContext>();
            
            // Inject mock into service under test
            _repository = new UserRepository(_mockContext.Object);
        }

        #region GetUser Tests

        [Fact]
        public void GetUser_WithValidId_ReturnsUser()
        {
            // Arrange: Setup mock to return a test user
            var userId = 5;
            var expectedUser = new User { Id = userId, Name = "John Doe", Email = "john@example.com" };
            
            _mockContext
                .Setup(c => c.GetUser(userId))
                .Returns(expectedUser);

            // Act: Call method under test
            var result = _repository.GetUser(userId);

            // Assert: Verify result
            Assert.NotNull(result);
            Assert.Equal(expectedUser.Id, result.Id);
            Assert.Equal(expectedUser.Name, result.Name);
            
            // Verify the mock was called with correct parameters
            _mockContext.Verify(c => c.GetUser(userId), Times.Once);
        }

        [Fact]
        public void GetUser_WithNegativeId_ThrowsArgumentException()
        {
            // Arrange: Setup mock to throw exception for invalid input
            _mockContext
                .Setup(c => c.GetUser(It.Is<int>(id => id < 0)))
                .Throws<ArgumentException>();

            // Act & Assert: Combined for exception tests
            var ex = Assert.Throws<ArgumentException>(
                () => _repository.GetUser(-1)
            );
            
            Assert.Contains("ID", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void GetUser_WithNonExistentId_ReturnsNull()
        {
            // Arrange: Setup mock to return null (user not found)
            _mockContext
                .Setup(c => c.GetUser(It.IsAny<int>()))
                .Returns((User)null);

            // Act
            var result = _repository.GetUser(999);

            // Assert
            Assert.Null(result);
            
            // Verify the repository called the context
            _mockContext.Verify(c => c.GetUser(999), Times.Once);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(int.MaxValue)]
        public void GetUser_WithVariousValidIds_CallsContextWithCorrectId(int userId)
        {
            // Arrange
            _mockContext
                .Setup(c => c.GetUser(It.IsAny<int>()))
                .Returns(new User { Id = userId });

            // Act
            var result = _repository.GetUser(userId);

            // Assert: Verify the exact ID was passed to context
            _mockContext.Verify(
                c => c.GetUser(It.Is<int>(id => id == userId)),
                Times.Once
            );
        }

        #endregion

        #region GetAllUsers Tests

        [Fact]
        public void GetAllUsers_WithExistingUsers_ReturnsAll()
        {
            // Arrange: Setup mock to return multiple users
            var users = new List<User>
            {
                new User { Id = 1, Name = "Alice" },
                new User { Id = 2, Name = "Bob" },
                new User { Id = 3, Name = "Charlie" }
            };
            
            _mockContext
                .Setup(c => c.GetAllUsers())
                .Returns(users);

            // Act
            var result = _repository.GetAllUsers();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Contains(users[0], result);
            Assert.Contains(users[1], result);
            
            // Verify the correct method was called
            _mockContext.Verify(c => c.GetAllUsers(), Times.Once);
        }

        [Fact]
        public void GetAllUsers_WithNoUsers_ReturnsEmptyList()
        {
            // Arrange: Setup mock to return empty list
            _mockContext
                .Setup(c => c.GetAllUsers())
                .Returns(new List<User>());

            // Act
            var result = _repository.GetAllUsers();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region SaveUser Tests

        [Fact]
        public void SaveUser_WithValidUser_CallsContextSave()
        {
            // Arrange: Create a valid user
            var user = new User { Id = 1, Name = "Test User", Email = "test@example.com" };
            
            _mockContext
                .Setup(c => c.SaveUser(It.IsAny<User>()))
                .Returns(user);

            // Act
            var result = _repository.SaveUser(user);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
            
            // Verify SaveUser was called with the correct user
            _mockContext.Verify(
                c => c.SaveUser(It.Is<User>(u => u.Name == "Test User")),
                Times.Once
            );
        }

        [Fact]
        public void SaveUser_WithNullUser_ThrowsArgumentNullException()
        {
            // Arrange: Don't setup anything - we expect an exception

            // Act & Assert
            Assert.Throws<ArgumentNullException>(
                () => _repository.SaveUser(null)
            );
            
            // Verify context was never called
            _mockContext.Verify(c => c.SaveUser(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public void SaveUser_WithInvalidEmail_ThrowsArgumentException()
        {
            // Arrange: Create user with invalid email
            var user = new User { Id = 1, Name = "Test", Email = "invalid-email" };
            
            _mockContext
                .Setup(c => c.SaveUser(It.Is<User>(u => !u.Email.Contains("@"))))
                .Throws<ArgumentException>();

            // Act & Assert
            Assert.Throws<ArgumentException>(
                () => _repository.SaveUser(user)
            );
        }

        [Fact]
        public void SaveUser_WithDuplicate_ThrowsDuplicateException()
        {
            // Arrange: Setup mock to throw duplicate exception
            var user = new User { Id = 1, Name = "Test", Email = "test@example.com" };
            
            _mockContext
                .Setup(c => c.SaveUser(It.IsAny<User>()))
                .Throws<DuplicateUserException>();

            // Act & Assert
            var ex = Assert.Throws<DuplicateUserException>(
                () => _repository.SaveUser(user)
            );
            
            Assert.Contains("already exists", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region DeleteUser Tests

        [Fact]
        public void DeleteUser_WithExistingId_ReturnsTrue()
        {
            // Arrange: Setup mock to indicate successful deletion
            var userId = 5;
            
            _mockContext
                .Setup(c => c.DeleteUser(userId))
                .Returns(true);

            // Act
            var result = _repository.DeleteUser(userId);

            // Assert
            Assert.True(result);
            
            // Verify the context was called exactly once
            _mockContext.Verify(c => c.DeleteUser(userId), Times.Once);
        }

        [Fact]
        public void DeleteUser_WithNonExistentId_ReturnsFalse()
        {
            // Arrange: Setup mock to indicate no deletion occurred
            _mockContext
                .Setup(c => c.DeleteUser(It.IsAny<int>()))
                .Returns(false);

            // Act
            var result = _repository.DeleteUser(999);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public void DeleteUser_WithInvalidId_ThrowsArgumentException(int userId)
        {
            // Arrange: Setup mock to throw for invalid IDs
            _mockContext
                .Setup(c => c.DeleteUser(It.Is<int>(id => id <= 0)))
                .Throws<ArgumentException>();

            // Act & Assert
            Assert.Throws<ArgumentException>(
                () => _repository.DeleteUser(userId)
            );
        }

        #endregion

        #region Async Tests

        [Fact]
        public async Task GetUserAsync_WithValidId_ReturnsUserAsync()
        {
            // Arrange: Setup mock for async operation
            var userId = 5;
            var expectedUser = new User { Id = userId, Name = "John Async" };
            
            _mockContext
                .Setup(c => c.GetUserAsync(userId))
                .ReturnsAsync(expectedUser);

            // Act: Always use await for async tests
            var result = await _repository.GetUserAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedUser.Name, result.Name);
        }

        [Fact]
        public async Task GetUserAsync_WithInvalidId_ThrowsArgumentException()
        {
            // Arrange: Setup mock to throw async
            _mockContext
                .Setup(c => c.GetUserAsync(It.IsAny<int>()))
                .ThrowsAsync(new ArgumentException("Invalid ID"));

            // Act & Assert: Use ThrowsAsync for async exception tests
            await Assert.ThrowsAsync<ArgumentException>(
                () => _repository.GetUserAsync(-1)
            );
        }

        [Fact]
        public async Task SaveUserAsync_WithValidUser_SavesSuccessfully()
        {
            // Arrange
            var user = new User { Id = 1, Name = "Async User" };
            
            _mockContext
                .Setup(c => c.SaveUserAsync(It.IsAny<User>()))
                .ReturnsAsync(user);

            // Act
            var result = await _repository.SaveUserAsync(user);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Name, result.Name);
            
            // Verify async call was made
            _mockContext.Verify(c => c.SaveUserAsync(It.IsAny<User>()), Times.Once);
        }

        #endregion

        #region Multiple Verifications

        [Fact]
        public void GetUserByEmail_WithValidEmail_RetrievesAndCaches()
        {
            // Arrange
            var email = "test@example.com";
            var user = new User { Id = 1, Name = "Test", Email = email };
            
            _mockContext
                .Setup(c => c.GetUserByEmail(email))
                .Returns(user);

            // Act: Call multiple times to test caching behavior
            var result1 = _repository.GetUserByEmail(email);
            var result2 = _repository.GetUserByEmail(email);

            // Assert: Results are identical
            Assert.Equal(result1.Id, result2.Id);
            
            // Verify that context was called only once (due to caching)
            _mockContext.Verify(
                c => c.GetUserByEmail(email),
                Times.Once  // Should be called once, not twice
            );
        }

        #endregion

        #region Behavior Verification

        [Fact]
        public void UpdateUser_WithModifiedUser_VerifiesCorrectFieldsChanged()
        {
            // Arrange: Create original and modified user
            var originalUser = new User { Id = 1, Name = "John", Email = "john@example.com" };
            var modifiedUser = new User { Id = 1, Name = "John Updated", Email = "john@example.com" };
            
            _mockContext
                .Setup(c => c.SaveUser(It.IsAny<User>()))
                .Returns(modifiedUser);

            // Act
            var result = _repository.UpdateUser(originalUser, modifiedUser);

            // Assert & Verify: Check that SaveUser was called with updated name
            _mockContext.Verify(
                c => c.SaveUser(It.Is<User>(u => 
                    u.Id == 1 && 
                    u.Name == "John Updated" &&
                    u.Email == "john@example.com"
                )),
                Times.Once
            );
        }

        #endregion
    }
}
