# FR-AUTH Redis Rate Limit Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Share the four FR-AUTH endpoint rate limits through Redis while preserving their current policies and falling back to per-instance memory when Redis fails.

**Architecture:** Keep the existing ASP.NET endpoint policy names and attributes. Add a Redis-backed fixed-window limiter for those four policies only, using an atomic Redis script and a local fixed-window fallback; leave all other endpoint policies unchanged.

**Tech Stack:** .NET 10, ASP.NET Core RateLimiter, StackExchange.Redis, xUnit, Moq, FluentAssertions.

**Spec:** `docs/superpowers/specs/2026-10-28-fr-auth-redis-rate-limit-design.md`

## Global Constraints

- Register: 10 requests per minute; Login: 10 requests per minute; Refresh: 30 requests per minute; Resend email confirmation: 5 requests per minute.
- In the Testing environment, each policy permits 1000 requests per minute.
- All four policies partition by client IP, using `anonymous` when no IP is available.
- A partition's one-minute fixed window starts with its first request, not at a wall-clock minute boundary.
- Redis keys include policy name and a hash of the partition value.
- On Redis failure, use per-instance local limiting; do not merge local counters into Redis after recovery.
- Propagate request cancellation; do not treat cancellation as Redis failure.
- The separate resend-email 60-second cooldown remains Redis-backed and fail-closed; during Redis outage an HTTP 202 may not enqueue an email.
- No endpoint outside FR-AUTH gains, loses, or changes rate limiting.

## Review Focus

- Concurrent requests racing at the permit limit: atomic script admits at most the configured number. Test in Task 1.
- Redis unavailable or command timeout: local fallback applies; ambiguous command outcomes may desynchronize counters. Test a definite Redis exception in Task 1 and document timeout ambiguity.
- Caller cancellation before or during Redis access: cancellation propagates and does not invoke local fallback; a command already executed may still consume a Redis permit. Test in Task 1.
- Same IP calling two policies, or different IPs calling one policy: counters stay isolated. Test in Task 1.
- Resend endpoint with Redis unavailable: endpoint policy can fall back locally, but email cooldown still fails closed and may not enqueue. Verify in Task 2.
- Non-auth endpoint behavior: this change must not attach an FR-AUTH limiter or cause new 429 responses. Verify in Task 2.

---

### Task 1: Implement Redis and local fixed-window limiter

**Files:**
- Create: `src/CulinaryBlog.API/Services/RedisFixedWindowRateLimiter.cs`
- Test: `tests/CulinaryBlog.Integration.Tests/RedisFixedWindowRateLimiterTests.cs`

**Interfaces:**
- Consumes: `IConnectionMultiplexer`, policy name, partition key, permit limit, window, and request cancellation token.
- Produces: `RedisFixedWindowRateLimiter : RateLimiter`; constructor takes `(IConnectionMultiplexer redis, string policyName, string partitionKey, int permitLimit, TimeSpan window, ILogger logger)`. Implement both `AttemptAcquireCore(int)` and `AcquireAsyncCore(int, CancellationToken)`, plus the remaining `RateLimiter` abstract members. Both acquire paths use the same Redis counter and return leases with retry metadata expected by ASP.NET Core.

- [ ] **Step 1: Write tests for fixed-window permits and window expiry.** Assert that limit `3` grants a request for two permits then one permit, rejects another permit, and grants again after the window expires; use a short injected window in the test.
- [ ] **Step 2: Run the targeted test and confirm the limiter behavior is not implemented yet.**

Run: `dotnet test tests/CulinaryBlog.Integration.Tests/CulinaryBlog.Integration.Tests.csproj --filter FullyQualifiedName~RedisFixedWindowRateLimiterTests`
Expected: FAIL because `RedisFixedWindowRateLimiter` does not exist.

- [ ] **Step 3: Implement `RedisFixedWindowRateLimiter` in `src/CulinaryBlog.API/Services/RedisFixedWindowRateLimiter.cs`.** Use one Lua script to atomically admit only when `currentCount + permitCount <= permitLimit`, increment by `permitCount`, and set expiry on the first admitted request. On rejection, return the remaining key TTL in the same script result and expose it as `MetadataName.RetryAfter` on the failed lease. Requests larger than the permit limit return a failed lease without calling Redis. Use the synchronous Redis script call in `AttemptAcquireCore` and the asynchronous script call in `AcquireAsyncCore`; both use the same key and script semantics. Use a key containing policy name plus SHA-256 of the partition key. Use the same permit-count rule, limit, and window for the in-memory fallback; rejected local leases report the remaining duration of that partition's window as `RetryAfter`. Protect local fallback window and count checks/updates with one lock per limiter instance; do not hold that lock while calling Redis. The sync path may block while Redis responds; ASP.NET middleware uses the async path.
- [ ] **Step 4: Add boundary, retry-metadata, failure/recovery, cancellation, concurrency, and partition-isolation tests.** Assert a request larger than the permit limit is rejected without a Redis call; rejected Redis-backed and local leases expose a positive `MetadataName.RetryAfter` no greater than the remaining window; Redis exceptions use local limiting; a pre-canceled token causes no Redis call; cancellation while an async Redis command is pending propagates without local acquisition; concurrent requests never exceed the permit limit when Redis succeeds; when Redis always throws, concurrent acquires on one limiter instance grant no more than the local permit limit, including mixed `permitCount` values; different policy names and partition hashes have independent keys. Test recovery using one limiter with a controllable Redis mock: make Redis fail, verify the local fallback admits according to its own counter, restore Redis, verify the next request uses Redis, and verify two limiter instances again share the Redis quota without transferring local counts. StackExchange.Redis calls do not accept the request token, so check it immediately before the call and after its completion. If cancellation is observed after Redis executed the script, its permit can remain consumed; do not re-acquire locally. Use mocked `IConnectionMultiplexer`/`IDatabase` for failure, recovery, and cancellation. For atomic sharing between independent limiter instances, connect to the real Redis endpoint from `REDIS_TEST_CONNECTION`, using a unique policy prefix per test and deleting only those test keys afterward. Do not assert an HTTP `Retry-After` header unless middleware configuration explicitly writes lease metadata to that header.
- [ ] **Step 5: Run the targeted test suite and confirm it passes.**

Run: `docker run --rm -d --name culinary-redis-test -p 127.0.0.1:16379:6379 redis:7-alpine`
Expected: the temporary Redis test container starts and publishes Redis on `localhost:16379`.

Run: `REDIS_TEST_CONNECTION=localhost:16379 dotnet test tests/CulinaryBlog.Integration.Tests/CulinaryBlog.Integration.Tests.csproj --filter FullyQualifiedName~RedisFixedWindowRateLimiterTests`
Expected: PASS; Redis-backed sharing tests connect using `REDIS_TEST_CONNECTION`.

- [ ] **Step 6: Commit the limiter and its tests.**

```bash
git add src/CulinaryBlog.API/Services/RedisFixedWindowRateLimiter.cs tests/CulinaryBlog.Integration.Tests/RedisFixedWindowRateLimiterTests.cs
git commit -m "feat: add redis fixed-window rate limiter"
```

### Task 2: Wire only the FR-AUTH policies to Redis

**Files:**
- Modify: `src/CulinaryBlog.API/Program.cs:190-212`
- Create: `src/CulinaryBlog.API/Services/AuthRateLimiterOptions.cs`
- Test: `tests/CulinaryBlog.Integration.Tests/RedisRateLimitPolicyTests.cs`
- Reference: `src/CulinaryBlog.API/Services/RuntimeRegistration.cs:55-62`

**Interfaces:**
- Consumes: `RedisFixedWindowRateLimiter` from Task 1 and the existing singleton `IConnectionMultiplexer` registration.
- Produces: `AuthRateLimiterOptions : IConfigureOptions<RateLimiterOptions>` injects `IConnectionMultiplexer` and `IHostEnvironment`, then registers existing policy names `RegisterRateLimit`, `LoginRateLimit`, `RefreshRateLimit`, and `ResendEmailRateLimit`. The policies keep their endpoint attributes and partition by `RemoteIpAddress?.ToString() ?? "anonymous"`.

- [ ] **Step 1: Add policy-registration tests.** Instantiate `AuthRateLimiterOptions` with a test multiplexer and Testing/non-Testing environments. Assert policy setup uses limits `10`, `10`, `30`, and `5` per minute outside Testing; `1000` per policy in Testing; and each policy uses client IP or `anonymous` as its partition input.
- [ ] **Step 2: Run the targeted policy test and confirm it fails against the current in-memory-only registration.**

Run: `dotnet test tests/CulinaryBlog.Integration.Tests/CulinaryBlog.Integration.Tests.csproj --filter FullyQualifiedName~RedisRateLimitPolicyTests`
Expected: FAIL because policies still create only built-in process-local fixed-window limiters.

- [ ] **Step 3: Register `AuthRateLimiterOptions` and remove only the four inline policy factories from `Program.cs`.** Keep `RejectionStatusCode = 429`, policy names, thresholds, one-minute windows, endpoint attributes, and Testing overrides. Leave other policies and `UseRateLimiter()` unchanged.
- [ ] **Step 4: Add middleware, Redis multi-instance, and non-auth regression coverage.** Keep the production/Testing quota mapping assertions in Step 1. Separately, build a focused ASP.NET test pipeline using `RedisFixedWindowRateLimiter` with a test permit limit of `1`; assert the first request is admitted and the second returns HTTP `429`. Do not add test-only quota configuration to production. Configure each test host's `Redis:Connection` from `REDIS_TEST_CONNECTION`. Two independent API limiter instances sharing that Redis endpoint and policy/IP key must share their quota. Verify a non-auth endpoint does not acquire an FR-AUTH policy or start returning 429 due to this change. Verify resend still returns `202` when endpoint-level fallback admits it, while its existing email cooldown does not enqueue work if Redis is unavailable.
- [ ] **Step 5: Run targeted policy and integration tests.**

Run: `REDIS_TEST_CONNECTION=localhost:16379 dotnet test tests/CulinaryBlog.Integration.Tests/CulinaryBlog.Integration.Tests.csproj --filter FullyQualifiedName~RedisRateLimitPolicyTests`
Expected: PASS; Redis-backed shared-counter cases connect to the temporary Redis test container.

- [ ] **Step 6: Commit the FR-AUTH policy wiring and tests.**

```bash
git add src/CulinaryBlog.API/Program.cs src/CulinaryBlog.API/Services/AuthRateLimiterOptions.cs tests/CulinaryBlog.Integration.Tests/RedisRateLimitPolicyTests.cs
git commit -m "feat: distribute FR-AUTH rate limits with redis"
```

### Task 3: Run final regression checks

**Files:**
- No additional source files.

- [ ] **Step 1: Run the complete API integration test project.**

Run: `REDIS_TEST_CONNECTION=localhost:16379 dotnet test tests/CulinaryBlog.Integration.Tests/CulinaryBlog.Integration.Tests.csproj`
Expected: PASS.

- [ ] **Step 2: Stop the temporary Redis test container.**

Run: `docker stop culinary-redis-test`
Expected: the temporary container stops and is removed (`--rm`).

- [ ] **Step 3: Review the final diff for scope.** Confirm only the four FR-AUTH policies use the custom limiter, no dependency was added, non-auth endpoint behavior is unchanged, and the separate resend cooldown remains unchanged.
