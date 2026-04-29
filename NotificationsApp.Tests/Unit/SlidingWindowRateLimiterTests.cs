using FluentAssertions;
using Microsoft.Extensions.Options;
using NotificationsApp.Configuration;
using NotificationsApp.Services;

namespace NotificationsApp.Tests.Unit;

public class SlidingWindowRateLimiterTests
{
    [Fact]
    public void TryAcquire_WithinLimit_ReturnsTrue()
    {
        var limiter = CreateLimiter(maxPerMinute: 5);

        for (var i = 0; i < 5; i++)
        {
            limiter.TryAcquire().Should().BeTrue($"request {i + 1} should be allowed");
        }
    }

    [Fact]
    public void TryAcquire_ExceedsLimit_ReturnsFalse()
    {
        var limiter = CreateLimiter(maxPerMinute: 3);

        for (var i = 0; i < 3; i++)
            limiter.TryAcquire();

        limiter.TryAcquire().Should().BeFalse("the 4th request exceeds the limit of 3");
    }

    [Fact]
    public void TryAcquire_AfterWindowExpires_AllowsAgain()
    {
        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var limiter = CreateLimiter(maxPerMinute: 2, fakeTime);

        limiter.TryAcquire().Should().BeTrue();
        limiter.TryAcquire().Should().BeTrue();
        limiter.TryAcquire().Should().BeFalse("limit is exhausted");

        // Advance time past the 1-minute window
        fakeTime.Advance(TimeSpan.FromMinutes(1).Add(TimeSpan.FromMilliseconds(1)));

        limiter.TryAcquire().Should().BeTrue("window has slid past the old entries");
    }

    [Fact]
    public void TryAcquire_ConcurrentAccess_NeverExceedsLimit()
    {
        const int limit = 10;
        var limiter = CreateLimiter(maxPerMinute: limit);
        var successes = 0;

        Parallel.For(0, 50, _ =>
        {
            if (limiter.TryAcquire())
                Interlocked.Increment(ref successes);
        });

        successes.Should().Be(limit, "exactly {0} permits should be granted", limit);
    }

    private static SlidingWindowRateLimiter CreateLimiter(
        int maxPerMinute, TimeProvider? timeProvider = null)
    {
        var options = Options.Create(new RateLimitingSettings
        {
            MaxMessagesPerMinute = maxPerMinute
        });

        return new SlidingWindowRateLimiter(options, timeProvider);
    }
}

/// <summary>
/// Minimal fake <see cref="TimeProvider"/> for deterministic time-based testing.
/// </summary>
internal sealed class FakeTimeProvider(DateTimeOffset startTime) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => startTime;

    public void Advance(TimeSpan duration) => startTime = startTime.Add(duration);
}
