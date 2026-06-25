using CommunityToolkit.WinUI.Notifications;
using OrderClientApp.Application.Abstractions.Notifications;

namespace OrderClientApp.Wpf;

public sealed class WindowsToastNotifier : INotifier
{
    public Task NotifyAsync(AppNotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        try
        {
            _ = new ToastContentBuilder()
                .AddText(notification.Title)
                .AddText(notification.Message);
        }
        catch
        {
            // no-op fallback for unsupported/non-toast environments
        }

        return Task.CompletedTask;
    }
}
