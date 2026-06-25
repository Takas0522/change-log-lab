namespace OrderClientApp.Domain.Suppliers;

public sealed class Supplier
{
    public Supplier(
        Guid id,
        string companyName,
        string? contactName,
        string? contactEmail,
        string? contactPhone,
        string? notes,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Supplier id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(companyName))
        {
            throw new ArgumentException("Company name is required.", nameof(companyName));
        }

        Id = id;
        CompanyName = companyName.Trim();
        ContactName = NormalizeOptional(contactName);
        ContactEmail = NormalizeOptional(contactEmail);
        ContactPhone = NormalizeOptional(contactPhone);
        Notes = NormalizeOptional(notes);
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid Id { get; }

    public string CompanyName { get; private set; }

    public string? ContactName { get; private set; }

    public string? ContactEmail { get; private set; }

    public string? ContactPhone { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void Update(
        string companyName,
        string? contactName,
        string? contactEmail,
        string? contactPhone,
        string? notes,
        DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(companyName))
        {
            throw new ArgumentException("Company name is required.", nameof(companyName));
        }

        CompanyName = companyName.Trim();
        ContactName = NormalizeOptional(contactName);
        ContactEmail = NormalizeOptional(contactEmail);
        ContactPhone = NormalizeOptional(contactPhone);
        Notes = NormalizeOptional(notes);
        UpdatedAtUtc = nowUtc;
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
