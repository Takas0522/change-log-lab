namespace OrderClientApp.Application.Abstractions.Orders;

public interface IOrderNumberSequenceRepository
{
    Task<int> GetNextSequenceAsync(CancellationToken cancellationToken = default);
}
