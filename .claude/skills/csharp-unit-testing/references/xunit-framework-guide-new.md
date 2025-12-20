# xUnit Framework Guide for C# Unit Testing

## xUnit Basics

xUnit is the modern testing framework for .NET, replacing NUnit and MSTest for most projects.

### Installation

```bash
dotnet add package xunit
dotnet add package xunit.runner.visualstudio
dotnet add package xunit.runner.console  # For CLI execution
```

### Project Structure

Test projects follow naming convention: `[ProjectName].Tests`

```
src/
  ├── MyApp/
  │   ├── Services/
  │   │   └── UserService.cs
  │   └── Models/
  │       └── User.cs
  └── MyApp.Tests/
      ├── Services/
      │   └── UserServiceTests.cs
      └── Models/
          └── UserTests.cs
```

## Core Attributes

### [Fact] - Single Test

Use for a single test case with no parameters:

```csharp
public class CalculatorTests
{
    [Fact]
    public void Add_TwoNumbers_ReturnsSum()
    {
        var result = new Calculator().Add(2, 3);
        Assert.Equal(5, result);
    }
}
```

### [Theory] - Parameterized Tests

Use for multiple test cases with different inputs:

```csharp
[Theory]
[InlineData(2, 3, 5)]
[InlineData(0, 0, 0)]
[InlineData(-1, 1, 0)]
public void Add_WithVariousInputs_ReturnsSum(int a, int b, int expected)
{
    var result = new Calculator().Add(a, b);
    Assert.Equal(expected, result);
}
```

## Data Sources for [Theory]

### InlineData - Simple Values

```csharp
[Theory]
[InlineData(1)]
[InlineData(2)]
[InlineData(3)]
public void Process_WithNumbers_ReturnsValue(int number)
{
    var result = Processor.Process(number);
    Assert.NotNull(result);
}
```

### MemberData - Complex Objects

```csharp
public class StringProcessorTests
{
    [Theory]
    [MemberData(nameof(GetTestData))]
    public void Process_WithVariousInputs_ReturnsExpected(string input, string expected)
    {
        var result = StringProcessor.Process(input);
        Assert.Equal(expected, result);
    }
    
    public static IEnumerable<object[]> GetTestData()
    {
        yield return new object[] { "hello", "HELLO" };
        yield return new object[] { "world", "WORLD" };
        yield return new object[] { "", "" };
    }
}
```

### ClassData - Encapsulated Test Data

```csharp
public class UserTestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { new User { Id = 1, Name = "Alice" }, true };
        yield return new object[] { new User { Id = 0, Name = "" }, false };
    }
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class UserValidationTests
{
    [Theory]
    [ClassData(typeof(UserTestData))]
    public void ValidateUser_WithVariousInputs_ReturnsExpected(User user, bool isValid)
    {
        var result = UserValidator.IsValid(user);
        Assert.Equal(isValid, result);
    }
}
```

## Assertions

### Common Assertions

```csharp
// Equality
Assert.Equal(expected, actual);
Assert.NotEqual(expected, actual);

// Null checks
Assert.Null(value);
Assert.NotNull(value);

// Boolean
Assert.True(condition);
Assert.False(condition);

// Collections
Assert.Empty(collection);
Assert.NotEmpty(collection);
Assert.Contains(item, collection);
Assert.DoesNotContain(item, collection);
Assert.Single(collection);  // Exactly one item
Assert.Equal(new[] { 1, 2, 3 }, actual);  // Exact match

// Exceptions
Assert.Throws<ArgumentException>(() => code());
Assert.ThrowsAny<Exception>(() => code());
Assert.ThrowsAsync<ArgumentException>(() => asyncCode());

// String
Assert.Contains("substring", "full string");
Assert.StartsWith("prefix", "prefixSuffix");
Assert.EndsWith("suffix", "prefixSuffix");
Assert.Matches(@"^[a-z]+$", "abc");

// Type
Assert.IsType<String>(value);
Assert.IsAssignableFrom<IEnumerable>(value);

// Range
Assert.InRange(value, low, high);

// Custom predicates
Assert.All(collection, x => Assert.True(x.IsValid));
```

## Test Lifecycle

### Constructor and IDisposable

xUnit creates a **new instance** of the test class for each test method, ensuring isolation.

```csharp
public class DatabaseTests : IDisposable
{
    private readonly DbConnection _connection;
    
    // Called before each test
    public DatabaseTests()
    {
        _connection = new SqlConnection("...");
        _connection.Open();
    }
    
    // Called after each test
    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }
    
    [Fact]
    public void Test_UsesConnection()
    {
        // _connection is ready to use
        var command = _connection.CreateCommand();
        // ...
    }
}
```

### IAsyncLifetime - Async Setup/Teardown

For async initialization and cleanup:

```csharp
public class AsyncDatabaseTests : IAsyncLifetime
{
    private IRepository _repository;
    
    public AsyncDatabaseTests()
    {
        _repository = new TestRepository();
    }
    
    public async Task InitializeAsync()
    {
        // Async setup
        await _repository.ConnectAsync();
        await _repository.SeedDataAsync();
    }
    
    public async Task DisposeAsync()
    {
        // Async cleanup
        await _repository.ClearAsync();
        await _repository.DisconnectAsync();
    }
    
    [Fact]
    public async Task Test_WithAsyncSetup()
    {
        var data = await _repository.GetAllAsync();
        Assert.NotEmpty(data);
    }
}
```

## Class Fixtures and Collection Fixtures

### Class Fixture - Shared Context Across Tests in One Class

Use when you need to share expensive setup across multiple tests in a single test class:

```csharp
// Define the fixture
public class DatabaseFixture : IDisposable
{
    public DbConnection Connection { get; private set; }
    
    public DatabaseFixture()
    {
        Connection = new SqlConnection("Server=localhost;Database=TestDb");
        Connection.Open();
    }
    
    public void Dispose()
    {
        Connection?.Close();
    }
}

// Use the fixture in a test class
public class UserRepositoryTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    
    public UserRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public void GetUser_WithValidId_ReturnsUser()
    {
        // Use _fixture.Connection
        var repo = new UserRepository(_fixture.Connection);
        var user = repo.GetUser(1);
        Assert.NotNull(user);
    }
}
```

### Collection Fixture - Shared Context Across Multiple Test Classes

Use when you need to share context across multiple test classes:

```csharp
// Define the fixture
public class DatabaseFixture : IDisposable
{
    public DbConnection Connection { get; private set; }
    
    public DatabaseFixture()
    {
        Connection = new SqlConnection("Server=localhost;Database=TestDb");
        Connection.Open();
    }
    
    public void Dispose()
    {
        Connection?.Close();
    }
}

// Define a collection
[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    // This class has no code, serves as marker
}

// Use the collection in multiple test classes
[Collection("Database collection")]
public class UserRepositoryTests
{
    private readonly DatabaseFixture _fixture;
    
    public UserRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public void GetUser_WithValidId_ReturnsUser()
    {
        var repo = new UserRepository(_fixture.Connection);
        var user = repo.GetUser(1);
        Assert.NotNull(user);
    }
}

[Collection("Database collection")]
public class OrderRepositoryTests
{
    private readonly DatabaseFixture _fixture;
    
    public OrderRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;  // Same instance as UserRepositoryTests
    }
    
    [Fact]
    public void GetOrder_WithValidId_ReturnsOrder()
    {
        var repo = new OrderRepository(_fixture.Connection);
        var order = repo.GetOrder(1);
        Assert.NotNull(order);
    }
}
```

**Important**: All test classes in the same collection will NOT run in parallel, but will share the same fixture instance.

## Best Practices

### DO:
✅ Use `[Fact]` for single scenario tests  
✅ Use `[Theory]` with `[InlineData]` for multiple similar scenarios  
✅ Use descriptive test names: `Method_Scenario_ExpectedResult`  
✅ Follow AAA pattern (Arrange, Act, Assert)  
✅ Keep tests isolated (no dependencies between tests)  
✅ Use constructor for common setup  
✅ Use fixtures for expensive shared resources  
✅ Use `ITestOutputHelper` for debugging output  
✅ Make assertions specific (`Assert.Equal` over `Assert.True`)

### DON'T:
❌ Don't use `[Skip]` long-term (fix or delete the test)  
❌ Don't share mutable state between tests  
❌ Don't use static fields for test data  
❌ Don't rely on test execution order  
❌ Don't use `Thread.Sleep` or `Task.Delay`  
❌ Don't use `.Result` or `.Wait()` on async operations  
❌ Don't catch exceptions yourself (use `Assert.Throws`)  
❌ Don't test multiple unrelated behaviors in one test

## xUnit vs NUnit vs MSTest

| Feature | xUnit | NUnit | MSTest |
|---------|-------|-------|--------|
| Single test | `[Fact]` | `[Test]` | `[TestMethod]` |
| Parameterized | `[Theory]` | `[TestCase]` | `[DataRow]` |
| Setup | Constructor | `[SetUp]` | `[TestInitialize]` |
| Teardown | `IDisposable` | `[TearDown]` | `[TestCleanup]` |
| Class setup | `IClassFixture<T>` | `[OneTimeSetUp]` | `[ClassInitialize]` |
| Test class | No attribute | `[TestFixture]` | `[TestClass]` |
| Isolation | New instance per test | Shared instance | Shared instance |
| Async setup | `IAsyncLifetime` | Limited | Limited |
| Modern .NET | ✅ Best support | ✅ Good | ⚠️ Limited |

**xUnit is recommended** for new .NET projects due to modern design and better async support.

## References

- [xUnit Documentation](https://xunit.net/)
- [Assertions Reference](https://xunit.net/docs/assertions)
- [Shared Context](https://xunit.net/docs/shared-context)
- [Running Tests](https://xunit.net/docs/running-tests)
