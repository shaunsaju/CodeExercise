using Microsoft.Extensions.Logging;
using NotificationsApp.Models;

namespace NotificationsApp.Services;

/// <summary>
/// Orchestrates notification processing: evaluates severity, enforces rate limits,
/// and delegates forwarding to <see cref="INotificationForwarder"/>.
/// </summary>
public sealed class NotificationService(
    INotificationForwarder forwarder,
    IRateLimiter rateLimiter,
    ILogger<NotificationService> logger)
{
    private const NotificationLevel ForwardingThreshold = NotificationLevel.Warning;

    /// <summary>
    /// Processes an incoming notification. Forwards it if the severity warrants it
    /// and the rate limit has not been exceeded.
    /// </summary>
    /// <returns>A result describing what happened.</returns>
    public async Task<NotificationResult> ProcessAsync(
        Notification notification, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Received {Level} notification: {Title}", notification.Level, notification.Title);

        if (notification.Level < ForwardingThreshold)
        {
            logger.LogDebug(
                "Notification '{Title}' below forwarding threshold ({Level} < {Threshold})",
                notification.Title, notification.Level, ForwardingThreshold);

            return NotificationResult.NotForwarded("Level below forwarding threshold.");
        }

        if (!rateLimiter.TryAcquire())
        {
            logger.LogWarning(
                "Rate limit exceeded — notification '{Title}' not forwarded", notification.Title);

            return NotificationResult.RateLimited();
        }

        var success = await forwarder.ForwardAsync(notification, ct);

        return success
            ? NotificationResult.Forwarded("Sent to Discord.")
            : NotificationResult.ForwardingFailed("Discord webhook call failed.");
    }
}

/// <summary>
/// Represents the outcome of processing a notification.
/// </summary>
public sealed record NotificationResult(
    bool WasForwarded,
    bool WasRateLimited,
    string Details)
{
    public static NotificationResult NotForwarded(string details) =>
        new(false, false, details);

    public static NotificationResult Forwarded(string details) =>
        new(true, false, details);

    public static NotificationResult RateLimited() =>
        new(false, true, "Rate limit exceeded. Max messages per minute reached.");

    public static NotificationResult ForwardingFailed(string details) =>
        new(false, false, details);
}
