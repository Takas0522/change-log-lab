namespace OrderClientApp.Application.Abstractions.Operations;

public interface IOperationLogRepository
{
    Task AddAsync(OperationLogEntryDto entry, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<OperationLogEntryDto>> QueryAsync(OperationLogQuery query, CancellationToken cancellationToken = default);
}
