using OrderClientApp.Application.Abstractions.Orders;
using OrderClientApp.Domain.Orders;

namespace OrderClientApp.Application.Services.Orders;

public sealed class OrderService : IOrderService
{
    private static readonly int[] AllowedPageSizes = [10, 50, 100];

    private readonly IOrderRepository _orderRepository;
    private readonly IOrderTemplateRepository _templateRepository;
    private readonly IOrderNumberSequenceRepository _orderNumberSequenceRepository;
    private readonly TimeProvider _timeProvider;

    public OrderService(
        IOrderRepository orderRepository,
        IOrderTemplateRepository templateRepository,
        IOrderNumberSequenceRepository orderNumberSequenceRepository,
        TimeProvider? timeProvider = null)
    {
        _orderRepository = orderRepository;
        _templateRepository = templateRepository;
        _orderNumberSequenceRepository = orderNumberSequenceRepository;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateLineItems(request.LineItems);

        var sequence = await _orderNumberSequenceRepository.GetNextSequenceAsync(cancellationToken);
        var orderNumber = OrderNumber.FromSequence(sequence);
        var nowUtc = _timeProvider.GetUtcNow();

        var order = new Order(
            Guid.NewGuid(),
            orderNumber,
            request.CreatedByUserId,
            request.SupplierName,
            request.OrderedAtUtc,
            OrderStatus.Unprocessed,
            request.Note,
            request.TaxRate,
            request.LineItems.Select(ToOrderLineItem),
            isDeleted: false,
            deletedAtUtc: null,
            createdAtUtc: nowUtc,
            updatedAtUtc: nowUtc);

        await _orderRepository.AddAsync(order, cancellationToken);
        return ToDto(order);
    }

    public Task<OrderDto> CreateBulkAsync(CreateBulkOrderRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateLineItems(request.LineItems);

        return CreateAsync(
            new CreateOrderRequest(
                request.CreatedByUserId,
                request.SupplierName,
                request.OrderedAtUtc,
                request.Note,
                request.TaxRate,
                request.LineItems),
            cancellationToken);
    }

    public async Task<OrderDto> UpdateAsync(UpdateOrderRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateLineItems(request.LineItems);

        var order = await _orderRepository.GetByIdAsync(request.OrderId, includeDeleted: true, cancellationToken)
            ?? throw new InvalidOperationException("Order was not found.");

        var nowUtc = _timeProvider.GetUtcNow();
        order.UpdateHeader(request.SupplierName, request.OrderedAtUtc, request.Note, request.TaxRate, nowUtc);
        order.ReplaceLineItems(request.LineItems.Select(ToOrderLineItem), nowUtc);
        order.TransitionTo(request.Status, nowUtc);

        await _orderRepository.UpdateAsync(order, cancellationToken);
        return ToDto(order);
    }

    public async Task SoftDeleteAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("Order id is required.", nameof(orderId));
        }

        await _orderRepository.SoftDeleteAsync(orderId, _timeProvider.GetUtcNow(), cancellationToken);
    }

    public async Task<OrderDto?> GetByIdAsync(Guid orderId, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, includeDeleted, cancellationToken);
        return order is null ? null : ToDto(order);
    }

    public async Task<PagedResult<OrderDto>> ListAsync(OrderListQuery query, CancellationToken cancellationToken = default)
    {
        ValidateQuery(query);
        var totalCount = await _orderRepository.CountAsync(query, cancellationToken);
        var orders = await _orderRepository.ListAsync(query, cancellationToken);

        return new PagedResult<OrderDto>(orders.Select(ToDto).ToArray(), totalCount, query.PageNumber, query.PageSize);
    }

    public async Task<OrderTemplateDto> SaveTemplateAsync(SaveOrderTemplateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateLineItems(request.LineItems);

        var template = new OrderTemplate(
            Guid.NewGuid(),
            request.CreatedByUserId,
            request.TemplateName,
            request.Note,
            request.TaxRate,
            request.LineItems.Select(ToTemplateLineItem),
            _timeProvider.GetUtcNow());

        await _templateRepository.AddAsync(template, cancellationToken);
        return ToDto(template);
    }

    public async Task<IReadOnlyCollection<OrderTemplateDto>> ListTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var templates = await _templateRepository.ListAsync(cancellationToken);
        return templates.Select(ToDto).ToArray();
    }

    public async Task<OrderTemplateDto?> GetTemplateByIdAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var template = await _templateRepository.GetByIdAsync(templateId, cancellationToken);
        return template is null ? null : ToDto(template);
    }

    private static void ValidateLineItems(IReadOnlyCollection<CreateOrderLineItemInput> lineItems)
    {
        if (lineItems is null || lineItems.Count == 0)
        {
            throw new ArgumentException("At least one line item is required.", nameof(lineItems));
        }
    }

    private static void ValidateQuery(OrderListQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.PageNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(query.PageNumber));
        }

        if (!AllowedPageSizes.Contains(query.PageSize))
        {
            throw new ArgumentOutOfRangeException(nameof(query.PageSize));
        }
    }

    private static OrderLineItem ToOrderLineItem(CreateOrderLineItemInput input)
        => new(Guid.NewGuid(), input.ProductCode, input.ProductName, input.Quantity, input.UnitPriceExcludingTax);

    private static OrderTemplateLineItem ToTemplateLineItem(CreateOrderLineItemInput input)
        => new(input.ProductCode, input.ProductName, input.Quantity, input.UnitPriceExcludingTax);

    private static OrderDto ToDto(Order order)
        => new(
            order.Id,
            order.OrderNumber.Value,
            order.CreatedByUserId,
            order.SupplierName,
            order.OrderedAtUtc,
            order.Status,
            order.Note,
            order.TaxRate,
            order.IsDeleted,
            order.DeletedAtUtc,
            order.CreatedAtUtc,
            order.UpdatedAtUtc,
            order.AmountExcludingTax,
            order.AmountIncludingTax,
            order.LineItems
                .Select(li => new OrderLineItemDto(
                    li.Id,
                    li.ProductCode,
                    li.ProductName,
                    li.Quantity,
                    li.UnitPriceExcludingTax,
                    li.AmountExcludingTax))
                .ToArray());

    private static OrderTemplateDto ToDto(OrderTemplate template)
        => new(
            template.Id,
            template.TemplateName,
            template.Note,
            template.TaxRate,
            template.CreatedAtUtc,
            template.LineItems.Select(li => new OrderTemplateLineItemDto(
                li.ProductCode,
                li.ProductName,
                li.Quantity,
                li.UnitPriceExcludingTax)).ToArray());
}
