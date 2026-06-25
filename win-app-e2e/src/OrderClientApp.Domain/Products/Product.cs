namespace OrderClientApp.Domain.Products;

public sealed class Product
{
    public Product(
        Guid id,
        string productCode,
        string name,
        decimal unitPriceExcludingTax,
        string unit,
        string? notes,
        string category,
        int reorderPoint,
        Guid? preferredSupplierId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Product id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(productCode))
        {
            throw new ArgumentException("Product code is required.", nameof(productCode));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(unit))
        {
            throw new ArgumentException("Unit is required.", nameof(unit));
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Category is required.", nameof(category));
        }

        if (unitPriceExcludingTax < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPriceExcludingTax));
        }

        if (reorderPoint < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(reorderPoint));
        }

        Id = id;
        ProductCode = productCode.Trim();
        Name = name.Trim();
        UnitPriceExcludingTax = unitPriceExcludingTax;
        Unit = unit.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        Category = category.Trim();
        ReorderPoint = reorderPoint;
        PreferredSupplierId = preferredSupplierId;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid Id { get; }

    public string ProductCode { get; private set; }

    public string Name { get; private set; }

    public decimal UnitPriceExcludingTax { get; private set; }

    public string Unit { get; private set; }

    public string? Notes { get; private set; }

    public string Category { get; private set; }

    public int ReorderPoint { get; private set; }

    public Guid? PreferredSupplierId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void Update(
        string productCode,
        string name,
        decimal unitPriceExcludingTax,
        string unit,
        string? notes,
        string category,
        int reorderPoint,
        DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(productCode))
        {
            throw new ArgumentException("Product code is required.", nameof(productCode));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(unit))
        {
            throw new ArgumentException("Unit is required.", nameof(unit));
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Category is required.", nameof(category));
        }

        if (unitPriceExcludingTax < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPriceExcludingTax));
        }

        if (reorderPoint < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(reorderPoint));
        }

        ProductCode = productCode.Trim();
        Name = name.Trim();
        UnitPriceExcludingTax = unitPriceExcludingTax;
        Unit = unit.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        Category = category.Trim();
        ReorderPoint = reorderPoint;
        UpdatedAtUtc = nowUtc;
    }

    public void SetPreferredSupplier(Guid? supplierId, DateTimeOffset nowUtc)
    {
        PreferredSupplierId = supplierId;
        UpdatedAtUtc = nowUtc;
    }
}
