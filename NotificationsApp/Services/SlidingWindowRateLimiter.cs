using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using NotificationsApp.Configuration;

namespace NotificationsApp.Services;

/// <summary>
/// Thread-safe sliding window rate limiter.
/// Tracks timestamps of permitted actions in a <see cref="ConcurrentQueue{T}"/>
/// and rejects new actions when the window is full.
/// </summary>
public sealed class SlidingWindowRateLimiter(IOptions<RateLimitingSettings> settings, TimeProvider? timeProvider = null) : IRateLimiter
{
    private readonly ConcurrentQueue<DateTime> _timestamps = new();
    private readonly int _maxRequests = settings.Value.MaxMessagesPerMinute;
    private readonly TimeSpan _window = TimeSpan.FromMinutes(1);
    private readonly object _lock = new();
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public bool TryAcquire()
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // Purge expired entries outside the lock for better throughput on reads
        PurgeExpired(now);

        lock (_lock)
        {
            // Re-check after acquiring the lock
            PurgeExpired(now);

            if (_timestamps.Count >= _maxRequests)
                return false;

            _timestamps.Enqueue(now);
            return true;
        }
    }

    private void PurgeExpired(DateTime now)
    {
        var cutoff = now - _window;
        while (_timestamps.TryPeek(out var oldest) && oldest < cutoff)
        {
            _timestamps.TryDequeue(out _);
        }
    }
}
