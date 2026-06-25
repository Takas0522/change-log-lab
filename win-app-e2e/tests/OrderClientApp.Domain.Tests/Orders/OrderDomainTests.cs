using OrderClientApp.Domain.Orders;

namespace OrderClientApp.Domain.Tests.Orders;

public class OrderDomainTests
{
    [Fact]
    public void OrderNumber_FromSequence_GeneratesExpectedFormat()
    {
        var orderNumber = OrderNumber.FromSequence(1);

        Assert.Equal("PO-0001", orderNumber.Value);
    }

    [Fact]
    public void AmountCalculation_WorksForTaxExclusiveAndInclusive()
    {
        var order = CreateOrder(
            taxRate: 0.1m,
            new OrderLineItem(Guid.NewGuid(), "P001", "商品A", 2, 100m),
            new OrderLineItem(Guid.NewGuid(), "P002", "商品B", 1, 50m));

        Assert.Equal(250m, order.AmountExcludingTax);
        Assert.Equal(275m, order.AmountIncludingTax);
    }

    [Fact]
    public void TransitionTo_ThrowsForInvalidTransition()
    {
        var order = CreateOrder();

        Assert.Throws<InvalidOperationException>(() => order.TransitionTo(OrderStatus.Completed, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void SoftDelete_SetsDeleteFlagAndCanceledStatus()
    {
        var order = CreateOrder();

        order.SoftDelete(DateTimeOffset.UtcNow);

        Assert.True(order.IsDeleted);
        Assert.NotNull(order.DeletedAtUtc);
        Assert.Equal(OrderStatus.Canceled, order.Status);
    }

    private static Order CreateOrder(decimal taxRate = 0.1m, params OrderLineItem[] lineItems)
    {
        var items = lineItems.Length == 0
            ? new[] { new OrderLineItem(Guid.NewGuid(), "P001", "商品A", 1, 100m) }
            : lineItems;

        return new Order(
            Guid.NewGuid(),
            OrderNumber.FromSequence(1),
            Guid.NewGuid(),
            "仕入先A",
            DateTimeOffset.UtcNow,
            OrderStatus.Unprocessed,
            null,
            taxRate,
            items,
            isDeleted: false,
            deletedAtUtc: null,
            createdAtUtc: DateTimeOffset.UtcNow,
            updatedAtUtc: DateTimeOffset.UtcNow);
    }
}
