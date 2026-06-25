namespace OrderClientApp.Domain.Orders;

public sealed class OrderLineItem
{
    public OrderLineItem(
        Guid id,
        string productCode,
        string productName,
        int quantity,
        decimal unitPriceExcludingTax)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Line item id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(productCode))
        {
            throw new ArgumentException("Product code is required.", nameof(productCode));
        }

        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new ArgumentException("Product name is required.", nameof(productName));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        if (unitPriceExcludingTax < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPriceExcludingTax));
        }

        Id = id;
        ProductCode = productCode.Trim();
        ProductName = productName.Trim();
        Quantity = quantity;
        UnitPriceExcludingTax = unitPriceExcludingTax;
    }

    public Guid Id { get; }

    public string ProductCode { get; }

    public string ProductName { get; }

    public int Quantity { get; }

    public decimal UnitPriceExcludingTax { get; }

    public decimal AmountExcludingTax => UnitPriceExcludingTax * Quantity;
}
