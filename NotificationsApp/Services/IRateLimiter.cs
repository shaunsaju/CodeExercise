namespace NotificationsApp.Services;

/// <summary>
/// Abstraction for rate limiting. Implementations decide whether an action is permitted.
/// </summary>
public interface IRateLimiter
{
    /// <summary>
    /// Attempts to acquire a permit. Returns <c>true</c> if the action is allowed,
    /// <c>false</c> if the rate limit has been exceeded.
    /// </summary>
    bool TryAcquire();
}
