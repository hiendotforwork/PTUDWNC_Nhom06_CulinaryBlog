using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using CulinaryBlog.API.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Xunit;

namespace CulinaryBlog.Integration.Tests;

public sealed class RedisRateLimitPolicyTests
{
    public static TheoryData<string, int> ProductionPolicies => new()
    {
        { "RegisterRateLimit", 10 },
        { "LoginRateLimit", 10 },
        { "RefreshRateLimit", 30 },
        { "ResendEmailRateLimit", 5 }
    };

    public static TheoryData<string, int> TestingPolicies => new()
    {
        { "RegisterRateLimit", 1000 },
        { "LoginRateLimit", 1000 },
        { "RefreshRateLimit", 1000 },
        { "ResendEmailRateLimit", 1000 }
    };

    [Theory]
    [MemberData(nameof(ProductionPolicies))]
    public Task ProductionPolicies_UseConfiguredLimit(string policy, int permitLimit) =>
        AssertPolicyLimit("Production", policy, permitLimit);

    [Theory]
    [MemberData(nameof(TestingPolicies))]
    public Task TestingPolicies_UseHighTestLimit(string policy, int permitLimit) =>
        AssertPolicyLimit("Testing", policy, permitLimit);

    [Fact]
    public async Task AuthPolicyPipeline_Returns429WhenLimitIsExceeded()
    {
        var redis = await ConnectRedis();
        var policy = "middleware-" + Guid.NewGuid().ToString("N");
        var partition = CreatePartition();
        var key = BuildKey(policy, partition);
        try
        {
            using var firstServer = CreatePipeline("Production", redis, partition, options =>
            {
                options.AddPolicy(policy, context => RateLimitPartition.Get(
                    context.Connection.RemoteIpAddress!.ToString()!,
                    keyValue => new RedisFixedWindowRateLimiter(
                        redis, policy, keyValue, 1, TimeSpan.FromMinutes(1),
                        NullLogger<RedisFixedWindowRateLimiter>.Instance)));
            }, policy);
            using var secondServer = CreatePipeline("Production", redis, partition, options =>
            {
                options.AddPolicy(policy, context => RateLimitPartition.Get(
                    context.Connection.RemoteIpAddress!.ToString()!,
                    keyValue => new RedisFixedWindowRateLimiter(
                        redis, policy, keyValue, 1, TimeSpan.FromMinutes(1),
                        NullLogger<RedisFixedWindowRateLimiter>.Instance)));
            }, policy);
            using var firstClient = firstServer.CreateClient();
            using var secondClient = secondServer.CreateClient();

            Assert.Equal(HttpStatusCode.OK, (await firstClient.GetAsync("/auth")).StatusCode);
            Assert.Equal(HttpStatusCode.TooManyRequests, (await secondClient.GetAsync("/auth")).StatusCode);
        }
        finally
        {
            await redis.GetDatabase().KeyDeleteAsync(key);
            await redis.DisposeAsync();
        }
    }

    [Fact]
    public async Task NonAuthEndpoint_DoesNotAcquireAnAuthRateLimit()
    {
        var redis = await ConnectRedis();
        var partition = CreatePartition();
        using var server = CreatePipeline("Production", redis, partition, _ => { }, null);
        using var client = server.CreateClient();

        for (var i = 0; i < 20; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/other")).StatusCode);

        await redis.DisposeAsync();
    }

    private static async Task AssertPolicyLimit(string environment, string policy, int permitLimit)
    {
        var redis = await ConnectRedis();
        var partition = CreatePartition();
        var key = BuildKey(policy, partition);
        try
        {
            using var server = CreatePipeline(environment, redis, partition, _ => { }, policy);
            using var client = server.CreateClient();

            for (var i = 0; i < permitLimit; i++)
                Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/auth")).StatusCode);
            Assert.Equal(HttpStatusCode.TooManyRequests, (await client.GetAsync("/auth")).StatusCode);
        }
        finally
        {
            await redis.GetDatabase().KeyDeleteAsync(key);
            await redis.DisposeAsync();
        }
    }

    private static TestServer CreatePipeline(
        string environment,
        IConnectionMultiplexer redis,
        string partition,
        Action<RateLimiterOptions> extraPolicies,
        string? selectedPolicy)
    {
        return new TestServer(new WebHostBuilder()
            .UseEnvironment(environment)
            .ConfigureServices(services =>
            {
                services.AddSingleton(redis);
                services.AddSingleton<IConfigureOptions<RateLimiterOptions>, AuthRateLimiterOptions>();
                services.AddRouting();
                services.AddRateLimiter(options =>
                {
                    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                    extraPolicies(options);
                });
            })
            .Configure(app =>
            {
                app.Use(async (context, next) =>
                {
                    context.Connection.RemoteIpAddress = IPAddress.Parse(partition);
                    await next();
                });
                app.UseRouting();
                app.UseRateLimiter();
                app.UseEndpoints(endpoints =>
                {
                    var auth = endpoints.MapGet("/auth", () => Results.Ok());
                    if (selectedPolicy is not null) auth.RequireRateLimiting(selectedPolicy);
                    endpoints.MapGet("/other", () => Results.Ok());
                });
            }));
    }

    private static async Task<IConnectionMultiplexer> ConnectRedis() =>
        await ConnectionMultiplexer.ConnectAsync(Environment.GetEnvironmentVariable("REDIS_TEST_CONNECTION") ?? "localhost:16379");

    private static string CreatePartition() => $"198.51.100.{Random.Shared.Next(1, 255)}";

    private static string BuildKey(string policy, string partition)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(partition))).ToLowerInvariant();
        return $"auth_rate_limit:{policy}:{hash}";
    }
}
