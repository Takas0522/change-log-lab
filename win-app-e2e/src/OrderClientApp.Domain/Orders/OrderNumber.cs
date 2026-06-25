using System.Text.RegularExpressions;

namespace OrderClientApp.Domain.Orders;

public readonly partial record struct OrderNumber
{
    private static readonly Regex Pattern = OrderNumberRegex();

    private OrderNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static OrderNumber Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Order number is required.", nameof(value));
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (!Pattern.IsMatch(normalized))
        {
            throw new ArgumentException("Order number format is invalid.", nameof(value));
        }

        return new OrderNumber(normalized);
    }

    public static OrderNumber FromSequence(int sequence)
    {
        if (sequence <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence));
        }

        return new OrderNumber($"PO-{sequence:D4}");
    }

    public override string ToString() => Value;

    [GeneratedRegex("^PO-[0-9]{4,}$", RegexOptions.Compiled)]
    private static partial Regex OrderNumberRegex();
}
