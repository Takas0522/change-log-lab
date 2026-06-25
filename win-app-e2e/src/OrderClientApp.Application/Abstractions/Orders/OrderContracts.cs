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
    string? Note,
    decimal TaxRate,
    IReadOnlyCollection<CreateOrderLineItemInput> LineItems);

public sealed record UpdateOrderRequest(
    Guid OrderId,
    string SupplierName,
    DateTimeOffset OrderedAtUtc,
    OrderStatus Status,
    string? Note,
    decimal TaxRate,
    IReadOnlyCollection<CreateOrderLineItemInput> LineItems);

public sealed record CreateBulkOrderRequest(
    Guid CreatedByUserId,
    string SupplierName,
    DateTimeOffset OrderedAtUtc,
    string? Note,
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
    decimal UnitPriceExcludingTax,
    decimal AmountExcludingTax);

public sealed record OrderDto(
    Guid Id,
    string OrderNumber,
    Guid CreatedByUserId,
    string SupplierName,
    DateTimeOffset OrderedAtUtc,
    OrderStatus Status,
    string? Note,
    decimal TaxRate,
    bool IsDeleted,
    DateTimeOffset? DeletedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    decimal AmountExcludingTax,
    decimal AmountIncludingTax,
    IReadOnlyCollection<OrderLineItemDto> LineItems);

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
