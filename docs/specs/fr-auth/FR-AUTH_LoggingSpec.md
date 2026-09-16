# FR-AUTH Logging Specification

> **Tài liệu nguồn:** FR-AUTH_BaoCao.md v1.0.0  
> **Phạm vi:** Logging requirements cho module Authentication  
> **Phiên bản:** 1.0.0  
> **Ngày tạo:** 04/06/2026

---

## 1. Tổng quan Logging

### 1.1. Mục đích

Tài liệu này định nghĩa chi tiết các yêu cầu logging cho module FR-AUTH:
- Structured logging với Serilog
- Correlation ID propagation
- Log levels cho từng event
- Log fields cho security events
- Log storage và retention
- Monitoring và alerting

### 1.2. Logging Principles

1. **Structured Logging** - Log as JSON, không phải plain text
2. **Correlation** - Mọi request có unique traceId
3. **Security Events** - Security-sensitive actions được log riêng
4. **PII Protection** - Không log sensitive data
5. **Performance** - Async logging, không block main thread

---

## 2. Serilog Configuration

### 2.1. Serilog Setup

```csharp
// Program.cs
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Seq;
using Serilog.Sinks.Console;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Information)
    .MinimumLevel.Override("CulinaryBlog", LogEventLevel.Debug)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProperty("Application", "CulinaryBlog.API")
    .Enrich.WithProperty("Environment", env)
    .WriteTo.Console(new CompactJsonFormatter())
    .WriteTo.Seq(config.SeqUrl, apiKey: config.SeqApiKey)
    .WriteTo.File(
        new CompactJsonFormatter(),
        path: "logs/auth-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 100_000_000,
        rollOnFileSizeLimit: true)
    .CreateLogger();

builder.Host.UseSerilog();
```

### 2.2. Serilog Configuration

```json
// appsettings.json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "System": "Warning",
        "CulinaryBlog": "Debug"
      }
    },
    "Enrich": [
      "FromLogContext",
      "WithMachineName",
      "WithEnvironmentName",
      "WithProperty:Application:CulinaryBlog.API"
    ],
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/auth-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "fileSizeLimitBytes": 104857600,
          "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
        }
      },
      {
        "Name": "Seq",
        "Args": {
          "serverUrl": "${SEQ_URL}",
          "apiKey": "${SEQ_API_KEY}",
          "compactJsonFormat": true
        }
      }
    ]
  }
}
```

---

## 3. Correlation ID Implementation

### 3.1. Correlation Middleware

```csharp
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private const string TraceIdHeader = "X-Trace-Id";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Get or generate correlation ID
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        var traceId = context.Request.Headers[TraceIdHeader].FirstOrDefault()
            ?? Activity.Current?.Id
            ?? correlationId;

        // Add to log context
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("TraceId", traceId))
        {
            // Add to response headers
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[CorrelationIdHeader] = correlationId;
                context.Response.Headers[TraceIdHeader] = traceId;
                return Task.CompletedTask;
            });

            // Add to items for access in code
            context.Items["CorrelationId"] = correlationId;
            context.Items["TraceId"] = traceId;

            await _next(context);
        }
    }
}
```

### 3.2. Usage in Code

```csharp
public class RegisterCommandHandler
{
    private readonly ILogger<RegisterCommandHandler> _logger;

    public async Task<Result<AuthResponseDto>> Handle(
        RegisterCommand request, 
        CancellationToken cancellationToken)
    {
        var correlationId = _httpContextAccessor.HttpContext.Items["CorrelationId"];
        
        _logger.LogInformation(
            "Processing registration request. " +
            "CorrelationId: {CorrelationId}, " +
            "Email: {Email}, " +
            "IP: {IPAddress}",
            correlationId,
            request.Email,
            GetClientIp());
    }
}
```

---

## 4. Logging Events

### 4.1. Authentication Events

#### REGISTRATION EVENTS

| Event | Log Level | When |
|-------|-----------|------|
| Registration Started | DEBUG | User submits registration form |
| Registration Successful | INFO | User registered successfully |
| Registration Failed (Validation) | WARNING | Validation errors |
| Registration Failed (Email Exists) | WARNING | Email already registered |

**Registration Started (DEBUG):**
```json
{
  "@timestamp": "2026-06-04T15:30:00.000Z",
  "@level": "Debug",
  "@message": "Processing registration request",
  "eventType": "REGISTRATION_STARTED",
  "correlationId": "abc123def456",
  "traceId": "xyz789",
  "email": "user@example.com",
  "displayName": "Nguyễn Văn A",
  "ipAddress": "192.168.1.100",
  "userAgent": "Mozilla/5.0...",
  "application": "CulinaryBlog.API",
  "environment": "Production"
}
```

**Registration Successful (INFO):**
```json
{
  "@timestamp": "2026-06-04T15:30:00.500Z",
  "@level": "Information",
  "@message": "User registered successfully",
  "eventType": "REGISTRATION_SUCCESS",
  "correlationId": "abc123def456",
  "traceId": "xyz789",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com",
  "displayName": "Nguyễn Văn A",
  "roles": ["Author"],
  "ipAddress": "192.168.1.100",
  "userAgent": "Mozilla/5.0...",
  "durationMs": 250,
  "application": "CulinaryBlog.API",
  "environment": "Production"
}
```

**Registration Failed - Email Exists (WARNING):**
```json
{
  "@timestamp": "2026-06-04T15:30:00.500Z",
  "@level": "Warning",
  "@message": "Registration failed: email already exists",
  "eventType": "REGISTRATION_FAILED",
  "correlationId": "abc123def456",
  "traceId": "xyz789",
  "reason": "EMAIL_EXISTS",
  "errorCode": "AUTH_REG_001",
  "email": "user@example.com",
  "ipAddress": "192.168.1.100",
  "userAgent": "Mozilla/5.0...",
  "application": "CulinaryBlog.API",
  "environment": "Production"
}
```

---

#### LOGIN EVENTS

| Event | Log Level | When |
|-------|-----------|------|
| Login Started | DEBUG | User submits login form |
| Login Successful | INFO | User logged in successfully |
| Login Failed (Invalid Credentials) | WARNING | Wrong email/password |
| Login Failed (Locked) | WARNING | Account is locked |
| Account Locked | WARNING | Account locked due to failed attempts |
| Account Unlocked | INFO | Lockout period ended |

**Login Successful (INFO):**
```json
{
  "@timestamp": "2026-06-04T15:30:00.500Z",
  "@level": "Information",
  "@message": "User logged in successfully",
  "eventType": "LOGIN_SUCCESS",
  "correlationId": "abc123def456",
  "traceId": "xyz789",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com",
  "roles": ["Author"],
  "loginMethod": "EMAIL_PASSWORD",
  "ipAddress": "192.168.1.100",
  "userAgent": "Mozilla/5.0...",
  "durationMs": 150,
  "application": "CulinaryBlog.API",
  "environment": "Production"
}
```

**Login Failed - Invalid Credentials (WARNING):**
```json
{
  "@timestamp": "2026-06-04T15:30:00.500Z",
  "@level": "Warning",
  "@message": "Login failed: invalid credentials",
  "eventType": "LOGIN_FAILED",
  "correlationId": "abc123def456",
  "traceId": "xyz789",
  "reason": "INVALID_CREDENTIALS",
  "errorCode": "AUTH_LOGIN_001",
  "failedAttempts": 2,
  "email": "user@example.com",
  "ipAddress": "192.168.1.100",
  "userAgent": "Mozilla/5.0...",
  "application": "CulinaryBlog.API",
  "environment": "Production"
}
```

**Account Locked (WARNING):**
```json
{
  "@timestamp": "2026-06-04T15:30:00.500Z",
  "@level": "Warning",
  "@message": "Account locked due to failed login attempts",
  "eventType": "ACCOUNT_LOCKED",
  "correlationId": "abc123def456",
  "traceId": "xyz789",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com",
  "failedAttempts": 5,
  "lockoutDuration": "00:15:00",
  "lockoutUntil": "2026-06-04T15:45:00Z",
  "ipAddress": "192.168.1.100",
  "application": "CulinaryBlog.API",
  "environment": "Production"
}
```

---

#### TOKEN EVENTS

| Event | Log Level | When |
|-------|-----------|------|
| Token Refresh | DEBUG | Refresh token used |
| Token Refresh Failed | WARNING | Refresh token invalid/expired |
| Token Reuse Detected | CRITICAL | Possible attack! |
| Logout | INFO | User logged out |

**Token Reuse Detected (CRITICAL):**
```json
{
  "@timestamp": "2026-06-04T15:30:00.500Z",
  "@level": "Critical",
  "@message": "SECURITY ALERT: Refresh token reuse detected",
  "eventType": "TOKEN_REUSE_DETECTED",
  "correlationId": "abc123def456",
  "traceId": "xyz789",
  "severity": "CRITICAL",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com",
  "tokenHash": "a1b2c3d4...",  // Partial hash for debugging
  "originalTokenCreatedAt": "2026-06-04T08:30:00Z",
  "originalTokenIp": "192.168.1.100",
  "reuseDetectedAt": "2026-06-04T15:30:00Z",
  "reuseIp": "203.0.113.50",
  "reuseUserAgent": "Mozilla/5.0...",
  "actionTaken": "PARANOID_MODE_REVOKED_ALL",
  "application": "CulinaryBlog.API",
  "environment": "Production"
}
```

**Logout (INFO):**
```json
{
  "@timestamp": "2026-06-04T16:00:00.000Z",
  "@level": "Information",
  "@message": "User logged out",
  "eventType": "LOGOUT",
  "correlationId": "abc123def456",
  "traceId": "xyz789",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com",
  "ipAddress": "192.168.1.100",
  "sessionDuration": "01:30:00",
  "application": "CulinaryBlog.API",
  "environment": "Production"
}
```

---

#### GOOGLE OAUTH EVENTS

| Event | Log Level | When |
|-------|-----------|------|
| Google OAuth Started | DEBUG | User initiates Google login |
| Google OAuth Success | INFO | Google login successful |
| Google OAuth Failed | WARNING | Google token invalid |

**Google OAuth Success (INFO):**
```json
{
  "@timestamp": "2026-06-04T15:30:00.500Z",
  "@level": "Information",
  "@message": "Google OAuth login successful",
  "eventType": "GOOGLE_OAUTH_SUCCESS",
  "correlationId": "abc123def456",
  "traceId": "xyz789",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com",
  "googleUserId": "google-123456",
  "accountCreated": true,
  "accountLinked": false,
  "ipAddress": "192.168.1.100",
  "userAgent": "Mozilla/5.0...",
  "durationMs": 350,
  "application": "CulinaryBlog.API",
  "environment": "Production"
}
```

---

#### EMAIL VERIFICATION EVENTS

| Event | Log Level | When |
|-------|-----------|------|
| Verification Email Sent | INFO | Email verification link sent |
| Email Verified | INFO | User confirmed email |
| Verification Failed | WARNING | Invalid/expired token |

---

#### PROFILE EVENTS

| Event | Log Level | When |
|-------|-----------|------|
| Profile Viewed | DEBUG | User views own profile |
| Profile Updated | INFO | User updates profile |
| Profile Update Failed | WARNING | Validation errors |

---

### 4.2. Security Audit Events

All security events should be logged with the following structure:

```csharp
public class SecurityAuditLog
{
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; }
    public string Severity { get; set; } // INFO, WARNING, CRITICAL
    public string CorrelationId { get; set; }
    public string TraceId { get; set; }
    public string UserId { get; set; }
    public string Email { get; set; }
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
    public string Action { get; set; }
    public string Result { get; set; } // SUCCESS, FAILURE
    public string ErrorCode { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; }
}
```

---

## 5. Log Levels

### 5.1. Log Level Definitions

| Level | Usage | Examples |
|-------|-------|----------|
| **DEBUG** | Detailed information for debugging | Request processing, parameter values |
| **INFO** | General operational events | Successful login, registration, logout |
| **WARNING** | Potential issues or anomalies | Failed login, validation errors, rate limit |
| **ERROR** | Errors that need attention | Database errors, external service failures |
| **CRITICAL** | Security threats, system failures | Token reuse, database connection lost |

### 5.2. Log Level by Module

| Module | Default Level | Production Level |
|--------|-------------|------------------|
| FR-AUTH Registration | DEBUG | INFO |
| FR-AUTH Login | DEBUG | INFO |
| FR-AUTH Token | DEBUG | INFO |
| FR-AUTH Profile | DEBUG | WARNING |
| Security Events | WARNING | WARNING |
| Token Reuse | CRITICAL | CRITICAL |

---

## 6. PII Protection

### 6.1. Data Classification

| Data Type | Can Log? | Example |
|-----------|---------|---------|
| User ID | ✅ Yes | `userId: "abc123"` |
| Email (masked) | ✅ Yes | `email: "u***@example.com"` |
| IP Address | ✅ Yes | `ipAddress: "192.168.1.100"` |
| User Agent | ✅ Yes | `userAgent: "Mozilla/5.0..."` |
| Password | ❌ Never | - |
| JWT Token | ❌ Never | - |
| Refresh Token | ❌ Never | - |
| Full Email | ⚠️ Masked | `email: "us***@example.com"` |

### 6.2. PII Masking

```csharp
public static class PiiMasking
{
    public static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return null;
        
        var parts = email.Split('@');
        if (parts.Length != 2) return email;
        
        var local = parts[0];
        var domain = parts[1];
        
        if (local.Length <= 2)
            return $"**@{domain}";
        
        return $"{local[0]}***{local[^1]}@{domain}";
    }
    
    public static string MaskPhone(string phone)
    {
        if (string.IsNullOrEmpty(phone)) return null;
        if (phone.Length < 4) return "****";
        return $"***{phone[^4..]}";
    }
}
```

---

## 7. Log Storage

### 7.1. Log Retention

| Environment | Storage | Retention | Purpose |
|-------------|--------|----------|---------|
| Development | Console + File | 7 days | Debugging |
| Staging | Seq + File | 30 days | Testing, QA |
| Production | Seq + File + S3 | 90 days | Audit, Compliance |

### 7.2. Log Storage Configuration

```json
// appsettings.json
{
  "Logging": {
    "Retention": {
      "Development": {
        "console": true,
        "fileRetentionDays": 7,
        "seqEnabled": false
      },
      "Staging": {
        "console": false,
        "fileRetentionDays": 30,
        "seqEnabled": true,
        "seqRetentionDays": 30
      },
      "Production": {
        "console": false,
        "fileRetentionDays": 90,
        "seqEnabled": true,
        "seqRetentionDays": 90,
        "s3Enabled": true,
        "s3Bucket": "culinaryblog-logs",
        "s3RetentionDays": 365
      }
    }
  }
}
```

---

## 8. Monitoring & Alerting

### 8.1. Alert Rules

| Alert | Condition | Severity | Action |
|-------|-----------|----------|--------|
| Token Reuse | 1 occurrence | CRITICAL | Page on-call, revoke user tokens |
| Failed Logins | > 50 / 5 min per IP | WARNING | Block IP temporarily |
| High Error Rate | > 10% errors in 5 min | WARNING | Page on-call |
| Latency Spike | p95 > 2s | WARNING | Investigate |
| Registration Spam | > 20 / min from same IP | WARNING | Block IP |

### 8.2. Seq Dashboard Queries

**Failed Login Attempts:**
```sql
@level = 'Warning' AND eventType = 'LOGIN_FAILED'
| group count() by ipAddress
| order by count_ desc
| limit 10
```

**Token Reuse Detection:**
```sql
@level = 'Critical' AND eventType = 'TOKEN_REUSE_DETECTED'
| select timestamp, userId, ipAddress, actionTaken
| order by timestamp desc
```

**Suspicious Activity:**
```sql
@level = 'Warning' AND (eventType = 'LOGIN_FAILED' OR eventType = 'REGISTRATION_FAILED')
| where timestamp > now() - 1h
| group count() by ipAddress
| where count_ > 10
```

### 8.3. Grafana Alerts (Optional)

```yaml
# Prometheus alerting rules
groups:
  - name: authentication_alerts
    rules:
      - alert: HighFailedLoginRate
        expr: |
          rate(auth_login_failed_total[5m]) > 0.1
        for: 2m
        labels:
          severity: warning
        annotations:
          summary: "High rate of failed login attempts"
          description: "{{ $value }} failed logins per second"

      - alert: TokenReuseDetected
        expr: |
          auth_token_reuse_total > 0
        for: 0m
        labels:
          severity: critical
        annotations:
          summary: "Token reuse attack detected!"
          description: "User {{ $labels.userId }} attempted token reuse from {{ $labels.ip }}"
```

---

## 9. Log Sampling

### 9.1. High-Volume Event Sampling

For high-volume events like DEBUG logs:

```csharp
public class SampledLogger
{
    private readonly ILogger _logger;
    private readonly Random _random = new();

    public void LogDebugSampled(string message, double sampleRate = 0.1)
    {
        if (_random.NextDouble() < sampleRate)
        {
            _logger.LogDebug(message);
        }
    }
}
```

### 9.2. Configuration

```json
{
  "Logging": {
    "Sampling": {
      "Enabled": true,
      "SampleRate": 0.1,
      "ExcludedLevels": ["Warning", "Error", "Critical"],
      "ExcludedEvents": [
        "REGISTRATION_STARTED",
        "PROFILE_VIEWED"
      ]
    }
  }
}
```

---

## 10. Appendix

### 10.1. Complete Log Field Reference

| Field | Type | Description | Required |
|-------|------|-------------|----------|
| @timestamp | datetime | Event timestamp (UTC) | Yes |
| @level | string | Log level (Debug/Info/Warning/Error/Critical) | Yes |
| @message | string | Human-readable message | Yes |
| eventType | string | Business event type | Yes |
| correlationId | string | Request correlation ID | Yes |
| traceId | string | Distributed tracing ID | Yes |
| userId | string | User identifier (if authenticated) | No |
| email | string | User email (masked) | No |
| ipAddress | string | Client IP address | Yes |
| userAgent | string | Browser/client user agent | No |
| durationMs | number | Request duration in ms | No |
| errorCode | string | Application error code | No |
| application | string | Application name | Yes |
| environment | string | Environment (dev/staging/prod) | Yes |

### 10.2. Event Type Reference

| Event Type | Category | Level |
|------------|----------|-------|
| REGISTRATION_STARTED | Auth | DEBUG |
| REGISTRATION_SUCCESS | Auth | INFO |
| REGISTRATION_FAILED | Auth | WARNING |
| LOGIN_STARTED | Auth | DEBUG |
| LOGIN_SUCCESS | Auth | INFO |
| LOGIN_FAILED | Auth | WARNING |
| ACCOUNT_LOCKED | Security | WARNING |
| ACCOUNT_UNLOCKED | Auth | INFO |
| TOKEN_REFRESH | Auth | DEBUG |
| TOKEN_REFRESH_FAILED | Auth | WARNING |
| TOKEN_REUSE_DETECTED | Security | CRITICAL |
| LOGOUT | Auth | INFO |
| GOOGLE_OAUTH_STARTED | Auth | DEBUG |
| GOOGLE_OAUTH_SUCCESS | Auth | INFO |
| GOOGLE_OAUTH_FAILED | Auth | WARNING |
| VERIFICATION_EMAIL_SENT | Auth | INFO |
| EMAIL_VERIFIED | Auth | INFO |
| VERIFICATION_FAILED | Auth | WARNING |
| PROFILE_VIEWED | Auth | DEBUG |
| PROFILE_UPDATED | Auth | INFO |
| PROFILE_UPDATE_FAILED | Auth | WARNING |
| RATE_LIMIT_EXCEEDED | Security | WARNING |

---

**Document Status:** Complete  
**Last Updated:** 04/06/2026  
**Reviewed by:** ________________  
**Date:** ________________
