using Microsoft.Extensions.DependencyInjection;
using OrderClientApp.Application.Abstractions.Analytics;
using OrderClientApp.Application.Abstractions.Auth;
using OrderClientApp.Application.Abstractions.Orders;
using OrderClientApp.Application.Abstractions.Products;
using OrderClientApp.Application.Abstractions.Suppliers;
using OrderClientApp.Application.DependencyInjection;
using OrderClientApp.Infrastructure.DependencyInjection;

namespace OrderClientApp.Domain.Tests.Orders;

public sealed class MasterAnalyticsIntegrationTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"order-client-master-test-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task ProductCsvImport_ValidatesAndImportsRows()
    {
        using var provider = BuildProvider();
        await InitializeAsync(provider);

        using var scope = provider.CreateScope();
        var productService = scope.ServiceProvider.GetRequiredService<IProductService>();

        var csv = """
            productCode,name,unitPriceExcludingTax,unit,notes,category,reorderPoint
            P-001,商品A,100,個,備考A,カテゴリA,10
            P-002,商品B,-10,個,備考B,カテゴリA,5
            P-001,商品C,200,個,,カテゴリB,3
            """;

        var result = await productService.ImportCsvAsync(csv);
        Assert.Equal(3, result.TotalRows);
        Assert.Equal(1, result.ImportedCount);
        Assert.True(result.ErrorCount >= 2);

        var products = await productService.ListAsync(new ProductFilter(null, null, null));
        Assert.Single(products);
        Assert.Equal("P-001", products.First().ProductCode);
    }

    [Fact]
    public async Task SupplierPriceAndPreferredSupplierMapping_Works()
    {
        using var provider = BuildProvider();
        await InitializeAsync(provider);

        using var scope = provider.CreateScope();
        var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
        var supplierService = scope.ServiceProvider.GetRequiredService<ISupplierService>();

        var product = await productService.CreateAsync(new CreateProductRequest("P-100", "商品100", 150m, "個", null, "カテゴリX", 5));
        var supplier = await supplierService.CreateAsync(new CreateSupplierRequest("株式会社サプライヤ", "担当A", "a@example.com", "000-1111", null));

        await supplierService.SetProductSupplierPriceAsync(product.Id, supplier.Id, 120m);
        await productService.SetPreferredSupplierAsync(product.Id, supplier.Id);

        var prices = await supplierService.GetProductSupplierPricesAsync(product.Id);
        Assert.Single(prices);
        Assert.Equal(120m, prices.First().UnitPriceExcludingTax);

        var updatedProduct = await productService.GetByIdAsync(product.Id);
        Assert.NotNull(updatedProduct);
        Assert.Equal(supplier.Id, updatedProduct!.PreferredSupplierId);
    }

    [Fact]
    public async Task AnalyticsDashboard_ReturnsMonthlyAndProductAggregates()
    {
        using var provider = BuildProvider();
        await InitializeAsync(provider);

        using var scope = provider.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var analyticsService = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();
        var userId = Guid.NewGuid();

        await orderService.CreateAsync(new CreateOrderRequest(
            userId,
            "仕入先A",
            new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero),
            null,
            null,
            null,
            null,
            null,
            null,
            0.1m,
            [new CreateOrderLineItemInput("P-1", "商品1", 2, 100m)]));

        await orderService.CreateAsync(new CreateOrderRequest(
            userId,
            "仕入先A",
            new DateTimeOffset(2026, 2, 10, 0, 0, 0, TimeSpan.Zero),
            null,
            null,
            null,
            null,
            null,
            null,
            0.1m,
            [new CreateOrderLineItemInput("P-1", "商品1", 1, 100m), new CreateOrderLineItemInput("P-2", "商品2", 1, 300m)]));

        var dashboard = await analyticsService.GetDashboardAsync(
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 12, 31, 23, 59, 59, TimeSpan.Zero),
            10);

        Assert.True(dashboard.Monthly.Count >= 2);
        Assert.Contains(dashboard.Monthly, x => x.Month == "2026-01" && x.OrderCount == 1);
        Assert.Contains(dashboard.ProductWise, x => x.ProductCode == "P-1");
        Assert.NotEmpty(dashboard.MonthlyAmountLine);
        Assert.NotEmpty(dashboard.ProductAmountBar);
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

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();
        services.AddInfrastructure(_databasePath);
        return services.BuildServiceProvider();
    }

    private static async Task InitializeAsync(ServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var authInitializer = scope.ServiceProvider.GetRequiredService<IAuthDatabaseInitializer>();
        await authInitializer.InitializeAsync();
        var orderInitializer = scope.ServiceProvider.GetRequiredService<IOrderDatabaseInitializer>();
        await orderInitializer.InitializeAsync();
    }
}
