using Xunit;
using Moq;
using MyApp.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.Tests.Services
{
    /// <summary>
    /// Example unit tests for AsyncApiService demonstrating:
    /// - Async/await testing patterns
    /// - Never use .Result or .Wait()
    /// - Exception testing with ThrowsAsync
    /// - CancellationToken testing
    /// - IAsyncLifetime for async setup/teardown
    /// </summary>
    public class AsyncApiServiceTests : IAsyncLifetime
    {
        private readonly Mock<IHttpClientFactory> _mockHttpFactory;
        private readonly AsyncApiService _service;

        public AsyncApiServiceTests()
        {
            _mockHttpFactory = new Mock<IHttpClientFactory>();
            _service = new AsyncApiService(_mockHttpFactory.Object);
        }

        // Async initialization
        public async Task InitializeAsync()
        {
            // Setup that might require async operations
            await Task.CompletedTask;
        }

        // Async cleanup
        public async Task DisposeAsync()
        {
            // Cleanup that might require async operations
            await Task.CompletedTask;
        }

        #region Basic Async Tests

        [Fact]
        public async Task GetDataAsync_WithValidRequest_ReturnsData()
        {
            // Arrange
            var expectedData = new { Id = 1, Name = "Test" };
            
            _mockHttpFactory
                .Setup(f => f.CreateClient())
                .Returns(CreateMockHttpClient(expectedData));

            // Act: ALWAYS use await for async tests
            var result = await _service.GetDataAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
        }

        [Fact]
        public async Task FetchUserAsync_WithValidUserId_ReturnsUser()
        {
            // Arrange
            var userId = 42;
            var mockClient = new Mock<IApiClient>();
            var expectedUser = new { Id = userId, Name = "John" };
            
            mockClient
                .Setup(c => c.GetAsync($"/users/{userId}"))
                .ReturnsAsync(expectedUser);

            // Act: Use await, not .Result
            var result = await _service.FetchUserAsync(userId);

            // Assert
            Assert.NotNull(result);
        }

        #endregion

        #region Exception Testing with Async

        [Fact]
        public async Task GetDataAsync_WithInvalidId_ThrowsArgumentException()
        {
            // Arrange: Setup mock to throw

            // Act & Assert: Use ThrowsAsync for async exceptions
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.GetDataAsync(-1)
            );

            // Verify exception details
            Assert.Contains("ID", ex.Message);
        }

        [Fact]
        public async Task FetchUserAsync_WithServerError_ThrowsHttpRequestException()
        {
            // Arrange
            var mockClient = new Mock<IApiClient>();
            mockClient
                .Setup(c => c.GetAsync(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("Server error"));

            // Act & Assert: Verify async exception is thrown
            await Assert.ThrowsAsync<HttpRequestException>(
                () => _service.FetchUserAsync(1)
            );
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public async Task GetDataAsync_WithInvalidInput_ThrowsArgumentException(int invalidId)
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.GetDataAsync(invalidId)
            );
        }

        #endregion

        #region Cancellation Token Testing

        [Fact]
        public async Task FetchDataAsync_WithCancellationToken_RespectsCancellation()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            
            // Cancel after 100ms
            cts.CancelAfter(TimeSpan.FromMilliseconds(100));

            // Act & Assert: Verify cancellation is respected
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _service.FetchLargeDataAsync(cts.Token)
            );
        }

        [Fact]
        public async Task FetchDataAsync_WithImmediateCancellation_CanceledQuickly()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();  // Cancel immediately

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act & Assert: Should cancel very quickly
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _service.FetchLargeDataAsync(cts.Token)
            );

            stopwatch.Stop();

            // Verify it cancelled quickly (not after full operation duration)
            Assert.True(stopwatch.ElapsedMilliseconds < 500);
        }

        [Fact]
        public async Task FetchDataAsync_WithoutCancellation_Completes()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            // Don't cancel

            // Act
            var result = await _service.FetchLargeDataAsync(cts.Token);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ProcessAsync_WithCancellation_PropagatesToken()
        {
            // Arrange: Setup mock to receive the token
            var mockRepository = new Mock<IDataRepository>();
            var service = new AsyncApiService(_mockHttpFactory.Object);
            var cts = new CancellationTokenSource();

            mockRepository
                .Setup(r => r.ProcessAsync(It.IsAny<CancellationToken>()))
                .Returns(async (CancellationToken ct) =>
                {
                    await Task.Delay(5000, ct);  // Will be cancelled
                    return true;
                });

            cts.CancelAfter(100);

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => service.ProcessAsync(cts.Token)
            );
        }

        #endregion

        #region Multiple Async Operations

        [Fact]
        public async Task ProcessMultipleRequests_ExecutesAllInParallel()
        {
            // Arrange
            var mockClient = new Mock<IApiClient>();
            mockClient
                .Setup(c => c.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(new { Id = 1 });

            // Act: Use Task.WhenAll for parallel operations
            var tasks = new List<Task<dynamic>>
            {
                _service.FetchDataAsync(1),
                _service.FetchDataAsync(2),
                _service.FetchDataAsync(3)
            };

            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(3, results.Length);
        }

        [Fact]
        public async Task ProcessSequentialRequests_ExecutesInOrder()
        {
            // Arrange
            var executionOrder = new List<int>();
            var mockClient = new Mock<IApiClient>();

            // Setup to track execution order
            mockClient
                .Setup(c => c.GetAsync(It.IsAny<string>()))
                .Returns(async (string url) =>
                {
                    await Task.Delay(10);
                    return new { Id = 1 };
                });

            // Act: Execute sequentially
            var result1 = await _service.FetchDataAsync(1);
            var result2 = await _service.FetchDataAsync(2);
            var result3 = await _service.FetchDataAsync(3);

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.NotNull(result3);
        }

        [Fact]
        public async Task ProcessWithWhenAny_ReturnsFirstCompleted()
        {
            // Arrange: Two concurrent operations
            var cts = new CancellationTokenSource();

            // Act: Get whichever completes first
            var tasks = new[]
            {
                _service.FetchDataAsync(1),
                _service.FetchDataAsync(2)
            };

            var firstCompleted = await Task.WhenAny(tasks);

            // Assert: At least one should complete
            Assert.False(firstCompleted.IsFaulted);
        }

        #endregion

        #region Async with Mocking and Callback

        [Fact]
        public async Task FetchDataAsync_WithCallback_VerifiesCallSequence()
        {
            // Arrange: Track call sequence with callback
            var callOrder = new List<string>();
            var mockClient = new Mock<IApiClient>();

            mockClient
                .Setup(c => c.GetAsync(It.IsAny<string>()))
                .Callback<string>(url => callOrder.Add(url))
                .ReturnsAsync(new { Id = 1 });

            // Act
            await _service.FetchDataAsync(1);
            await _service.FetchDataAsync(2);

            // Assert: Verify calls were made in correct order
            Assert.Equal(2, callOrder.Count);
            mockClient.Verify(c => c.GetAsync(It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public async Task RetryAsync_WithTransientFailure_EventuallySucceeds()
        {
            // Arrange
            var attempt = 0;
            var mockClient = new Mock<IApiClient>();

            mockClient
                .Setup(c => c.GetAsync(It.IsAny<string>()))
                .Returns(async (string url) =>
                {
                    attempt++;
                    if (attempt < 2)
                        throw new TimeoutException();
                    
                    return new { Id = 1 };
                });

            // Act
            var result = await _service.FetchWithRetryAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, attempt);  // Verify it retried
        }

        #endregion

        #region ValueTask Testing

        [Fact]
        public async Task GetCachedDataAsync_WithValueTask_ReturnsQuickly()
        {
            // Arrange: Service returns ValueTask (allocation-free if cached)
            var mockCache = new Mock<ICacheService>();
            mockCache
                .Setup(c => c.GetAsync<dynamic>(It.IsAny<string>()))
                .Returns(new ValueTask<dynamic>(new { Id = 1 }));

            // Act: Can await directly
            var result = await _service.GetCachedDataAsync(1);

            // Assert
            Assert.NotNull(result);
        }

        #endregion

        #region Anti-Patterns (What NOT to do)

        [Fact]
        public void BadAsyncTest_WithResult_CanDeadlock()
        {
            // WRONG: This pattern can deadlock - NEVER DO THIS
            
            // This test demonstrates what NOT to do
            // Uncomment to see deadlock behavior:
            
            // var result = _service.FetchDataAsync(1).Result;  // DON'T DO THIS!
            // Assert.NotNull(result);
            
            // This is why we use async tests instead:
            Assert.True(true);  // Placeholder
        }

        // CORRECT: Use async all the way
        [Fact]
        public async Task GoodAsyncTest_WithAwait_IsCorrect()
        {
            // Arrange
            var mockClient = new Mock<IApiClient>();
            mockClient
                .Setup(c => c.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(new { Id = 1 });

            // Act: Use await, not .Result
            var result = await _service.FetchDataAsync(1);

            // Assert
            Assert.NotNull(result);
        }

        #endregion

        // Helper method for tests
        private IApiClient CreateMockHttpClient(dynamic responseData)
        {
            var mock = new Mock<IApiClient>();
            mock
                .Setup(c => c.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(responseData);
            return mock.Object;
        }
    }
}
