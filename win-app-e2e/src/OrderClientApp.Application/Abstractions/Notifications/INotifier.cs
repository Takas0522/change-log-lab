namespace OrderClientApp.Application.Abstractions.Notifications;

public sealed record AppNotification(
    string Title,
    string Message);

public interface INotifier
{
    Task NotifyAsync(AppNotification notification, CancellationToken cancellationToken = default);
}
