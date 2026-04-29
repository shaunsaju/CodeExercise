using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationsApp.Configuration;
using NotificationsApp.Models;

namespace NotificationsApp.Services;

/// <summary>
/// Forwards notifications to a Discord channel via webhook.
/// </summary>
public sealed class DiscordNotificationForwarder(
    HttpClient httpClient,
    IOptions<DiscordSettings> settings,
    ILogger<DiscordNotificationForwarder> logger) : INotificationForwarder
{
    private readonly DiscordSettings _settings = settings.Value;

    public async Task<bool> ForwardAsync(Notification notification, CancellationToken ct = default)
    {
        var payload = BuildPayload(notification);

        try
        {
            var response = await httpClient.PostAsJsonAsync(_settings.WebhookUrl, payload, ct);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation(
                    "Forwarded {Level} notification '{Title}' to Discord",
                    notification.Level, notification.Title);
                return true;
            }

            logger.LogWarning(
                "Discord webhook returned {StatusCode} for notification '{Title}'",
                (int)response.StatusCode, notification.Title);
            return false;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex,
                "Failed to forward notification '{Title}' to Discord", notification.Title);
            return false;
        }
    }

    /// <summary>
    /// Builds a Discord webhook payload with a color-coded embed.
    /// </summary>
    private static object BuildPayload(Notification notification)
    {
        return new
        {
            embeds = new[]
            {
                new
                {
                    title = $"[{notification.Level}] {notification.Title}",
                    description = notification.Message,
                    color = GetColorForLevel(notification.Level),
                    timestamp = notification.Timestamp.ToString("o"),
                    footer = new { text = "NotificationsApp" }
                }
            }
        };
    }

    /// <summary>
    /// Maps notification levels to Discord embed colors (decimal RGB).
    /// </summary>
    private static int GetColorForLevel(NotificationLevel level) => level switch
    {
        NotificationLevel.Warning  => 0xFFA500, // Orange
        NotificationLevel.Error    => 0xFF4444, // Red
        NotificationLevel.Critical => 0x8B0000, // Dark red
        _                          => 0x808080  // Grey (should not occur for forwarded messages)
    };
}
