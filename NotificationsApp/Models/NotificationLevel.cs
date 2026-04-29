namespace NotificationsApp.Models;

/// <summary>
/// Defines the severity levels for notifications, ordered from lowest to highest.
/// Notifications at <see cref="Warning"/> or above trigger forwarding to external services.
/// </summary>
public enum NotificationLevel
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3,
    Critical = 4
}
