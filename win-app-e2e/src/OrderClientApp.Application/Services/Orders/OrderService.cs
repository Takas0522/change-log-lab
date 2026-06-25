using OrderClientApp.Application.Abstractions.Auth;
using OrderClientApp.Application.Abstractions.Notifications;
using OrderClientApp.Application.Abstractions.Orders;
using OrderClientApp.Application.Abstractions.Operations;
using OrderClientApp.Domain.Auth;
using OrderClientApp.Domain.Orders;

namespace OrderClientApp.Application.Services.Orders;

public sealed class OrderService : IOrderService
{
    private static readonly int[] AllowedPageSizes = [10, 50, 100];

    private readonly IOrderRepository _orderRepository;
    private readonly IOrderTemplateRepository _templateRepository;
    private readonly IOrderNumberSequenceRepository _orderNumberSequenceRepository;
    private readonly IBudgetSettingsRepository _budgetSettingsRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IAuthorizationService _authorizationService;
    private readonly IOperationLogService _operationLogService;
    private readonly INotifier _notifier;
    private readonly TimeProvider _timeProvider;

    public OrderService(
        IOrderRepository orderRepository,
        IOrderTemplateRepository templateRepository,
        IOrderNumberSequenceRepository orderNumberSequenceRepository,
        IBudgetSettingsRepository budgetSettingsRepository,
        IInventoryRepository inventoryRepository,
        IAuthorizationService authorizationService,
        IOperationLogService operationLogService,
        INotifier notifier,
        TimeProvider? timeProvider = null)
    {
        _orderRepository = orderRepository;
        _templateRepository = templateRepository;
        _orderNumberSequenceRepository = orderNumberSequenceRepository;
        _budgetSettingsRepository = budgetSettingsRepository;
        _inventoryRepository = inventoryRepository;
        _authorizationService = authorizationService;
        _operationLogService = operationLogService;
        _notifier = notifier;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateLineItems(request.LineItems);
        var budget = await _budgetSettingsRepository.GetAsync(cancellationToken);

        var sequence = await _orderNumberSequenceRepository.GetNextSequenceAsync(cancellationToken);
        var orderNumber = OrderNumber.FromSequence(sequence);
        var nowUtc = _timeProvider.GetUtcNow();
        var status = DetermineInitialStatus(request.TaxRate, request.LineItems, budget.ApprovalThreshold);

        var order = new Order(
            Guid.NewGuid(),
            orderNumber,
            request.CreatedByUserId,
            request.SupplierName,
            request.OrderedAtUtc,
            status,
            request.ExpectedReceivingDateUtc,
            rejectionReason: null,
            request.Note,
            request.DeliveryNoteNumber,
            request.DeliveryNoteDateUtc,
            request.InvoiceNumber,
            request.InvoiceDateUtc,
            request.TaxRate,
            request.LineItems.Select(ToOrderLineItem),
            isDeleted: false,
            deletedAtUtc: null,
            createdAtUtc: nowUtc,
            updatedAtUtc: nowUtc);

        await _orderRepository.AddAsync(order, cancellationToken);
        await _operationLogService.LogAsync("Order", "Create", request.CreatedByUserId.ToString(), $"発注を作成しました: {order.OrderNumber.Value}", cancellationToken);
        if (status == OrderStatus.PendingApproval)
        {
            await _notifier.NotifyAsync(new AppNotification("承認依頼", $"発注 {order.OrderNumber.Value} の承認が必要です。"), cancellationToken);
        }
        return await ToDtoAsync(order, budget, cancellationToken);
    }

    public Task<OrderDto> CreateBulkAsync(CreateBulkOrderRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return CreateAsync(
            new CreateOrderRequest(
                request.CreatedByUserId,
                request.SupplierName,
                request.OrderedAtUtc,
                request.ExpectedReceivingDateUtc,
                request.Note,
                request.DeliveryNoteNumber,
                request.DeliveryNoteDateUtc,
                request.InvoiceNumber,
                request.InvoiceDateUtc,
                request.TaxRate,
                request.LineItems),
            cancellationToken);
    }

    public async Task<OrderDto> UpdateAsync(UpdateOrderRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateLineItems(request.LineItems);
        var budget = await _budgetSettingsRepository.GetAsync(cancellationToken);

        var order = await _orderRepository.GetByIdAsync(request.OrderId, includeDeleted: true, cancellationToken)
            ?? throw new InvalidOperationException("Order was not found.");

        var nowUtc = _timeProvider.GetUtcNow();
        order.UpdateHeader(
            request.SupplierName,
            request.OrderedAtUtc,
            request.Note,
            request.DeliveryNoteNumber,
            request.DeliveryNoteDateUtc,
            request.InvoiceNumber,
            request.InvoiceDateUtc,
            request.TaxRate,
            nowUtc);
        order.SetExpectedReceivingDate(request.ExpectedReceivingDateUtc, nowUtc);
        order.ReplaceLineItems(request.LineItems.Select(ToOrderLineItem), nowUtc);

        var desiredStatus = request.Status;
        var shouldRequireApproval = order.AmountIncludingTax >= budget.ApprovalThreshold && budget.ApprovalThreshold > 0;
        if (shouldRequireApproval && order.Status is OrderStatus.Unprocessed or OrderStatus.Rejected)
        {
            desiredStatus = OrderStatus.PendingApproval;
        }

        order.TransitionTo(desiredStatus, nowUtc);
        await _orderRepository.UpdateAsync(order, cancellationToken);
        await _operationLogService.LogAsync("Order", "Update", null, $"発注を更新しました: {order.OrderNumber.Value}", cancellationToken);
        if (order.Status == OrderStatus.PendingApproval)
        {
            await _notifier.NotifyAsync(new AppNotification("承認依頼", $"発注 {order.OrderNumber.Value} の承認が必要です。"), cancellationToken);
        }
        return await ToDtoAsync(order, budget, cancellationToken);
    }

    public async Task<IReadOnlyCollection<PendingApprovalOrderDto>> ListPendingApprovalsAsync(CancellationToken cancellationToken = default)
    {
        var list = await _orderRepository.ListAsync(new OrderListQuery(OrderStatus.PendingApproval, null, null, 1, 100), cancellationToken);
        return list
            .Select(x => new PendingApprovalOrderDto(x.Id, x.OrderNumber.Value, x.SupplierName, x.AmountIncludingTax, x.OrderedAtUtc))
            .ToArray();
    }

    public async Task<OrderDto> ApproveAsync(Guid orderId, AuthenticatedUser actor, CancellationToken cancellationToken = default)
    {
        EnsureApprover(actor);
        var order = await _orderRepository.GetByIdAsync(orderId, includeDeleted: true, cancellationToken)
            ?? throw new InvalidOperationException("Order was not found.");
        var now = _timeProvider.GetUtcNow();
        order.TransitionTo(OrderStatus.Approved, now);
        order.TransitionTo(OrderStatus.Processing, now);
        await _orderRepository.UpdateAsync(order, cancellationToken);
        await _operationLogService.LogAsync("Order", "Approve", actor.Username, $"発注を承認しました: {order.OrderNumber.Value}", cancellationToken);
        await _notifier.NotifyAsync(new AppNotification("承認完了", $"発注 {order.OrderNumber.Value} の承認が完了しました。"), cancellationToken);
        return await ToDtoAsync(order, null, cancellationToken);
    }

    public async Task<OrderDto> RejectAsync(Guid orderId, string reason, AuthenticatedUser actor, CancellationToken cancellationToken = default)
    {
        EnsureApprover(actor);
        var order = await _orderRepository.GetByIdAsync(orderId, includeDeleted: true, cancellationToken)
            ?? throw new InvalidOperationException("Order was not found.");
        order.Reject(reason, _timeProvider.GetUtcNow());
        await _orderRepository.UpdateAsync(order, cancellationToken);
        await _operationLogService.LogAsync("Order", "Reject", actor.Username, $"発注を却下しました: {order.OrderNumber.Value}", cancellationToken);
        return await ToDtoAsync(order, null, cancellationToken);
    }

    public async Task<OrderDto> ConfirmReceivingAsync(ConfirmReceivingRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var order = await _orderRepository.GetByIdAsync(request.OrderId, includeDeleted: true, cancellationToken)
            ?? throw new InvalidOperationException("Order was not found.");
        var receivingMap = request.ReceivedLineItems.ToDictionary(x => x.OrderLineItemId, x => x.Quantity);
        order.ConfirmReceiving(receivingMap, _timeProvider.GetUtcNow());
        foreach (var line in order.LineItems)
        {
            if (receivingMap.TryGetValue(line.Id, out var quantity) && quantity > 0)
            {
                await _inventoryRepository.IncreaseStockAsync(line.ProductCode, line.ProductName, quantity, cancellationToken);
            }
        }

        await _orderRepository.UpdateAsync(order, cancellationToken);
        await _operationLogService.LogAsync("Order", "Receive", null, $"発注の入荷を記録しました: {order.OrderNumber.Value}", cancellationToken);
        return await ToDtoAsync(order, null, cancellationToken);
    }

    public async Task SoftDeleteAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("Order id is required.", nameof(orderId));
        }

        await _orderRepository.SoftDeleteAsync(orderId, _timeProvider.GetUtcNow(), cancellationToken);
        await _operationLogService.LogAsync("Order", "Delete", null, $"発注を削除しました: {orderId}", cancellationToken);
    }

    public async Task<OrderDto?> GetByIdAsync(Guid orderId, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, includeDeleted, cancellationToken);
        return order is null ? null : await ToDtoAsync(order, null, cancellationToken);
    }

    public async Task<PagedResult<OrderDto>> ListAsync(OrderListQuery query, CancellationToken cancellationToken = default)
    {
        ValidateQuery(query);
        var totalCount = await _orderRepository.CountAsync(query, cancellationToken);
        var orders = await _orderRepository.ListAsync(query, cancellationToken);
        var budget = await _budgetSettingsRepository.GetAsync(cancellationToken);
        var items = new List<OrderDto>(orders.Count);
        foreach (var order in orders)
        {
            items.Add(await ToDtoAsync(order, budget, cancellationToken));
        }

        return new PagedResult<OrderDto>(items, totalCount, query.PageNumber, query.PageSize);
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

    public async Task<IReadOnlyCollection<InventoryAlertDto>> GetInventoryAlertsAsync(CancellationToken cancellationToken = default)
    {
        var alerts = await _inventoryRepository.ListAlertsAsync(cancellationToken);
        if (alerts.Count > 0)
        {
            await _notifier.NotifyAsync(new AppNotification("在庫アラート", $"{alerts.Count} 件の発注推奨商品があります。"), cancellationToken);
        }

        return alerts;
    }

    public Task<BudgetSettingsDto> GetBudgetSettingsAsync(CancellationToken cancellationToken = default)
        => _budgetSettingsRepository.GetAsync(cancellationToken);

    public async Task<BudgetSettingsDto> SaveBudgetSettingsAsync(decimal approvalThreshold, decimal? monthlyLimit, decimal? yearlyLimit, CancellationToken cancellationToken = default)
    {
        if (approvalThreshold < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(approvalThreshold));
        }

        if (monthlyLimit < 0 || yearlyLimit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(monthlyLimit), "Budget cannot be negative.");
        }

        var result = await _budgetSettingsRepository.UpsertAsync(
            approvalThreshold,
            monthlyLimit,
            yearlyLimit,
            _timeProvider.GetUtcNow(),
            cancellationToken);
        await _operationLogService.LogAsync("Settings", "BudgetUpdate", null, $"予算設定を更新しました: Threshold={approvalThreshold}", cancellationToken);
        return result;
    }

    private void EnsureApprover(AuthenticatedUser actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        if (!_authorizationService.CanAccess(actor, UserRole.Approver))
        {
            throw new UnauthorizedAccessException("承認権限がありません。");
        }
    }

    private static OrderStatus DetermineInitialStatus(decimal taxRate, IReadOnlyCollection<CreateOrderLineItemInput> lineItems, decimal approvalThreshold)
    {
        var subtotal = lineItems.Sum(x => x.UnitPriceExcludingTax * x.Quantity);
        var total = decimal.Round(subtotal * (1 + taxRate), 2, MidpointRounding.AwayFromZero);
        return approvalThreshold > 0 && total >= approvalThreshold
            ? OrderStatus.PendingApproval
            : OrderStatus.Unprocessed;
    }

    private async Task<OrderDto> ToDtoAsync(Order order, BudgetSettingsDto? budgetSettings, CancellationToken cancellationToken)
    {
        var budget = budgetSettings ?? await _budgetSettingsRepository.GetAsync(cancellationToken);
        var now = _timeProvider.GetUtcNow();
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
        var yearStart = new DateTimeOffset(now.Year, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var yearEnd = yearStart.AddYears(1).AddTicks(-1);
        var monthUsage = await _orderRepository.SumAmountIncludingTaxAsync(monthStart, monthEnd, cancellationToken);
        var yearUsage = await _orderRepository.SumAmountIncludingTaxAsync(yearStart, yearEnd, cancellationToken);
        decimal? monthlyRemaining = budget.MonthlyLimit.HasValue ? budget.MonthlyLimit.Value - monthUsage : null;
        decimal? yearlyRemaining = budget.YearlyLimit.HasValue ? budget.YearlyLimit.Value - yearUsage : null;
        var budgetExceeded = (monthlyRemaining.HasValue && monthlyRemaining.Value < 0)
            || (yearlyRemaining.HasValue && yearlyRemaining.Value < 0);

        return new OrderDto(
            order.Id,
            order.OrderNumber.Value,
            order.CreatedByUserId,
            order.SupplierName,
            order.OrderedAtUtc,
            order.ExpectedReceivingDateUtc,
            order.Status,
            budget.ApprovalThreshold > 0 && order.AmountIncludingTax >= budget.ApprovalThreshold,
            budgetExceeded,
            monthlyRemaining,
            yearlyRemaining,
            order.RejectionReason,
            order.Note,
            order.DeliveryNoteNumber,
            order.DeliveryNoteDateUtc,
            order.InvoiceNumber,
            order.InvoiceDateUtc,
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
                    li.ReceivedQuantity,
                    li.RemainingQuantity,
                    li.UnitPriceExcludingTax,
                    li.AmountExcludingTax))
                .ToArray());
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
