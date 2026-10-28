using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using CulinaryBlog.API.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace CulinaryBlog.Integration.Tests;

public sealed class RedisFixedWindowRateLimiterTests
{
    [Fact]
    public async Task AcquireAsync_UsesPermitCountAndReplenishesAfterWindow()
    {
        var connection = Environment.GetEnvironmentVariable("REDIS_TEST_CONNECTION") ?? "localhost:16379";
        await using var redis = await ConnectionMultiplexer.ConnectAsync(connection);
        var policy = "test-" + Guid.NewGuid().ToString("N");
        const string partition = "127.0.0.1";
        var key = BuildKey(policy, partition);
        try
        {
            using var limiter = CreateLimiter(redis, policy, partition, 3, TimeSpan.FromMilliseconds(200));
            using var first = await limiter.AcquireAsync(2);
            using var second = await limiter.AcquireAsync(1);
            using var rejected = await limiter.AcquireAsync(1);

            Assert.True(first.IsAcquired);
            Assert.True(second.IsAcquired);
            Assert.False(rejected.IsAcquired);
            Assert.True(rejected.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter));
            Assert.InRange(retryAfter, TimeSpan.Zero, TimeSpan.FromMilliseconds(200));

            await Task.Delay(250);
            using var afterWindow = await limiter.AcquireAsync(1);
            Assert.True(afterWindow.IsAcquired);
        }
        finally
        {
            await redis.GetDatabase().KeyDeleteAsync(key);
        }
    }

    [Fact]
    public async Task AttemptAcquire_ZeroPermitsReportsAvailabilityWithoutConsumingQuota()
    {
        var connection = Environment.GetEnvironmentVariable("REDIS_TEST_CONNECTION") ?? "localhost:16379";
        await using var redis = await ConnectionMultiplexer.ConnectAsync(connection);
        var policy = "zero-" + Guid.NewGuid().ToString("N");
        const string partition = "127.0.0.2";
        var key = BuildKey(policy, partition);
        try
        {
            using var limiter = CreateLimiter(redis, policy, partition, 2, TimeSpan.FromMinutes(1));
            using var availability = limiter.AttemptAcquire(0);
            using var fullQuota = await limiter.AcquireAsync(2);
            using var exhausted = limiter.AttemptAcquire(0);

            Assert.True(availability.IsAcquired);
            Assert.True(fullQuota.IsAcquired);
            Assert.False(exhausted.IsAcquired);
        }
        finally
        {
            await redis.GetDatabase().KeyDeleteAsync(key);
        }
    }

    [Fact]
    public async Task IdleDuration_TracksActivityForRedisBackedPartitions()
    {
        var connection = Environment.GetEnvironmentVariable("REDIS_TEST_CONNECTION") ?? "localhost:16379";
        await using var redis = await ConnectionMultiplexer.ConnectAsync(connection);
        var policy = "idle-" + Guid.NewGuid().ToString("N");
        const string partition = "127.0.0.3";
        var key = BuildKey(policy, partition);
        try
        {
            using var limiter = CreateLimiter(redis, policy, partition, 1, TimeSpan.FromMinutes(1));
            Assert.Null(limiter.IdleDuration);

            using var lease = await limiter.AcquireAsync(1);

            Assert.True(lease.IsAcquired);
            Assert.NotNull(limiter.IdleDuration);
            Assert.InRange(limiter.IdleDuration!.Value, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }
        finally
        {
            await redis.GetDatabase().KeyDeleteAsync(key);
        }
    }

    [Fact]
    public async Task AcquireAsync_RejectsPermitCountLargerThanLimitWithoutRedisCall()
    {
        var database = new Mock<IDatabase>(MockBehavior.Strict);
        var redis = CreateMultiplexer(database.Object);
        using var limiter = CreateLimiter(redis.Object, "oversized", "partition", 2, TimeSpan.FromMinutes(1));

        using var lease = await limiter.AcquireAsync(3);

        Assert.False(lease.IsAcquired);
        database.Verify(db => db.ScriptEvaluateAsync(
            It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task AcquireAsync_UsesAtomicLocalFallbackWhenRedisFailsConcurrently()
    {
        var database = FailingDatabase();
        var redis = CreateMultiplexer(database.Object);
        using var limiter = CreateLimiter(redis.Object, "fallback-race", "partition", 3, TimeSpan.FromMinutes(1));

        var leases = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => limiter.AcquireAsync(1).AsTask()));
        try
        {
            Assert.Equal(3, leases.Count(lease => lease.IsAcquired));
        }
        finally
        {
            foreach (var lease in leases) lease.Dispose();
        }
    }

    [Fact]
    public async Task AcquireAsync_LocalFallbackDoesNotOvergrantMixedPermitCounts()
    {
        var redis = CreateMultiplexer(FailingDatabase().Object);
        using var limiter = CreateLimiter(redis.Object, "fallback-mixed", "partition", 3, TimeSpan.FromMinutes(1));
        var requests = new[] { 1, 2, 1, 2, 1, 2 };

        var leases = await Task.WhenAll(requests.Select(count => limiter.AcquireAsync(count).AsTask()));
        try
        {
            Assert.InRange(leases.Select((lease, index) => lease.IsAcquired ? requests[index] : 0).Sum(), 1, 3);
        }
        finally
        {
            foreach (var lease in leases) lease.Dispose();
        }
    }

    [Fact]
    public async Task AcquireAsync_PropagatesCancellationWithoutUsingFallback()
    {
        var database = new Mock<IDatabase>(MockBehavior.Strict);
        var redis = CreateMultiplexer(database.Object);
        using var limiter = CreateLimiter(redis.Object, "cancel", "partition", 2, TimeSpan.FromMinutes(1));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await limiter.AcquireAsync(1, cancellation.Token));
        database.Verify(db => db.ScriptEvaluateAsync(
            It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task AcquireAsync_PropagatesCancellationAfterPendingRedisCommandWithoutUsingFallback()
    {
        var pendingResult = new TaskCompletionSource<RedisResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var commandStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var database = new Mock<IDatabase>();
        database.SetupSequence(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .Returns(() =>
            {
                commandStarted.SetResult();
                return pendingResult.Task;
            })
            .ReturnsAsync(ScriptResult(acquired: true, retryMilliseconds: 0));
        var redis = CreateMultiplexer(database.Object);
        using var limiter = CreateLimiter(redis.Object, "cancel-pending", "partition", 1, TimeSpan.FromMinutes(1));
        using var cancellation = new CancellationTokenSource();

        var acquire = limiter.AcquireAsync(1, cancellation.Token).AsTask();
        await commandStarted.Task;
        cancellation.Cancel();
        pendingResult.SetResult(ScriptResult(acquired: true, retryMilliseconds: 0));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => acquire);
        using var afterCancellation = await limiter.AcquireAsync(1);
        Assert.True(afterCancellation.IsAcquired);
    }

    [Fact]
    public async Task AcquireAsync_ResumesRedisAfterFailureWithoutMergingFallbackCount()
    {
        var database = new Mock<IDatabase>();
        database.Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(RedisFailure());
        database.SetupSequence(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(RedisFailure())
            .ReturnsAsync(ScriptResult(acquired: true, retryMilliseconds: 0))
            .ReturnsAsync(ScriptResult(acquired: false, retryMilliseconds: 30_000));
        var redis = CreateMultiplexer(database.Object);
        using var first = CreateLimiter(redis.Object, "recover", "partition", 1, TimeSpan.FromMinutes(1));
        using var second = CreateLimiter(redis.Object, "recover", "partition", 1, TimeSpan.FromMinutes(1));

        using var duringFailure = await first.AcquireAsync(1);
        using var afterRecovery = await first.AcquireAsync(1);
        using var sharedQuota = await second.AcquireAsync(1);

        Assert.True(duringFailure.IsAcquired);
        Assert.True(afterRecovery.IsAcquired);
        Assert.False(sharedQuota.IsAcquired);
    }

    [Fact]
    public async Task AcquireAsync_UsesIndependentRedisCountersForPoliciesAndPartitions()
    {
        var connection = Environment.GetEnvironmentVariable("REDIS_TEST_CONNECTION") ?? "localhost:16379";
        await using var redis = await ConnectionMultiplexer.ConnectAsync(connection);
        var policy = "isolation-" + Guid.NewGuid().ToString("N");
        var partitions = new[] { "10.0.0.1", "10.0.0.2" };
        var policies = new[] { policy, policy + "-other" };
        var keys = policies.SelectMany(name => partitions.Select(partition => BuildKey(name, partition))).ToArray();
        try
        {
            using var first = CreateLimiter(redis, policies[0], partitions[0], 1, TimeSpan.FromMinutes(1));
            using var otherPartition = CreateLimiter(redis, policies[0], partitions[1], 1, TimeSpan.FromMinutes(1));
            using var otherPolicy = CreateLimiter(redis, policies[1], partitions[0], 1, TimeSpan.FromMinutes(1));
            using var firstLease = await first.AcquireAsync(1);
            using var partitionLease = await otherPartition.AcquireAsync(1);
            using var policyLease = await otherPolicy.AcquireAsync(1);

            Assert.True(firstLease.IsAcquired);
            Assert.True(partitionLease.IsAcquired);
            Assert.True(policyLease.IsAcquired);
        }
        finally
        {
            await redis.GetDatabase().KeyDeleteAsync(keys.Select(key => (RedisKey)key).ToArray());
        }
    }

    private static RedisFixedWindowRateLimiter CreateLimiter(
        IConnectionMultiplexer redis, string policy, string partition, int limit, TimeSpan window) =>
        new(redis, policy, partition, limit, window, NullLogger<RedisFixedWindowRateLimiter>.Instance);

    private static Mock<IConnectionMultiplexer> CreateMultiplexer(IDatabase database)
    {
        var multiplexer = new Mock<IConnectionMultiplexer>();
        multiplexer.SetupGet(redis => redis.IsConnected).Returns(true);
        multiplexer.Setup(redis => redis.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(database);
        return multiplexer;
    }

    private static Mock<IDatabase> FailingDatabase()
    {
        var database = new Mock<IDatabase>();
        database.Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(RedisFailure());
        database.Setup(db => db.ScriptEvaluate(
                It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .Throws(RedisFailure());
        return database;
    }

    private static RedisConnectionException RedisFailure() =>
        new(ConnectionFailureType.UnableToConnect, CommandFlags.None, "test Redis unavailable", null, CommandStatus.Unknown);

    private static RedisResult ScriptResult(bool acquired, long retryMilliseconds) =>
        RedisResult.Create(new[] { RedisResult.Create((RedisValue)(acquired ? 1 : 0)), RedisResult.Create((RedisValue)retryMilliseconds) });

    private static string BuildKey(string policy, string partition)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(partition))).ToLowerInvariant();
        return $"auth_rate_limit:{policy}:{hash}";
    }
}
