# NotificationsApp

A lightweight ASP.NET Core Web API that receives notifications via HTTP and automatically forwards high-severity ones to Discord via webhook, with built-in rate limiting.

### Severity Levels

| Ordinal | Level      | Forwarded? |
| ------- | ---------- | ---------- |
| 0       | `Debug`    | No         |
| 1       | `Info`     | No         |
| 2       | `Warning`  | Yes        |
| 3       | `Error`    | Yes        |
| 4       | `Critical` | Yes        |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A Discord webhook URL for live forwarding

## Configuration

Edit `appsettings.json`:

```json
{
  "Discord": {
    "WebhookUrl": "https://discord.com/api/webhooks/YOUR_ID/YOUR_TOKEN"
  },
  "RateLimiting": {
    "MaxMessagesPerMinute": 10
  }
}
```

| Setting                             | Description                                                             | Default     |
| ----------------------------------- | ----------------------------------------------------------------------- | ----------- |
| `Discord:WebhookUrl`                | Discord webhook endpoint. Replace the placeholder with your actual URL. | Placeholder |
| `RateLimiting:MaxMessagesPerMinute` | Maximum forwarded messages per sliding 1-minute window.                 | `10`        |

> Or configure via environment variables:
> `Discord__WebhookUrl=https://...` and `RateLimiting__MaxMessagesPerMinute=10`

## Build & Run

```bash
# Build the solution
dotnet build NotificationsApp/NotificationsApp.slnx

# Run the API
dotnet run --project NotificationsApp/NotificationsApp.csproj

# The API listens on http://localhost:5078
```

## API Reference

### `POST /api/notifications`

Receives a notification and forwards it to Discord if the severity is Warning or higher.

**Request body:**

```json
{
  "title": "High memory usage",
  "message": "Server memory has exceeded 85% threshold.",
  "level": "Warning"
}
```

| Field       | Type   | Required | Description                                             |
| ----------- | ------ | -------- | ------------------------------------------------------- |
| `title`     | string | Yes      | Short title (max 200 chars)                             |
| `message`   | string | Yes      | Detailed message body (max 4000 chars)                  |
| `level`     | string | Yes      | One of: `Debug`, `Info`, `Warning`, `Error`, `Critical` |
| `timestamp` | string | No       | ISO 8601 timestamp. Defaults to UTC now.                |

**Responses:**

| Status                  | When                      | Body                                                                   |
| ----------------------- | ------------------------- | ---------------------------------------------------------------------- |
| `201 Created`           | Notification accepted     | `{ "id": "...", "forwarded": true/false, "forwardingDetails": "..." }` |
| `400 Bad Request`       | Invalid or missing fields | Validation error details                                               |
| `429 Too Many Requests` | Rate limit exceeded       | `{ "error": "Rate limit exceeded..." }`                                |

**Example with curl:**

```bash
# Info notification (not forwarded)
curl -X POST http://localhost:5078/api/notifications \
  -H "Content-Type: application/json" \
  -d '{"title":"Deploy started","message":"v2.4.1 deploying.","level":"Info"}'

# Warning notification (forwarded to Discord)
curl -X POST http://localhost:5078/api/notifications \
  -H "Content-Type: application/json" \
  -d '{"title":"High CPU","message":"CPU at 95%.","level":"Warning"}'
```

## Running Tests

```bash
# Run all tests (unit + integration)
dotnet test NotificationsApp.Tests/NotificationsApp.Tests.csproj

# Run with verbose output
dotnet test NotificationsApp.Tests/NotificationsApp.Tests.csproj --verbosity normal
```

### Test Coverage

| Category        | Class                               | Tests  | What's Covered                                            |
| --------------- | ----------------------------------- | ------ | --------------------------------------------------------- |
| **Unit**        | `NotificationServiceTests`          | 6      | Threshold logic, rate limiting, forwarder failure         |
| **Unit**        | `SlidingWindowRateLimiterTests`     | 4      | Within limit, over limit, window expiry, concurrency      |
| **Unit**        | `DiscordNotificationForwarderTests` | 5      | HTTP success/error, network failure, payload format, URL  |
| **Integration** | `NotificationsEndpointTests`        | 7      | Full HTTP pipeline: all levels, validation, rate limiting |
|                 | **Total**                           | **22** |                                                           |

### Manual Testing

Open `NotificationsApp.http` in VS Code and run the requests by clicking the "Send Request" button.
