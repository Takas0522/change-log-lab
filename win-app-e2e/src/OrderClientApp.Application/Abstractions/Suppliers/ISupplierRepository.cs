using OrderClientApp.Domain.Suppliers;

namespace OrderClientApp.Application.Abstractions.Suppliers;

public interface ISupplierRepository
{
    Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default);

    Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid supplierId, CancellationToken cancellationToken = default);

    Task<Supplier?> GetByIdAsync(Guid supplierId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Supplier>> ListAsync(string? keyword, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid supplierId, CancellationToken cancellationToken = default);

    Task<bool> ExistsByCompanyNameAsync(string companyName, Guid? excludingSupplierId = null, CancellationToken cancellationToken = default);

    Task SetProductSupplierPriceAsync(
        Guid productId,
        Guid supplierId,
        decimal unitPriceExcludingTax,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ProductSupplierPriceDto>> GetProductSupplierPricesAsync(Guid productId, CancellationToken cancellationToken = default);
}
