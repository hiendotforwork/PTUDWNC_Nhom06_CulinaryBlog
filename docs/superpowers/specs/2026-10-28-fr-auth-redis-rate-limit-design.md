# FR-AUTH distributed rate limiting design

## Intent

Share rate-limit counters through Redis for FR-AUTH so multiple API instances apply the existing endpoint policies together. If Redis is unavailable, fall back to per-instance in-memory limiting so auth requests remain available.

## Goals

- Convert only the FR-AUTH endpoint policies: register, login, refresh, and resend email confirmation.
- Preserve current policy names, thresholds, windows, partition keys, and HTTP 429 behavior:
  - Register: 10 requests per minute.
  - Login: 10 requests per minute.
  - Refresh: 30 requests per minute.
  - Resend email confirmation: 5 requests per minute.
  - In the Testing environment, each policy permits 1000 requests per minute.
  - All four policies partition by client IP, using `anonymous` when no IP is available.
- Keep Redis operations atomic so concurrent requests cannot exceed a shared limit.
- Fall back to per-instance local limiting on Redis errors and resume Redis-backed limiting when Redis recovers.
- Keep rate-limit behavior for every non-auth endpoint unchanged.

## Non-goals

- Change rate limiting for other API modules.
- Change the separate 60-second resend-email cooldown or authentication logic.
- Add Redis caching or move refresh-token storage out of the database.

## Approaches

1. **Redis counters only for FR-AUTH (chosen):** retain the existing endpoint policies and scope the distributed counter implementation to them. This meets the multi-instance goal without changing other modules.
2. **Redis rate limiting for the whole API:** gives shared limits everywhere, but expands scope beyond FR-AUTH.
3. **Fail closed when Redis is unavailable:** enforces the limit strictly but can block auth requests during Redis outages. The chosen behavior is local fallback for availability; limits then apply per instance until Redis recovers.

## Design

Reuse the existing StackExchange.Redis connection. Keep the existing endpoint attributes and policy names, and provide a custom limiter only for the four FR-AUTH policies. Each Redis key must include the policy name and its partition key so different policies never share a counter. Hash the partition value in the key to avoid storing raw IP addresses in Redis keys.

Preserve the existing fixed-window behavior: a partition's one-minute window starts with its first request, rather than at a wall-clock minute boundary. Use one atomic Redis operation (for example, a Lua script) to create/increment the counter and set its one-minute expiry on the first request. The operation must allow a request only while the counter is below that policy's permit limit. Leave all other policies and endpoints on their current limiter implementation.

If Redis is unavailable or a Redis operation fails, use an equivalent in-memory limiter on that API instance for the endpoint rate-limit policy only. Propagate request cancellation; do not treat it as a Redis failure or switch to the local limiter. When Redis recovers, use Redis again; do not attempt to merge local counters into Redis. Requests handled during the outage are therefore not reflected in the shared counters. A Redis timeout with an unknown command outcome can also leave local and Redis counters out of sync. Rate limiting is best effort during the outage and around recovery, and the aggregate allowance can grow with the number of API instances.

The resend-email handler's separate 60-second cooldown continues to fail closed when Redis is unavailable. In that case it does not enqueue the email, even if the endpoint-level rate limiter used its local fallback and the API returns `202 Accepted`. The response does not guarantee that an email was queued during a Redis outage.

## Constraints and risks

- Local fallback is not globally distributed. During Redis failure, and until Redis-backed limiting resumes, the effective aggregate allowance can grow with the number of API instances. Local counts are not synchronized to Redis on recovery.
- Existing policy thresholds, windows, and partition keys must be read from current configuration and preserved exactly.
- Redis counters must be isolated by policy name and partition; do not use calendar-minute buckets because they change the current fixed-window reset behavior.
- The resend endpoint already has a distinct Redis cooldown in its handler; retain it independently of the endpoint rate-limit policy.
- Endpoint-level local fallback does not change the resend-email handler cooldown: if Redis is unavailable, the handler may skip enqueuing the email while the endpoint still returns `202 Accepted`.
- A custom limiter implementation must be scoped to FR-AUTH policy names so unrelated endpoints cannot be switched accidentally.

## Acceptance

- Requests across multiple API instances share each FR-AUTH policy's limit while Redis is available.
- Redis failure activates local limiting without causing auth requests to fail solely because Redis is down; each instance limits independently during this period.
- After Redis recovers, FR-AUTH uses Redis counters again; no synchronization of outage-period local counts is promised.
- Existing thresholds, partitioning, 429 responses, and all non-auth rate limits remain unchanged.
- Redis keys for distinct FR-AUTH policies and client IP partitions do not share counters; each partition resets one minute after its first request.
- At least one non-auth endpoint continues to use its existing rate limiter unchanged.
- During Redis outage, the resend endpoint's existing cooldown may prevent queueing an email even when the endpoint-level limiter falls back locally; `202 Accepted` does not guarantee queueing in this case.
