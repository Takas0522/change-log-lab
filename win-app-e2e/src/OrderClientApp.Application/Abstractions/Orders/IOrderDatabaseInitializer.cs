namespace OrderClientApp.Application.Abstractions.Orders;

public interface IOrderDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
