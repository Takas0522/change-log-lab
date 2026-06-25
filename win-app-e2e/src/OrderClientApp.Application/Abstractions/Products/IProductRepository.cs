using OrderClientApp.Domain.Products;

namespace OrderClientApp.Application.Abstractions.Products;

public interface IProductRepository
{
    Task AddAsync(Product product, CancellationToken cancellationToken = default);

    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<Product?> GetByIdAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<Product?> GetByCodeAsync(string productCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Product>> ListAsync(ProductFilter filter, CancellationToken cancellationToken = default);

    Task<bool> ExistsByCodeAsync(string productCode, Guid? excludingProductId = null, CancellationToken cancellationToken = default);

    Task SetPreferredSupplierAsync(Guid productId, Guid? supplierId, DateTimeOffset nowUtc, CancellationToken cancellationToken = default);
}
