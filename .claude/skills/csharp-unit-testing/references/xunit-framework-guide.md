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
