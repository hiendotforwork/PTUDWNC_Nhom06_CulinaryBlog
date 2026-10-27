namespace CulinaryBlog.API.Services;

using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CulinaryBlog.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

public class EmailConfirmationResendLimiter(IConnectionMultiplexer redis, ILogger<EmailConfirmationResendLimiter> logger) : IEmailConfirmationResendLimiter
{
    public async Task<bool> TryAcquireAsync(string normalizedEmail, CancellationToken ct = default)
    {
        try
        {
            if (!redis.IsConnected)
            {
                logger.LogWarning("Redis is not connected; email confirmation resend cooldown failing closed.");
                return false;
            }

            var db = redis.GetDatabase();
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedEmail));
            var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();
            var key = $"email_confirm_resend:{hash}";

            return await db.StringSetAsync(key, "1", TimeSpan.FromSeconds(60), When.NotExists);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check or set email confirmation resend cooldown in Redis; failing closed.");
            return false;
        }
    }
}

public class InMemoryEmailConfirmationResendLimiter : IEmailConfirmationResendLimiter
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, DateTimeOffset> _cooldowns = new();

    public Task<bool> TryAcquireAsync(string normalizedEmail, CancellationToken ct = default)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedEmail));
        var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        var now = DateTimeOffset.UtcNow;

        var acquired = false;
        _cooldowns.AddOrUpdate(hash,
            _ => { acquired = true; return now.AddSeconds(60); },
            (_, existingExpiry) =>
            {
                if (existingExpiry <= now)
                {
                    acquired = true;
                    return now.AddSeconds(60);
                }
                acquired = false;
                return existingExpiry;
            });

        return Task.FromResult(acquired);
    }
}
