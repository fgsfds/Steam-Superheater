using Avalonia.Controls.Notifications;

namespace Avalonia.Desktop.Helpers;

/// <summary>
/// Helper that fixes crash when multiple notifications with the same text are shown.
/// </summary>
public static class NotificationsHelper
{
    public static WindowNotificationManager NotificationManager { get; }

    static NotificationsHelper()
    {
        NotificationManager = new(AvaloniaProperties.TopLevel)
        {
            MaxItems = 3,
            Position = NotificationPosition.TopRight,
            Margin = new(0, 50, 10, 0)
        };
    }

    public static void Show(
        object content,
        NotificationType type,
        TimeSpan? expiration = null,
        Action? onClick = null,
        Action? onClose = null,
        string[]? classes = null)
    {
        NotificationManager.Show(
            content,
            type,
            expiration,
            onClick,
            onClose,
            classes
            );
    }
}
