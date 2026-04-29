using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationsApp.Configuration;
using NotificationsApp.Models;
using NotificationsApp.Services;

namespace NotificationsApp.Tests.Unit;

public class DiscordNotificationForwarderTests
{
    private static readonly DiscordSettings DefaultSettings = new()
    {
        WebhookUrl = "https://discord.com/api/webhooks/test/token"
    };

    [Fact]
    public async Task ForwardAsync_SuccessfulResponse_ReturnsTrue()
    {
        // Expected behavior
        var handler = new MockHttpMessageHandler(HttpStatusCode.NoContent);
        var forwarder = CreateForwarder(handler);

        var result = await forwarder.ForwardAsync(CreateNotification(NotificationLevel.Warning));

        result.Should().BeTrue();
        handler.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task ForwardAsync_HttpError_ReturnsFalse()
    {
        // Simulate a 400 Bad Request response from the Discord webhook
        var handler = new MockHttpMessageHandler(HttpStatusCode.BadRequest);
        var forwarder = CreateForwarder(handler);

        var result = await forwarder.ForwardAsync(CreateNotification(NotificationLevel.Error));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ForwardAsync_NetworkFailure_ReturnsFalse()
    {
        var handler = new MockHttpMessageHandler(
            new HttpRequestException("Connection refused"));
        var forwarder = CreateForwarder(handler);

        var result = await forwarder.ForwardAsync(CreateNotification(NotificationLevel.Critical));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ForwardAsync_SendsCorrectPayloadStructure()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.NoContent);
        var forwarder = CreateForwarder(handler);

        await forwarder.ForwardAsync(CreateNotification(NotificationLevel.Warning, "Test title"));

        handler.LastRequestContent.Should().NotBeNull();
        var body = await handler.LastRequestContent!.ReadAsStringAsync();
        body.Should().Contain("Test title");
        // Discords embed structure should be present in the payload
        body.Should().Contain("embeds");
    }

    [Fact]
    public async Task ForwardAsync_UsesConfiguredWebhookUrl()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.NoContent);
        var forwarder = CreateForwarder(handler);

        await forwarder.ForwardAsync(CreateNotification(NotificationLevel.Warning));

        handler.LastRequestUri.Should().Be(DefaultSettings.WebhookUrl);
    }

    private static DiscordNotificationForwarder CreateForwarder(MockHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        return new DiscordNotificationForwarder(
            httpClient,
            Options.Create(DefaultSettings),
            new LoggerFactory().CreateLogger<DiscordNotificationForwarder>());
    }

    private static Notification CreateNotification(
        NotificationLevel level, string title = "Alert") =>
        new()
        {
            Title = title,
            Message = "Test message",
            Level = level
        };
}

/// <summary>
/// A configurable <see cref="HttpMessageHandler"/> for testing HTTP calls
/// without making real network requests.
/// </summary>
internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode? _statusCode;
    private readonly HttpRequestException? _exception;

    public int RequestCount { get; private set; }
    public HttpContent? LastRequestContent { get; private set; }
    public string? LastRequestUri { get; private set; }

    public MockHttpMessageHandler(HttpStatusCode statusCode) => _statusCode = statusCode;

    public MockHttpMessageHandler(HttpRequestException exception) => _exception = exception;

    // Intercept the HTTP request, and record details
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestCount++;
        LastRequestUri = request.RequestUri?.ToString();
        // Read and buffer the content before the stream is disposed
        if (request.Content is not null)
        {
            var content = await request.Content.ReadAsStringAsync(cancellationToken);
            LastRequestContent = new StringContent(content);
        }

        if (_exception is not null)
            throw _exception;

        return new HttpResponseMessage(_statusCode!.Value);
    }
}
