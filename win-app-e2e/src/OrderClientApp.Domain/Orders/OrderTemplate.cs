namespace OrderClientApp.Domain.Orders;

public sealed class OrderTemplate
{
    private readonly List<OrderTemplateLineItem> _lineItems;

    public OrderTemplate(
        Guid id,
        Guid createdByUserId,
        string templateName,
        string? note,
        decimal taxRate,
        IEnumerable<OrderTemplateLineItem> lineItems,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Template id is required.", nameof(id));
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new ArgumentException("Created by user id is required.", nameof(createdByUserId));
        }

        if (string.IsNullOrWhiteSpace(templateName))
        {
            throw new ArgumentException("Template name is required.", nameof(templateName));
        }

        if (taxRate < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(taxRate));
        }

        var normalizedLineItems = lineItems?.ToList()
            ?? throw new ArgumentNullException(nameof(lineItems));
        if (normalizedLineItems.Count == 0)
        {
            throw new ArgumentException("At least one line item is required.", nameof(lineItems));
        }

        Id = id;
        CreatedByUserId = createdByUserId;
        TemplateName = templateName.Trim();
        Note = note?.Trim();
        TaxRate = taxRate;
        _lineItems = normalizedLineItems;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }

    public Guid CreatedByUserId { get; }

    public string TemplateName { get; }

    public string? Note { get; }

    public decimal TaxRate { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public IReadOnlyCollection<OrderTemplateLineItem> LineItems => _lineItems;
}

public sealed class OrderTemplateLineItem
{
    public OrderTemplateLineItem(
        string productCode,
        string productName,
        int quantity,
        decimal unitPriceExcludingTax)
    {
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

        ProductCode = productCode.Trim();
        ProductName = productName.Trim();
        Quantity = quantity;
        UnitPriceExcludingTax = unitPriceExcludingTax;
    }

    public string ProductCode { get; }

    public string ProductName { get; }

    public int Quantity { get; }

    public decimal UnitPriceExcludingTax { get; }
}
