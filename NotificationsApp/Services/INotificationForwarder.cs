using NotificationsApp.Models;

namespace NotificationsApp.Services;

/// <summary>
/// Abstraction for forwarding high-severity notifications to an external service.
/// </summary>
public interface INotificationForwarder
{
    /// <summary>
    /// Forwards the notification to the external service.
    /// </summary>
    /// <returns><c>true</c> if the notification was forwarded successfully; otherwise <c>false</c>.</returns>
    Task<bool> ForwardAsync(Notification notification, CancellationToken ct = default);
}
