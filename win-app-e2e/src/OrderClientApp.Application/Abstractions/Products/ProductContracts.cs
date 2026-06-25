namespace OrderClientApp.Application.Abstractions.Products;

public sealed record ProductDto(
    Guid Id,
    string ProductCode,
    string Name,
    decimal UnitPriceExcludingTax,
    string Unit,
    string? Notes,
    string Category,
    int ReorderPoint,
    Guid? PreferredSupplierId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record ProductFilter(
    string? ProductCode,
    string? Name,
    string? Category);

public sealed record CreateProductRequest(
    string ProductCode,
    string Name,
    decimal UnitPriceExcludingTax,
    string Unit,
    string? Notes,
    string Category,
    int ReorderPoint);

public sealed record UpdateProductRequest(
    Guid Id,
    string ProductCode,
    string Name,
    decimal UnitPriceExcludingTax,
    string Unit,
    string? Notes,
    string Category,
    int ReorderPoint);

public sealed record ProductImportRowError(
    int RowNumber,
    string Field,
    string Message);

public sealed record ProductCsvImportResult(
    int TotalRows,
    int ImportedCount,
    int ErrorCount,
    IReadOnlyCollection<ProductImportRowError> Errors);
