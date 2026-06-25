using OrderClientApp.Application.Abstractions.Products;
using OrderClientApp.Application.Abstractions.Operations;
using OrderClientApp.Application.Abstractions.Suppliers;
using OrderClientApp.Domain.Suppliers;

namespace OrderClientApp.Application.Services.Suppliers;

public sealed class SupplierService : ISupplierService
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly IProductRepository _productRepository;
    private readonly IOperationLogService _operationLogService;
    private readonly TimeProvider _timeProvider;

    public SupplierService(
        ISupplierRepository supplierRepository,
        IProductRepository productRepository,
        IOperationLogService operationLogService,
        TimeProvider? timeProvider = null)
    {
        _supplierRepository = supplierRepository;
        _productRepository = productRepository;
        _operationLogService = operationLogService;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<SupplierDto> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureCompanyNameUniqueAsync(request.CompanyName, null, cancellationToken);

        var nowUtc = _timeProvider.GetUtcNow();
        var supplier = new Supplier(
            Guid.NewGuid(),
            request.CompanyName,
            request.ContactName,
            request.ContactEmail,
            request.ContactPhone,
            request.Notes,
            nowUtc,
            nowUtc);

        await _supplierRepository.AddAsync(supplier, cancellationToken);
        await _operationLogService.LogAsync("Supplier", "Create", null, $"仕入先を作成しました: {supplier.CompanyName}", cancellationToken);
        return ToDto(supplier);
    }

    public async Task<SupplierDto> UpdateAsync(UpdateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var supplier = await _supplierRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException("Supplier was not found.");
        await EnsureCompanyNameUniqueAsync(request.CompanyName, request.Id, cancellationToken);

        supplier.Update(
            request.CompanyName,
            request.ContactName,
            request.ContactEmail,
            request.ContactPhone,
            request.Notes,
            _timeProvider.GetUtcNow());

        await _supplierRepository.UpdateAsync(supplier, cancellationToken);
        await _operationLogService.LogAsync("Supplier", "Update", null, $"仕入先を更新しました: {supplier.CompanyName}", cancellationToken);
        return ToDto(supplier);
    }

    public async Task DeleteAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        if (supplierId == Guid.Empty)
        {
            throw new ArgumentException("Supplier id is required.", nameof(supplierId));
        }

        await _supplierRepository.DeleteAsync(supplierId, cancellationToken);
        await _operationLogService.LogAsync("Supplier", "Delete", null, $"仕入先を削除しました: {supplierId}", cancellationToken);
    }

    public async Task<SupplierDto?> GetByIdAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierRepository.GetByIdAsync(supplierId, cancellationToken);
        return supplier is null ? null : ToDto(supplier);
    }

    public async Task<IReadOnlyCollection<SupplierDto>> ListAsync(string? keyword, CancellationToken cancellationToken = default)
    {
        var suppliers = await _supplierRepository.ListAsync(keyword, cancellationToken);
        return suppliers.Select(ToDto).ToArray();
    }

    public async Task SetProductSupplierPriceAsync(
        Guid productId,
        Guid supplierId,
        decimal unitPriceExcludingTax,
        CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product id is required.", nameof(productId));
        }

        if (supplierId == Guid.Empty)
        {
            throw new ArgumentException("Supplier id is required.", nameof(supplierId));
        }

        if (unitPriceExcludingTax < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPriceExcludingTax));
        }

        var product = await _productRepository.GetByIdAsync(productId, cancellationToken)
            ?? throw new InvalidOperationException("Product was not found.");
        if (!await _supplierRepository.ExistsAsync(supplierId, cancellationToken))
        {
            throw new InvalidOperationException("Supplier was not found.");
        }

        await _supplierRepository.SetProductSupplierPriceAsync(
            product.Id,
            supplierId,
            unitPriceExcludingTax,
            _timeProvider.GetUtcNow(),
            cancellationToken);
    }

    public Task<IReadOnlyCollection<ProductSupplierPriceDto>> GetProductSupplierPricesAsync(Guid productId, CancellationToken cancellationToken = default)
        => _supplierRepository.GetProductSupplierPricesAsync(productId, cancellationToken);

    private async Task EnsureCompanyNameUniqueAsync(string companyName, Guid? excludingSupplierId, CancellationToken cancellationToken)
    {
        if (await _supplierRepository.ExistsByCompanyNameAsync(companyName, excludingSupplierId, cancellationToken))
        {
            throw new InvalidOperationException("Company name already exists.");
        }
    }

    private static SupplierDto ToDto(Supplier supplier)
        => new(
            supplier.Id,
            supplier.CompanyName,
            supplier.ContactName,
            supplier.ContactEmail,
            supplier.ContactPhone,
            supplier.Notes,
            supplier.CreatedAtUtc,
            supplier.UpdatedAtUtc);
}
