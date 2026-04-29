namespace NotificationsApp.Configuration;

/// <summary>
/// Strongly-typed configuration for the notification forwarding rate limiter.
/// </summary>
public sealed class RateLimitingSettings
{
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// Maximum number of forwarded messages allowed per minute. Default: 10.
    /// </summary>
    public int MaxMessagesPerMinute { get; set; } = 10;
}
