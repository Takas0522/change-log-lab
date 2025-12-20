# Moq Mocking Patterns & Best Practices

## Why Mocking Matters

Mocking allows you to:
- Isolate units under test from external dependencies
- Control dependency behavior for edge cases
- Test error paths that are hard to trigger with real dependencies
- Speed up tests (avoid database calls, API calls, file I/O)
- Make tests deterministic (no random/flaky behavior)

## Moq Installation

```bash
dotnet add package Moq
```

## Basic Mock Setup

### Creating Mocks

```csharp
// Create a mock of an interface
var mockRepository = new Mock<IUserRepository>();

// Setup return value
mockRepository
    .Setup(r => r.GetUser(It.IsAny<int>()))
    .Returns(new User { Id = 1, Name = "Test" });

// Inject the mock (using the MockObject property)
var service = new UserService(mockRepository.Object);

// Use in test
var user = service.GetUser(1);
Assert.Equal("Test", user.Name);

// Verify the mock was called as expected
mockRepository.Verify(r => r.GetUser(1), Times.Once);
```

## Setup Methods

### Returns - Synchronous Return

```csharp
var mockRepository = new Mock<IRepository>();

// Fixed return value
mockRepository
    .Setup(r => r.GetItem(It.IsAny<int>()))
    .Returns(new Item { Id = 1 });

// Delegate for dynamic return based on input
mockRepository
    .Setup(r => r.GetItem(It.IsAny<int>()))
    .Returns((int id) => new Item { Id = id });

// Sequence of returns (different each call)
mockRepository
    .SetupSequence(r => r.GetItem(It.IsAny<int>()))
    .Returns(new Item { Id = 1 })
    .Returns(new Item { Id = 2 })
    .Throws<ArgumentException>();
```

### ReturnsAsync - Asynchronous Return

```csharp
var mockRepository = new Mock<IAsyncRepository>();

// Fixed async return
mockRepository
    .Setup(r => r.GetItemAsync(It.IsAny<int>()))
    .ReturnsAsync(new Item { Id = 1 });

// Callback-based return
mockRepository
    .Setup(r => r.GetItemAsync(It.IsAny<int>()))
    .ReturnsAsync((int id) => new Item { Id = id });

// Sequence of async returns
mockRepository
    .SetupSequence(r => r.GetItemAsync(It.IsAny<int>()))
    .ReturnsAsync(new Item { Id = 1 })
    .ReturnsAsync(new Item { Id = 2 })
    .ThrowsAsync(new InvalidOperationException());
```

### Throws - Exception Return

```csharp
var mockRepository = new Mock<IRepository>();

// Throw on specific call
mockRepository
    .Setup(r => r.DeleteItem(It.Is<int>(id => id < 0)))
    .Throws<ArgumentException>();

// Throw specific exception with message
mockRepository
    .Setup(r => r.GetItem(99))
    .Throws(new EntityNotFoundException("Item not found"));

// Throw asynchronously
mockRepository
    .Setup(r => r.GetItemAsync(99))
    .ThrowsAsync(new EntityNotFoundException("Item not found"));
```

### Callback - Custom Logic

```csharp
var mockRepository = new Mock<IRepository>();
var capturedId = 0;

// Execute custom logic when method is called
mockRepository
    .Setup(r => r.GetItem(It.IsAny<int>()))
    .Callback<int>(id => capturedId = id)
    .Returns((int id) => new Item { Id = id });

var service = new Service(mockRepository.Object);
var item = service.GetItem(42);

Assert.Equal(42, capturedId);  // Verify callback was called with correct value
```

## Matching Arguments (It.)

```csharp
var mockRepository = new Mock<IRepository>();

// Match any value
mockRepository
    .Setup(r => r.GetItem(It.IsAny<int>()))
    .Returns(new Item());

// Match specific value
mockRepository
    .Setup(r => r.GetItem(5))
    .Returns(new Item { Id = 5 });

// Match with predicate
mockRepository
    .Setup(r => r.GetItem(It.Is<int>(id => id > 0)))
    .Returns(new Item());

// Match range
mockRepository
    .Setup(r => r.GetItem(It.IsInRange(1, 10, Range.Inclusive)))
    .Returns(new Item());

// Match string patterns
mockRepository
    .Setup(r => r.GetByName(It.IsAny<string>()))
    .Returns(new Item());

mockRepository
    .Setup(r => r.GetByName(It.Is<string>(name => name.StartsWith("Test"))))
    .Returns(new Item { Name = "Test..." });

// Match null
mockRepository
    .Setup(r => r.SaveItem(It.IsNull<Item>()))
    .Throws<ArgumentNullException>();

// Match non-null
mockRepository
    .Setup(r => r.SaveItem(It.IsNotNull<Item>()))
    .Returns(new Item());
```

## Verification (Verifying Calls)

### Verify Method Was Called

```csharp
var mockRepository = new Mock<IRepository>();
var service = new Service(mockRepository.Object);

service.GetItem(5);

// Verify called with specific value
mockRepository.Verify(r => r.GetItem(5), Times.Once);

// Verify called any number of times
mockRepository.Verify(r => r.SaveItem(It.IsAny<Item>()), Times.Once);

// Verify never called
mockRepository.Verify(r => r.DeleteItem(It.IsAny<int>()), Times.Never);

// Verify called multiple times
mockRepository.Verify(r => r.Log(It.IsAny<string>()), Times.Exactly(3));

// Verify called between N times
mockRepository.Verify(r => r.Log(It.IsAny<string>()), Times.Between(1, 3, Range.Inclusive));
```

### Verify All Setups Were Called

```csharp
var mockRepository = new Mock<IRepository>();
mockRepository.Setup(r => r.GetItem(It.IsAny<int>())).Returns(new Item());
mockRepository.Setup(r => r.SaveItem(It.IsAny<Item>())).Returns(true);

var service = new Service(mockRepository.Object);
service.GetItem(1);  // Only GetItem called

// This throws: SaveItem setup was never invoked
mockRepository.VerifyAll();
```

### No Unexpected Calls

```csharp
var mockRepository = new Mock<IRepository>();
mockRepository.Setup(r => r.GetItem(5)).Returns(new Item());

var service = new Service(mockRepository.Object);
service.SomeOtherOperation();  // This might call GetItem without explicit setup

// This throws if any methods were called unexpectedly
mockRepository.VerifyNoOtherCalls();
```

## Mock Strictness

### Loose Mock (Default)

```csharp
var mockRepository = new Mock<IRepository>();
// No setup for GetItem

var service = new Service(mockRepository.Object);
var item = service.GetItem(1);  // Returns default(Item) = null

// No exception thrown
```

### Strict Mock

```csharp
var mockRepository = new Mock<IRepository>(MockBehavior.Strict);
// No setup for GetItem

var service = new Service(mockRepository.Object);
var item = service.GetItem(1);  // Throws MockException!
```

## Complete Example: User Service

```csharp
public interface IUserRepository
{
    User GetUser(int id);
    void SaveUser(User user);
    bool DeleteUser(int id);
}

public class UserService
{
    private readonly IUserRepository _repository;
    
    public UserService(IUserRepository repository)
    {
        _repository = repository;
    }
    
    public User GetUserDetails(int id)
    {
        var user = _repository.GetUser(id);
        if (user == null)
            throw new UserNotFoundException(id);
        return user;
    }
    
    public void UpdateUser(User user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));
        _repository.SaveUser(user);
    }
}

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly UserService _service;
    
    public UserServiceTests()
    {
        _mockRepository = new Mock<IUserRepository>();
        _service = new UserService(_mockRepository.Object);
    }
    
    [Fact]
    public void GetUserDetails_WithValidId_ReturnsUser()
    {
        // Arrange
        var userId = 5;
        var expectedUser = new User { Id = userId, Name = "John" };
        
        _mockRepository
            .Setup(r => r.GetUser(userId))
            .Returns(expectedUser);
        
        // Act
        var result = _service.GetUserDetails(userId);
        
        // Assert
        Assert.Equal(expectedUser, result);
        
        // Verify the dependency was called correctly
        _mockRepository.Verify(r => r.GetUser(userId), Times.Once);
    }
    
    [Fact]
    public void GetUserDetails_WithInvalidId_ThrowsUserNotFoundException()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetUser(It.IsAny<int>()))
            .Returns((User)null);
        
        // Act & Assert
        var ex = Assert.Throws<UserNotFoundException>(
            () => _service.GetUserDetails(999)
        );
        
        Assert.Equal(999, ex.UserId);
    }
    
    [Fact]
    public void UpdateUser_WithValidUser_CallsSaveUser()
    {
        // Arrange
        var user = new User { Id = 1, Name = "Updated" };
        
        // Act
        _service.UpdateUser(user);
        
        // Assert
        _mockRepository.Verify(
            r => r.SaveUser(It.Is<User>(u => u.Id == 1 && u.Name == "Updated")),
            Times.Once
        );
    }
    
    [Fact]
    public void UpdateUser_WithNullUser_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => _service.UpdateUser(null)
        );
    }
}
```

## Common Pitfalls

### Pitfall 1: Over-Mocking Real Logic

**Bad:**
```csharp
var mockService = new Mock<CalculationService>();
mockService.Setup(s => s.Add(2, 3)).Returns(5);
mockService.Setup(s => s.Add(10, 20)).Returns(30);
// You're just testing the mock, not the real logic!
```

**Good:**
```csharp
var service = new CalculationService();  // Use real implementation
var result = service.Add(2, 3);
Assert.Equal(5, result);
```

### Pitfall 2: Mocking Dependencies of Dependencies

**Bad:**
```csharp
var mockDatabase = new Mock<IDatabase>();
var mockRepository = new Mock<IRepository>();
mockRepository.Setup(r => r.GetUser(1)).Returns(new User());
var service = new UserService(mockRepository.Object, mockDatabase.Object);
// Too many mocks makes test hard to understand
```

**Good:**
```csharp
var repository = new MockRepository();  // Lightweight fake, not a mock
var service = new UserService(repository);
// Easier to understand, clearer test intent
```

### Pitfall 3: Expecting Mock Behavior in Integration Tests

**Bad:**
```csharp
// In integration tests, don't use mocks for real components
[IntegrationTest]
public void SaveUser_ToRealDatabase()
{
    var mockDatabase = new Mock<IDatabase>();  // Wrong!
    var service = new UserService(mockDatabase.Object);
}

// Good: Use real database or in-memory database
[IntegrationTest]
public async Task SaveUser_ToRealDatabase()
{
    using var context = new TestDbContext();
    var repository = new UserRepository(context);
    var service = new UserService(repository);
    
    await service.SaveUserAsync(testUser);
    
    var saved = await context.Users.FirstOrDefaultAsync(u => u.Id == testUser.Id);
    Assert.NotNull(saved);
}
```

## Best Practices

1. **Mock external dependencies only** (API calls, database, file system)
2. **Test real logic** - don't mock the code you're testing
3. **Setup mocks before creating service** - make test setup clear
4. **Verify critical interactions** - not every method call
5. **Use meaningful test names** - make mock behavior obvious
6. **Keep mocks simple** - complex mock logic = complex test = hard to debug
