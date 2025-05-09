using Avalonia.Controls.Notifications;
using Avalonia.Desktop.Helpers;

namespace Avalonia.Desktop.Helpers;

/// <summary>
/// Helper that fixes crash when multiple notifications with the same text are shown.
/// </summary>
public static class NotificationsHelper
{
    public static WindowNotificationManager NotificationManager { get; }

    private static Random Random { get; }

    static NotificationsHelper()
    {
        Random = new();

        NotificationManager = new(AvaloniaProperties.TopLevel)
        {
            MaxItems = 3,
            Position = NotificationPosition.TopRight,
            Margin = new(0, 50, 10, 0)
        };
    }

    [Obsolete("Remove when https://github.com/AvaloniaUI/Avalonia/issues/15766 is fixed.")]
    public static void Show(
        object content,
        NotificationType type,
        TimeSpan? expiration = null,
        Action? onClick = null,
        Action? onClose = null,
        string[]? classes = null)
    {
        var length = Random.Next(1, 200);
        var repeatedString = new string('\u200B', length);

        NotificationManager.Show(
            content + repeatedString,
            type,
            expiration,
            onClick,
            onClose,
            classes
            );
    }
}
