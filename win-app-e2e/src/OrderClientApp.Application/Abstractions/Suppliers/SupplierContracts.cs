namespace OrderClientApp.Application.Abstractions.Suppliers;

public sealed record SupplierDto(
    Guid Id,
    string CompanyName,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string? Notes,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CreateSupplierRequest(
    string CompanyName,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string? Notes);

public sealed record UpdateSupplierRequest(
    Guid Id,
    string CompanyName,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string? Notes);

public sealed record ProductSupplierPriceDto(
    Guid ProductId,
    Guid SupplierId,
    decimal UnitPriceExcludingTax,
    DateTimeOffset UpdatedAtUtc);
