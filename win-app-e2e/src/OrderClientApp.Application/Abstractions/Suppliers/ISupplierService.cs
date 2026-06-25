namespace OrderClientApp.Application.Abstractions.Suppliers;

public interface ISupplierService
{
    Task<SupplierDto> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default);

    Task<SupplierDto> UpdateAsync(UpdateSupplierRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid supplierId, CancellationToken cancellationToken = default);

    Task<SupplierDto?> GetByIdAsync(Guid supplierId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<SupplierDto>> ListAsync(string? keyword, CancellationToken cancellationToken = default);

    Task SetProductSupplierPriceAsync(
        Guid productId,
        Guid supplierId,
        decimal unitPriceExcludingTax,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ProductSupplierPriceDto>> GetProductSupplierPricesAsync(Guid productId, CancellationToken cancellationToken = default);
}
