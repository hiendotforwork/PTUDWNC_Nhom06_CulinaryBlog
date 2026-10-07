# FR-AUTH-004: Token Refresh Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Triển khai chức năng làm mới Access Token (Token Refresh) với Token Rotation, bao gồm backend endpoint và frontend auto-refresh + manual refresh.

**Architecture:** Backend sử dụng MediatR CQRS pattern đã có sẵn. Refresh endpoint nhận refresh token cũ, revoke và tạo cặp token mới. Frontend sử dụng hybrid approach: auto-refresh khi nhận 401 từ protected routes, và manual refresh khi user refresh page.

**Tech Stack:** ASP.NET Core, MediatR, FluentValidation, React, TypeScript, Next.js App Router

**Spec:** `docs/specs/SRS_Culinary_Blog_v1.0.0.md` (trang 20-21), `docs/specs/fr-auth/FR-AUTH_APIContract.md`

---

## Global Constraints

- Access token expires: 15 phút
- Refresh token TTL: 7 ngày
- Token Rotation: refresh token cũ bị revoke trước khi tạo mới
- Rate limit refresh endpoint: 30 requests/minute/IP
- Frontend auto-refresh trigger: khi API trả 401

---

## Review Focus

1. **Token reuse attack**: Khi refresh token đã bị revoke được gửi lại → phải log security alert
2. **Expired refresh token**: Token hết hạn → trả 401, client phải login lại
3. **User deleted/locked**: User bị xóa hoặc khóa sau khi token được cấp → trả 401
4. **Concurrent refresh**: Nhiều request refresh cùng lúc → Serializable isolation prevent race condition
5. **Frontend token expiry detection**: Frontend phải decode JWT hoặc so sánh expiresAt để detect token sắp hết hạn

---

## File Structure

```
src/CulinaryBlog.Application/
├── Commands/Auth/
│   └── RefreshToken/
│       ├── RefreshTokenCommand.cs
│       ├── RefreshTokenCommandHandler.cs
│       └── RefreshTokenCommandValidator.cs

src/CulinaryBlog.Application/DTOs/Auth/
└── RefreshTokenRequest.cs

src/CulinaryBlog.Application/Exceptions/
└── AuthException.cs (add TokenReuseException)

src/CulinaryBlog.Infrastructure/Services/
└── TokenService.cs (add ValidateTokenExpiry helper)

frontend/app/lib/
├── api.ts (add refreshToken)
└── auth.ts (add refreshAccessToken, isTokenExpiringSoon)

frontend/app/context/
└── AppContext.tsx (add auto-refresh logic)

tests/CulinaryBlog.Application.Tests/Commands/
└── RefreshTokenCommandHandlerTests.cs

tests/CulinaryBlog.Integration.Tests/
└── AuthControllerTests.cs (add refresh endpoint tests)
```

---

## Task 1: Backend - RefreshTokenCommand & Handler

**Files:**
- Create: `src/CulinaryBlog.Application/Commands/Auth/RefreshToken/RefreshTokenCommand.cs`
- Create: `src/CulinaryBlog.Application/Commands/Auth/RefreshToken/RefreshTokenCommandHandler.cs`
- Create: `src/CulinaryBlog.Application/Commands/Auth/RefreshToken/RefreshTokenCommandValidator.cs`
- Modify: `src/CulinaryBlog.Application/DTOs/Auth/RefreshTokenRequest.cs`
- Modify: `src/CulinaryBlog.Application/Exceptions/AuthException.cs` (add TokenReuseException)
- Test: `tests/CulinaryBlog.Application.Tests/Commands/RefreshTokenCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `IRefreshTokenRepository`, `ITokenService`, `IUserRepository`, `IUnitOfWork`
- Produces: `RefreshTokenCommand : IRequest<AuthResponse>`

**Steps:**

- [ ] **Step 1: Tạo RefreshTokenCommand**

```csharp
// src/CulinaryBlog.Application/Commands/Auth/RefreshToken/RefreshTokenCommand.cs
namespace CulinaryBlog.Application.Commands.Auth.RefreshToken;

using CulinaryBlog.Application.DTOs.Auth;
using MediatR;

public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponse>;
```

- [ ] **Step 2: Tạo RefreshTokenRequest DTO**

```csharp
// src/CulinaryBlog.Application/DTOs/Auth/RefreshTokenRequest.cs
namespace CulinaryBlog.Application.DTOs.Auth;

public record RefreshTokenRequest(string RefreshToken);
```

- [ ] **Step 3: Tạo Validator**

```csharp
// src/CulinaryBlog.Application/Commands/Auth/RefreshToken/RefreshTokenCommandValidator.cs
namespace CulinaryBlog.Application.Commands.Auth.RefreshToken;

using FluentValidation;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.")
            .MinimumLength(10).WithMessage("Invalid refresh token format.");
    }
}
```

- [ ] **Step 4: Thêm TokenReuseException vào AuthException**

```csharp
// Trong AuthException.cs, thêm method:
public static Exception TokenReuseDetected()
{
    return new UnauthorizedAccessException("Refresh token reuse detected. Possible security threat.");
}
```

- [ ] **Step 5: Tạo RefreshTokenCommandHandler**

```csharp
// src/CulinaryBlog.Application/Commands/Auth/RefreshToken/RefreshTokenCommandHandler.cs
namespace CulinaryBlog.Application.Commands.Auth.RefreshToken;

using CulinaryBlog.Application.DTOs.Auth;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = RefreshToken.HashToken(request.RefreshToken);
        var refreshToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        // A1: Token not found
        if (refreshToken == null)
        {
            _logger.LogWarning("Refresh token not found");
            throw AuthException.InvalidCredentials();
        }

        // A3: Token reuse attack detected
        if (refreshToken.IsRevoked)
        {
            _logger.LogWarning("SECURITY ALERT: Refresh token reuse detected for user {UserId}", refreshToken.UserId);
            throw AuthException.TokenReuseDetected();
        }

        // A2: Token expired
        if (refreshToken.IsExpired)
        {
            _logger.LogWarning("Refresh token expired for user {UserId}", refreshToken.UserId);
            throw AuthException.InvalidCredentials();
        }

        // A4: User deleted or locked
        var user = refreshToken.User;
        if (user == null || !user.IsActive)
        {
            _logger.LogWarning("User not found or inactive for refresh token");
            throw AuthException.InvalidCredentials();
        }

        // Get user roles
        var roles = await _userRepository.GetRolesAsync(user);

        // Begin transaction with Serializable isolation
        await using var transaction = await _unitOfWork.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);

        try
        {
            // Revoke old token with reference to new token
            var newRawToken = _tokenService.GenerateRefreshToken();
            refreshToken.Revoke(RefreshToken.HashToken(newRawToken));

            // Generate new tokens
            var newAccessToken = _tokenService.GenerateAccessToken(user, roles.ToList());
            var newRefreshTokenEntity = RefreshToken.Create(
                user.Id,
                newRawToken,
                ipAddress: null,
                daysToLive: 7);

            await _refreshTokenRepository.AddAsync(newRefreshTokenEntity, cancellationToken);
            await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var userDto = new UserDto(
                user.Id,
                user.Email ?? string.Empty,
                user.UserName ?? string.Empty,
                user.DisplayName,
                user.AvatarUrl,
                user.Bio,
                roles.ToList());

            return new AuthResponse(
                newAccessToken,
                newRawToken,
                DateTime.UtcNow.AddMinutes(15),
                userDto);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
```

- [ ] **Step 6: Viết unit tests**

```csharp
// tests/CulinaryBlog.Application.Tests/Commands/RefreshTokenCommandHandlerTests.cs
namespace CulinaryBlog.Application.Tests.Commands;

using CulinaryBlog.Application.Commands.Auth.RefreshToken;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepoMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _refreshTokenRepoMock = new Mock<IRefreshTokenRepository>();
        _tokenServiceMock = new Mock<ITokenService>();
        _userRepoMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        var loggerMock = new Mock<ILogger<RefreshTokenCommandHandler>>();

        _handler = new RefreshTokenCommandHandler(
            _refreshTokenRepoMock.Object,
            _tokenServiceMock.Object,
            _userRepoMock.Object,
            _unitOfWorkMock.Object,
            loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidToken_ReturnsNewAuthResponse()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var rawToken = "valid-refresh-token";
        var user = ApplicationUser.Create("Test User", "test@test.com", "testuser");
        user.Id = userId;
        user.IsActive = true;

        var refreshToken = RefreshToken.Create(userId, rawToken, daysToLive: 7);
        refreshToken.User = user;

        _refreshTokenRepoMock.Setup(x => x.GetByTokenHashAsync(It.IsAny<string>(), default))
            .ReturnsAsync(refreshToken);
        _userRepoMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Author" });
        _tokenServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("new-raw-token");
        _tokenServiceMock.Setup(x => x.GenerateAccessToken(user, It.IsAny<IList<string>>()))
            .Returns("new-access-token");

        var transactionMock = new Mock<System.Data.Common.DbTransaction>();
        var connectionMock = new Mock<System.Data.Common.DbConnection>();
        _unitOfWorkMock.Setup(x => x.BeginTransactionAsync(It.IsAny<System.Data.IsolationLevel>(), default))
            .ReturnsAsync(Mock.Of<System.Data.Common.DbTransaction>());

        // Act
        var result = await _handler.Handle(new RefreshTokenCommand(rawToken));

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("new-access-token");
        result.RefreshToken.Should().Be("new-raw-token");
    }

    [Fact]
    public async Task Handle_TokenNotFound_ThrowsUnauthorized()
    {
        // Arrange
        _refreshTokenRepoMock.Setup(x => x.GetByTokenHashAsync(It.IsAny<string>(), default))
            .ReturnsAsync((RefreshToken?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(new RefreshTokenCommand("invalid-token")));
    }

    [Fact]
    public async Task Handle_TokenRevoked_ThrowsSecurityException()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = ApplicationUser.Create("Test", "test@test.com", "test");
        var token = RefreshToken.Create(userId, "token", daysToLive: 7);
        token.User = user;
        token.Revoke(); // Mark as revoked

        _refreshTokenRepoMock.Setup(x => x.GetByTokenHashAsync(It.IsAny<string>(), default))
            .ReturnsAsync(token);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(new RefreshTokenCommand("revoked-token")));
    }

    [Fact]
    public async Task Handle_TokenExpired_ThrowsUnauthorized()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = ApplicationUser.Create("Test", "test@test.com", "test");
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = RefreshToken.HashToken("expired-token"),
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired
            User = user
        };

        _refreshTokenRepoMock.Setup(x => x.GetByTokenHashAsync(It.IsAny<string>(), default))
            .ReturnsAsync(token);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(new RefreshTokenCommand("expired-token")));
    }

    [Fact]
    public async Task Handle_UserInactive_ThrowsUnauthorized()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = ApplicationUser.Create("Test", "test@test.com", "test");
        user.Id = userId;
        user.IsActive = false; // Inactive

        var token = RefreshToken.Create(userId, "valid-token", daysToLive: 7);
        token.User = user;

        _refreshTokenRepoMock.Setup(x => x.GetByTokenHashAsync(It.IsAny<string>(), default))
            .ReturnsAsync(token);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(new RefreshTokenCommand("valid-token")));
    }
}
```

- [ ] **Step 7: Run tests**

Run: `dotnet test tests/CulinaryBlog.Application.Tests/Commands/RefreshTokenCommandHandlerTests.cs`
Expected: All tests pass

- [ ] **Step 8: Commit**

```bash
git add src/CulinaryBlog.Application/Commands/Auth/RefreshToken/
git add src/CulinaryBlog.Application/DTOs/Auth/RefreshTokenRequest.cs
git add src/CulinaryBlog.Application/Exceptions/AuthException.cs
git add tests/CulinaryBlog.Application.Tests/Commands/RefreshTokenCommandHandlerTests.cs
git commit -m "feat(auth): add RefreshTokenCommand with token rotation (FR-AUTH-004)"
```

---

## Task 2: Backend - Refresh Endpoint Controller

**Files:**
- Modify: `src/CulinaryBlog.API/Controllers/AuthController.cs`

**Interfaces:**
- Consumes: `RefreshTokenCommand`, `RefreshTokenCommandValidator`
- Produces: `POST /api/v1/auth/refresh` endpoint

**Steps:**

- [ ] **Step 1: Thêm endpoint vào AuthController**

```csharp
// Thêm vào AuthController.cs, sau Login endpoint:

[HttpPost("refresh")]
[EnableRateLimiting("RefreshRateLimit")]
[ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
{
    var command = new RefreshTokenCommand(request.RefreshToken);
    var result = await _mediator.Send(command, cancellationToken);
    return Ok(result);
}
```

- [ ] **Step 2: Thêm import**

```csharp
using CulinaryBlog.Application.Commands.Auth.RefreshToken;
```

- [ ] **Step 3: Thêm rate limiting policy (nếu chưa có trong Program.cs)**

Kiểm tra `Program.cs` và thêm policy nếu cần:

```csharp
// Trong Program.cs, ConfigureServices:
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("RefreshRateLimit", limiterOptions =>
    {
        limiterOptions.PermitLimit = 30;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
    });
});
```

- [ ] **Step 4: Test endpoint**

Start backend: `dotnet run --project src/CulinaryBlog.API`

Test with curl:
```bash
curl -X POST http://localhost:5058/api/v1/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken": "your-valid-refresh-token"}'
```

Expected: 200 OK with new tokens, or 401 if token invalid

- [ ] **Step 5: Commit**

```bash
git add src/CulinaryBlog.API/Controllers/AuthController.cs
git add src/CulinaryBlog.API/Program.cs
git commit -m "feat(auth): add POST /auth/refresh endpoint with rate limiting"
```

---

## Task 3: Frontend - API Layer

**Files:**
- Modify: `frontend/app/lib/api.ts`

**Interfaces:**
- Consumes: `AuthResponse`
- Produces: `refreshToken(token: string): Promise<AuthResponse>`

**Steps:**

- [ ] **Step 1: Thêm refreshToken API function**

```typescript
// Thêm vào frontend/app/lib/api.ts

export async function refreshToken(refreshTokenValue: string): Promise<AuthResponse> {
  const res = await fetch(`${API_BASE}/api/v1/auth/refresh`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ refreshToken: refreshTokenValue }),
  });
  return handleResponse<AuthResponse>(res);
}
```

- [ ] **Step 2: Test build**

Run: `cd frontend && pnpm build`
Expected: Build successful

- [ ] **Step 3: Commit**

```bash
git add frontend/app/lib/api.ts
git commit -m "feat(frontend): add refreshToken API function"
```

---

## Task 4: Frontend - Auth Service Enhancement

**Files:**
- Modify: `frontend/app/lib/auth.ts`

**Interfaces:**
- Produces: `refreshAccessToken(): Promise<boolean>`, `isTokenExpiringSoon(): boolean`

**Steps:**

- [ ] **Step 1: Thêm helper functions**

```typescript
// Thêm vào frontend/app/lib/auth.ts

/**
 * Decode JWT to get expiration time
 * Note: This is a minimal decode, not validation (validation is server-side)
 */
function decodeJwt(token: string): { exp: number } | null {
  if (typeof window === "undefined") return null;
  try {
    const base64Url = token.split(".")[1];
    if (!base64Url) return null;
    const base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/");
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split("")
        .map((c) => "%" + ("00" + c.charCodeAt(0).toString(16)).slice(-2))
        .join("")
    );
    return JSON.parse(jsonPayload);
  } catch {
    return null;
  }
}

/**
 * Check if the current access token is expiring soon (within 1 minute)
 */
export function isTokenExpiringSoon(): boolean {
  const token = getToken();
  if (!token) return true;

  const decoded = decodeJwt(token);
  if (!decoded || !decoded.exp) return true;

  const currentTime = Math.floor(Date.now() / 1000);
  const timeUntilExpiry = decoded.exp - currentTime;

  // Return true if expires within 60 seconds
  return timeUntilExpiry < 60;
}

/**
 * Get token expiration timestamp
 */
export function getTokenExpiry(): Date | null {
  const token = getToken();
  if (!token) return null;

  const decoded = decodeJwt(token);
  if (!decoded || !decoded.exp) return null;

  return new Date(decoded.exp * 1000);
}

/**
 * Refresh the access token using the stored refresh token
 */
export async function refreshAccessToken(): Promise<boolean> {
  if (typeof window === "undefined") return false;

  const refreshTokenValue = getRefreshToken();
  if (!refreshTokenValue) {
    clearAuth();
    return false;
  }

  try {
    const response = await refreshToken(refreshTokenValue);

    // Update stored tokens
    setToken(response.accessToken);
    setRefreshToken(response.refreshToken);

    // Update user if provided
    if (response.user) {
      setStoredUser(response.user);
    }

    return true;
  } catch (error) {
    // Refresh failed, clear auth
    clearAuth();
    return false;
  }
}
```

- [ ] **Step 2: Update import in api.ts if needed**

Ensure `refreshToken` is imported in auth.ts:
```typescript
import { refreshToken } from "./api";
```

- [ ] **Step 3: Test build**

Run: `cd frontend && pnpm build`
Expected: Build successful

- [ ] **Step 4: Commit**

```bash
git add frontend/app/lib/auth.ts
git commit -m "feat(frontend): add token expiry detection and refreshAccessToken"
```

---

## Task 5: Frontend - AppContext Integration

**Files:**
- Modify: `frontend/app/context/AppContext.tsx`

**Interfaces:**
- Consumes: `refreshAccessToken`, `isTokenExpiringSoon`, `getRefreshToken`
- Produces: Updated auth state management

**Steps:**

- [ ] **Step 1: Đọc AppContext hiện tại và thêm auto-refresh logic**

Tìm phần `login` function và thêm logic auto-refresh khi mount. Thêm:

```typescript
// Trong AppProvider component, thêm useEffect để auto-refresh on mount
useEffect(() => {
  const tryAutoRefresh = async () => {
    const storedRefreshToken = getRefreshToken();
    if (storedRefreshToken && isTokenExpiringSoon()) {
      const success = await refreshAccessToken();
      if (success) {
        // Update current user from storage
        const user = getStoredUser();
        if (user) {
          setCurrentUser({
            id: user.id,
            displayName: user.displayName,
            userName: user.userName,
            email: user.email,
            avatarUrl: user.avatarUrl || "...",
            bio: user.bio,
            role: (user.roles?.includes("Author") ? "User" : (user.roles?.[0] as "User" | "Admin")) || "User",
            createdAt: user.createdAt || new Date().toISOString(),
          });
        }
      }
    }
  };

  tryAutoRefresh();
}, []);
```

- [ ] **Step 2: Update imports**

```typescript
import { getRefreshToken, isTokenExpiringSoon, refreshAccessToken, getStoredUser } from "../lib/auth";
```

- [ ] **Step 3: Test build**

Run: `cd frontend && pnpm build`
Expected: Build successful

- [ ] **Step 4: Commit**

```bash
git add frontend/app/context/AppContext.tsx
git commit -m "feat(frontend): add auto-refresh on app mount"
```

---

## Task 6: Integration Tests

**Files:**
- Modify: `tests/CulinaryBlog.Integration.Tests/AuthControllerTests.cs`

**Steps:**

- [ ] **Step 1: Thêm integration tests cho refresh endpoint**

```csharp
// Thêm vào AuthControllerTests.cs

[Fact]
public async Task Refresh_ValidToken_ReturnsNewTokens()
{
    // Arrange - Login first to get tokens
    var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new
    {
        email = _testEmail,
        password = _testPassword
    });

    var loginContent = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
    var refreshToken = loginContent!.RefreshToken;

    // Act
    var refreshResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new
    {
        refreshToken = refreshToken
    });

    // Assert
    Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
    var result = await refreshResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
    Assert.NotNull(result);
    Assert.NotEmpty(result.AccessToken);
    Assert.NotEmpty(result.RefreshToken);
    Assert.NotEqual(refreshToken, result.RefreshToken); // Token should be rotated
}

[Fact]
public async Task Refresh_InvalidToken_ReturnsUnauthorized()
{
    // Act
    var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new
    {
        refreshToken = "invalid-token"
    });

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}

[Fact]
public async Task Refresh_TokenRotation_OldTokenInvalidated()
{
    // Arrange
    var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new
    {
        email = _testEmail,
        password = _testPassword
    });
    var loginContent = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
    var originalRefreshToken = loginContent!.RefreshToken;

    // First refresh
    var firstRefresh = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new
    {
        refreshToken = originalRefreshToken
    });
    Assert.Equal(HttpStatusCode.OK, firstRefresh.StatusCode);

    // Act - Try to use original token again (should fail - reuse attack)
    var secondRefresh = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new
    {
        refreshToken = originalRefreshToken
    });

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, secondRefresh.StatusCode);
}
```

- [ ] **Step 2: Run integration tests**

Run: `dotnet test tests/CulinaryBlog.Integration.Tests/AuthControllerTests.cs`
Expected: All refresh tests pass

- [ ] **Step 3: Commit**

```bash
git add tests/CulinaryBlog.Integration.Tests/AuthControllerTests.cs
git commit -m "test: add integration tests for refresh endpoint (FR-AUTH-004)"
```

---

## Task 7: Manual End-User Test Scenarios

**Files:**
- Create: `docs/specs/fr-auth/FR-AUTH-004_EndUser_Test_Scenarios.md`

**Content:**

```markdown
# FR-AUTH-004: Token Refresh - End-User Test Scenarios

## Prerequisites
- Backend running at http://localhost:5058
- Frontend running at http://localhost:3000
- Test user account registered and logged in

---

## TC001: Token Refresh - Successful Refresh

**Objective:** Verify user can refresh tokens when access token is about to expire

**Pre-conditions:**
- User is logged in
- Store current access token and refresh token

**Test Steps:**
1. Wait for access token to approach expiration (or decode JWT to see exp claim)
2. Call API to refresh: `POST /api/v1/auth/refresh` with `{ "refreshToken": "<current_refresh_token>" }`
3. Observe response

**Expected Results:**
- HTTP 200 OK
- Response contains new `accessToken` (15 min validity)
- Response contains new `refreshToken` (different from old one - rotation)
- Response contains `expiresAt` timestamp
- Response contains `user` object with correct user info

**Pass Criteria:** All expected results met

---

## TC002: Token Rotation - Old Token Invalidated

**Objective:** Verify old refresh token is invalidated after refresh (token rotation)

**Pre-conditions:**
- User has performed a successful token refresh (TC001)

**Test Steps:**
1. Call API to refresh using the **old** refresh token (from before TC001)
2. Observe response

**Expected Results:**
- HTTP 401 Unauthorized
- Error message indicates token reuse detected

**Pass Criteria:** Old token correctly rejected

---

## TC003: Invalid Refresh Token

**Objective:** Verify system rejects invalid refresh tokens

**Test Steps:**
1. Call API to refresh with: `{ "refreshToken": "completely-invalid-token" }`
2. Observe response

**Expected Results:**
- HTTP 401 Unauthorized
- Appropriate error message

**Pass Criteria:** Invalid token rejected

---

## TC004: Expired Refresh Token

**Objective:** Verify system rejects expired refresh tokens

**Pre-conditions:**
- Have a refresh token that has passed its expiration (7+ days old)

**Test Steps:**
1. Call API to refresh with expired token
2. Observe response

**Expected Results:**
- HTTP 401 Unauthorized
- Appropriate error message indicating token expired

**Pass Criteria:** Expired token rejected

---

## TC005: Rate Limiting

**Objective:** Verify refresh endpoint respects rate limits

**Test Steps:**
1. Make 30+ rapid refresh requests in 1 minute
2. Observe response after limit exceeded

**Expected Results:**
- First 30 requests: HTTP 200 (successful refresh)
- 31st+ request: HTTP 429 Too Many Requests
- Response includes `Retry-After` header

**Pass Criteria:** Rate limiting enforced

---

## TC006: Frontend Auto-Refresh on Page Load

**Objective:** Verify frontend auto-refreshes token when page is refreshed

**Pre-conditions:**
- User is logged in
- Access token is about to expire

**Test Steps:**
1. Open browser DevTools > Application > Local Storage
2. Note current `auth_refresh_token` value
3. Wait for token to approach expiration
4. Refresh the page (F5)
5. Check Local Storage for new token values

**Expected Results:**
- Page loads successfully without login prompt
- `auth_access_token` is updated (new token)
- `auth_refresh_token` is updated (rotated)
- No login modal appears

**Pass Criteria:** Auto-refresh works on page load

---

## TC007: Frontend Manual Refresh

**Objective:** Verify frontend exposes manual refresh capability

**Test Steps:**
1. User is logged in
2. Trigger manual refresh (if exposed in UI, e.g., a refresh button or API call)
3. Check Local Storage for updated tokens

**Expected Results:**
- `auth_access_token` updated
- `auth_refresh_token` updated (rotated)
- User remains logged in

**Pass Criteria:** Manual refresh works

---

## TC008: Session Persistence After Token Expiry

**Objective:** Verify user stays logged in after token expiry via refresh

**Pre-conditions:**
- User is logged in

**Test Steps:**
1. Wait for access token to expire (15 minutes)
2. On the frontend, make a protected API call
3. Observe if system auto-refreshes and completes the request

**Expected Results:**
- Request succeeds (auto-refresh triggered)
- User remains logged in
- No login prompt appears

**Pass Criteria:** Session persists via refresh

---

## TC009: Login Required After All Tokens Expired

**Objective:** Verify user must re-login when refresh token also expires

**Pre-conditions:**
- User has been inactive for 7+ days (or manually clear tokens)

**Test Steps:**
1. Clear all auth data from Local Storage
2. Attempt to access protected route

**Expected Results:**
- Login/register page appears
- User must authenticate again

**Pass Criteria:** Correctly requires re-authentication

---

## TC010: Concurrent Refresh Requests

**Objective:** Verify system handles concurrent refresh requests correctly

**Test Steps:**
1. User is logged in
2. Send multiple refresh requests simultaneously (e.g., via Postman or curl with `&` background jobs)
3. Observe responses

**Expected Results:**
- Only one refresh succeeds
- Other requests return 401 (token already rotated)
- No errors or data corruption

**Pass Criteria:** Concurrent requests handled safely

---

## Summary

| Test Case | Description | Priority |
|-----------|-------------|----------|
| TC001 | Successful token refresh | P0 |
| TC002 | Token rotation (old invalidated) | P0 |
| TC003 | Invalid token rejection | P0 |
| TC004 | Expired token rejection | P1 |
| TC005 | Rate limiting | P1 |
| TC006 | Frontend auto-refresh | P0 |
| TC007 | Frontend manual refresh | P1 |
| TC008 | Session persistence | P0 |
| TC009 | Re-login after expiry | P1 |
| TC010 | Concurrent requests | P2 |
```

- [ ] **Step 2: Commit**

```bash
git add docs/specs/fr-auth/FR-AUTH-004_EndUser_Test_Scenarios.md
git commit -m "docs: add FR-AUTH-004 end-user test scenarios"
```

---

## Task 8: Final Verification

**Steps:**

- [ ] **Step 1: Run all tests**

```bash
dotnet test tests/CulinaryBlog.Application.Tests/
dotnet test tests/CulinaryBlog.Integration.Tests/
cd frontend && pnpm build
```

- [ ] **Step 2: Manual smoke test**

1. Start backend: `dotnet run --project src/CulinaryBlog.API`
2. Start frontend: `cd frontend && pnpm dev`
3. Register/login a test user
4. Test refresh endpoint via curl or Postman
5. Test frontend refresh on page load

- [ ] **Step 3: Commit final changes**

```bash
git add -A
git commit -m "feat(auth): complete FR-AUTH-004 token refresh implementation"
```

---

## Summary

| Task | Description | Files Changed |
|------|-------------|---------------|
| 1 | Backend Command/Handler | 5 new files |
| 2 | Controller Endpoint | 1 modified file |
| 3 | Frontend API | 1 modified file |
| 4 | Auth Service | 1 modified file |
| 5 | AppContext | 1 modified file |
| 6 | Integration Tests | 1 modified file |
| 7 | E2E Test Scenarios | 1 new file |
| 8 | Final Verification | - |

---

## Post-Implementation Notes

- **Security**: Token reuse detection logs at WARNING level. Consider adding alerting for production.
- **Scalability**: Current implementation uses Serializable isolation. For high-scale, consider optimistic concurrency.
- **Frontend**: Auto-refresh logic in AppContext covers most cases. Consider adding explicit refresh button for power users.
