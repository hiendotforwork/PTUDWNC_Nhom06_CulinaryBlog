# FR-AUTH Security Specification

> **Tài liệu nguồn:** FR-AUTH_BaoCao.md v1.0.0  
> **Phạm vi:** Chi tiết bảo mật cho module Authentication  
> **Phiên bản:** 1.0.0  
> **Ngày tạo:** 04/06/2026  

---

## 1. Tổng quan Bảo mật

### 1.1. Mục tiêu

Tài liệu này định nghĩa chi tiết các yêu cầu bảo mật cho module FR-AUTH, bao gồm:
- JWT Token Security
- Password Storage & Hashing
- Refresh Token Rotation
- Rate Limiting
- CORS Configuration
- Input Validation
- Logging & Monitoring

### 1.2. Security Principles

1. **Defense in Depth** - Nhiều lớp bảo mật
2. **Least Privilege** - Chỉ cấp quyền cần thiết
3. **Secure by Default** - Cấu hình an toàn mặc định
4. **Fail Securely** - Xử lý lỗi an toàn
5. **No Information Leakage** - Không tiết lộ thông tin nhạy cảm

---

## 2. JWT Token Security

### 2.1. JWT Access Token Specification

#### Token Structure

```json
{
  "header": {
    "alg": "HS256",
    "typ": "JWT",
    "kid": "2026-06-04"
  },
  "payload": {
    "sub": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "user@example.com",
    "displayName": "Nguyễn Văn A",
    "roles": ["Author"],
    "jti": "unique-token-id-12345",
    "iat": 1717500000,
    "iss": "https://culinaryblog.com",
    "aud": "culinaryblog-api",
    "exp": 1717500900
  }
}
```

#### JWT Configuration

| Parameter | Value | Description |
|-----------|-------|-------------|
| Algorithm | HS256 | HMAC using SHA-256 |
| Token Type | JWT | JSON Web Token |
| Issuer | culinaryblog.com | Token issuer |
| Audience | culinaryblog-api | API audience |
| TTL | 15 minutes | Access token lifetime |
| Clock Skew | 60 seconds | Allowed time difference |

#### JWT Generation Code

```csharp
public class JwtService : IJwtService
{
    private readonly JwtSettings _settings;
    private readonly IDateTimeProvider _dateTimeProvider;

    public string GenerateAccessToken(ApplicationUser user, IList<string> roles)
    {
        var tokenId = Guid.NewGuid().ToString();
        var now = _dateTimeProvider.UtcNow;
        var expires = now.AddMinutes(_settings.AccessTokenTTL);

        var claims = new List<Claim>
        {
            // Required claims
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti, tokenId),
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            
            // Custom claims
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new("display_name", user.DisplayName),
            
            // Role claims
            new(ClaimTypes.Role, "Author"),
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_settings.SecretKey));
        var credentials = new SigningCredentials(
            key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

### 2.2. Refresh Token Specification

#### Token Properties

| Property | Value | Description |
|-----------|-------|-------------|
| Length | 512 bits (64 bytes) | Cryptographically secure random |
| Format | Base64URL encoded | URL-safe encoding |
| TTL | 7 days | Refresh token lifetime |
| Storage | SHA-256 hash in DB | Never store raw token |
| Rotation | Required | Old token invalidated on use |

#### Refresh Token Generation

```csharp
public class RefreshToken
{
    public static RefreshToken Create(string userId, string ipAddress, int ttlDays = 7)
    {
        var id = Guid.NewGuid();
        var rawToken = GenerateSecureToken(64); // 512 bits
        var tokenHash = HashToken(rawToken);
        
        return new RefreshToken
        {
            Id = id,
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(ttlDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };
    }

    private static string GenerateSecureToken(int byteLength)
    {
        var bytes = new byte[byteLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Base64UrlEncoder.Encode(bytes);
    }

    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
```

### 2.3. Token Rotation Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    REFRESH TOKEN ROTATION                        │
└─────────────────────────────────────────────────────────────────┘

1. Client gửi refresh token cũ
   ┌──────────────────────────────────────┐
   │ POST /api/v1/auth/refresh             │
   │ { "refreshToken": "old-token-abc123" } │
   └──────────────────────────────────────┘

2. Backend verify token:
   ├── Token tồn tại trong DB? ── NO ──→ 401 Unauthorized
   ├── Token chưa hết hạn? ────── NO ──→ 401 Unauthorized (EXPIRED)
   ├── Token chưa bị revoke? ────── NO ──→ 401 Unauthorized (REUSE ALERT!)
   └── Token thuộc user đang active? ─ NO ──→ 401 Unauthorized

3. Nếu valid:
   ├── Tạo access token MỚI
   ├── Tạo refresh token MỚI
   ├── Lưu refresh token MỚI vào DB
   ├── Đánh dấu token CŨ: IsRevoked=true, RevokedAt=now
   └── Trả về cặp token MỚI

4. Return:
   ┌──────────────────────────────────────┐
   │ 200 OK                               │
   │ {                                   │
   │   "accessToken": "eyJhbGciOi...",   │
   │   "refreshToken": "new-token-xyz",   │
   │   "expiresAt": "2026-06-11T15:30:00Z"│
   │ }                                   │
   └──────────────────────────────────────┘
```

### 2.4. Token Reuse Detection

```csharp
public class RefreshTokenHandler
{
    public async Task<AuthResponseDto> HandleRefreshTokenAsync(
        RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Hash incoming token
        var tokenHash = HashToken(command.RefreshToken);
        
        // 2. Find token in database
        var token = await _refreshTokenRepository
            .GetByHashAsync(tokenHash, cancellationToken);
        
        if (token == null)
        {
            _logger.LogWarning(
                "Refresh token not found. Hash: {TokenHash}, IP: {IP}",
                tokenHash[..8] + "...", // Log partial hash only
                command.IpAddress);
            throw new UnauthorizedException("AUTH_TOKEN_003");
        }
        
        // 3. Check expiration
        if (token.ExpiresAt < DateTime.UtcNow)
        {
            throw new UnauthorizedException("AUTH_TOKEN_001");
        }
        
        // 4. Check revocation - THIS DETECTS REUSE ATTACK!
        if (token.RevokedAt != null)
        {
            // ⚠️ SECURITY ALERT: Token was already used!
            _logger.LogCritical(
                "REFRESH TOKEN REUSE DETECTED! " +
                "UserId: {UserId}, OriginalTokenCreated: {CreatedAt}, " +
                "RevokedAt: {RevokedAt}, ReusedAt: {Now}, IP: {IP}",
                token.UserId, token.CreatedAt, token.RevokedAt,
                DateTime.UtcNow, command.IpAddress);
            
            // Optional: Revoke all user tokens (paranoid mode)
            if (_settings.EnableParanoidMode)
            {
                await _refreshTokenRepository
                    .RevokeAllForUserAsync(token.UserId, cancellationToken);
            }
            
            throw new UnauthorizedException("AUTH_TOKEN_002");
        }
        
        // 5. Token is valid - proceed with rotation
        var user = await _userManager.FindByIdAsync(token.UserId);
        
        // 6. Create new tokens
        var newAccessToken = _jwtService.GenerateAccessToken(user, userRoles);
        var newRefreshToken = RefreshToken.Create(
            user.Id, 
            command.IpAddress);
        
        // 7. Save new refresh token
        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);
        
        // 8. Mark old token as revoked
        token.Revoke(newRefreshToken.TokenHash);
        
        // 9. Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        // 10. Return new tokens
        return new AuthResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken.Token, // Return raw token
            ExpiresAt = DateTime.UtcNow.Add(_jwtSettings.RefreshTokenTTL),
            User = _mapper.Map<UserDto>(user)
        };
    }
}
```

---

## 3. Password Security

### 3.1. Password Hashing Configuration

| Setting | Value | Description |
|-----------|-------|-------------|
| Algorithm | PBKDF2 | Password-Based Key Derivation Function 2 |
| Hash Algorithm | HMACSHA512 | SHA-512 for HMAC |
| Iterations | 100,000+ | PBKDF2 iterations (NFR-SEC-001) |
| Salt | 128 bits | Random per password |
| Hash Size | 256 bits | Output hash length |

### 3.2. ASP.NET Core Identity Configuration

```csharp
// Program.cs
builder.Services.Configure<IdentityOptions>(options =>
{
    // Password Settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 4;
    
    // Lockout Settings (Brute Force Protection)
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    
    // User Settings
    options.User.AllowedUserNameCharacters = 
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_";
    options.User.RequireUniqueEmail = true;
});

// Configure PBKDF2 with higher iterations
builder.Services.Configure<PasswordHasherOptions>(options =>
{
    options.IterationCount = 100000;
});
```

### 3.3. Password Validation Rules

| Rule | Requirement | Error Message |
|------|-------------|---------------|
| Minimum Length | 8 characters | "Password must be at least 8 characters" |
| Uppercase | At least 1 uppercase (A-Z) | "Password must contain at least 1 uppercase letter" |
| Lowercase | At least 1 lowercase (a-z) | "Password must contain at least 1 lowercase letter" |
| Digit | At least 1 number (0-9) | "Password must contain at least 1 digit" |
| Special Character | At least 1 special (!@#$%^&*) | "Password must contain at least 1 special character" |

### 3.4. Password Strength Indicator

```json
{
  "password": "MyPass123!",
  "strength": {
    "score": 4,
    "label": "Strong",
    "requirements": {
      "length": true,
      "uppercase": true,
      "lowercase": true,
      "digit": true,
      "special": true
    }
  }
}
```

| Score | Label | Criteria |
|-------|-------|----------|
| 0 | Very Weak | < 8 characters |
| 1 | Weak | 8+ chars, missing 2+ rules |
| 2 | Fair | 8+ chars, missing 1 rule |
| 3 | Good | 8+ chars, all basic rules |
| 4 | Strong | 10+ chars, all rules |
| 5 | Very Strong | 14+ chars, all rules, common check |

---

## 4. Rate Limiting

### 4.1. Rate Limit Configuration

| Endpoint | Limit | Window | Scope |
|----------|-------|--------|-------|
| POST /auth/register | 10 | 1 minute | Per IP |
| POST /auth/login | 10 | 1 minute | Per IP |
| POST /auth/google | 10 | 1 minute | Per IP |
| POST /auth/refresh | 30 | 1 minute | Per IP |
| POST /auth/logout | 10 | 1 minute | Per IP |
| GET /auth/me | 60 | 1 minute | Per IP |
| PATCH /auth/me | 10 | 1 minute | Per IP |
| GET /auth/verify-email | 5 | 1 minute | Per IP |

### 4.2. Redis Sliding Window Implementation

```csharp
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRedisConnection _redis;
    private readonly RateLimitSettings _settings;

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip rate limiting for non-auth endpoints
        if (!IsAuthEndpoint(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var clientIp = GetClientIp(context.Request);
        var endpoint = context.Request.Path.Value ?? "";
        var key = $"rate_limit:auth:{endpoint}:{clientIp}";

        // Lua script for atomic sliding window
        var script = @"
            local key = KEYS[1]
            local window = tonumber(ARGV[1])
            local limit = tonumber(ARGV[2])
            local now = tonumber(ARGV[3])

            -- Remove expired entries
            redis.call('ZREMRANGEBYSCORE', key, 0, now - window)

            -- Count current requests
            local current = redis.call('ZCARD', key)

            if current >= limit then
                -- Get oldest entry to calculate retry-after
                local oldest = redis.call('ZRANGE', key, 0, 0, 'WITHSCORES')
                local retryAfter = oldest[2] + window - now
                return {0, retryAfter, limit - current}
            end

            -- Add current request
            redis.call('ZADD', key, now, now .. '-' .. math.random())
            redis.call('EXPIRE', key, window)

            return {1, 0, limit - current - 1}
        ";

        var result = await _redis.EvaluateAsync<long[]>(
            script,
            new RedisKey[] { key },
            new RedisValue[] 
            { 
                _settings.WindowMs, 
                _settings.MaxRequests,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });

        var allowed = result[0] == 1;
        var retryAfter = result[1];
        var remaining = result[2];

        // Add rate limit headers
        context.Response.Headers["X-RateLimit-Limit"] = _settings.MaxRequests.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
        context.Response.Headers["X-RateLimit-Reset"] = 
            DateTimeOffset.UtcNow.AddMilliseconds(_settings.WindowMs).ToUnixTimeSeconds().ToString();

        if (!allowed)
        {
            context.Response.Headers["Retry-After"] = retryAfter.ToString();
            
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.ContentType = "application/problem+json";
            
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://culinaryblog.com/errors/auth/AUTH_RATE_001",
                title = "Rate limit exceeded",
                status = 429,
                detail = $"Too many requests. Please wait {retryAfter} seconds.",
                extensions = new
                {
                    code = "RATE_LIMIT_EXCEEDED",
                    retryAfterSeconds = retryAfter,
                    limit = _settings.MaxRequests,
                    window = "1 minute"
                }
            });
            return;
        }

        await _next(context);
    }
}
```

### 4.3. Distributed Rate Limiting

```
┌─────────────────────────────────────────────────────────────────┐
│              DISTRIBUTED RATE LIMITING ARCHITECTURE              │
└─────────────────────────────────────────────────────────────────┘

                    ┌─────────────┐
                    │   Client    │
                    └──────┬──────┘
                           │ Request
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                     Nginx Reverse Proxy                          │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │  Rate Limit (Basic) - 100 req/s per IP                     ││
│  └─────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                     .NET API Backend                             │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │  Redis Sliding Window - 10 req/min per IP per endpoint      ││
│  │                                                              ││
│  │  Key Pattern: rate_limit:auth:{endpoint}:{ip}             ││
│  │                                                              ││
│  │  Window: Sliding (last 60 seconds)                         ││
│  │  Algorithm: Redis Sorted Set with timestamps                 ││
│  └─────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
                           │
                           ▼
                    ┌─────────────┐
                    │    Redis    │
                    │  10.0.0.5   │
                    └─────────────┘
```

---

## 5. CORS Configuration

### 5.1. CORS Settings

| Setting | Value | Description |
|-----------|-------|-------------|
| Allowed Origins | Frontend URL | https://culinaryblog.com |
| Allowed Methods | GET, POST, PATCH | HTTP methods |
| Allowed Headers | Content-Type, Authorization | Request headers |
| Allow Credentials | true | Include cookies/auth |
| Max Age | 3600 seconds | Preflight cache |
| Support Headers | X-Requested-With | AJAX requests |

### 5.2. CORS Implementation

```csharp
// Program.cs
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() 
    ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AuthPolicy", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders(
                "X-RateLimit-Limit",
                "X-RateLimit-Remaining", 
                "X-RateLimit-Reset",
                "X-Account-Locked-Until",
                "Retry-After")
            .SetPreflightMaxAge(TimeSpan.FromSeconds(3600));
    });
});

// appsettings.json
{
  "Cors": {
    "AllowedOrigins": [
      "https://culinaryblog.com",
      "https://www.culinaryblog.com",
      "http://localhost:3000"
    ]
  }
}
```

### 5.3. CORS Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                        CORS PREFLIGHT                            │
└─────────────────────────────────────────────────────────────────┘

1. Browser sends OPTIONS request (preflight)
   ┌──────────────────────────────────────┐
   │ OPTIONS /api/v1/auth/register         │
   │ Origin: https://culinaryblog.com     │
   │ Access-Control-Request-Method: POST  │
   │ Access-Control-Request-Headers:      │
   │   Content-Type, Authorization       │
   └──────────────────────────────────────┘

2. Server checks origin
   ├── Origin in allowed list? ──── NO ──→ 403 Forbidden
   └── YES → Continue

3. Server responds with CORS headers
   ┌──────────────────────────────────────┐
   │ 200 OK                               │
   │ Access-Control-Allow-Origin:         │
   │   https://culinaryblog.com           │
   │ Access-Control-Allow-Methods:        │
   │   GET, POST, PATCH                   │
   │ Access-Control-Allow-Headers:        │
   │   Content-Type, Authorization       │
   │ Access-Control-Max-Age: 3600         │
   └──────────────────────────────────────┘

4. Browser sends actual request
   ┌──────────────────────────────────────┐
   │ POST /api/v1/auth/register           │
   │ Origin: https://culinaryblog.com      │
   │ Authorization: Bearer eyJhbGciOi... │
   └──────────────────────────────────────┘
```

---

## 6. Input Validation

### 6.1. FluentValidation Rules

```csharp
// RegisterCommandValidator.cs
public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .WithMessage("Display name is required.")
            .MinimumLength(2)
            .WithMessage("Display name must be at least 2 characters.")
            .MaximumLength(100)
            .WithMessage("Display name must not exceed 100 characters.")
            .Matches(@"^[a-zA-ZÀ-ỹ\s]+$")
            .WithMessage("Display name can only contain letters and spaces.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Invalid email format.")
            .MaximumLength(256)
            .WithMessage("Email must not exceed 256 characters.");

        RuleFor(x => x.UserName)
            .NotEmpty()
            .WithMessage("Username is required.")
            .MinimumLength(3)
            .WithMessage("Username must be at least 3 characters.")
            .MaximumLength(30)
            .WithMessage("Username must not exceed 30 characters.")
            .Matches(@"^[a-zA-Z0-9_]+$")
            .WithMessage("Username can only contain letters, numbers, and underscores.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters.")
            .MaximumLength(128)
            .WithMessage("Password must not exceed 128 characters.")
            .Matches(@"[A-Z]")
            .WithMessage("Password must contain at least 1 uppercase letter.")
            .Matches(@"[a-z]")
            .WithMessage("Password must contain at least 1 lowercase letter.")
            .Matches(@"[0-9]")
            .WithMessage("Password must contain at least 1 digit.")
            .Matches(@"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]")
            .WithMessage("Password must contain at least 1 special character.");
    }
}
```

### 6.2. Additional Security Validations

```csharp
// GoogleLoginCommandValidator.cs
public class GoogleLoginCommandValidator : AbstractValidator<GoogleLoginCommand>
{
    public GoogleLoginCommandValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty()
            .WithMessage("Google ID token is required.");

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Invalid email format.");

        RuleFor(x => x.AvatarUrl)
            .Must(BeValidUrl)
            .When(x => !string.IsNullOrEmpty(x.AvatarUrl))
            .WithMessage("Avatar URL must be a valid URL.")
            .Must(BeGoogleAvatarUrl)
            .When(x => !string.IsNullOrEmpty(x.AvatarUrl))
            .WithMessage("Avatar must be from Google servers.");

        RuleFor(x => x.DisplayName)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.DisplayName))
            .WithMessage("Display name must not exceed 100 characters.");
    }

    private bool BeValidUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }

    private bool BeGoogleAvatarUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return true;
        return url.Contains("googleusercontent.com") || url.Contains("google.com");
    }
}
```

---

## 7. Security Headers

### 7.1. Required Headers

```csharp
public class SecurityHeadersMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Prevent XSS
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        
        // Prevent clickjacking
        context.Response.Headers["X-Frame-Options"] = "DENY";
        
        // Content type sniffing protection
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        
        // Strict transport security (HTTPS only)
        context.Response.Headers["Strict-Transport-Security"] = 
            "max-age=31536000; includeSubDomains; preload";
        
        // Referrer policy
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        
        // Content Security Policy
        context.Response.Headers["Content-Security-Policy"] = 
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' https://www.google.com/recaptcha/; " +
            "frame-src 'self' https://www.google.com/recaptcha/; " +
            "img-src 'self' data: https://*.googleusercontent.com https://storage.googleapis.com; " +
            "style-src 'self' 'unsafe-inline'; " +
            "font-src 'self'; " +
            "connect-src 'self' https://accounts.google.com;";
        
        // Permissions Policy
        context.Response.Headers["Permissions-Policy"] = 
            "geolocation=(), microphone=(), camera=()";

        await _next(context);
    }
}
```

### 7.2. Header Reference

| Header | Value | Purpose |
|--------|-------|---------|
| X-XSS-Protection | 1; mode=block | XSS filter (legacy browsers) |
| X-Frame-Options | DENY | Prevent clickjacking |
| X-Content-Type-Options | nosniff | Prevent MIME sniffing |
| Strict-Transport-Security | max-age=31536000 | Force HTTPS |
| Referrer-Policy | strict-origin-when-cross-origin | Control referrer info |
| Content-Security-Policy | (see above) | Prevent XSS/injection |
| Permissions-Policy | geolocation=() | Restrict browser features |

---

## 8. Logging & Monitoring

### 8.1. Security Events to Log

| Event | Level | Fields |
|-------|-------|--------|
| Registration Success | INFO | userId, email, ip, timestamp |
| Registration Failed | WARNING | email, reason, ip, timestamp |
| Login Success | INFO | userId, email, ip, timestamp |
| Login Failed | WARNING | email, reason, ip, failedAttempts, timestamp |
| Account Locked | WARNING | userId, ip, lockoutUntil, timestamp |
| Token Refresh | DEBUG | userId, tokenId, ip, timestamp |
| Token Reuse Detected | CRITICAL | userId, tokenHash, ip, timestamp |
| Logout | INFO | userId, ip, timestamp |
| Email Verified | INFO | userId, timestamp |
| Rate Limit Exceeded | WARNING | ip, endpoint, timestamp |

### 8.2. Structured Log Format

```json
{
  "@timestamp": "2026-06-04T15:30:00.000Z",
  "@level": "Warning",
  "@message": "Failed login attempt",
  "@renderedMessage": "Failed login attempt for email: user@example.com",
  "@tags": ["Security", "Authentication"],
  "@props": {
    "traceId": "4bf2c8d3-1a5e-4b9c-8f7d-2e6a1b3c5d7e",
    "spanId": "1a2b3c4d",
    "application": "CulinaryBlog.API",
    "environment": "Production",
    
    "security": {
      "eventType": "LOGIN_FAILED",
      "errorCode": "AUTH_INVALID_CREDENTIALS",
      "clientIp": "192.168.1.100",
      "userAgent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64)...",
      "failedAttempts": 3,
      "accountLocked": false
    },
    
    "user": {
      "email": "user@example.com",
      "userId": null
    },
    
    "request": {
      "method": "POST",
      "path": "/api/v1/auth/login",
      "queryString": null
    }
  }
}
```

### 8.3. Security Alert Thresholds

| Alert | Threshold | Action |
|-------|-----------|--------|
| Failed Logins | 5 failed attempts / 15 min | Lock account |
| Token Reuse | 1 occurrence | Log CRITICAL, consider revoke all |
| Rate Limit | > 100 requests / min | Block IP temporarily |
| Registration Failures | > 20 / hour from same IP | Temporary block |

---

## 9. Security Checklist

### 9.1. Pre-Deployment

- [ ] JWT secret key is at least 256 bits
- [ ] JWT secret key is stored in environment variable or secrets manager
- [ ] PBKDF2 iteration count is ≥ 100,000
- [ ] HTTPS is enforced
- [ ] CORS is configured with specific origins (no wildcard)
- [ ] Rate limiting is enabled
- [ ] Security headers are set
- [ ] Logging is configured with trace IDs
- [ ] Error messages don't leak sensitive data
- [ ] Database credentials are not hardcoded

### 9.2. Code Review Checklist

- [ ] No password logged in plain text
- [ ] No JWT secret hardcoded
- [ ] All inputs are validated
- [ ] SQL injection prevention (EF Core parameterized queries)
- [ ] XSS prevention (proper encoding)
- [ ] CSRF protection for state-changing operations
- [ ] Token rotation is implemented
- [ ] Account lockout is working
- [ ] Rate limiting is working
- [ ] Error responses follow RFC 7807

### 9.3. Testing Checklist

- [ ] Test brute force protection
- [ ] Test token reuse detection
- [ ] Test rate limiting
- [ ] Test account lockout
- [ ] Test password validation
- [ ] Test CORS configuration
- [ ] Test security headers
- [ ] Test error message leakage
- [ ] Test JWT expiration

---

## 10. Appendix

### 10.1. Configuration Reference

```json
// appsettings.json
{
  "Jwt": {
    "SecretKey": "${JWT_SECRET_KEY}", // Min 32 chars, from env var
    "Issuer": "https://culinaryblog.com",
    "Audience": "culinaryblog-api",
    "AccessTokenTTL": "00:15:00", // 15 minutes
    "RefreshTokenTTL": "7.00:00:00", // 7 days
    "ClockSkew": "00:01:00" // 1 minute
  },
  
  "Identity": {
    "Password": {
      "RequiredLength": 8,
      "RequiredUniqueChars": 4,
      "RequireDigit": true,
      "RequireLowercase": true,
      "RequireUppercase": true,
      "RequireNonAlphanumeric": true
    },
    "Lockout": {
      "DefaultLockoutTimeSpan": "00:15:00",
      "MaxFailedAccessAttempts": 5,
      "AllowedForNewUsers": true
    },
    "User": {
      "RequireUniqueEmail": true
    }
  },
  
  "RateLimiting": {
    "WindowMs": 60000, // 1 minute
    "MaxRequests": 10,
    "EnableParanoidMode": false
  },
  
  "Cors": {
    "AllowedOrigins": [
      "https://culinaryblog.com"
    ]
  }
}
```

### 10.2. Environment Variables

```bash
# Required
JWT_SECRET_KEY=your-super-secret-key-at-least-32-chars

# Optional (for production)
GOOGLE_CLIENT_ID=your-google-client-id
GOOGLE_CLIENT_SECRET=your-google-client-secret
SMTP_HOST=smtp.example.com
SMTP_USERNAME=your-smtp-username
SMTP_PASSWORD=your-smtp-password
```

---

**Document Status:** Complete  
**Last Updated:** 04/06/2026  
**Reviewed by:** ________________  
**Date:** ________________
