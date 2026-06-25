using Microsoft.Extensions.DependencyInjection;
using OrderClientApp.Application.Abstractions.Auth;
using OrderClientApp.Application.Abstractions.Orders;
using OrderClientApp.Application.DependencyInjection;
using OrderClientApp.Domain.Auth;
using OrderClientApp.Domain.Orders;
using OrderClientApp.Infrastructure.DependencyInjection;

namespace OrderClientApp.Domain.Tests.Orders;

public sealed class ApprovalInventoryBudgetIntegrationTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"order-client-test-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task Create_WithAmountOverThreshold_SetsPendingApproval()
    {
        using var provider = await CreateProviderAsync();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IOrderService>();
        await service.SaveBudgetSettingsAsync(approvalThreshold: 500m, monthlyLimit: null, yearlyLimit: null);

        var created = await service.CreateAsync(CreateRequest(2, 300m));
        Assert.Equal(OrderStatus.PendingApproval, created.Status);
        Assert.True(created.RequiresApproval);
    }

    [Fact]
    public async Task ApproveReject_EnforcesRoleAndTransitions()
    {
        using var provider = await CreateProviderAsync();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IOrderService>();
        await service.SaveBudgetSettingsAsync(approvalThreshold: 500m, monthlyLimit: null, yearlyLimit: null);
        var created = await service.CreateAsync(CreateRequest(2, 300m));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ApproveAsync(created.Id, new AuthenticatedUser(Guid.NewGuid(), "general", UserRole.General)));

        var rejected = await service.RejectAsync(
            created.Id,
            "金額精査",
            new AuthenticatedUser(Guid.NewGuid(), "approver", UserRole.Approver));
        Assert.Equal(OrderStatus.Rejected, rejected.Status);
        Assert.Equal("金額精査", rejected.RejectionReason);
    }

    [Fact]
    public async Task ConfirmReceiving_UpdatesStatusAndInventory()
    {
        using var provider = await CreateProviderAsync();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var inventoryRepository = scope.ServiceProvider.GetRequiredService<IInventoryRepository>();
        var created = await service.CreateAsync(CreateRequest(2, 100m));

        await service.UpdateAsync(ToUpdate(created, OrderStatus.Processing));
        var waiting = await service.UpdateAsync(ToUpdate(created, OrderStatus.WaitingForArrival));
        var firstLine = waiting.LineItems.First();

        var partial = await service.ConfirmReceivingAsync(new ConfirmReceivingRequest(
            waiting.Id,
            [new ConfirmReceivingLineInput(firstLine.Id, 1)]));
        Assert.Equal(OrderStatus.PartiallyReceived, partial.Status);
        Assert.Equal(1, await inventoryRepository.GetQuantityAsync(firstLine.ProductCode));

        var completed = await service.ConfirmReceivingAsync(new ConfirmReceivingRequest(
            waiting.Id,
            [new ConfirmReceivingLineInput(firstLine.Id, 1)]));
        Assert.Equal(OrderStatus.Completed, completed.Status);
        Assert.Equal(2, await inventoryRepository.GetQuantityAsync(firstLine.ProductCode));
    }

    [Fact]
    public async Task BudgetExceeded_IsReturnedInOrderDto()
    {
        using var provider = await CreateProviderAsync();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IOrderService>();
        await service.SaveBudgetSettingsAsync(approvalThreshold: 0m, monthlyLimit: 100m, yearlyLimit: 1000m);

        var created = await service.CreateAsync(CreateRequest(1, 150m));
        Assert.True(created.BudgetExceeded);
        Assert.NotNull(created.MonthlyBudgetRemaining);
        Assert.True(created.MonthlyBudgetRemaining < 0);
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

    private static UpdateOrderRequest ToUpdate(OrderDto order, OrderStatus status)
        => new(
            order.Id,
            order.SupplierName,
            order.OrderedAtUtc,
            order.ExpectedReceivingDateUtc,
            status,
            order.Note,
            order.DeliveryNoteNumber,
            order.DeliveryNoteDateUtc,
            order.InvoiceNumber,
            order.InvoiceDateUtc,
            order.TaxRate,
            order.LineItems.Select(x => new CreateOrderLineItemInput(x.ProductCode, x.ProductName, x.Quantity, x.UnitPriceExcludingTax)).ToArray());

    private static CreateOrderRequest CreateRequest(int quantity, decimal unitPrice)
        => new(
            Guid.NewGuid(),
            "仕入先A",
            DateTimeOffset.UtcNow,
            null,
            "integration",
            "DN-001",
            DateTimeOffset.UtcNow,
            "INV-001",
            DateTimeOffset.UtcNow,
            0.1m,
            [new CreateOrderLineItemInput("P001", "商品A", quantity, unitPrice)]);

    private async Task<ServiceProvider> CreateProviderAsync()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();
        services.AddInfrastructure(_databasePath);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var authInitializer = scope.ServiceProvider.GetRequiredService<IAuthDatabaseInitializer>();
        await authInitializer.InitializeAsync();
        var orderInitializer = scope.ServiceProvider.GetRequiredService<IOrderDatabaseInitializer>();
        await orderInitializer.InitializeAsync();
        return provider;
    }
}
