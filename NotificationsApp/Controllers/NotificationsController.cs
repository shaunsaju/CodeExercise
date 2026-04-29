using Microsoft.AspNetCore.Mvc;
using NotificationsApp.Models;
using NotificationsApp.Services;

namespace NotificationsApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class NotificationsController(NotificationService service, ILogger<NotificationsController> logger) : ControllerBase
{

    /// <summary>
    /// Receives a notification and optionally forwards it to Discord
    /// if the level is Warning or higher.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(NotificationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Create(
        [FromBody] Notification notification, CancellationToken ct)
    {
        var result = await service.ProcessAsync(notification, ct);

        if (result.WasRateLimited)
        {
            logger.LogWarning("Returning 429 for notification '{Title}'", notification.Title);
            return StatusCode(StatusCodes.Status429TooManyRequests, new { error = result.Details });
        }

        var response = new NotificationResponse(
            Id: Guid.NewGuid(),
            Forwarded: result.WasForwarded,
            ForwardingDetails: result.Details);

        return CreatedAtAction(null, response);
    }
}
