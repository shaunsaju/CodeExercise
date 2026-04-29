using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationsApp.Models;
using NotificationsApp.Services;

namespace NotificationsApp.Tests.Unit;

public class NotificationServiceTests
{
    private readonly Mock<INotificationForwarder> _forwarderMock = new();
    private readonly Mock<IRateLimiter> _rateLimiterMock = new();
    private readonly NotificationService _sut;

    public NotificationServiceTests()
    {
        _rateLimiterMock.Setup(r => r.TryAcquire()).Returns(true);
        _forwarderMock
            .Setup(f => f.ForwardAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _sut = new NotificationService(
            _forwarderMock.Object,
            _rateLimiterMock.Object,
            Mock.Of<ILogger<NotificationService>>());
    }

    [Theory]
    [InlineData(NotificationLevel.Debug)]
    [InlineData(NotificationLevel.Info)]
    public async Task ProcessAsync_BelowThreshold_DoesNotForward(NotificationLevel level)
    {
        var notification = CreateNotification(level);

        var result = await _sut.ProcessAsync(notification);

        result.WasForwarded.Should().BeFalse();
        result.WasRateLimited.Should().BeFalse();
        _forwarderMock.Verify(
            f => f.ForwardAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(NotificationLevel.Warning)]
    [InlineData(NotificationLevel.Error)]
    [InlineData(NotificationLevel.Critical)]
    public async Task ProcessAsync_AtOrAboveThreshold_Forwards(NotificationLevel level)
    {
        var notification = CreateNotification(level);

        var result = await _sut.ProcessAsync(notification);

        result.WasForwarded.Should().BeTrue();
        result.WasRateLimited.Should().BeFalse();
        _forwarderMock.Verify(
            f => f.ForwardAsync(notification, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_RateLimited_DoesNotForward()
    {
        _rateLimiterMock.Setup(r => r.TryAcquire()).Returns(false);
        var notification = CreateNotification(NotificationLevel.Warning);

        var result = await _sut.ProcessAsync(notification);

        result.WasForwarded.Should().BeFalse();
        result.WasRateLimited.Should().BeTrue();
        _forwarderMock.Verify(
            f => f.ForwardAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_ForwarderFails_ReturnsNotForwarded()
    {
        // Simulate a failure in the forwarder (i.e., Discord webhook call fails)
        _forwarderMock
            .Setup(f => f.ForwardAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var notification = CreateNotification(NotificationLevel.Error);

        var result = await _sut.ProcessAsync(notification);

        result.WasForwarded.Should().BeFalse();
        result.WasRateLimited.Should().BeFalse();
        result.Details.Should().Contain("failed");
    }

    private static Notification CreateNotification(NotificationLevel level) =>
        new()
        {
            Title = "Test notification",
            Message = "Test message body",
            Level = level
        };
}
