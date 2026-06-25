namespace OrderClientApp.Domain.Orders;

public sealed class Order
{
    private readonly List<OrderLineItem> _lineItems;

    public Order(
        Guid id,
        OrderNumber orderNumber,
        Guid createdByUserId,
        string supplierName,
        DateTimeOffset orderedAtUtc,
        OrderStatus status,
        string? note,
        decimal taxRate,
        IEnumerable<OrderLineItem> lineItems,
        bool isDeleted,
        DateTimeOffset? deletedAtUtc,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Order id is required.", nameof(id));
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new ArgumentException("Created by user id is required.", nameof(createdByUserId));
        }

        if (string.IsNullOrWhiteSpace(supplierName))
        {
            throw new ArgumentException("Supplier name is required.", nameof(supplierName));
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
        OrderNumber = orderNumber;
        CreatedByUserId = createdByUserId;
        SupplierName = supplierName.Trim();
        OrderedAtUtc = orderedAtUtc;
        Status = status;
        Note = note?.Trim();
        TaxRate = taxRate;
        _lineItems = normalizedLineItems;
        IsDeleted = isDeleted;
        DeletedAtUtc = deletedAtUtc;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid Id { get; }

    public OrderNumber OrderNumber { get; }

    public Guid CreatedByUserId { get; }

    public string SupplierName { get; private set; }

    public DateTimeOffset OrderedAtUtc { get; private set; }

    public OrderStatus Status { get; private set; }

    public string? Note { get; private set; }

    public decimal TaxRate { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<OrderLineItem> LineItems => _lineItems;

    public decimal AmountExcludingTax => _lineItems.Sum(x => x.AmountExcludingTax);

    public decimal AmountIncludingTax => decimal.Round(AmountExcludingTax * (1 + TaxRate), 2, MidpointRounding.AwayFromZero);

    public void UpdateHeader(
        string supplierName,
        DateTimeOffset orderedAtUtc,
        string? note,
        decimal taxRate,
        DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(supplierName))
        {
            throw new ArgumentException("Supplier name is required.", nameof(supplierName));
        }

        if (taxRate < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(taxRate));
        }

        SupplierName = supplierName.Trim();
        OrderedAtUtc = orderedAtUtc;
        Note = note?.Trim();
        TaxRate = taxRate;
        UpdatedAtUtc = nowUtc;
    }

    public void ReplaceLineItems(IEnumerable<OrderLineItem> lineItems, DateTimeOffset nowUtc)
    {
        var normalized = lineItems?.ToList()
            ?? throw new ArgumentNullException(nameof(lineItems));
        if (normalized.Count == 0)
        {
            throw new ArgumentException("At least one line item is required.", nameof(lineItems));
        }

        _lineItems.Clear();
        _lineItems.AddRange(normalized);
        UpdatedAtUtc = nowUtc;
    }

    public void TransitionTo(OrderStatus nextStatus, DateTimeOffset nowUtc)
    {
        if (nextStatus == Status)
        {
            return;
        }

        if (Status == OrderStatus.Canceled)
        {
            throw new InvalidOperationException("Canceled order cannot transition.");
        }

        if (nextStatus == OrderStatus.Canceled)
        {
            SoftDelete(nowUtc);
            return;
        }

        var allowed = Status switch
        {
            OrderStatus.Unprocessed => nextStatus is OrderStatus.Processing,
            OrderStatus.Processing => nextStatus is OrderStatus.WaitingForArrival,
            OrderStatus.WaitingForArrival => nextStatus is OrderStatus.PartiallyReceived or OrderStatus.Completed,
            OrderStatus.PartiallyReceived => nextStatus is OrderStatus.WaitingForArrival or OrderStatus.Completed,
            _ => false
        };

        if (!allowed)
        {
            throw new InvalidOperationException($"Invalid status transition: {Status} -> {nextStatus}");
        }

        Status = nextStatus;
        UpdatedAtUtc = nowUtc;
    }

    public void SoftDelete(DateTimeOffset nowUtc)
    {
        IsDeleted = true;
        DeletedAtUtc = nowUtc;
        Status = OrderStatus.Canceled;
        UpdatedAtUtc = nowUtc;
    }
}
