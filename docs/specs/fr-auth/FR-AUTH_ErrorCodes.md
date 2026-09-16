# FR-AUTH Error Codes Specification

> **Tài liệu nguồn:** FR-AUTH_BaoCao.md v1.0.0  
> **Phạm vi:** Application Error Codes cho module Authentication  
> **Phiên bản:** 1.0.0  
> **Ngày tạo:** 04/06/2026

---

## 1. Tổng quan

### 1.1. Mục đích

Tài liệu này định nghĩa tất cả Application Error Codes cho module FR-AUTH, bao gồm:
- Mã lỗi (Error Code)
- HTTP Status Code tương ứng
- Thông báo lỗi (Error Message)
- Nguyên nhân gây ra lỗi
- Response body theo RFC 7807

### 1.2. RFC 7807 Problem Details

Tất cả error responses phải tuân theo RFC 7807 với định dạng:

```json
{
  "type": "https://culinaryblog.com/errors/auth/{error-code}",
  "title": "Human-readable error title",
  "status": 400,
  "detail": "Detailed explanation of what went wrong",
  "instance": "/api/v1/auth/register",
  "extensions": {
    "code": "AUTH_EMAIL_EXISTS",
    "timestamp": "2026-06-04T15:30:00Z",
    "traceId": "abc123..."
  }
}
```

---

## 2. Authentication Error Codes

### 2.1. Registration Errors (AUTH_REG_*)

#### AUTH_REG_001: Email Already Exists

| Thuộc tính | Giá trị |
|------------|----------|
| **Error Code** | `AUTH_REG_001` |
| **HTTP Status** | 409 Conflict |
| **Title** | "Email already registered" |
| **Detail** | "The email address '{email}' is already registered in the system." |
| **Extension Code** | `AUTH_EMAIL_EXISTS` |

**Request Example:**
```http
POST /api/v1/auth/register
Content-Type: application/json

{
  "displayName": "Nguyễn Văn A",
  "email": "user@example.com",
  "userName": "nguyenvana",
  "password": "Password123!"
}
```

**Response Example:**
```json
{
  "type": "https://culinaryblog.com/errors/auth/AUTH_REG_001",
  "title": "Email already registered",
  "status": 409,
  "detail": "The email address 'user@example.com' is already registered in the system.",
  "instance": "/api/v1/auth/register",
  "extensions": {
    "code": "AUTH_EMAIL_EXISTS",
    "field": "email",
    "timestamp": "2026-06-04T15:30:00Z",
    "traceId": "4bf2c8d3-1a5e-4b9c-8f7d-2e6a1b3c5d7e"
  }
}
```

**When Occurs:**
- User attempts to register with an email that already exists in the system

**Resolution:**
- User should login with existing credentials
- Or use "Forgot Password" feature (v1.1+)

---

#### AUTH_REG_002: Username Already Exists

| Thuộc tính | Giá trị |
|------------|----------|
| **Error Code** | `AUTH_REG_002` |
| **HTTP Status** | 409 Conflict |
| **Title** | "Username already taken" |
| **Detail** | "The username '{userName}' is already taken by another user." |
| **Extension Code** | `AUTH_USERNAME_EXISTS` |

**Response Example:**
```json
{
  "type": "https://culinaryblog.com/errors/auth/AUTH_REG_002",
  "title": "Username already taken",
  "status": 409,
  "detail": "The username 'nguyenvana' is already taken by another user.",
  "extensions": {
    "code": "AUTH_USERNAME_EXISTS",
    "field": "userName",
    "timestamp": "2026-06-04T15:30:00Z",
    "traceId": "4bf2c8d3-1a5e-4b9c-8f7d-2e6a1b3c5d7e"
  }
}
```

**When Occurs:**
- User attempts to register with a username that is already in use

**Resolution:**
- User should choose a different username

---

#### AUTH_REG_003: Validation Error

| Thuộc tính | Giá trị |
|------------|----------|
| **Error Code** | `AUTH_REG_003` |
| **HTTP Status** | 422 Unprocessable Entity |
| **Title** | "Validation failed" |
| **Detail** | "One or more fields failed validation." |
| **Extension Code** | `VALIDATION_ERROR` |

**Response Example:**
```json
{
  "type": "https://culinaryblog.com/errors/auth/AUTH_REG_003",
  "title": "Validation failed",
  "status": 422,
  "detail": "One or more fields failed validation.",
  "instance": "/api/v1/auth/register",
  "extensions": {
    "code": "VALIDATION_ERROR",
    "errors": [
      {
        "field": "password",
        "message": "Password must be at least 8 characters with 1 uppercase, 1 digit, and 1 special character.",
        "code": "PASSWORD_TOO_WEAK"
      },
      {
        "field": "displayName",
        "message": "Display name must be between 2 and 100 characters.",
        "code": "DISPLAYNAME_TOO_SHORT"
      }
    ],
    "timestamp": "2026-06-04T15:30:00Z",
    "traceId": "4bf2c8d3-1a5e-4b9c-8f7d-2e6a1b3c5d7e"
  }
}
```

**Validation Rules:**

| Field | Rules | Error Code |
|-------|-------|------------|
| `displayName` | Required, 2-100 characters | `DISPLAYNAME_REQUIRED`, `DISPLAYNAME_TOO_SHORT`, `DISPLAYNAME_TOO_LONG` |
| `email` | Required, valid email format | `EMAIL_REQUIRED`, `EMAIL_INVALID` |
| `userName` | Required, alphanumeric + underscore only, 3-30 chars | `USERNAME_REQUIRED`, `USERNAME_INVALID`, `USERNAME_TOO_SHORT`, `USERNAME_TOO_LONG` |
| `password` | Required, min 8 chars, 1 upper, 1 digit, 1 special | `PASSWORD_REQUIRED`, `PASSWORD_TOO_WEAK` |

---

### 2.2. Login Errors (AUTH_LOGIN_*)

#### AUTH_LOGIN_001: Invalid Credentials

| Thuộc tính | Giá trị |
|------------|----------|
| **Error Code** | `AUTH_LOGIN_001` |
| **HTTP Status** | 401 Unauthorized |
| **Title** | "Invalid credentials" |
| **Detail** | "The email or password you entered is incorrect." |
| **Extension Code** | `AUTH_INVALID_CREDENTIALS` |
| **Security Note** | Generic message - does NOT reveal if email exists |

**Response Example:**
```json
{
  "type": "https://culinaryblog.com/errors/auth/AUTH_LOGIN_001",
  "title": "Invalid credentials",
  "status": 401,
  "detail": "The email or password you entered is incorrect.",
  "instance": "/api/v1/auth/login",
  "extensions": {
    "code": "AUTH_INVALID_CREDENTIALS",
    "timestamp": "2026-06-04T15:30:00Z",
    "traceId": "4bf2c8d3-1a5e-4b9c-8f7d-2e6a1b3c5d7e"
  }
}
```

**Security Notes:**
- Message is intentionally generic to prevent user enumeration attacks
- Log should NOT include whether email or password was wrong
- Both cases return the same response

**When Occurs:**
- User enters wrong password
- User enters non-existent email
- Account is locked due to failed attempts

---

#### AUTH_LOGIN_002: Account Locked

| Thuộc tính | Giá trị |
|------------|----------|
| **Error Code** | `AUTH_LOGIN_002` |
| **HTTP Status** | 423 Locked |
| **Title** | "Account locked" |
| **Detail** | "Your account has been locked due to multiple failed login attempts. Please try again after {unlockTime}." |
| **Extension Code** | `AUTH_ACCOUNT_LOCKED` |

**Response Example:**
```json
{
  "type": "https://culinaryblog.com/errors/auth/AUTH_LOGIN_002",
  "title": "Account locked",
  "status": 423,
  "detail": "Your account has been locked due to multiple failed login attempts. Please try again after 15 minutes.",
  "instance": "/api/v1/auth/login",
  "extensions": {
    "code": "AUTH_ACCOUNT_LOCKED",
    "unlockAt": "2026-06-04T15:45:00Z",
    "retryAfterSeconds": 900,
    "timestamp": "2026-06-04T15:30:00Z",
    "traceId": "4bf2c8d3-1a5e-4b9c-8f7d-2e6a1b3c5d7e"
  }
}
```

**Response Headers:**
```
Retry-After: 900
X-Account-Locked-Until: 2026-06-04T15:45:00Z
```

**When Occurs:**
- User fails 5 consecutive login attempts
- Lockout duration: 15 minutes

---

### 2.3. Token Errors (AUTH_TOKEN_*)

#### AUTH_TOKEN_001: Token Expired

| Thuộc tính | Giá trị |
|------------|----------|
| **Error Code** | `AUTH_TOKEN_001` |
| **HTTP Status** | 401 Unauthorized |
| **Title** | "Token expired" |
| **Detail** | "The refresh token has expired. Please login again." |
| **Extension Code** | `AUTH_REFRESH_TOKEN_EXPIRED` |

**Response Example:**
```json
{
  "type": "https://culinaryblog.com/errors/auth/AUTH_TOKEN_001",
  "title": "Token expired",
  "status": 401,
  "detail": "The refresh token has expired. Please login again.",
  "instance": "/api/v1/auth/refresh",
  "extensions": {
    "code": "AUTH_REFRESH_TOKEN_EXPIRED",
    "tokenType": "refresh_token",
    "expiredAt": "2026-06-04T08:30:00Z",
    "timestamp": "2026-06-04T15:30:00Z",
    "traceId": "4bf2c8d3-1a5e-4b9c-8f7d-2e6a1b3c5d7e"
  }
}
```

**When Occurs:**
- Refresh token TTL (7 days) has passed
- User must re-authenticate

---

#### AUTH_TOKEN_002: Token Revoked

| Thuộc tính | Giá trị |
|------------|----------|
| **Error Code** | `AUTH_TOKEN_002` |
| **HTTP Status** | 401 Unauthorized |
| **Title** | "Token revoked" |
| **Detail** | "The refresh token has been revoked. Please login again." |
| **Extension Code** | `AUTH_REFRESH_TOKEN_REVOKED` |
| **Security Alert** | ⚠️ Log as WARNING - Possible token reuse attack |

**Response Example:**
```json
{
  "type": "https://culinaryblog.com/errors/auth/AUTH_TOKEN_002",
  "title": "Token revoked",
  "status": 401,
  "detail": "The refresh token has been revoked. Please login again.",
  "instance": "/api/v1/auth/refresh",
  "extensions": {
    "code": "AUTH_REFRESH_TOKEN_REVOKED",
    "reason": "LOGOUT",
    "timestamp": "2026-06-04T15:30:00Z",
    "traceId": "4bf2c8d3-1a5e-4b9c-8f7d-2e6a1b3c5d7e"
  }
}
```

**When Occurs:**
- User logged out (token was revoked)
- Token rotation occurred
- **SECURITY: Possible refresh token reuse attack**

**Security Actions:**
```csharp
if (token.ReplacedByTokenHash != null && !isValidRotation)
{
    // SECURITY ALERT: Possible token reuse attack!
    _logger.LogWarning(
        "Refresh token reuse detected. UserId: {UserId}, TokenHash: {TokenHash}, IP: {IP}",
        userId, tokenHash, ipAddress);
    
    // Optional: Revoke all user tokens (paranoid mode)
    await RevokeAllUserTokensAsync(userId);
}
```

---

#### AUTH_TOKEN_003: Token Invalid

| Thuộc tính | Giá trị |
|------------|----------|
| **Error Code** | `AUTH_TOKEN_003` |
| **HTTP Status** | 401 Unauthorized |
| **Title** | "Token invalid" |
| **Detail** | "The refresh token is invalid or malformed." |
| **Extension Code** | `AUTH_REFRESH_TOKEN_INVALID` |

**Response Example:**
```json
{
  "type": "https://culinaryblog.com/errors/auth/AUTH_TOKEN_003",
  "title": "Token invalid",
  "status": 401,
  "detail": "The refresh token is invalid or malformed.",
  "instance": "/api/v1/auth/refresh",
  "extensions": {
    "code": "AUTH_REFRESH_TOKEN_INVALID",
    "timestamp": "2026-06-04T15:30:00Z",
    "traceId": "4bf2c8d3-1a5e-4b9c-8f7d-2e6a1b3c5d7e"
  }
}
```

**When Occurs:**
- Token format is invalid
- Token is malformed
- Token hash doesn't match any record

---

#### AUTH_TOKEN_004: Access Token Invalid

| Thuộc tính | Giá trị |
|------------|----------|
| **Error Code** | `AUTH_TOKEN_004` |
| **HTTP Status** | 401 Unauthorized |
| **Title** | "Access denied" |
| **Detail** | "Your access token is invalid or has expired." |
| **Extension Code** | `AUTH_ACCESS_TOKEN_INVALID` |

**Response Example:**
```json
{
  "type": "https://culinaryblog.com/errors/auth/AUTH_TOKEN_004",
  "title": "Access denied",
  "status": 401,
  "detail": "Your access token is invalid or has expired.",
  "instance": "/api/v1/auth/me",
  "extensions": {
    "code": "AUTH_ACCESS_TOKEN_INVALID",
    "timestamp": "2026-06-04T15:30:00Z",
    "traceId": "4bf2c8d3-1a5e-4b9c-8f7d-2e6a1b3c5d7e"
  }
}
```

**When Occurs:**
- JWT signature is invalid
- JWT is malformed
- JWT is tampered with

---

### 2.4. Google OAuth Errors (AUTH_GOOGLE_*)

#### AUTH_GOOGLE_001: Invalid Google Token

| Thuộc tính | Giá trị |
|------------|----------|
| **Error Code** | `AUTH_GOOGLE_001` |
| **HTTP Status** | 401 Unauthorized |
| **Title** | "Invalid Google token" |
| **Detail** | "The Google ID token could not be verified." |
| **Extension Code** | `AUTH_GOOGLE_TOKEN_INVALID` |

**Response Example:**
```json
{
  "type": "https://culinaryblog.com/errors/auth/AUTH_GOOGLE_001",
  "title": "Invalid Google token",
  "status": 401,
  "detail": "The Google ID token could not be verified.",
  "instance": "/api/v1/auth/google",
  "extensions": {
    "code": "AUTH_GOOGLE_TOKEN_INVALID",
    "timestamp": "2026-06-04T15:30:00Z",
    "traceId": "4bf2c8d3-1a5e-4b9c-8f7d-2e6a1b3c5d7e"
  }
}
```

**When Occurs:**
- idToken is expired
- idToken is tampered with
- idToken audience doesn't match our Client ID

---

#### AUTH_GOOGLE_002: Google API Error

| Thuộc tính | Giá trị |
|------------|----------|
| **Error Code** | `AUTH_GOOGLE_002` |
| **HTTP Status** | 502 Bad Gateway |
| **Title** | "Google service unavailable" |
| **Detail** | "Unable to verify your Google account. Please try again later." |
| **Extension Code** | `AUTH_GOOGLE_API_ERROR` |

**Response Example:**
```json
{
  "type": "https://culinaryblog.com/errors/auth/AUTH_GOOGLE_002",
  "title": "Google service unavailable",
  "status": 502,
  "detail": "Unable to verify your Google account. Please try again later.",
  "instance": "/api/v1/auth/google",
  "extensions": {
    "code": "AUTH_GOOGLE_API_ERROR",
    "googleError": "access_denied",
    "timestamp": "2026-06-04T15:30:00Z",
    "traceId": "4bf2c8d3-1a5e-4b9c-8f7d-2e6a1b3c5d7e"
  }
}
```

**When Occurs:**
- Google OAuth API is down
- Network connectivity issues
- User revoked permissions

---

### 2.5. Profile Errors (AUTH_PROFILE_*)

#### AUTH_PROFILE_001: Profile Not Found

| Thuộc tính | Giá trị |
|------------|----------|
| **Error Code** | `AUTH_PROFILE_001` |
| **HTTP Status** | 404 Not Found |
| **Title** | "Profile not found" |
| **Detail** | "The requested user profile could not be found." |
| **Extension Code** | `AUTH_USER_NOT_FOUND` |

**Response Example:**
```json
{
  "type": "https://culinaryblog.com/errors/auth/AUTH_PROFILE_001",
  "title": "Profile not found",
  "status": 404,
  "detail": "The requested user profile could not be found.",
  "instance": "/api/v1/auth/me",
  "extensions": {
    "code": "AUTH_USER_NOT_FOUND",
    "timestamp": "2026-06-04T15:30:00Z",
    "traceId": "4bf2c8d3-1a5e-4b9c-8f7d-2e6a1b3c5d7e"
  }
}
```

**When Occurs:**
- User was deleted after token was issued
- User ID in token doesn't exist

---

#### AUTH_PROFILE_002: Profile Update Validation

| Thuộc tính | Giá trị |
|------------|----------|
| **Error Code** | `AUTH_PROFILE_002` |
| **HTTP Status** | 422 Unprocessable Entity |
| **Title** | "Profile update failed" |
| **Detail** | "One or more fields failed validation." |
| **Extension Code** | `VALIDATION_ERROR` |

**Response Example:**
```json
{
  "type": "https://culinaryblog.com/errors/auth/AUTH_PROFILE_002",
  "title": "Profile update failed",
  "status": 422,
  "detail": "One or more fields failed validation.",
  "instance": "/api/v1/auth/me",
  "extensions": {
    "code": "VALIDATION_ERROR",
    "errors": [
      {
        "field": "displayName",
        "message": "Display name must be between 2 and 100 characters.",
        "code": "DISPLAYNAME_INVALID_LENGTH"
      },
      {
        "field": "avatarUrl",
        "message": "Avatar URL must be a valid URL.",
        "code": "AVATAR_URL_INVALID"
      },
      {
        "field": "bio",
        "message": "Bio must not exceed 500 characters.",
        "code": "BIO_TOO_LONG"
      }
    ],
    "timestamp": "2026-06-04T15:30:00Z",
    "traceId": "4bf2c8d3-1a5e-4b9c-8f7d-2e6a1b3c5d7e"
  }
}
```

---

### 2.6. Email Verification Errors (AUTH_EMAIL_*)

#### AUTH_EMAIL_001: Verification Token Invalid

| Thuộc tính | Giá trị |
|------------|----------|
| **Error Code** | `AUTH_EMAIL_001` |
| **HTTP Status** | 400 Bad Request |
| **Title** | "Invalid verification token" |
| **Detail** | "The email verification token is invalid or has already been used." |
| **Extension Code** | `AUTH_VERIFY_TOKEN_INVALID` |

**Response Example:**
```json
{
  "type": "https://culinaryblog.com/errors/auth/AUTH_EMAIL_001",
  "title": "Invalid verification token",
  "status": 400,
  "detail": "The email verification token is invalid or has already been used.",
  "instance": "/api/v1/auth/verify-email",
  "extensions": {
    "code": "AUTH_VERIFY_TOKEN_INVALID",
    "reason": "INVALID_TOKEN",
    "timestamp": "2026-06-04T15:30:00Z",
    "traceId": "4bf2c8d3-1a5e-4b9c-8f7d-2e6a1b3c5d7e"
  }
}
```

---

#### AUTH_EMAIL_002: Verification Token Expired

| Thuộc tính | Giá trị |
|------------|----------|
| **Error Code** | `AUTH_EMAIL_002` |
| **HTTP Status** | 400 Bad Request |
| **Title** | "Verification token expired" |
| **Detail** | "The email verification token has expired. Please request a new verification email." |
| **Extension Code** | `AUTH_VERIFY_TOKEN_EXPIRED` |

**Response Example:**
```json
{
  "type": "https://culinaryblog.com/errors/auth/AUTH_EMAIL_002",
  "title": "Verification token expired",
  "status": 400,
  "detail": "The email verification token has expired. Please request a new verification email.",
  "instance": "/api/v1/auth/verify-email",
  "extensions": {
    "code": "AUTH_VERIFY_TOKEN_EXPIRED",
    "expiredAt": "2026-06-03T15:30:00Z",
    "timestamp": "2026-06-04T15:30:00Z",
    "traceId": "4bf2c8d3-1a5e-4b9c-8f7d-2e6a1b3c5d7e"
  }
}
```

---

#### AUTH_EMAIL_003: Email Already Verified

| Thuộc tính | Giá trị |
|------------|----------|
| **Error Code** | `AUTH_EMAIL_003` |
| **HTTP Status** | 400 Bad Request |
| **Title** | "Email already verified" |
| **Detail** | "This email has already been verified." |
| **Extension Code** | `AUTH_EMAIL_ALREADY_VERIFIED` |

---

### 2.7. Rate Limiting Errors (AUTH_RATE_*)

#### AUTH_RATE_001: Rate Limit Exceeded

| Thuộc tính | Giá trị |
|------------|----------|
| **Error Code** | `AUTH_RATE_001` |
| **HTTP Status** | 429 Too Many Requests |
| **Title** | "Rate limit exceeded" |
| **Detail** | "Too many requests. Please wait {retryAfter} seconds before trying again." |
| **Extension Code** | `RATE_LIMIT_EXCEEDED` |

**Response Example:**
```json
{
  "type": "https://culinaryblog.com/errors/auth/AUTH_RATE_001",
  "title": "Rate limit exceeded",
  "status": 429,
  "detail": "Too many requests. Please wait 60 seconds before trying again.",
  "instance": "/api/v1/auth/login",
  "extensions": {
    "code": "RATE_LIMIT_EXCEEDED",
    "retryAfterSeconds": 60,
    "limit": 10,
    "window": "1 minute",
    "timestamp": "2026-06-04T15:30:00Z",
    "traceId": "4bf2c8d3-1a5e-4b9c-8f7d-2e6a1b3c5d7e"
  }
}
```

**Response Headers:**
```
Retry-After: 60
X-RateLimit-Limit: 10
X-RateLimit-Remaining: 0
X-RateLimit-Reset: 1717500060
```

---

## 3. Error Code Reference Table

### 3.1. By HTTP Status Code

| HTTP Status | Error Codes |
|-------------|-------------|
| **400 Bad Request** | AUTH_EMAIL_001, AUTH_EMAIL_002, AUTH_EMAIL_003 |
| **401 Unauthorized** | AUTH_LOGIN_001, AUTH_TOKEN_001, AUTH_TOKEN_002, AUTH_TOKEN_003, AUTH_TOKEN_004, AUTH_GOOGLE_001 |
| **404 Not Found** | AUTH_PROFILE_001 |
| **409 Conflict** | AUTH_REG_001, AUTH_REG_002 |
| **422 Unprocessable Entity** | AUTH_REG_003, AUTH_PROFILE_002 |
| **423 Locked** | AUTH_LOGIN_002 |
| **429 Too Many Requests** | AUTH_RATE_001 |
| **500 Internal Server Error** | (Generic, see 3.2) |
| **502 Bad Gateway** | AUTH_GOOGLE_002 |

### 3.2. Internal Server Errors

| HTTP Status | Error Code | Title |
|-------------|------------|-------|
| 500 | INTERNAL_ERROR | "An unexpected error occurred" |

**Response Example:**
```json
{
  "type": "https://culinaryblog.com/errors/auth/INTERNAL_ERROR",
  "title": "An unexpected error occurred",
  "status": 500,
  "detail": "An unexpected error occurred. Please try again later.",
  "instance": "/api/v1/auth/register",
  "extensions": {
    "code": "INTERNAL_ERROR",
    "errorId": "ERR-20240604-001",
    "timestamp": "2026-06-04T15:30:00Z",
    "traceId": "4bf2c8d3-1a5e-4b9c-8f7d-2e6a1b3c5d7e"
  }
}
```

**Security Note:**
- Error details are logged but NOT exposed to client
- Only `errorId` is returned for support reference

---

## 4. Client-Side Error Handling

### 4.1. Frontend Error Handling Pattern

```typescript
// React Hook Example
const useAuth = () => {
  const [error, setError] = useState<AuthError | null>(null);

  const handleAuthError = (errorResponse: AuthErrorResponse) => {
    switch (errorResponse.extensions.code) {
      case 'AUTH_EMAIL_EXISTS':
        return 'This email is already registered.';
      
      case 'AUTH_INVALID_CREDENTIALS':
        return 'Incorrect email or password.';
      
      case 'AUTH_ACCOUNT_LOCKED':
        const unlockTime = new Date(
          errorResponse.extensions.unlockAt
        );
        return `Account locked. Try again at ${unlockTime.toLocaleTimeString()}`;
      
      case 'AUTH_REFRESH_TOKEN_EXPIRED':
        // Redirect to login
        router.push('/auth/login');
        return 'Session expired. Please login again.';
      
      case 'AUTH_REFRESH_TOKEN_REVOKED':
        // Force logout
        logout();
        return 'Session invalidated. Please login again.';
      
      case 'RATE_LIMIT_EXCEEDED':
        return `Too many attempts. Wait ${errorResponse.extensions.retryAfterSeconds} seconds.`;
      
      case 'VALIDATION_ERROR':
        return errorResponse.extensions.errors
          .map(e => `${e.field}: ${e.message}`)
          .join('\n');
      
      default:
        return 'An unexpected error occurred. Please try again.';
    }
  };

  return { error, handleAuthError };
};
```

### 4.2. Error Type Definitions

```typescript
interface AuthErrorResponse {
  type: string;
  title: string;
  status: number;
  detail: string;
  instance: string;
  extensions: {
    code: string;
    timestamp: string;
    traceId: string;
    [key: string]: any;
  };
}

interface ValidationError {
  field: string;
  message: string;
  code: string;
}
```

---

## 5. Logging Requirements

### 5.1. Error Logging Structure

All errors must be logged with the following fields:

```json
{
  "timestamp": "2026-06-04T15:30:00Z",
  "level": "Warning",
  "message": "Failed login attempt",
  "traceId": "4bf2c8d3-1a5e-4b9c-8f7d-2e6a1b3c5d7e",
  "application": "CulinaryBlog.API",
  "environment": "Production",
  "user": {
    "email": "user@example.com",
    "userId": null
  },
  "request": {
    "method": "POST",
    "path": "/api/v1/auth/login",
    "ipAddress": "192.168.1.1",
    "userAgent": "Mozilla/5.0..."
  },
  "error": {
    "code": "AUTH_INVALID_CREDENTIALS",
    "type": "AuthenticationError",
    "details": {}
  }
}
```

### 5.2. Security Alert Logging

For security-critical events:

```csharp
// Log Security Events at WARNING level
_logger.LogWarning(
    "Security Event: {EventType}. UserId: {UserId}, IP: {IPAddress}, " +
    "ErrorCode: {ErrorCode}, Timestamp: {Timestamp}",
    "REFRESH_TOKEN_REUSE",
    userId,
    ipAddress,
    "AUTH_TOKEN_002",
    DateTime.UtcNow);
```

---

## 6. Appendix

### 6.1. Error Code Naming Convention

```
AUTH_{CATEGORY}_{NUMBER}
```

| Category | Description |
|----------|-------------|
| REG | Registration errors |
| LOGIN | Login errors |
| TOKEN | Token-related errors |
| GOOGLE | Google OAuth errors |
| PROFILE | Profile errors |
| EMAIL | Email verification errors |
| RATE | Rate limiting errors |
| INTERNAL | Internal server errors |

### 6.2. HTTP Status Code Reference

| Status | Name | Usage |
|--------|------|-------|
| 400 | Bad Request | Invalid request format, bad input |
| 401 | Unauthorized | Missing or invalid authentication |
| 404 | Not Found | Resource not found |
| 409 | Conflict | Resource conflict (duplicate) |
| 422 | Unprocessable Entity | Validation failed |
| 423 | Locked | Resource temporarily locked |
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Internal Server Error | Unexpected server error |
| 502 | Bad Gateway | External service error |

---

**Document Status:** Complete  
**Last Updated:** 04/06/2026  
**Reviewed by:** ________________  
**Date:** ________________
