using OrderClientApp.Application.Abstractions.Operations;

namespace OrderClientApp.Application.Services.Operations;

public sealed class OperationLogService : IOperationLogService
{
    private readonly IOperationLogRepository _repository;
    private readonly TimeProvider _timeProvider;

    public OperationLogService(
        IOperationLogRepository repository,
        TimeProvider? timeProvider = null)
    {
        _repository = repository;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Task<IReadOnlyCollection<OperationLogEntryDto>> QueryAsync(OperationLogQuery query, CancellationToken cancellationToken = default)
        => _repository.QueryAsync(query, cancellationToken);

    public Task LogAsync(
        string category,
        string action,
        string? actor,
        string description,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Category is required.", nameof(category));
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Action is required.", nameof(action));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.", nameof(description));
        }

        var entry = new OperationLogEntryDto(
            Guid.NewGuid(),
            category.Trim(),
            action.Trim(),
            string.IsNullOrWhiteSpace(actor) ? null : actor.Trim(),
            description.Trim(),
            _timeProvider.GetUtcNow());
        return _repository.AddAsync(entry, cancellationToken);
    }
}
