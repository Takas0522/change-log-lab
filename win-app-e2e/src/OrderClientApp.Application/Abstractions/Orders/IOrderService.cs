using OrderClientApp.Domain.Auth;

namespace OrderClientApp.Application.Abstractions.Orders;

public interface IOrderService
{
    Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);

    Task<OrderDto> CreateBulkAsync(CreateBulkOrderRequest request, CancellationToken cancellationToken = default);

    Task<OrderDto> UpdateAsync(UpdateOrderRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PendingApprovalOrderDto>> ListPendingApprovalsAsync(CancellationToken cancellationToken = default);

    Task<OrderDto> ApproveAsync(Guid orderId, AuthenticatedUser actor, CancellationToken cancellationToken = default);

    Task<OrderDto> RejectAsync(Guid orderId, string reason, AuthenticatedUser actor, CancellationToken cancellationToken = default);

    Task<OrderDto> ConfirmReceivingAsync(ConfirmReceivingRequest request, CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<OrderDto?> GetByIdAsync(Guid orderId, bool includeDeleted = false, CancellationToken cancellationToken = default);

    Task<PagedResult<OrderDto>> ListAsync(OrderListQuery query, CancellationToken cancellationToken = default);

    Task<OrderTemplateDto> SaveTemplateAsync(SaveOrderTemplateRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<OrderTemplateDto>> ListTemplatesAsync(CancellationToken cancellationToken = default);

    Task<OrderTemplateDto?> GetTemplateByIdAsync(Guid templateId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<InventoryAlertDto>> GetInventoryAlertsAsync(CancellationToken cancellationToken = default);

    Task<BudgetSettingsDto> GetBudgetSettingsAsync(CancellationToken cancellationToken = default);

    Task<BudgetSettingsDto> SaveBudgetSettingsAsync(decimal approvalThreshold, decimal? monthlyLimit, decimal? yearlyLimit, CancellationToken cancellationToken = default);
}
