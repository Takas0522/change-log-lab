using System.Globalization;
using System.Text;
using OrderClientApp.Application.Abstractions.Products;
using OrderClientApp.Domain.Products;

namespace OrderClientApp.Application.Services.Products;

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly TimeProvider _timeProvider;

    public ProductService(IProductRepository productRepository, TimeProvider? timeProvider = null)
    {
        _productRepository = productRepository;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureCodeIsUniqueAsync(request.ProductCode, null, cancellationToken);

        var nowUtc = _timeProvider.GetUtcNow();
        var product = new Product(
            Guid.NewGuid(),
            request.ProductCode,
            request.Name,
            request.UnitPriceExcludingTax,
            request.Unit,
            request.Notes,
            request.Category,
            request.ReorderPoint,
            preferredSupplierId: null,
            nowUtc,
            nowUtc);

        await _productRepository.AddAsync(product, cancellationToken);
        return ToDto(product);
    }

    public async Task<ProductDto> UpdateAsync(UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException("Product was not found.");
        await EnsureCodeIsUniqueAsync(request.ProductCode, request.Id, cancellationToken);

        product.Update(
            request.ProductCode,
            request.Name,
            request.UnitPriceExcludingTax,
            request.Unit,
            request.Notes,
            request.Category,
            request.ReorderPoint,
            _timeProvider.GetUtcNow());

        await _productRepository.UpdateAsync(product, cancellationToken);
        return ToDto(product);
    }

    public Task DeleteAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product id is required.", nameof(productId));
        }

        return _productRepository.DeleteAsync(productId, cancellationToken);
    }

    public async Task<ProductDto?> GetByIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
        return product is null ? null : ToDto(product);
    }

    public async Task<IReadOnlyCollection<ProductDto>> ListAsync(ProductFilter filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        var products = await _productRepository.ListAsync(filter, cancellationToken);
        return products.Select(ToDto).ToArray();
    }

    public async Task SetPreferredSupplierAsync(Guid productId, Guid? supplierId, CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product id is required.", nameof(productId));
        }

        await _productRepository.SetPreferredSupplierAsync(productId, supplierId, _timeProvider.GetUtcNow(), cancellationToken);
    }

    public async Task<ProductCsvImportResult> ImportCsvAsync(string csvContent, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(csvContent))
        {
            return new ProductCsvImportResult(0, 0, 0, []);
        }

        var errors = new List<ProductImportRowError>();
        var lines = csvContent.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
        {
            return new ProductCsvImportResult(0, 0, 0, []);
        }

        var nowUtc = _timeProvider.GetUtcNow();
        var imported = 0;
        var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 1; index < lines.Length; index++)
        {
            var rowNumber = index + 1;
            var columns = ParseCsvLine(lines[index]);
            if (columns.Count < 7)
            {
                errors.Add(new ProductImportRowError(rowNumber, "row", "Column count is invalid."));
                continue;
            }

            var productCode = columns[0].Trim();
            var name = columns[1].Trim();
            var unitPriceText = columns[2].Trim();
            var unit = columns[3].Trim();
            var notes = string.IsNullOrWhiteSpace(columns[4]) ? null : columns[4].Trim();
            var category = columns[5].Trim();
            var reorderPointText = columns[6].Trim();

            ValidateRequired(productCode, "productCode", rowNumber, errors);
            ValidateRequired(name, "name", rowNumber, errors);
            ValidateRequired(unit, "unit", rowNumber, errors);
            ValidateRequired(category, "category", rowNumber, errors);

            if (!decimal.TryParse(unitPriceText, NumberStyles.Any, CultureInfo.InvariantCulture, out var unitPrice) || unitPrice < 0)
            {
                errors.Add(new ProductImportRowError(rowNumber, "unitPriceExcludingTax", "Unit price must be a non-negative number."));
            }

            if (!int.TryParse(reorderPointText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var reorderPoint) || reorderPoint < 0)
            {
                errors.Add(new ProductImportRowError(rowNumber, "reorderPoint", "Reorder point must be a non-negative integer."));
            }

            if (seenCodes.Contains(productCode))
            {
                errors.Add(new ProductImportRowError(rowNumber, "productCode", "Duplicate code in CSV."));
            }

            if (errors.Any(x => x.RowNumber == rowNumber))
            {
                continue;
            }

            seenCodes.Add(productCode);
            if (await _productRepository.ExistsByCodeAsync(productCode, null, cancellationToken))
            {
                errors.Add(new ProductImportRowError(rowNumber, "productCode", "Product code already exists."));
                continue;
            }

            var product = new Product(
                Guid.NewGuid(),
                productCode,
                name,
                unitPrice,
                unit,
                notes,
                category,
                reorderPoint,
                preferredSupplierId: null,
                nowUtc,
                nowUtc);
            await _productRepository.AddAsync(product, cancellationToken);
            imported++;
        }

        return new ProductCsvImportResult(lines.Length - 1, imported, errors.Count, errors);
    }

    private async Task EnsureCodeIsUniqueAsync(string productCode, Guid? excludingProductId, CancellationToken cancellationToken)
    {
        if (await _productRepository.ExistsByCodeAsync(productCode, excludingProductId, cancellationToken))
        {
            throw new InvalidOperationException("Product code already exists.");
        }
    }

    private static void ValidateRequired(string value, string field, int rowNumber, List<ProductImportRowError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ProductImportRowError(rowNumber, field, $"{field} is required."));
        }
    }

    private static ProductDto ToDto(Product product)
        => new(
            product.Id,
            product.ProductCode,
            product.Name,
            product.UnitPriceExcludingTax,
            product.Unit,
            product.Notes,
            product.Category,
            product.ReorderPoint,
            product.PreferredSupplierId,
            product.CreatedAtUtc,
            product.UpdatedAtUtc);

    private static IReadOnlyList<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        foreach (var ch in line)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        values.Add(current.ToString());
        return values;
    }
}
