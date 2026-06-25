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
        DateTimeOffset? expectedReceivingDateUtc,
        string? rejectionReason,
        string? note,
        string? deliveryNoteNumber,
        DateTimeOffset? deliveryNoteDateUtc,
        string? invoiceNumber,
        DateTimeOffset? invoiceDateUtc,
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
        ExpectedReceivingDateUtc = expectedReceivingDateUtc?.ToUniversalTime();
        RejectionReason = string.IsNullOrWhiteSpace(rejectionReason) ? null : rejectionReason.Trim();
        Note = note?.Trim();
        DeliveryNoteNumber = deliveryNoteNumber?.Trim();
        DeliveryNoteDateUtc = deliveryNoteDateUtc;
        InvoiceNumber = invoiceNumber?.Trim();
        InvoiceDateUtc = invoiceDateUtc;
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

    public DateTimeOffset? ExpectedReceivingDateUtc { get; private set; }

    public string? RejectionReason { get; private set; }

    public string? DeliveryNoteNumber { get; private set; }

    public DateTimeOffset? DeliveryNoteDateUtc { get; private set; }

    public string? InvoiceNumber { get; private set; }

    public DateTimeOffset? InvoiceDateUtc { get; private set; }

    public decimal TaxRate { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<OrderLineItem> LineItems => _lineItems;

    public decimal AmountExcludingTax => _lineItems.Sum(x => x.AmountExcludingTax);

    public decimal AmountIncludingTax => decimal.Round(AmountExcludingTax * (1 + TaxRate), 2, MidpointRounding.AwayFromZero);

    public bool IsFullyReceived => _lineItems.All(x => x.ReceivedQuantity >= x.Quantity);

    public void UpdateHeader(
        string supplierName,
        DateTimeOffset orderedAtUtc,
        string? note,
        string? deliveryNoteNumber,
        DateTimeOffset? deliveryNoteDateUtc,
        string? invoiceNumber,
        DateTimeOffset? invoiceDateUtc,
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
        DeliveryNoteNumber = deliveryNoteNumber?.Trim();
        DeliveryNoteDateUtc = deliveryNoteDateUtc;
        InvoiceNumber = invoiceNumber?.Trim();
        InvoiceDateUtc = invoiceDateUtc;
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
            OrderStatus.Unprocessed => nextStatus is OrderStatus.PendingApproval or OrderStatus.Processing,
            OrderStatus.PendingApproval => nextStatus is OrderStatus.Approved or OrderStatus.Rejected,
            OrderStatus.Approved => nextStatus is OrderStatus.Processing,
            OrderStatus.Rejected => nextStatus is OrderStatus.Unprocessed or OrderStatus.PendingApproval,
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
        if (nextStatus is OrderStatus.Unprocessed or OrderStatus.PendingApproval)
        {
            RejectionReason = null;
        }
        UpdatedAtUtc = nowUtc;
    }

    public void Reject(string reason, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Rejection reason is required.", nameof(reason));
        }

        if (Status != OrderStatus.PendingApproval)
        {
            throw new InvalidOperationException("Only pending approval order can be rejected.");
        }

        Status = OrderStatus.Rejected;
        RejectionReason = reason.Trim();
        UpdatedAtUtc = nowUtc;
    }

    public void SetExpectedReceivingDate(DateTimeOffset? expectedReceivingDateUtc, DateTimeOffset nowUtc)
    {
        ExpectedReceivingDateUtc = expectedReceivingDateUtc?.ToUniversalTime();
        UpdatedAtUtc = nowUtc;
    }

    public void ConfirmReceiving(IReadOnlyDictionary<Guid, int> receivedByLineItemId, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(receivedByLineItemId);
        if (receivedByLineItemId.Count == 0)
        {
            throw new ArgumentException("At least one receiving entry is required.", nameof(receivedByLineItemId));
        }

        if (Status is not OrderStatus.WaitingForArrival and not OrderStatus.PartiallyReceived)
        {
            throw new InvalidOperationException("Receiving can only be confirmed from waiting/partial status.");
        }

        var touched = false;
        foreach (var lineItem in _lineItems)
        {
            if (!receivedByLineItemId.TryGetValue(lineItem.Id, out var quantity) || quantity <= 0)
            {
                continue;
            }

            lineItem.RegisterReceiving(quantity);
            touched = true;
        }

        if (!touched)
        {
            throw new InvalidOperationException("No receiving quantity was provided.");
        }

        Status = IsFullyReceived ? OrderStatus.Completed : OrderStatus.PartiallyReceived;
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
