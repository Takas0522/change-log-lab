namespace OrderClientApp.Application.Abstractions.Operations;

public interface IOperationLogService
{
    Task LogAsync(
        string category,
        string action,
        string? actor,
        string description,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<OperationLogEntryDto>> QueryAsync(OperationLogQuery query, CancellationToken cancellationToken = default);
}
