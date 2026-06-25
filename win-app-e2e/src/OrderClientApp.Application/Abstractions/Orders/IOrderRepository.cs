using OrderClientApp.Domain.Orders;

namespace OrderClientApp.Application.Abstractions.Orders;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken = default);

    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);

    Task<Order?> GetByIdAsync(Guid orderId, bool includeDeleted = false, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Order>> ListAsync(OrderListQuery query, CancellationToken cancellationToken = default);

    Task<int> CountAsync(OrderListQuery query, CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(Guid orderId, DateTimeOffset deletedAtUtc, CancellationToken cancellationToken = default);

    Task<decimal> SumAmountIncludingTaxAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default);
}
