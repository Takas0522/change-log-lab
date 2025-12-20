using Xunit;
using MyApp.Services;
using MyApp.Models;

namespace MyApp.Tests.Services
{
    /// <summary>
    /// Example unit tests for CalculatorService demonstrating:
    /// - Equivalence partitioning
    /// - Boundary value analysis
    /// - Multiple assertion patterns
    /// - Test naming conventions
    /// </summary>
    public class CalculatorServiceTests
    {
        private readonly CalculatorService _calculator;

        public CalculatorServiceTests()
        {
            _calculator = new CalculatorService();
        }

        #region Addition Tests - Equivalence Partitioning & Boundary Values

        [Theory]
        [InlineData(0, 0, 0)]              // Boundary: zero values
        [InlineData(1, 1, 2)]              // Valid: positive integers
        [InlineData(-1, -1, -2)]           // Valid: negative integers
        [InlineData(100, 200, 300)]        // Valid: larger numbers
        [InlineData(int.MaxValue - 1, 1, int.MaxValue)]  // Boundary: near max
        public void Add_WithValidIntegers_ReturnsSum(int a, int b, int expected)
        {
            // Arrange - test data already provided by InlineData

            // Act
            var result = _calculator.Add(a, b);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Add_WithMaxIntegerOverflow_ThrowsOverflowException()
        {
            // Arrange
            var a = int.MaxValue;
            var b = 1;  // This will overflow

            // Act & Assert
            Assert.Throws<OverflowException>(() => _calculator.Add(a, b));
        }

        #endregion

        #region Subtraction Tests

        [Theory]
        [InlineData(10, 5, 5)]             // Valid: positive - positive = positive
        [InlineData(5, 5, 0)]              // Boundary: equal values (zero result)
        [InlineData(5, 10, -5)]            // Valid: smaller - larger = negative
        [InlineData(-10, -5, -5)]          // Valid: negative - negative
        public void Subtract_WithVariousInputs_ReturnsCorrectDifference(int a, int b, int expected)
        {
            var result = _calculator.Subtract(a, b);
            Assert.Equal(expected, result);
        }

        #endregion

        #region Division Tests - Boundary & Error Cases

        [Theory]
        [InlineData(100, 2, 50)]           // Valid: even division
        [InlineData(100, 10, 10)]          // Valid: clean division
        [InlineData(1, 1, 1)]              // Boundary: divide by itself
        [InlineData(0, 5, 0)]              // Boundary: zero numerator
        public void Divide_WithValidInputs_ReturnsQuotient(int dividend, int divisor, int expected)
        {
            var result = _calculator.Divide(dividend, divisor);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Divide_ByZero_ThrowsDivideByZeroException()
        {
            // Arrange - implicit (no setup needed)

            // Act & Assert - combined for exception tests
            Assert.Throws<DivideByZeroException>(() => _calculator.Divide(100, 0));
        }

        [Theory]
        [InlineData(101, 10)]              // Quotient > expected range
        [InlineData(-1, 10)]               // Negative inputs
        public void Divide_WithInvalidInputs_ThrowsArgumentException(int dividend, int divisor)
        {
            var ex = Assert.Throws<ArgumentException>(() => _calculator.Divide(dividend, divisor));
            Assert.Contains("invalid", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region Percentage Calculation Tests - Decision Table Pattern

        // Decision Table: IsValidPercentage conditions
        // Input | Expected | Description
        // 0     | true     | Boundary: minimum
        // 50    | true     | Valid: mid-range
        // 100   | true     | Boundary: maximum
        // -1    | false    | Invalid: below minimum
        // 101   | false    | Invalid: above maximum
        // null  | false    | Invalid: null input

        [Theory]
        [InlineData(0, true)]              // Boundary: minimum valid
        [InlineData(50, true)]             // Equivalence: valid range
        [InlineData(100, true)]            // Boundary: maximum valid
        [InlineData(-1, false)]            // Equivalence: invalid (below range)
        [InlineData(101, false)]           // Equivalence: invalid (above range)
        public void IsValidPercentage_WithVariousValues_ReturnsCorrectly(decimal percentage, bool expected)
        {
            // Arrange (via InlineData)

            // Act
            var result = _calculator.IsValidPercentage(percentage);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(100, 0, 100)]          // Discount: 0% → no change
        [InlineData(100, 0.5m, 50)]        // Discount: 50% → half price
        [InlineData(100, 1, 0)]            // Discount: 100% → free
        [InlineData(200, 0.1m, 180)]       // Discount: 10% → 90% of original
        public void ApplyDiscount_WithValidPercentage_ReturnsDiscountedAmount(
            decimal originalAmount, decimal discountPercentage, decimal expected)
        {
            var result = _calculator.ApplyDiscount(originalAmount, discountPercentage);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ApplyDiscount_WithInvalidPercentage_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => _calculator.ApplyDiscount(100, 1.5m)
            );
            Assert.Contains("percentage", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region State Transition Tests (Accumulator Pattern)

        [Fact]
        public void Reset_ClearsAccumulator_SetsToZero()
        {
            // Arrange: Create new calculator with some accumulated value
            _calculator.Add(5, 5);  // Result: 10 (accumulated)
            Assert.Equal(10, _calculator.Accumulator);

            // Act
            _calculator.Reset();

            // Assert: Verify state transition from "has value" to "empty"
            Assert.Equal(0, _calculator.Accumulator);
        }

        [Fact]
        public void UseAccumulator_WithoutPriorCalculation_ThrowsInvalidOperationException()
        {
            // Arrange: Fresh calculator (no prior calculation)

            // Act & Assert
            Assert.Throws<InvalidOperationException>(
                () => _calculator.UseAccumulator()
            );
        }

        [Fact]
        public void UseAccumulator_AfterCalculation_ReturnsAccumulatedValue()
        {
            // Arrange: Perform a calculation to populate accumulator
            _calculator.Add(10, 20);

            // Act
            var result = _calculator.UseAccumulator();

            // Assert: Verify state used correctly
            Assert.Equal(30, result);
        }

        #endregion

        #region Multiple Assertions (Testing Single Cohesive Behavior)

        [Fact]
        public void CalculatorInstance_CreatedFresh_IsInitializedCorrectly()
        {
            // Arrange & Act: Implicit - we're testing the constructor state
            var freshCalculator = new CalculatorService();

            // Assert: All assertions verify the SAME behavior: proper initialization
            // This is appropriate because they all verify the initial state
            Assert.Equal(0, freshCalculator.Accumulator);
            Assert.False(freshCalculator.HasPendingValue);
            Assert.NotNull(freshCalculator.LastOperation);
            Assert.Equal(CalculatorOperation.None, freshCalculator.LastOperation);
        }

        #endregion

        #region Edge Case & Stress Tests

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(1000)]
        [InlineData(int.MaxValue / 2)]
        public void Divide_WithExtremeBoundaries_StaysWithinRange(int value)
        {
            // Arrange
            var divisor = 2;

            // Act
            var result = _calculator.Divide(value, divisor);

            // Assert: Verify no overflow and sensible result
            Assert.True(result >= 0);
            Assert.True(result <= value);
        }

        #endregion
    }
}
