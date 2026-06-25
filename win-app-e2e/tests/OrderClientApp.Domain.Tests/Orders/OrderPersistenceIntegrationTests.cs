using Microsoft.Extensions.DependencyInjection;
using OrderClientApp.Application.Abstractions.Auth;
using OrderClientApp.Application.Abstractions.Orders;
using OrderClientApp.Application.DependencyInjection;
using OrderClientApp.Domain.Orders;
using OrderClientApp.Infrastructure.DependencyInjection;

namespace OrderClientApp.Domain.Tests.Orders;

public sealed class OrderPersistenceIntegrationTests : IDisposable
{
    private readonly string _databasePath;

    public OrderPersistenceIntegrationTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"order-client-test-{Guid.NewGuid():N}.db");
    }

    [Fact]
    public async Task CreateAndListAndSoftDeleteOrder_WorksWithSqlitePersistence()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();
        services.AddInfrastructure(_databasePath);

        using var provider = services.BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            var authInitializer = scope.ServiceProvider.GetRequiredService<IAuthDatabaseInitializer>();
            await authInitializer.InitializeAsync();
            var orderInitializer = scope.ServiceProvider.GetRequiredService<IOrderDatabaseInitializer>();
            await orderInitializer.InitializeAsync();
        }

        Guid orderId;
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IOrderService>();
            var created = await service.CreateAsync(new CreateOrderRequest(
                Guid.NewGuid(),
                "仕入先A",
                DateTimeOffset.UtcNow,
                "integration",
                0.1m,
                new[]
                {
                    new CreateOrderLineItemInput("P001", "商品A", 2, 300m),
                    new CreateOrderLineItemInput("P002", "商品B", 1, 200m)
                }));

            orderId = created.Id;
            Assert.Equal("PO-0001", created.OrderNumber);
            Assert.Equal(800m, created.AmountExcludingTax);
            Assert.Equal(880m, created.AmountIncludingTax);
        }

        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IOrderService>();
            var list = await service.ListAsync(new OrderListQuery(OrderStatus.Unprocessed, null, null, 1, 10));
            Assert.Single(list.Items);

            await service.SoftDeleteAsync(orderId);

            var listAfterDelete = await service.ListAsync(new OrderListQuery(null, null, null, 1, 10));
            Assert.Empty(listAfterDelete.Items);

            var deleted = await service.GetByIdAsync(orderId, includeDeleted: true);
            Assert.NotNull(deleted);
            Assert.True(deleted!.IsDeleted);
            Assert.Equal(OrderStatus.Canceled, deleted.Status);
        }
    }

    public void Dispose()
    {
        if (File.Exists(_databasePath))
        {
            try
            {
                File.Delete(_databasePath);
            }
            catch (IOException)
            {
                // ignore cleanup race on test host.
            }
        }
    }
}
