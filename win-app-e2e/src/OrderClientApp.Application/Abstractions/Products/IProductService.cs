namespace OrderClientApp.Application.Abstractions.Products;

public interface IProductService
{
    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);

    Task<ProductDto> UpdateAsync(UpdateProductRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<ProductDto?> GetByIdAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ProductDto>> ListAsync(ProductFilter filter, CancellationToken cancellationToken = default);

    Task<ProductCsvImportResult> ImportCsvAsync(string csvContent, CancellationToken cancellationToken = default);

    Task SetPreferredSupplierAsync(Guid productId, Guid? supplierId, CancellationToken cancellationToken = default);
}
