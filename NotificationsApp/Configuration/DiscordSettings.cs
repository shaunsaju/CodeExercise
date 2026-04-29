using System.ComponentModel.DataAnnotations;

namespace NotificationsApp.Configuration;

/// <summary>
/// Strongly-typed configuration for the Discord webhook integration.
/// </summary>
public sealed class DiscordSettings
{
    public const string SectionName = "Discord";

    [Required, Url]
    public required string WebhookUrl { get; set; }
}
