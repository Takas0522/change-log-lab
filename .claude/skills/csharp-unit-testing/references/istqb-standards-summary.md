# ISTQB Standards Summary for Unit Testing

## ISTQB Foundation Level - Key Concepts

### Testing Principles (ISTQB Foundation)

1. **Testing shows presence of defects**, not absence
   - Testing can reduce risk but cannot guarantee complete correctness
   - Exhaustive testing is impossible

2. **Early testing** saves time and cost
   - Defect prevention is more effective than defect detection
   - Testing should start as early as possible in development

3. **Defects cluster together**
   - A small number of modules contain most defects (Pareto principle)
   - Focus testing efforts on high-risk areas

4. **Pesticide paradox**
   - Repeatedly running same tests becomes ineffective
   - Tests must be reviewed and updated regularly

5. **Testing is context-dependent**
   - Testing approach varies by:
     - Software type (safety-critical vs. web application)
     - Development methodology (Agile vs. Waterfall)
     - Risk profile and stakeholder needs

6. **Absence-of-errors fallacy**
   - Achieving high coverage doesn't guarantee user satisfaction
   - Testing must verify both functionality AND usability

7. **Testing requires independence**
   - Test design by same person who wrote code has blind spots
   - Independent testing finds different defects

### Testing Levels (V-Model)

```
Unit Testing (Component Testing)
↓
Integration Testing
↓
System Testing
↓
Acceptance Testing
```

**Unit Testing Focus:**
- Individual components tested in isolation
- Defects detected early (cheaper to fix)
- Uses stubs and mocks for dependencies
- Tests focus on:
  - Correct input/output
  - Boundary conditions
  - Error handling
  - State transitions

### Test Case Components (ISTQB Definition)

A complete test case includes:

1. **Test Case ID**: Unique identifier
2. **Test Case Name**: Clear description
3. **Preconditions**: Initial state required
4. **Test Steps**: Detailed sequence of actions
5. **Test Data**: Input values and expected outputs
6. **Expected Results**: What should happen
7. **Postconditions**: Final state after test

```csharp
/*
Test Case: UserRegistration_ValidData_CreatesUser
Precondition: User database is empty
Test Steps:
  1. Call RegisterUser("john@example.com", "password123")
  2. Verify user is created in database
  3. Verify email confirmation sent
Expected Result: User account created, confirmation email sent
Postcondition: User exists in system with ACTIVE status
*/
```

## Test Design Techniques (Static & Dynamic)

### Specification-Based Techniques

#### 1. Equivalence Partitioning (EP)

**Concept**: Divide input domain into equivalence classes where:
- All inputs in a class should behave the same way
- One representative from each class is sufficient

**Example: Age-based discount calculation**

```
Valid partitions:
  - Age < 18: No discount
  - Age 18-64: 10% discount
  - Age 65+: 20% discount

Invalid partition:
  - Age < 0: Error
  - Age > 150: Error (unrealistic)
```

**Test cases needed**: 5 minimum (3 valid + 2 invalid)

#### 2. Boundary Value Analysis (BVA)

**Concept**: Test at partition boundaries and immediately adjacent values

**For range [18, 65]:**
```
Lower boundary:  17, 18, 19
Upper boundary:  64, 65, 66
```

**Why**: Off-by-one errors are common, especially with `<` vs `<=`

#### 3. Decision Table Testing

**Concept**: Test all combinations of conditions

**Example: Loan approval (simplified)**

| Annual Income | Credit Score | Employment | Approved |
|---|---|---|---|
| ≥ $50K | ≥ 650 | ≥ 2 years | YES |
| ≥ $50K | ≥ 650 | < 2 years | NO |
| ≥ $50K | < 650 | Any | NO |
| < $50K | Any | Any | NO |

**Test cases**: One test per row = 4 tests minimum

#### 4. State Transition Testing

**Concept**: Test valid and invalid state transitions

**Example: Order processing**

```
States: New → Confirmed → Shipped → Delivered → Closed
Canceled (from any state)

Valid transitions:
  New → Confirmed (action: confirm)
  Confirmed → Shipped (action: ship)
  Shipped → Delivered (action: deliver)
  Delivered → Closed (action: close)
  Any → Canceled (action: cancel)

Invalid transitions to test:
  New → Shipped (skip confirmed)
  Closed → Confirmed (can't reopen)
  Delivered → Shipped (can't go backward)
```

### Structure-Based Techniques

#### Statement Coverage
- Percentage of executable statements executed by test suite
- **Goal**: 70-80% minimum
- **Limitation**: Doesn't ensure all branches are tested

```csharp
public bool IsEligible(int age, bool hasJob)
{
    if (age >= 18)                    // Statement 1
    {
        if (hasJob)                   // Statement 2
            return true;              // Statement 3
    }
    return false;                     // Statement 4
}

// Coverage: 100% with just:
// Test 1: age=25, hasJob=true → passes Statement 1, 2, 3
// BUT Statement 2 never evaluates to false!
```

#### Branch Coverage (Decision Coverage)
- Each decision has true AND false outcomes
- **Goal**: 80%+ for business logic
- **Stronger** than statement coverage

```csharp
// To achieve branch coverage above:
// Test 1: age=25, hasJob=true (if-age=true, if-hasJob=true)
// Test 2: age=25, hasJob=false (if-age=true, if-hasJob=false)
// Test 3: age=10, hasJob=true (if-age=false, if-hasJob=N/A)
```

#### Path Coverage
- Each possible execution path through code
- Often infeasible due to exponential combinations
- Reserved for critical code paths

## Test Phases & Activities

### Planning Phase
1. Analyze test strategy and risk
2. Identify test scope
3. Define test objectives
4. Document entry/exit criteria

### Preparation Phase
1. Design test cases
2. Create test data
3. Set up test environment
4. Prepare test tools and automation

### Execution Phase
1. Run tests according to test plan
2. Log actual vs. expected results
3. Report defects with reproducibility information
4. Track test progress

### Closure Phase
1. Verify all planned tests executed
2. Archive test artifacts
3. Evaluate test effectiveness
4. Identify lessons learned

## Defect Management

### Defect Report Essentials

Every defect report should include:
1. **Title**: Clear, concise description
2. **Severity**: Critical, Major, Minor, Trivial
3. **Reproducibility**: Always, Often, Sometimes, Cannot reproduce
4. **Steps to reproduce**: Exact sequence (Arrange-Act-Assert format)
5. **Expected vs. Actual**: Clear comparison
6. **Environment**: OS, .NET version, configuration
7. **Attachments**: Screenshots, logs, test data

### Severity Classification (ISTQB)

| Severity | Impact | Example |
|---|---|---|
| **Critical** | System unusable, data loss, security breach | Application crashes on startup |
| **Major** | Significant functional impact | Calculation produces wrong results |
| **Minor** | Workaround exists, cosmetic issue | Button label misaligned |
| **Trivial** | Insignificant impact | Typo in help text |

## Entry & Exit Criteria

### Test Entry Criteria (Must be satisfied to START testing)
- Requirements baseline approved
- Test environment available and verified
- Test data prepared and verified
- Build deployed successfully
- Critical path functionality available

### Test Exit Criteria (Must be satisfied to COMPLETE testing)
- All planned tests executed
- Coverage target met (80%+)
- All critical/major defects resolved
- All test deliverables signed off
- Time/resource limits within plan

## Test Case Organization in xUnit

```csharp
namespace MyProject.Tests
{
    public class CalculatorTests                              // Test Class
    {
        private readonly Calculator _calculator;
        
        public CalculatorTests()
        {
            _calculator = new Calculator();                   // Fixture Setup
        }
        
        [Fact]                                                // Test Case
        public void Add_TwoPositiveNumbers_ReturnsSum()       // Test Name (descriptive)
        {
            // Arrange
            var a = 5;
            var b = 3;
            var expected = 8;
            
            // Act
            var result = _calculator.Add(a, b);
            
            // Assert
            Assert.Equal(expected, result);
        }
    }
}
```

## ISTQB Certification Levels

### Foundation Level
- Basic testing concepts
- Test design techniques
- Test management
- Tool support

### Advanced Level - Test Analyst
- Advanced test design
- Test organization and management
- Risk-based testing
- Defect management

### Advanced Level - Technical Test Analyst
- Advanced technical test design
- Test automation
- Performance and security testing

## References

- **ISTQB Foundation Syllabus** - Version 4.0 (covers fundamental concepts)
- **ISTQB Advanced Test Analyst Syllabus** - For advanced techniques
- **Applying the ISTQB Standard** - Real-world application guide
