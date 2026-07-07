using FsCheck.Xunit;
using OrderClientApp.Domain.Orders;

namespace OrderClientApp.Domain.Tests.Orders;

public class OrderPropertyTests
{
    private static readonly DateTimeOffset FixedNowUtc = new(2026, 7, 6, 0, 0, 0, TimeSpan.Zero);

    [Property(MaxTest = 200, QuietOnSuccess = true)]
    public void OrderNumber_FromSequenceAndParse_PreservesCanonicalFormat(int rawSequence)
    {
        var sequence = NormalizePositive(rawSequence, max: 999_999);

        var orderNumber = OrderNumber.FromSequence(sequence);
        var parsed = OrderNumber.Parse(orderNumber.Value.ToLowerInvariant());

        Assert.Equal(orderNumber.Value, parsed.Value);
        Assert.StartsWith("PO-", parsed.Value, StringComparison.Ordinal);
        Assert.True(parsed.Value.Length >= 7);
    }

    [Property(MaxTest = 200, QuietOnSuccess = true)]
    public void OrderLineItem_RegisterReceiving_KeepsRemainingQuantityAtZeroOrPositive(int rawQuantity, int rawReceived)
    {
        var quantity = NormalizePositive(rawQuantity, max: 10_000);
        var received = NormalizePositive(rawReceived, max: 20_000);
        var lineItem = new OrderLineItem(Guid.NewGuid(), "P001", "商品A", quantity, 100m);

        lineItem.RegisterReceiving(received);

        Assert.Equal(received, lineItem.ReceivedQuantity);
        Assert.Equal(Math.Max(0, quantity - received), lineItem.RemainingQuantity);
    }

    [Property(MaxTest = 200, QuietOnSuccess = true)]
    public void Order_AmountExcludingTax_EqualsSumOfLineAmounts(
        int rawQuantity1,
        int rawUnitPriceCents1,
        int rawQuantity2,
        int rawUnitPriceCents2)
    {
        var lineItem1 = CreateLineItem("P001", rawQuantity1, rawUnitPriceCents1);
        var lineItem2 = CreateLineItem("P002", rawQuantity2, rawUnitPriceCents2);
        var order = CreateOrder(0.1m, lineItem1, lineItem2);

        var expected = lineItem1.AmountExcludingTax + lineItem2.AmountExcludingTax;

        Assert.Equal(expected, order.AmountExcludingTax);
    }

    [Property(MaxTest = 200, QuietOnSuccess = true)]
    public void Order_AmountIncludingTax_RoundsSubtotalWithTaxAwayFromZero(
        int rawQuantity,
        int rawUnitPriceCents,
        int rawTaxBasisPoints)
    {
        var lineItem = CreateLineItem("P001", rawQuantity, rawUnitPriceCents);
        var taxRate = NormalizeNonNegative(rawTaxBasisPoints, max: 2_500) / 10_000m;
        var order = CreateOrder(taxRate, lineItem);

        var expected = decimal.Round(
            lineItem.AmountExcludingTax * (1 + taxRate),
            2,
            MidpointRounding.AwayFromZero);

        Assert.Equal(expected, order.AmountIncludingTax);
    }

    [Property(MaxTest = 200, QuietOnSuccess = true)]
    public void Order_TransitionToCanceled_FromAnyNonCanceledStatus_SoftDeletesOrder(int rawStatus)
    {
        var status = NormalizeStatus(rawStatus);
        var order = CreateOrder(0.1m, status, CreateLineItem("P001", rawQuantity: 1, rawUnitPriceCents: 10_000));

        order.TransitionTo(OrderStatus.Canceled, FixedNowUtc);

        Assert.True(order.IsDeleted);
        Assert.Equal(OrderStatus.Canceled, order.Status);
        Assert.Equal(FixedNowUtc, order.DeletedAtUtc);
        Assert.Equal(FixedNowUtc, order.UpdatedAtUtc);
    }

    private static OrderLineItem CreateLineItem(string productCode, int rawQuantity, int rawUnitPriceCents)
    {
        var quantity = NormalizePositive(rawQuantity, max: 1_000);
        var unitPrice = NormalizeNonNegative(rawUnitPriceCents, max: 1_000_000) / 100m;

        return new OrderLineItem(Guid.NewGuid(), productCode, $"商品{productCode}", quantity, unitPrice);
    }

    private static Order CreateOrder(decimal taxRate, params OrderLineItem[] lineItems)
        => CreateOrder(taxRate, OrderStatus.Unprocessed, lineItems);

    private static Order CreateOrder(decimal taxRate, OrderStatus status, params OrderLineItem[] lineItems)
        => new(
            Guid.NewGuid(),
            OrderNumber.FromSequence(1),
            Guid.NewGuid(),
            "仕入先A",
            FixedNowUtc,
            status,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            taxRate,
            lineItems,
            isDeleted: false,
            deletedAtUtc: null,
            createdAtUtc: FixedNowUtc,
            updatedAtUtc: FixedNowUtc);

    private static int NormalizePositive(int value, int max)
        => NormalizeNonNegative(value, max - 1) + 1;

    private static int NormalizeNonNegative(int value, int max)
        => (int)((uint)value % ((uint)max + 1));

    private static OrderStatus NormalizeStatus(int value)
    {
        var statuses = Enum.GetValues<OrderStatus>()
            .Where(status => status != OrderStatus.Canceled)
            .ToArray();

        return statuses[NormalizeNonNegative(value, statuses.Length - 1)];
    }
}
