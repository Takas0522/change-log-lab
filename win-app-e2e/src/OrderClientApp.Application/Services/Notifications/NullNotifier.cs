using OrderClientApp.Application.Abstractions.Notifications;

namespace OrderClientApp.Application.Services.Notifications;

public sealed class NullNotifier : INotifier
{
    public Task NotifyAsync(AppNotification notification, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
