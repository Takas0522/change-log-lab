namespace OrderClientApp.Application.Abstractions.Orders;

public interface IInventoryRepository
{
    Task IncreaseStockAsync(string productCode, string productName, int quantity, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<InventoryAlertDto>> ListAlertsAsync(CancellationToken cancellationToken = default);

    Task<int> GetQuantityAsync(string productCode, CancellationToken cancellationToken = default);
}
