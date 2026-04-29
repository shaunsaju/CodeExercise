using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationsApp.Models;
using NotificationsApp.Services;

namespace NotificationsApp.Tests.Integration;

public class NotificationsEndpointTests(NotificationsWebApplicationFactory factory) : IClassFixture<NotificationsWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly FakeNotificationForwarder _fakeForwarder = factory.FakeForwarder;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task PostNotification_InfoLevel_Returns201NotForwarded()
    {
        var notification = new { title = "Info test", message = "Info message", level = "Info" };

        var response = await _client.PostAsJsonAsync("/api/notifications", notification);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await DeserializeResponse(response);
        body.Forwarded.Should().BeFalse();
        _fakeForwarder.ForwardedCount.Should().Be(0);
    }

    [Fact]
    public async Task PostNotification_WarningLevel_Returns201Forwarded()
    {
        var notification = new { title = "Warn test", message = "Warning message", level = "Warning" };

        var response = await _client.PostAsJsonAsync("/api/notifications", notification);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await DeserializeResponse(response);
        body.Forwarded.Should().BeTrue();
    }

    [Fact]
    public async Task PostNotification_ErrorLevel_Returns201Forwarded()
    {
        var notification = new { title = "Error test", message = "Error message", level = "Error" };

        var response = await _client.PostAsJsonAsync("/api/notifications", notification);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await DeserializeResponse(response);
        body.Forwarded.Should().BeTrue();
    }

    [Fact]
    public async Task PostNotification_InvalidPayload_Returns400()
    {
        var invalid = new { message = "Missing title and level" };

        var response = await _client.PostAsJsonAsync("/api/notifications", invalid);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostNotification_ExceedsRateLimit_Returns429()
    {
        // We have to use a dedicated factory with a rate limit of 2 for this test, instead of the shared one from the fixture with a higher limit (100).
        using var factory = new NotificationsWebApplicationFactory(maxPerMinute: 2);
        using var client = factory.CreateClient();

        var warning = new { title = "Rate test", message = "msg", level = "Warning" };

        // First two should succeed
        (await client.PostAsJsonAsync("/api/notifications", warning)).StatusCode
            .Should().Be(HttpStatusCode.Created);
        (await client.PostAsJsonAsync("/api/notifications", warning)).StatusCode
            .Should().Be(HttpStatusCode.Created);

        // Third should be rate-limited
        var response = await client.PostAsJsonAsync("/api/notifications", warning);
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task PostNotification_DebugLevel_Returns201NotForwarded()
    {
        var notification = new { title = "Debug test", message = "Debug message", level = "Debug" };

        var response = await _client.PostAsJsonAsync("/api/notifications", notification);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await DeserializeResponse(response);
        body.Forwarded.Should().BeFalse();
    }

    private static async Task<NotificationResponse> DeserializeResponse(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<NotificationResponse>(JsonOptions);
        body.Should().NotBeNull();
        return body!;
    }
}

/// <summary>
/// Custom <see cref="WebApplicationFactory{TEntryPoint}"/> that replaces the real
/// <see cref="INotificationForwarder"/> with a test fake and configures a tight rate limit.
/// </summary>
public sealed class NotificationsWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly int _maxPerMinute;

    public FakeNotificationForwarder FakeForwarder { get; } = new();

    // Need this for xunit's IClassFixture
    public NotificationsWebApplicationFactory() : this(100) { }

    internal NotificationsWebApplicationFactory(int maxPerMinute)
    {
        _maxPerMinute = maxPerMinute;
    }

    // Replace the real forwarder with our fake and set a custom configuration for testing
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Provide a valid Discord URL so options validation passes
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Discord:WebhookUrl"] = "https://discord.com/api/webhooks/fake/token",
                ["RateLimiting:MaxMessagesPerMinute"] = _maxPerMinute.ToString()
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the real forwarder and replace with the fake
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(INotificationForwarder));

            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddSingleton<INotificationForwarder>(FakeForwarder);
        });
    }
}

/// <summary>
/// Test double that records forwarded notifications without making HTTP calls.
/// </summary>
public sealed class FakeNotificationForwarder : INotificationForwarder
{
    private int _forwardedCount;

    /// <summary>
    /// Simple way to check if the forwarder was called.
    /// </summary>
    public int ForwardedCount => _forwardedCount;

    public Task<bool> ForwardAsync(Notification notification, CancellationToken ct = default)
    {
        Interlocked.Increment(ref _forwardedCount);
        return Task.FromResult(true);
    }
}
