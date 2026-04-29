namespace NotificationsApp.Models;

/// <summary>
/// Response returned after a notification is processed.
/// </summary>
public sealed record NotificationResponse(
    Guid Id,
    bool Forwarded,
    string? ForwardingDetails);
