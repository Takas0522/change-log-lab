namespace OrderClientApp.Application.Abstractions.Operations;

public sealed record OperationLogEntryDto(
    Guid Id,
    string Category,
    string Action,
    string? Actor,
    string Description,
    DateTimeOffset CreatedAtUtc);

public sealed record OperationLogQuery(
    string? Keyword,
    string? Category,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc,
    int Limit = 200);
