using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NotificationsApp.Models;

/// <summary>
/// Represents an incoming notification payload.
/// </summary>
public sealed record Notification
{
    [Required, MaxLength(200)]
    public required string Title { get; init; }

    [Required, MaxLength(4000)]
    public required string Message { get; init; }

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required NotificationLevel Level { get; init; }

    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
