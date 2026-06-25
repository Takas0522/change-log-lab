using OrderClientApp.Domain.Orders;

namespace OrderClientApp.Application.Abstractions.Orders;

public interface IOrderTemplateRepository
{
    Task AddAsync(OrderTemplate template, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<OrderTemplate>> ListAsync(CancellationToken cancellationToken = default);

    Task<OrderTemplate?> GetByIdAsync(Guid templateId, CancellationToken cancellationToken = default);
}
