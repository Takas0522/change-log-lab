# Test Design Techniques - Detailed Guide

## Equivalence Partitioning (EP)

### Concept
Divide the input domain into equivalence classes where:
- Each class contains inputs that should be processed the same way
- One test from each class is representative
- Reduces test count while maintaining coverage

### Process

1. **Identify input conditions** from specification
2. **Partition into classes**:
   - Valid partitions (expected behavior)
   - Invalid partitions (error/boundary behavior)
3. **Write one test case** per partition

### Example 1: String Length Validation

**Requirement**: Username must be 3-20 characters

**Partitions**:
```
Invalid:  < 3 chars      → Error
Valid:    3-20 chars     → Accepted
Invalid:  > 20 chars     → Error
Invalid:  null/empty     → Error
Invalid:  special chars  → Error (depends on requirements)
```

**Test cases**:
```csharp
[Theory]
[InlineData("", "Username required")]           // Empty
[InlineData("ab", "Minimum 3 characters")]      // Too short (EP invalid)
[InlineData("abc", null)]                       // Valid (EP valid)
[InlineData("abcdefghijklmnopqrstu", "Maximum 20 characters")] // Too long (EP invalid)
public void ValidateUsername_WithVariousLengths_ReturnsExpected(string username, string expectedError)
{
    var result = Validator.ValidateUsername(username);
    if (expectedError == null)
        Assert.True(result.IsValid);
    else
        Assert.Contains(expectedError, result.ErrorMessage);
}
```

### Example 2: Age-Based Eligibility

**Requirement**: Adult (18+), Senior (65+) discounts, under 18 not eligible

**Partitions**:
```
Invalid:      < 0        → Error (negative age)
Invalid:      0          → Not eligible (too young)
Valid (type A): 1-17     → Not eligible
Valid (type B): 18-64    → Adult eligible
Valid (type C): 65-200   → Senior eligible
Invalid:      > 200      → Error (unrealistic)
```

**Test cases (one representative per partition)**:
```csharp
[Theory]
[InlineData(-1, false)]        // Invalid (negative)
[InlineData(10, false)]        // Valid but not eligible (type A)
[InlineData(25, true)]         // Valid and eligible (type B)
[InlineData(70, true)]         // Valid and eligible (type C)
[InlineData(250, false)]       // Invalid (unrealistic)
public void IsEligible_WithVariousAges_ReturnsExpected(int age, bool expected)
{
    var result = EligibilityChecker.IsEligible(age);
    Assert.Equal(expected, result);
}
```

## Boundary Value Analysis (BVA)

### Concept
Test at the edges and just beyond the edges of partitions

### Why It Works
- Off-by-one errors are extremely common
- Developers often make mistakes with `<` vs `<=`
- Boundaries are high-risk areas

### Process

1. **Identify boundaries** for each partition
2. **Test values**: boundary - 1, boundary, boundary + 1
3. **Apply to both limits** (min and max)

### Formula

For range [L, U]:
```
Lower boundary tests:  L-1, L, L+1
Upper boundary tests:  U-1, U, U+1
```

### Example 1: Integer Range [1, 100]

```csharp
[Theory]
[InlineData(0)]          // Just below lower boundary
[InlineData(1)]          // Lower boundary
[InlineData(2)]          // Just above lower boundary
[InlineData(99)]         // Just below upper boundary
[InlineData(100)]        // Upper boundary
[InlineData(101)]        // Just above upper boundary
[InlineData(-100)]       // Far below (sanity check)
public void Process_WithBoundaryValues_ReturnsExpected(int value)
{
    var result = Processor.Process(value);
    // Assertion depends on expected behavior
}
```

### Example 2: Date Range

**Requirement**: Process dates between 2024-01-01 and 2024-12-31

```csharp
[Theory]
[InlineData("2023-12-31")]     // Just before range
[InlineData("2024-01-01")]     // Start of range
[InlineData("2024-01-02")]     // Just after start
[InlineData("2024-12-30")]     // Just before end
[InlineData("2024-12-31")]     // End of range
[InlineData("2025-01-01")]     // Just after range
public void ProcessTransaction_WithBoundaryDates_ReturnsExpected(string dateString)
{
    var date = DateTime.Parse(dateString);
    var result = TransactionProcessor.Process(date);
    // Verify expected behavior
}
```

### Example 3: Floating-Point Precision

**Requirement**: Discount must be 0% to 100% (0.0 to 1.0)

```csharp
[Theory]
[InlineData(-0.01)]      // Just below
[InlineData(0.0)]        // Lower boundary
[InlineData(0.01)]       // Just above lower
[InlineData(0.99)]       // Just below upper
[InlineData(1.0)]        // Upper boundary
[InlineData(1.01)]       // Just above
public void ApplyDiscount_WithBoundaryRates_ReturnsExpected(decimal rate)
{
    var result = DiscountCalculator.Apply(100m, rate);
    // Verify within expected range or error thrown
}
```

## Decision Table Testing

### Concept
Create a table of all condition combinations and test each row

### When to Use
- Multiple conditions that interact
- Complex business rules
- Combinatorial logic

### Process

1. **Identify all conditions** (if/boolean checks)
2. **Identify all possible outcomes**
3. **Create rows** for each combination
4. **Define expected result** for each row
5. **Write test** for each row

### Example 1: Loan Approval (Complex)

**Conditions**:
- C1: Annual Income ≥ $50,000
- C2: Credit Score ≥ 650
- C3: Employment Duration ≥ 2 years
- C4: No recent bankruptcies

**Decision Table**:

| # | Income | Credit | Employ | Bankrupt | Approved |
|---|--------|--------|--------|----------|----------|
| 1 | Yes | Yes | Yes | No | **YES** |
| 2 | Yes | Yes | Yes | Yes | NO |
| 3 | Yes | Yes | No | No | NO |
| 4 | Yes | Yes | No | Yes | NO |
| 5 | Yes | No | Yes | No | NO |
| 6 | Yes | No | Yes | Yes | NO |
| 7 | Yes | No | No | No | NO |
| 8 | Yes | No | No | Yes | NO |
| 9 | No | Yes | Yes | No | NO |
| 10 | No | Yes | Yes | Yes | NO |
| 11 | No | Yes | No | No | NO |
| 12 | No | Yes | No | Yes | NO |
| 13 | No | No | Yes | No | NO |
| 14 | No | No | Yes | Yes | NO |
| 15 | No | No | No | No | NO |
| 16 | No | No | No | Yes | NO |

**Test Implementation**:

```csharp
[Theory]
[InlineData(60000, 700, 3, false, true)]    // Row 1: Approved
[InlineData(60000, 700, 3, true, false)]    // Row 2: Bankrupt blocks
[InlineData(60000, 700, 1, false, false)]   // Row 3: Employment too short
[InlineData(60000, 600, 3, false, false)]   // Row 5: Credit too low
[InlineData(40000, 700, 3, false, false)]   // Row 9: Income too low
public void ApproveLoan_WithVariousConditions_ReturnsExpected(
    decimal income, int creditScore, int employmentYears, bool hasBankruptcy, bool expectedApproved)
{
    var applicant = new LoanApplicant
    {
        AnnualIncome = income,
        CreditScore = creditScore,
        EmploymentDurationYears = employmentYears,
        HasRecentBankruptcy = hasBankruptcy
    };
    
    var result = LoanProcessor.ApproveApplication(applicant);
    Assert.Equal(expectedApproved, result.IsApproved);
}
```

### Example 2: Simplified Decision Table

**Requirement**: Shipping cost depends on weight and distance

| Weight | Distance | Cost |
|--------|----------|------|
| ≤ 5kg | Local | $5 |
| ≤ 5kg | National | $10 |
| ≤ 5kg | International | $25 |
| > 5kg | Local | $8 |
| > 5kg | National | $15 |
| > 5kg | International | $40 |

```csharp
public enum ShippingDistance { Local, National, International }

[Theory]
[InlineData(3, ShippingDistance.Local, 5)]
[InlineData(3, ShippingDistance.National, 10)]
[InlineData(3, ShippingDistance.International, 25)]
[InlineData(10, ShippingDistance.Local, 8)]
[InlineData(10, ShippingDistance.National, 15)]
[InlineData(10, ShippingDistance.International, 40)]
public void CalculateShippingCost_WithVariousConditions_ReturnsExpected(
    decimal weight, ShippingDistance distance, decimal expectedCost)
{
    var result = ShippingCalculator.Calculate(weight, distance);
    Assert.Equal(expectedCost, result);
}
```

## State Transition Testing

### Concept
Test valid and invalid state transitions, especially invalid ones

### When to Use
- Order processing, workflow engines
- User authentication states
- Document lifecycle management
- Any system with distinct states

### Process

1. **Identify all states**
2. **Map valid transitions** (transitions that should succeed)
3. **Map invalid transitions** (transitions that should fail/throw)
4. **Test each transition** with appropriate pre/post conditions

### Example 1: Order Lifecycle

**States**: `New` → `Confirmed` → `Shipped` → `Delivered` → `Closed`
**Special**: Can be `Canceled` from any state

**State Diagram**:
```
┌─────────────────────────────────────────────┐
│                                             ↓
New ──confirm──> Confirmed ──ship──> Shipped ──deliver──> Delivered ──close──> Closed
│                    │                   │                     │
└────────────────────┴───────────────────┴─────────────────────┴─ Cancel (valid from all)
```

**Test Implementation**:

```csharp
public class OrderStateTransitionTests
{
    private Order _order;
    
    [Fact]
    public void Confirm_FromNewState_TransitionsToConfirmed()
    {
        _order = new Order { Status = OrderStatus.New };
        
        _order.Confirm();
        
        Assert.Equal(OrderStatus.Confirmed, _order.Status);
    }
    
    [Fact]
    public void Confirm_FromConfirmedState_ThrowsInvalidOperationException()
    {
        _order = new Order { Status = OrderStatus.Confirmed };
        
        Assert.Throws<InvalidOperationException>(
            () => _order.Confirm()
        );
    }
    
    [Fact]
    public void Cancel_FromAnyState_TransitionsToCanceled()
    {
        var states = new[] { OrderStatus.New, OrderStatus.Confirmed, OrderStatus.Shipped };
        
        foreach (var state in states)
        {
            _order = new Order { Status = state };
            
            _order.Cancel();
            
            Assert.Equal(OrderStatus.Canceled, _order.Status);
        }
    }
    
    [Fact]
    public void Ship_FromNewState_ThrowsInvalidOperationException()
    {
        _order = new Order { Status = OrderStatus.New };
        
        // Should not be able to ship directly from New
        Assert.Throws<InvalidOperationException>(
            () => _order.Ship()
        );
    }
}
```

### Example 2: User Authentication States

**States**: `LoggedOut` → `LoggingIn` → `LoggedIn` → `LoggingOut` → `LoggedOut`

```csharp
public class AuthenticationStateTests
{
    [Theory]
    [InlineData(AuthState.LoggedOut)]
    [InlineData(AuthState.LoggedIn)]
    public void Login_FromValidStates_TransitionsToLoggedIn(AuthState startState)
    {
        var auth = new Authentication { State = startState };
        
        auth.Login("user", "password");
        
        Assert.Equal(AuthState.LoggedIn, auth.State);
    }
    
    [Fact]
    public void Login_FromLoggingInState_ThrowsInvalidStateException()
    {
        var auth = new Authentication { State = AuthState.LoggingIn };
        
        Assert.Throws<InvalidStateException>(
            () => auth.Login("user", "password")
        );
    }
}
```

## Combining Techniques

Real-world testing often combines multiple techniques:

```csharp
[Theory]
// Equivalence Partitions: Valid age range (18-65) and invalid ranges
// Boundary Values: 18, 19, 64, 65 (boundaries of valid range)
// Decision Table: Combine with employment status condition
[InlineData(17, true, false)]          // EP: too young, BVA: lower-1
[InlineData(18, true, true)]           // EP: valid, BVA: lower boundary, DT: employed
[InlineData(18, false, false)]         // EP: valid, BVA: lower boundary, DT: not employed
[InlineData(25, true, true)]           // EP: valid, DT: employed
[InlineData(64, true, true)]           // EP: valid, BVA: upper-1, DT: employed
[InlineData(65, true, false)]          // EP: senior (different partition), BVA: upper
[InlineData(66, true, false)]          // EP: senior, BVA: upper+1
public void IsEligible_WithVariousConditions_ReturnsExpected(
    int age, bool isEmployed, bool expectedEligible)
{
    var result = EligibilityService.IsEligible(age, isEmployed);
    Assert.Equal(expectedEligible, result);
}
```
