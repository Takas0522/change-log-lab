# Async/Await Testing Guide

## Why Async Testing Is Different

Async code introduces challenges:
- Race conditions and timing issues
- Deadlocks (from synchronous blocking)
- Exception handling across await boundaries
- Cancellation token propagation

## Testing Async Methods

### Basic Async Test

```csharp
[Fact]
public async Task GetUser_WithValidId_ReturnsUser()
{
    // Arrange
    var repository = new UserRepository();
    
    // Act
    var user = await repository.GetUserAsync(1);
    
    // Assert
    Assert.NotNull(user);
    Assert.Equal(1, user.Id);
}
```

### The Most Common Mistake: .Result and .Wait()

**NEVER DO THIS:**

```csharp
[Fact]
public void BadAsyncTest()
{
    // WRONG: This can deadlock or hide exceptions!
    var user = _repository.GetUserAsync(1).Result;
    
    Assert.NotNull(user);
}

[Fact]
public void AnotherBadTest()
{
    // WRONG: Same problem
    _repository.GetUserAsync(1).Wait();
}

[Fact]
public void WaitingForAllTasks()
{
    // WRONG: Can deadlock
    Task.WaitAll(_repository.GetUserAsync(1), _repository.GetUserAsync(2));
}
```

**WHY THIS IS BAD:**
1. `.Result` and `.Wait()` block the calling thread
2. If awaited code tries to resume on the same context, deadlock occurs
3. Hides async exceptions (wrapped in AggregateException)
4. Defeats the purpose of async testing

**CORRECT APPROACH:**

```csharp
[Fact]
public async Task CorrectAsyncTest()
{
    // CORRECT: Use async all the way
    var user = await _repository.GetUserAsync(1);
    
    Assert.NotNull(user);
}
```

## Testing Exceptions in Async Code

### ThrowsAsync for Async Methods

```csharp
[Fact]
public async Task GetUser_WithInvalidId_ThrowsUserNotFoundException()
{
    var repository = new UserRepository();
    
    // Assert before Act (correct pattern for exception testing)
    await Assert.ThrowsAsync<UserNotFoundException>(
        () => repository.GetUserAsync(-1)
    );
}

[Fact]
public async Task GetUser_WithInvalidId_VerifyExceptionMessage()
{
    var repository = new UserRepository();
    
    var ex = await Assert.ThrowsAsync<UserNotFoundException>(
        () => repository.GetUserAsync(-1)
    );
    
    Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
}

[Fact]
public async Task SaveUser_WithNullUser_ThrowsArgumentNullException()
{
    var repository = new UserRepository();
    
    await Assert.ThrowsAsync<ArgumentNullException>(
        () => repository.SaveUserAsync(null)
    );
}
```

### Exception from Async Lambda

```csharp
[Fact]
public async Task ProcessFile_WithInvalidPath_ThrowsIOException()
{
    var processor = new FileProcessor();
    
    // For async lambdas, include await in the lambda
    await Assert.ThrowsAsync<FileNotFoundException>(
        async () => await processor.ReadFileAsync("/invalid/path")
    );
}
```

## Testing Task Cancellation

### Cancellation Token Testing

```csharp
[Fact]
public async Task LongRunningOperation_WithCancellation_ThrowsOperationCanceledException()
{
    var service = new DataService();
    var cts = new CancellationTokenSource();
    
    // Cancel after 100ms
    cts.CancelAfter(TimeSpan.FromMilliseconds(100));
    
    // Assert & Act combined
    await Assert.ThrowsAsync<OperationCanceledException>(
        () => service.FetchLargeDatasetAsync(cts.Token)
    );
}

[Fact]
public async Task Operation_WithImmediateCancellation_CancelsFast()
{
    var service = new DataService();
    var cts = new CancellationTokenSource();
    
    // Cancel immediately
    cts.Cancel();
    
    var sw = Stopwatch.StartNew();
    
    await Assert.ThrowsAsync<OperationCanceledException>(
        () => service.FetchLargeDatasetAsync(cts.Token)
    );
    
    sw.Stop();
    
    // Should cancel almost instantly, not after full operation time
    Assert.True(sw.ElapsedMilliseconds < 1000);
}

[Fact]
public async Task Operation_WithoutCancellation_Completes()
{
    var service = new DataService();
    var cts = new CancellationTokenSource();
    
    // Don't cancel
    var result = await service.FetchLargeDatasetAsync(cts.Token);
    
    Assert.NotNull(result);
}
```

## Testing Async Sequences

### Multiple Async Operations

```csharp
[Fact]
public async Task ProcessMultipleUsers_ReturnsAllResults()
{
    var repository = new UserRepository();
    
    // Act
    var user1 = await repository.GetUserAsync(1);
    var user2 = await repository.GetUserAsync(2);
    var user3 = await repository.GetUserAsync(3);
    
    // Assert
    Assert.NotNull(user1);
    Assert.NotNull(user2);
    Assert.NotNull(user3);
}

// Using Task.WhenAll for parallel operations
[Fact]
public async Task ProcessMultipleUsers_InParallel_ReturnsAllResults()
{
    var repository = new UserRepository();
    
    // Act - execute in parallel
    var tasks = new[]
    {
        repository.GetUserAsync(1),
        repository.GetUserAsync(2),
        repository.GetUserAsync(3)
    };
    
    var users = await Task.WhenAll(tasks);
    
    // Assert
    Assert.Equal(3, users.Length);
    Assert.All(users, u => Assert.NotNull(u));
}

// With Task.WhenAny (first to complete)
[Fact]
public async Task FetchFromMultipleSources_ReturnsFirstAvailable()
{
    var api1 = new ApiClient("endpoint1");
    var api2 = new ApiClient("endpoint2");
    
    var tasks = new Task<User>[]
    {
        api1.GetUserAsync(1),
        api2.GetUserAsync(1)
    };
    
    // Get whichever completes first
    var firstCompleted = await Task.WhenAny(tasks);
    var user = (firstCompleted as Task<User>)?.Result;
    
    Assert.NotNull(user);
}
```

## Testing Async Properties and Methods Returning IAsyncEnumerable

### IAsyncEnumerable Testing

```csharp
[Fact]
public async Task GetUsers_WithValidCriteria_ReturnsAllMatching()
{
    var repository = new UserRepository();
    
    // Collect results from async enumerable
    var users = new List<User>();
    await foreach (var user in repository.GetUsersAsync(age: 25))
    {
        users.Add(user);
    }
    
    Assert.NotEmpty(users);
    Assert.All(users, u => Assert.Equal(25, u.Age));
}

[Fact]
public async Task GetUsers_WithCancellation_CancelsEnumeration()
{
    var repository = new UserRepository();
    var cts = new CancellationTokenSource();
    
    var usersProcessed = 0;
    
    cts.CancelAfter(100);
    
    try
    {
        await foreach (var user in repository.GetUsersAsync(ct: cts.Token))
        {
            usersProcessed++;
            await Task.Delay(50);  // Simulate processing
        }
    }
    catch (OperationCanceledException)
    {
        // Expected
    }
    
    // Should have processed only a few before cancellation
    Assert.True(usersProcessed < 10);
}
```

## Using fakeAsync and Incrementing Time

### With System.Threading.Tasks Extensions

```csharp
[Fact]
public async Task RetryLogic_WaitsCorrectDuration()
{
    var service = new RetryService();
    
    var sw = Stopwatch.StartNew();
    
    // This should retry after 100ms delay
    var result = await service.ExecuteWithRetryAsync(
        () => Task.FromResult(true),
        retryDelay: TimeSpan.FromMilliseconds(100),
        maxRetries: 1
    );
    
    sw.Stop();
    
    // Should have waited at least the delay
    Assert.True(sw.ElapsedMilliseconds >= 100);
}
```

### Mocking Time-Based Operations

```csharp
[Fact]
public async Task RetryLogic_WithMockClock_AvoidsSleepDelay()
{
    var mockClock = new Mock<IClock>();
    var service = new RetryService(mockClock.Object);
    
    // When clock advances, we don't actually wait
    mockClock
        .Setup(c => c.DelayAsync(It.IsAny<TimeSpan>()))
        .Returns(Task.CompletedTask);
    
    var sw = Stopwatch.StartNew();
    
    var result = await service.ExecuteWithRetryAsync(
        () => Task.FromResult(true),
        retryDelay: TimeSpan.FromSeconds(60)
    );
    
    sw.Stop();
    
    // Should complete instantly (no real wait)
    Assert.True(sw.ElapsedMilliseconds < 100);
}
```

## Testing ValueTask vs Task

### ValueTask Optimization

```csharp
public interface IRepository
{
    // Returns ValueTask (allocation-free if completes synchronously)
    ValueTask<User> GetUserAsync(int id);
}

[Fact]
public async Task GetUser_WithValueTask_ReturnsUser()
{
    var repository = new OptimizedRepository();
    
    // Can await directly
    var user = await repository.GetUserAsync(1);
    
    Assert.NotNull(user);
}

[Fact]
public async Task GetUser_WithValueTask_WhenAlreadyComplete()
{
    var repository = new OptimizedRepository();
    
    // This might not allocate a Task at all (optimization)
    var user = await repository.GetUserAsync(1);
    
    Assert.NotNull(user);
}
```

## Async Initialization with IAsyncLifetime

### Test Class with Async Setup

```csharp
public class DatabaseTests : IAsyncLifetime
{
    private readonly DatabaseContext _context;
    
    public DatabaseTests()
    {
        _context = new DatabaseContext();
    }
    
    public async Task InitializeAsync()
    {
        // Setup: Create tables, seed data, etc.
        await _context.Database.EnsureCreatedAsync();
        await _context.Users.AddAsync(new User { Id = 1, Name = "Test User" });
        await _context.SaveChangesAsync();
    }
    
    public async Task DisposeAsync()
    {
        // Cleanup: Drop tables, close connections, etc.
        await _context.Database.EnsureDeletedAsync();
    }
    
    [Fact]
    public async Task GetUser_ReturnsSeededData()
    {
        var user = await _context.Users.FirstAsync();
        
        Assert.Equal("Test User", user.Name);
    }
}
```

## Testing Async Middleware

```csharp
[Fact]
public async Task Middleware_ProcessesRequest_Asynchronously()
{
    var next = new Mock<RequestDelegate>();
    next
        .Setup(n => n(It.IsAny<HttpContext>()))
        .Returns(Task.CompletedTask);
    
    var middleware = new TimingMiddleware(next.Object);
    
    var context = new DefaultHttpContext();
    
    await middleware.InvokeAsync(context);
    
    next.Verify(n => n(context), Times.Once);
}
```

## Best Practices for Async Testing

1. **Always use async/await** - Never use `.Result` or `.Wait()`
2. **Use async test methods** - Mark tests as `async Task`, not `void`
3. **Test both success and cancellation** - Verify cancellation behavior
4. **Use CancellationTokenSource** for cancellation tests
5. **Await all async calls** - Don't "fire and forget"
6. **Use IAsyncLifetime** for async setup/teardown
7. **Mock time-dependent operations** - Avoid real Task.Delay in tests
8. **Test concurrent scenarios** - Use Task.WhenAll for parallel operations
9. **Verify exception types** - Use ThrowsAsync for async exceptions
10. **Keep async tests readable** - Extract complex async logic to helpers

## Common Async Test Patterns

```csharp
// Pattern 1: Simple async result
[Fact]
public async Task Method_Scenario_Result()
{
    var result = await service.MethodAsync();
    Assert.NotNull(result);
}

// Pattern 2: Async exception
[Fact]
public async Task Method_Scenario_ThrowsException()
{
    await Assert.ThrowsAsync<ExceptionType>(
        () => service.MethodAsync()
    );
}

// Pattern 3: Async with cancellation
[Fact]
public async Task Method_WithCancellation_ThrowsOperationCanceledException()
{
    var cts = new CancellationTokenSource();
    cts.CancelAfter(100);
    
    await Assert.ThrowsAsync<OperationCanceledException>(
        () => service.MethodAsync(cts.Token)
    );
}

// Pattern 4: Multiple async operations
[Fact]
public async Task Method_WithMultipleOperations_ReturnsAllResults()
{
    var results = await Task.WhenAll(
        service.MethodAsync(1),
        service.MethodAsync(2),
        service.MethodAsync(3)
    );
    
    Assert.Equal(3, results.Length);
}
```
