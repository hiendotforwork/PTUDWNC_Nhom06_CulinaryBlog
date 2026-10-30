using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CulinaryBlog.API.Services;

public sealed class AuthRateLimiterOptions(
    IConnectionMultiplexer redis,
    IHostEnvironment environment,
    ILoggerFactory loggerFactory) : IConfigureOptions<RateLimiterOptions>
{
    public void Configure(RateLimiterOptions options)
    {
        var isTesting = environment.IsEnvironment("Testing");
        AddPolicy(options, "RegisterRateLimit", isTesting ? 1000 : 10, redis, loggerFactory);
        AddPolicy(options, "LoginRateLimit", isTesting ? 1000 : 10, redis, loggerFactory);
        AddPolicy(options, "RefreshRateLimit", isTesting ? 1000 : 30, redis, loggerFactory);
        AddPolicy(options, "ResendEmailRateLimit", isTesting ? 1000 : 5, redis, loggerFactory);
    }

    private static void AddPolicy(
        RateLimiterOptions options,
        string policyName,
        int permitLimit,
        IConnectionMultiplexer redis,
        ILoggerFactory loggerFactory) =>
        options.AddPolicy(policyName, context =>
        {
            var partition = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
            return RateLimitPartition.Get(partition, key => new RedisFixedWindowRateLimiter(
                redis, policyName, key, permitLimit, TimeSpan.FromMinutes(1),
                loggerFactory.CreateLogger<RedisFixedWindowRateLimiter>()));
        });
}
