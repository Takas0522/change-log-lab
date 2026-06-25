using OrderClientApp.Domain.Orders;

namespace OrderClientApp.Application.Abstractions.Orders;

public sealed record CreateOrderLineItemInput(
    string ProductCode,
    string ProductName,
    int Quantity,
    decimal UnitPriceExcludingTax);

public sealed record CreateOrderRequest(
    Guid CreatedByUserId,
    string SupplierName,
    DateTimeOffset OrderedAtUtc,
    DateTimeOffset? ExpectedReceivingDateUtc,
    string? Note,
    string? DeliveryNoteNumber,
    DateTimeOffset? DeliveryNoteDateUtc,
    string? InvoiceNumber,
    DateTimeOffset? InvoiceDateUtc,
    decimal TaxRate,
    IReadOnlyCollection<CreateOrderLineItemInput> LineItems);

public sealed record UpdateOrderRequest(
    Guid OrderId,
    string SupplierName,
    DateTimeOffset OrderedAtUtc,
    DateTimeOffset? ExpectedReceivingDateUtc,
    OrderStatus Status,
    string? Note,
    string? DeliveryNoteNumber,
    DateTimeOffset? DeliveryNoteDateUtc,
    string? InvoiceNumber,
    DateTimeOffset? InvoiceDateUtc,
    decimal TaxRate,
    IReadOnlyCollection<CreateOrderLineItemInput> LineItems);

public sealed record CreateBulkOrderRequest(
    Guid CreatedByUserId,
    string SupplierName,
    DateTimeOffset OrderedAtUtc,
    DateTimeOffset? ExpectedReceivingDateUtc,
    string? Note,
    string? DeliveryNoteNumber,
    DateTimeOffset? DeliveryNoteDateUtc,
    string? InvoiceNumber,
    DateTimeOffset? InvoiceDateUtc,
    decimal TaxRate,
    IReadOnlyCollection<CreateOrderLineItemInput> LineItems);

public sealed record SaveOrderTemplateRequest(
    Guid CreatedByUserId,
    string TemplateName,
    string? Note,
    decimal TaxRate,
    IReadOnlyCollection<CreateOrderLineItemInput> LineItems);

public sealed record OrderListQuery(
    OrderStatus? Status,
    DateTimeOffset? DateFromUtc,
    DateTimeOffset? DateToUtc,
    int PageNumber,
    int PageSize,
    bool IncludeDeleted = false);

public sealed record OrderLineItemDto(
    Guid Id,
    string ProductCode,
    string ProductName,
    int Quantity,
    int ReceivedQuantity,
    int RemainingQuantity,
    decimal UnitPriceExcludingTax,
    decimal AmountExcludingTax);

public sealed record OrderDto(
    Guid Id,
    string OrderNumber,
    Guid CreatedByUserId,
    string SupplierName,
    DateTimeOffset OrderedAtUtc,
    DateTimeOffset? ExpectedReceivingDateUtc,
    OrderStatus Status,
    bool RequiresApproval,
    bool BudgetExceeded,
    decimal? MonthlyBudgetRemaining,
    decimal? YearlyBudgetRemaining,
    string? RejectionReason,
    string? Note,
    string? DeliveryNoteNumber,
    DateTimeOffset? DeliveryNoteDateUtc,
    string? InvoiceNumber,
    DateTimeOffset? InvoiceDateUtc,
    decimal TaxRate,
    bool IsDeleted,
    DateTimeOffset? DeletedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    decimal AmountExcludingTax,
    decimal AmountIncludingTax,
    IReadOnlyCollection<OrderLineItemDto> LineItems);

public sealed record PendingApprovalOrderDto(
    Guid OrderId,
    string OrderNumber,
    string SupplierName,
    decimal AmountIncludingTax,
    DateTimeOffset OrderedAtUtc);

public sealed record ConfirmReceivingLineInput(Guid OrderLineItemId, int Quantity);

public sealed record ConfirmReceivingRequest(
    Guid OrderId,
    IReadOnlyCollection<ConfirmReceivingLineInput> ReceivedLineItems);

public sealed record InventoryAlertDto(
    string ProductCode,
    string ProductName,
    int Quantity,
    int ReorderPoint);

public sealed record BudgetSettingsDto(
    decimal ApprovalThreshold,
    decimal? MonthlyLimit,
    decimal? YearlyLimit,
    DateTimeOffset UpdatedAtUtc);

public sealed record OrderTemplateLineItemDto(
    string ProductCode,
    string ProductName,
    int Quantity,
    decimal UnitPriceExcludingTax);

public sealed record OrderTemplateDto(
    Guid Id,
    string TemplateName,
    string? Note,
    decimal TaxRate,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyCollection<OrderTemplateLineItemDto> LineItems);

public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);
