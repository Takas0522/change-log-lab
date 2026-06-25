using Microsoft.Extensions.DependencyInjection;
using OrderClientApp.Application.Abstractions.Analytics;
using OrderClientApp.Application.Abstractions.Auth;
using OrderClientApp.Application.Abstractions.Orders;
using OrderClientApp.Application.Abstractions.Products;
using OrderClientApp.Application.Abstractions.Suppliers;
using OrderClientApp.Application.DependencyInjection;
using OrderClientApp.Domain.Auth;
using OrderClientApp.Domain.Orders;
using OrderClientApp.Infrastructure.DependencyInjection;

namespace OrderClientApp.Domain.Tests.Workflows;

public sealed class CriticalWorkflowIntegrationTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"order-client-critical-workflow-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task Auth_SeededUsersCanLoginAndRoleGatingWorks()
    {
        using var provider = await BuildProviderAsync();
        using var scope = provider.CreateScope();
        var authenticationService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
        var authorizationService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

        var general = await authenticationService.LoginAsync("general.user", "General#2026");
        var approver = await authenticationService.LoginAsync("approver.user", "Approver#2026");
        var admin = await authenticationService.LoginAsync("admin.user", "Admin#2026");

        Assert.True(general.IsSuccess);
        Assert.True(approver.IsSuccess);
        Assert.True(admin.IsSuccess);
        Assert.NotNull(admin.User);
        Assert.True(authorizationService.CanAccess(admin.User!, UserRole.Approver));
        Assert.False(authorizationService.CanAccess(general.User!, UserRole.Approver));
    }

    [Fact]
    public async Task OrderCore_BulkCreateTemplateAndPaging_Works()
    {
        using var provider = await BuildProviderAsync();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IOrderService>();

        await service.SaveBudgetSettingsAsync(approvalThreshold: 999999m, monthlyLimit: null, yearlyLimit: null);
        var userId = Guid.NewGuid();

        var bulk = await service.CreateBulkAsync(new CreateBulkOrderRequest(
            userId,
            "仕入先A",
            DateTimeOffset.UtcNow,
            null,
            "bulk",
            null,
            null,
            null,
            null,
            0.1m,
            [
                new CreateOrderLineItemInput("P-001", "商品1", 2, 100m),
                new CreateOrderLineItemInput("P-002", "商品2", 1, 200m)
            ]));

        Assert.Equal("PO-0001", bulk.OrderNumber);
        Assert.Equal(440m, bulk.AmountIncludingTax);

        var template = await service.SaveTemplateAsync(new SaveOrderTemplateRequest(
            userId,
            "TPL-CORE-001",
            "core template",
            bulk.TaxRate,
            bulk.LineItems.Select(x => new CreateOrderLineItemInput(x.ProductCode, x.ProductName, x.Quantity, x.UnitPriceExcludingTax)).ToArray()));
        Assert.Equal("TPL-CORE-001", template.TemplateName);

        for (var i = 0; i < 11; i++)
        {
            await service.CreateAsync(new CreateOrderRequest(
                userId,
                "仕入先B",
                DateTimeOffset.UtcNow.AddMinutes(i),
                null,
                null,
                null,
                null,
                null,
                null,
                0.1m,
                [new CreateOrderLineItemInput($"P-{i + 10:000}", $"商品{i + 10}", 1, 100m)]));
        }

        var page1 = await service.ListAsync(new OrderListQuery(null, null, null, 1, 10));
        var page2 = await service.ListAsync(new OrderListQuery(null, null, null, 2, 10));

        Assert.Equal(12, page1.TotalCount);
        Assert.Equal(10, page1.Items.Count);
        Assert.Equal(2, page2.Items.Count);
    }

    [Fact]
    public async Task CrossModule_ApprovalInventoryBudgetMastersAnalytics_Works()
    {
        using var provider = await BuildProviderAsync();
        using var scope = provider.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
        var supplierService = scope.ServiceProvider.GetRequiredService<ISupplierService>();
        var analyticsService = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();

        await orderService.SaveBudgetSettingsAsync(approvalThreshold: 500m, monthlyLimit: 1000m, yearlyLimit: 10000m);

        var product = await productService.CreateAsync(new CreateProductRequest("PX-001", "商品X", 250m, "個", null, "カテゴリX", 2));
        var supplier = await supplierService.CreateAsync(new CreateSupplierRequest("株式会社X", "担当X", null, null, null));
        await supplierService.SetProductSupplierPriceAsync(product.Id, supplier.Id, 240m);
        await productService.SetPreferredSupplierAsync(product.Id, supplier.Id);

        var created = await orderService.CreateAsync(new CreateOrderRequest(
            Guid.NewGuid(),
            supplier.CompanyName,
            DateTimeOffset.UtcNow,
            null,
            null,
            null,
            null,
            null,
            null,
            0.1m,
            [new CreateOrderLineItemInput(product.ProductCode, product.Name, 3, 240m)]));

        Assert.Equal(OrderStatus.PendingApproval, created.Status);
        Assert.True(created.RequiresApproval);

        var actor = new AuthenticatedUser(Guid.NewGuid(), "approver.user", UserRole.Approver);
        var approved = await orderService.ApproveAsync(created.Id, actor);
        var waiting = await orderService.UpdateAsync(ToUpdateRequest(approved, OrderStatus.WaitingForArrival));
        var line = waiting.LineItems.Single();

        var completed = await orderService.ConfirmReceivingAsync(new ConfirmReceivingRequest(
            waiting.Id,
            [new ConfirmReceivingLineInput(line.Id, line.RemainingQuantity)]));

        Assert.Equal(OrderStatus.Completed, completed.Status);

        var dashboard = await analyticsService.GetDashboardAsync(
            DateTimeOffset.UtcNow.AddMonths(-1),
            DateTimeOffset.UtcNow.AddMonths(1),
            10);
        Assert.Contains(dashboard.ProductWise, x => x.ProductCode == product.ProductCode);
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

    private async Task<ServiceProvider> BuildProviderAsync()
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

    private static UpdateOrderRequest ToUpdateRequest(OrderDto order, OrderStatus status)
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
}
