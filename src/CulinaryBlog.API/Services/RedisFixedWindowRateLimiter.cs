namespace CulinaryBlog.API.Services;

using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using StackExchange.Redis;

public sealed class RedisFixedWindowRateLimiter : RateLimiter
{
    private const string Script = """
        local count = tonumber(redis.call('GET', KEYS[1]) or '0')
        local requested = tonumber(ARGV[1])
        local limit = tonumber(ARGV[2])
        local window = tonumber(ARGV[3])
        if requested == 0 then
            if count < limit then return {1, 0} end
            return {0, redis.call('PTTL', KEYS[1])}
        end
        if count + requested <= limit then
            if count == 0 then
                redis.call('SET', KEYS[1], requested, 'PX', window)
            else
                redis.call('INCRBY', KEYS[1], requested)
            end
            return {1, 0}
        end
        return {0, redis.call('PTTL', KEYS[1])}
        """;

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger _logger;
    private readonly string _key;
    private readonly int _permitLimit;
    private readonly TimeSpan _window;
    private readonly object _fallbackLock = new();
    private int _fallbackCount;
    private DateTimeOffset _fallbackWindowStarted;
    private DateTimeOffset _lastActivity;

    public RedisFixedWindowRateLimiter(
        IConnectionMultiplexer redis,
        string policyName,
        string partitionKey,
        int permitLimit,
        TimeSpan window,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(redis);
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(permitLimit);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(window, TimeSpan.Zero);
        ArgumentNullException.ThrowIfNull(logger);

        _redis = redis;
        _logger = logger;
        _permitLimit = permitLimit;
        _window = window;
        var partitionHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(partitionKey))).ToLowerInvariant();
        _key = $"auth_rate_limit:{policyName}:{partitionHash}";
    }

    public override TimeSpan? IdleDuration
    {
        get
        {
            lock (_fallbackLock)
                return _lastActivity == default ? null : DateTimeOffset.UtcNow - _lastActivity;
        }
    }

    protected override RateLimitLease AttemptAcquireCore(int permitCount)
    {
        if (permitCount > _permitLimit)
            return Failed(_window);

        MarkActivity();
        try
        {
            if (!_redis.IsConnected)
                return AcquireFallback(permitCount);

            return ParseResult(_redis.GetDatabase().ScriptEvaluate(
                Script,
                [_key],
                [permitCount, _permitLimit, (long)_window.TotalMilliseconds]));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis rate limit failed for policy {Policy}; using local fallback", _key.Split(':')[1]);
            return AcquireFallback(permitCount);
        }
    }

    protected override async ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (permitCount > _permitLimit)
            return Failed(_window);

        MarkActivity();
        try
        {
            if (!_redis.IsConnected)
                return AcquireFallback(permitCount);

            var result = await _redis.GetDatabase().ScriptEvaluateAsync(
                Script,
                [_key],
                [permitCount, _permitLimit, (long)_window.TotalMilliseconds]).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return ParseResult(result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _logger.LogWarning(ex, "Redis rate limit failed for policy {Policy}; using local fallback", _key.Split(':')[1]);
            return AcquireFallback(permitCount);
        }
    }

    public override RateLimiterStatistics? GetStatistics() => null;

    protected override void Dispose(bool disposing) { }

    private RateLimitLease ParseResult(RedisResult result)
    {
        var values = (RedisResult[]?)result;
        if (values is not { Length: 2 })
            throw new InvalidOperationException("Redis returned an invalid rate-limit result.");
        var acquired = (long)values[0] == 1;
        var retryMilliseconds = (long)values[1];
        return acquired ? Lease.Acquired : Failed(TimeSpan.FromMilliseconds(Math.Max(1, retryMilliseconds)));
    }

    private void MarkActivity()
    {
        lock (_fallbackLock)
            _lastActivity = DateTimeOffset.UtcNow;
    }

    private RateLimitLease AcquireFallback(int permitCount)
    {
        lock (_fallbackLock)
        {
            var now = DateTimeOffset.UtcNow;
            if (_fallbackWindowStarted == default || now - _fallbackWindowStarted >= _window)
            {
                _fallbackWindowStarted = now;
                _fallbackCount = 0;
            }
            _lastActivity = now;

            var remaining = _window - (now - _fallbackWindowStarted);
            if (permitCount == 0)
                return _fallbackCount < _permitLimit ? Lease.Acquired : Failed(remaining);
            if (_fallbackCount + permitCount <= _permitLimit)
            {
                _fallbackCount += permitCount;
                return Lease.Acquired;
            }
            return Failed(remaining);
        }
    }

    private static RateLimitLease Failed(TimeSpan retryAfter) => new Lease(false, retryAfter);

    private sealed class Lease(bool acquired, TimeSpan? retryAfter = null) : RateLimitLease
    {
        private static readonly string[] RetryAfterMetadata = [MetadataName.RetryAfter.Name];
        public static Lease Acquired { get; } = new(true);

        public override bool IsAcquired => acquired;
        public override IEnumerable<string> MetadataNames => retryAfter.HasValue ? RetryAfterMetadata : Array.Empty<string>();

        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            if (retryAfter.HasValue && metadataName == MetadataName.RetryAfter.Name)
            {
                metadata = retryAfter.Value;
                return true;
            }

            metadata = null;
            return false;
        }
    }
}
