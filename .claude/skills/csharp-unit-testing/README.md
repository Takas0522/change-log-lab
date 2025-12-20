# C# Unit Testing Skill (ISTQB-Compliant)

A comprehensive Claude skill for creating high-quality unit tests in C# following ISTQB (International Software Testing Qualifications Board) standards and industry best practices.

## 📋 Skill Contents

### Main Skill File
- **SKILL.md** - Primary skill instructions with complete testing guidelines

### Reference Documentation
- **istqb-standards-summary.md** - ISTQB Foundation concepts and testing principles
- **test-design-techniques.md** - Detailed guide to equivalence partitioning, boundary analysis, decision tables, and state transition testing
- **xunit-framework-guide.md** - xUnit framework features and best practices
- **mocking-patterns.md** - Moq library patterns and anti-patterns
- **async-testing-guide.md** - Comprehensive async/await testing strategies

### Example Tests
- **calculator-service.tests.cs** - Basic arithmetic tests with boundary testing and equivalence partitioning
- **user-repository.tests.cs** - Data access tests with mocking patterns
- **authorization-service.tests.cs** - Complex business logic with decision table testing
- **async-api-service.tests.cs** - Async operations, cancellation, and proper async patterns

## 🎯 When to Use This Skill

Use this skill when you need to:
- ✅ **Create unit tests** for C# classes, methods, and business logic
- ✅ **Review test quality** for compliance with ISTQB standards
- ✅ **Design test cases** using systematic test design techniques
- ✅ **Refactor existing tests** to improve maintainability and coverage
- ✅ **Establish test strategies** for comprehensive coverage
- ✅ **Debug failing tests** and improve reliability
- ✅ **Set up test fixtures, mocks, and test data** properly

## 📚 What You'll Learn

### Test Fundamentals
- Naming conventions and test structure (Arrange-Act-Assert)
- Test characteristics: isolation, determinism, focus, readability, speed
- ISTQB testing principles and levels

### Test Design Techniques
- **Equivalence Partitioning** - Divide inputs into equivalence classes
- **Boundary Value Analysis** - Test at partition boundaries and edges
- **Decision Table Testing** - Test all combinations of conditions
- **State Transition Testing** - Test valid and invalid state changes

### Coverage Strategy
- Statement coverage (70-80% minimum)
- Branch coverage (80%+ target)
- Path coverage for critical paths
- Coverage guidelines by code type

### Testing Patterns
- Mocking with Moq
- Test doubles (Stub, Mock, Spy, Fake)
- Async/await testing without deadlocks
- Exception testing with message validation
- Parameterized testing with Theory and InlineData

### Advanced Topics
- CancellationToken testing
- IAsyncLifetime for async setup/teardown
- Test fixture management and builders
- Callback testing for verification
- Anti-patterns to avoid

## 🚀 Getting Started

1. **Review the main SKILL.md** for comprehensive guidelines
2. **Check examples/** for real-world test patterns
3. **Reference documentation** for detailed technique explanations
4. **Use test naming convention**: `MethodName_Scenario_ExpectedResult`
5. **Follow AAA pattern**: Arrange, Act, Assert

## 📖 Example Test Structure

```csharp
[Theory]
[InlineData(100, 2, 50)]      // Valid input
[InlineData(5, 0, null)]      // Boundary case
public void Divide_WithVariousInputs_ReturnsQuotient(int dividend, int divisor, int? expected)
{
    // Arrange
    var calculator = new Calculator();
    
    // Act
    var result = calculator.Divide(dividend, divisor);
    
    // Assert
    Assert.Equal(expected, result);
}
```

## ✅ Code Review Checklist

When creating or reviewing unit tests, verify:
- ☐ Test name clearly describes scenario and expected result
- ☐ AAA structure is evident (Arrange, Act, Assert)
- ☐ Single responsibility (one logical concept per test)
- ☐ No test interdependencies (isolated, run in any order)
- ☐ Appropriate use of mocks (external dependencies only)
- ☐ Assertions are specific (not generic True/False)
- ☐ Exception cases tested (both happy and error paths)
- ☐ Boundary values tested
- ☐ Setup/teardown minimal
- ☐ No blocking calls (.Result, .Wait())
- ☐ Async/await used correctly
- ☐ Test data is realistic
- ☐ Coverage is adequate (80%+ for business logic)
- ☐ Tests are deterministic
- ☐ Documentation is clear

## 🔑 Key Concepts

### ISTQB Testing Levels
1. **Unit Testing (Component Testing)** - Individual components in isolation
2. **Integration Testing** - Multiple components together
3. **System Testing** - Complete application
4. **Acceptance Testing** - Business requirements validation

### Test Double Types
- **Stub** - Returns predetermined values
- **Mock** - Verifies method calls
- **Spy** - Records interactions while calling real method
- **Fake** - Lightweight working implementation

### Coverage Targets
- **Core business logic**: 100%
- **Business features**: 80%+
- **Supporting code**: 70%+
- **Framework code**: Lower as needed

## 🎓 ISTQB Standards Covered

This skill covers concepts from:
- **ISTQB Foundation Level** - Basic principles and test design
- **ISTQB Advanced Test Analyst** - Advanced techniques
- **Best practices** for C# and .NET development

## 📝 Notes

- All examples are production-ready patterns
- Test code should be as maintainable as production code
- Mock external dependencies, test real logic
- Never use `.Result` or `.Wait()` in async tests
- Keep tests fast, focused, and deterministic

## 📄 License

This skill is licensed under the MIT License. See LICENSE.txt for details.

## 🤝 Integration

This skill is designed to integrate seamlessly with:
- xUnit testing framework
- Moq mocking library
- .NET/ASP.NET Core projects
- CI/CD pipelines

---

**Version**: 1.0  
**Created**: December 2025  
**Status**: Production-Ready
