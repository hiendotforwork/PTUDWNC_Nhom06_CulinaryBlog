namespace CulinaryBlog.API.Services;

using System.Security.Cryptography;
using System.Text;
using CulinaryBlog.Application.Interfaces;
using StackExchange.Redis;

public sealed class PasswordResetRequestLimiter(IConnectionMultiplexer redis, ILogger<PasswordResetRequestLimiter> logger)
    : IPasswordResetRequestLimiter
{
    public async Task<bool> TryAcquireAsync(string normalizedEmail, CancellationToken ct = default)
    {
        try
        {
            if (!redis.IsConnected) return false;
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedEmail))).ToLowerInvariant();
            return await redis.GetDatabase().StringSetAsync($"password_reset_request:{hash}", "1", TimeSpan.FromSeconds(60), When.NotExists);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check or set password reset cooldown in Redis; failing closed.");
            return false;
        }
    }
}

public sealed class InMemoryPasswordResetRequestLimiter : IPasswordResetRequestLimiter
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, DateTimeOffset> Cooldowns = new();

    public Task<bool> TryAcquireAsync(string normalizedEmail, CancellationToken ct = default)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedEmail))).ToLowerInvariant();
        var now = DateTimeOffset.UtcNow;
        var acquired = false;
        Cooldowns.AddOrUpdate(hash,
            _ => { acquired = true; return now.AddSeconds(60); },
            (_, expiry) =>
            {
                if (expiry > now) return expiry;
                acquired = true;
                return now.AddSeconds(60);
            });
        return Task.FromResult(acquired);
    }
}
