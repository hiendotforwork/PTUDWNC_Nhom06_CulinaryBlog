# FR-AUTH-003: Đăng nhập bằng Google OAuth 2.0 - Kế hoạch triển khai

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Triển khai chức năng đăng nhập/đăng ký bằng Google OAuth 2.0 sử dụng ASP.NET Core Google Authentication middleware, đảm bảo consistency với FR-AUTH-002 (đăng nhập email/password).

**Architecture:** Sử dụng ASP.NET Core Authentication middleware với Google provider. Backend xử lý toàn bộ OAuth flow: redirect đến Google → callback → verify → generate JWT tokens → redirect về frontend dashboard. Không sử dụng next-auth (giữ nguyên localStorage approach hiện tại).

**Tech Stack:**
- Backend: ASP.NET Core 10, `Microsoft.AspNetCore.Authentication.Google`
- Frontend: Next.js 16, React 19, TypeScript (giữ nguyên current approach)
- Protocol: OAuth 2.0 Authorization Code Flow

**Spec:** `docs/specs/SRS_Culinary_Blog_v1.0.0.md` (FR-AUTH-003, lines 845-901)

---

## Global Constraints

| Constraint | Value |
|------------|-------|
| .NET Version | .NET 10 |
| Frontend | Next.js 16, React 19, TypeScript |
| API Base URL | http://localhost:5058 |
| Frontend URL | http://localhost:3000 |
| JWT TTL | 15 phút |
| Refresh Token TTL | 7 ngày |
| Default User Role | Author |
| Language for commits | Tiếng Việt |

---

## Review Focus

1. **Token reuse attack:** Refresh token cũ phải bị revoke sau khi dùng (token rotation)
2. **User enumeration:** Không tiết lộ email có tồn tại hay không trong error message
3. **OAuth token validation:** Verify Google token trước khi tạo/link account
4. **Account linking:** Email đã tồn tại từ local registration phải link được với Google login
5. **Redirect URI security:** Chỉ redirect về frontend dashboard sau khi auth thành công

---

## File Structure

```
Backend (src/):
├── CulinaryBlog.API/
│   ├── Program.cs                          # Cấu hình Google OAuth middleware
│   ├── Controllers/
│   │   └── AuthController.cs              # Thêm endpoint external auth
│   └── appsettings.Development.json       # Thêm Google credentials
│
├── CulinaryBlog.Application/
│   └── Commands/Auth/
│       └── GoogleLogin/                   # [TẠO MỚI]
│           ├── GoogleLoginCommand.cs
│           └── GoogleLoginCommandHandler.cs
│
└── CulinaryBlog.Infrastructure/
    └── Services/
        └── UserRepository.cs              # Thêm method FindByLoginAsync

Frontend (frontend/):
├── app/
│   ├── login/page.tsx                     # Sửa: implement real Google redirect
│   ├── register/page.tsx                 # Sửa: implement real Google redirect
│   ├── auth/
│   │   └── google-callback/page.tsx      # [TẠO MỚI] Callback handler
│   └── context/
│       └── AppContext.tsx                # Thêm handleGoogleCallback
```

---

## Task 1: Cài đặt NuGet Package và Cấu hình appsettings

**Files:**
- Modify: `src/CulinaryBlog.API/CulinaryBlog.API.csproj`
- Modify: `src/CulinaryBlog.API/appsettings.Development.json`

**Interfaces:**
- Produces: Package `Microsoft.AspNetCore.Authentication.Google` được thêm vào project

- [ ] **Step 1: Thêm NuGet package Google Authentication**

```bash
cd /media/thanhhien/DATA/PTUDWNC_Nhom06_CulinaryBlog/src/CulinaryBlog.API
dotnet add package Microsoft.AspNetCore.Authentication.Google --version 10.0.0
```

- [ ] **Step 2: Cập nhật appsettings.Development.json**

Thêm section `Authentication:Google`:

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com",
      "ClientSecret": "GOCSPX-YOUR_SECRET"
    }
  }
}
```

> **Lưu ý:** Thay thế placeholder bằng credentials thực từ Google Cloud Console.

- [ ] **Step 3: Commit**

```bash
git add src/CulinaryBlog.API/CulinaryBlog.API.csproj src/CulinaryBlog.API/appsettings.Development.json
git commit -m "feat(backend): thêm package và cấu hình Google OAuth

- Cài đặt Microsoft.AspNetCore.Authentication.Google 10.0.0
- Thêm Google ClientId và ClientSecret vào appsettings.Development.json
- Credentials cần được thay thế bằng giá trị thực từ Google Cloud Console"
```

---

## Task 2: Cấu hình Google Authentication Middleware trong Program.cs

**Files:**
- Modify: `src/CulinaryBlog.API/Program.cs:1-50` (phần imports và service configuration)
- Modify: `src/CulinaryBlog.API/Program.cs:102-121` (phần AddAuthentication)

**Interfaces:**
- Consumes: Package `Microsoft.AspNetCore.Authentication.Google`
- Produces: Google OAuth authentication scheme được đăng ký

- [ ] **Step 1: Thêm imports cần thiết**

```csharp
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
```

- [ ] **Step 2: Thêm Google authentication scheme vào AddAuthentication**

Thay thế đoạn code hiện tại (dòng 102-121):

```csharp
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ClockSkew = TimeSpan.Zero
    };
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
    options.CallbackPath = "/api/v1/auth/google-callback";
    options.SaveTokens = true;
    options.Events.OnCreatingTicket = context =>
    {
        // Log token for debugging if needed
        return Task.CompletedTask;
    };
});
```

- [ ] **Step 3: Commit**

```bash
git add src/CulinaryBlog.API/Program.cs
git commit -m "feat(backend): cấu hình Google OAuth middleware trong Program.cs

- Thêm Google authentication scheme với ClientId và ClientSecret từ config
- Callback path: /api/v1/auth/google-callback
- Lưu tokens để sử dụng trong callback handler"
```

---

## Task 3: Mở rộng UserRepository với External Login Methods

> **Lưu ý:** Task này đi trước Task 5 (GoogleLoginCommand) vì Handler phụ thuộc vào các method mới.

**Files:**
- Modify: `src/CulinaryBlog.Infrastructure/Services/UserRepository.cs`
- Modify: `src/CulinaryBlog.Infrastructure/Services/IUserRepository.cs` (interface)

**Interfaces:**
- Consumes: `ApplicationUser`
- Produces: Method `FindByLoginAsync`, `AddLoginAsync`

- [ ] **Step 1: Thêm method FindByLoginAsync**

Thêm vào interface `IUserRepository`:

```csharp
Task<ApplicationUser?> FindByLoginAsync(string provider, string providerKey, CancellationToken cancellationToken = default);
```

Thêm implementation trong `UserRepository`:

```csharp
public async Task<ApplicationUser?> FindByLoginAsync(string provider, string providerKey, CancellationToken cancellationToken = default)
{
    var login = await _userManager.FindByLoginAsync(provider, providerKey);
    if (login == null) return null;
    return await _userManager.FindByIdAsync(login.UserId);
}
```

- [ ] **Step 2: Thêm method AddLoginAsync**

Thêm vào interface `IUserRepository`:

```csharp
Task<IdentityResult> AddLoginAsync(ApplicationUser user, string provider, string providerKey, string? displayName, string? avatarUrl);
```

Thêm implementation trong `UserRepository`:

```csharp
public async Task<IdentityResult> AddLoginAsync(ApplicationUser user, string provider, string providerKey, string? displayName, string? avatarUrl)
{
    // Update avatar if provided and user doesn't have one
    if (!string.IsNullOrEmpty(avatarUrl) && string.IsNullOrEmpty(user.AvatarUrl))
    {
        user.AvatarUrl = avatarUrl;
        await _userManager.UpdateAsync(user);
    }

    var loginInfo = new UserLoginInfo(provider, providerKey, displayName ?? provider);
    return await _userManager.AddLoginAsync(user, loginInfo);
}
```

- [ ] **Step 3: Commit**

```bash
git add src/CulinaryBlog.Infrastructure/Services/UserRepository.cs
git commit -m "feat(backend): thêm method xử lý external login trong UserRepository

- FindByLoginAsync: tìm user theo provider và providerKey
- AddLoginAsync: liên kết external login với user hiện có"
```

---

## Task 4: Tạo GoogleLoginCommand và Handler

**Files:**
- Create: `src/CulinaryBlog.Application/Commands/Auth/GoogleLogin/GoogleLoginCommand.cs`
- Create: `src/CulinaryBlog.Application/Commands/Auth/GoogleLogin/GoogleLoginCommandHandler.cs`

**Interfaces:**
- Consumes: `IUserRepository`, `ITokenService`, `IRefreshTokenRepository`, `IUnitOfWork`
- Produces: `AuthResponse` (accessToken, refreshToken, expiresAt, user)

- [ ] **Step 1: Tạo GoogleLoginCommand**

```csharp
namespace CulinaryBlog.Application.Commands.Auth.GoogleLogin;

using CulinaryBlog.Application.DTOs.Auth;
using MediatR;

public record GoogleLoginCommand(
    string Provider,
    string ProviderKey,
    string Email,
    string DisplayName,
    string? AvatarUrl
) : IRequest<AuthResponse>;
```

- [ ] **Step 2: Tạo GoogleLoginCommandHandler**

```csharp
namespace CulinaryBlog.Application.Commands.Auth.GoogleLogin;

using CulinaryBlog.Application.DTOs.Auth;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using MediatR;

public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly string _defaultRole = "Author";

    public GoogleLoginCommandHandler(
        IUserRepository userRepository,
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponse> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
    {
        // 1. Check if user exists with this external login
        var existingUser = await _userRepository.FindByLoginAsync(request.Provider, request.ProviderKey, cancellationToken);

        // 2. If not found by login, check by email
        if (existingUser == null)
        {
            existingUser = await _userRepository.FindByEmailAsync(request.Email, cancellationToken);

            if (existingUser != null)
            {
                // Link Google login to existing account
                await _userRepository.AddLoginAsync(
                    existingUser,
                    request.Provider,
                    request.ProviderKey,
                    request.DisplayName,
                    request.AvatarUrl
                );
            }
        }

        // 3. If still not found, create new user
        if (existingUser == null)
        {
            // Generate username from email (before @)
            var userName = GenerateUserName(request.Email);

            // Create new user
            existingUser = ApplicationUser.Create(request.DisplayName, request.Email, userName);

            if (!string.IsNullOrEmpty(request.AvatarUrl))
            {
                existingUser.AvatarUrl = request.AvatarUrl;
            }

            var createResult = await _userRepository.CreateAsync(existingUser, generatePassword: true);

            if (!createResult.Succeeded)
            {
                throw AuthException.InvalidCredentials();
            }

            // Add to Author role
            await _userRepository.AddToRoleAsync(existingUser, _defaultRole);

            // Link external login
            await _userRepository.AddLoginAsync(
                existingUser,
                request.Provider,
                request.ProviderKey,
                request.DisplayName,
                request.AvatarUrl
            );
        }

        // 4. Check if user is active
        if (!existingUser.IsActive)
        {
            throw AuthException.AccountLocked(DateTime.UtcNow.AddDays(30));
        }

        // 5. Get user roles
        var roles = await _userRepository.GetRolesAsync(existingUser);

        // 6. Begin transaction for token rotation
        await using var transaction = await _unitOfWork.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);

        try
        {
            // 7. Revoke all existing refresh tokens
            await _refreshTokenRepository.RevokeByUserIdAsync(existingUser.Id, cancellationToken);

            // 8. Generate new tokens
            var accessToken = _tokenService.GenerateAccessToken(existingUser, roles.ToList());
            var rawRefreshToken = _tokenService.GenerateRefreshToken();

            var refreshTokenEntity = RefreshToken.Create(
                existingUser.Id,
                rawRefreshToken,
                ipAddress: null,
                daysToLive: 7
            );

            // 9. Persist refresh token
            await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);
            await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

            // 10. Commit transaction
            await transaction.CommitAsync(cancellationToken);

            // 11. Return AuthResponse
            var userDto = new UserDto(
                existingUser.Id,
                existingUser.Email ?? request.Email,
                existingUser.UserName ?? string.Empty,
                existingUser.DisplayName,
                existingUser.AvatarUrl,
                existingUser.Bio,
                roles.ToList()
            );

            return new AuthResponse(
                accessToken,
                rawRefreshToken,
                DateTime.UtcNow.AddMinutes(15),
                userDto
            );
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static string GenerateUserName(string email)
    {
        // Get part before @, replace invalid characters
        var userName = email.Split('@')[0];
        userName = new string(userName
            .Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-')
            .ToArray());

        // Ensure not empty
        if (string.IsNullOrEmpty(userName))
        {
            userName = "user";
        }

        // Ensure minimum length
        if (userName.Length < 3)
        {
            userName = userName + "123";
        }

        return userName.ToLowerInvariant();
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add src/CulinaryBlog.Application/Commands/Auth/GoogleLogin/GoogleLoginCommand.cs src/CulinaryBlog.Application/Commands/Auth/GoogleLogin/GoogleLoginCommandHandler.cs
git commit -m "feat(backend): tạo GoogleLoginCommand và Handler cho OAuth callback

- Xử lý 3 trường hợp: link login, link với email tồn tại, tạo user mới
- Tạo user với role Author mặc định
- Token rotation: revoke tokens cũ, tạo tokens mới
- Transaction với Serializable isolation level"
```

---

## Task 5: Tạo API Endpoint cho External Login Callback

**Files:**
- Modify: `src/CulinaryBlog.API/Controllers/AuthController.cs`

**Interfaces:**
- Consumes: `GoogleLoginCommand`, `IMediator`
- Produces: Redirect về frontend với tokens trong query string hoặc cookie

- [ ] **Step 1: Thêm imports vào AuthController.cs**

```csharp
using CulinaryBlog.Application.Commands.Auth.GoogleLogin;
using System.Security.Claims;
```

- [ ] **Step 2: Thêm endpoint Google callback**

Thêm vào cuối class `AuthController`:

```csharp
[HttpGet("google-signin")]
public IActionResult GoogleSignIn([FromQuery] string? returnUrl = null)
{
    var redirectUrl = Url.Action(nameof(GoogleCallback), "Auth", new { returnUrl });
    var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
    return Challenge(properties, "Google");
}

[HttpGet("google-callback")]
public async Task<IActionResult> GoogleCallback([FromQuery] string? returnUrl = null, CancellationToken cancellationToken = default)
{
    try
    {
        // Authenticate with Google (handled by middleware)
        var authenticateResult = await HttpContext.AuthenticateAsync("Google");

        if (!authenticateResult.Succeeded)
        {
            // Log error for debugging
            var error = authenticateResult.Failure?.Message ?? "Google authentication failed";
            return Redirect($"/login?error={Uri.EscapeDataString(error)}");
        }

        // Extract Google user information
        var claims = authenticateResult.Principal?.Claims;
        var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var displayName = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
                          ?? claims?.FirstOrDefault(c => c.Type == "name")?.Value
                          ?? "Google User";
        var avatarUrl = claims?.FirstOrDefault(c => c.Type == "picture")?.Value;
        var providerKey = claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(providerKey))
        {
            return Redirect($"/login?error={Uri.EscapeDataString("Không thể lấy thông tin từ Google.")}");
        }

        // Process Google login
        var command = new GoogleLoginCommand(
            Provider: "Google",
            ProviderKey: providerKey,
            Email: email,
            DisplayName: displayName,
            AvatarUrl: avatarUrl
        );

        var authResponse = await _mediator.Send(command, cancellationToken);

        // Redirect to frontend with tokens
        // Using URL fragment (#) is more secure for tokens
        var frontendUrl = returnUrl ?? "/dashboard";
        var redirectUrl = $"http://localhost:3000/auth/google-callback" +
            $"?accessToken={Uri.EscapeDataString(authResponse.AccessToken)}" +
            $"&refreshToken={Uri.EscapeDataString(authResponse.RefreshToken)}" +
            $"&expiresAt={Uri.EscapeDataString(authResponse.ExpiresAt.ToString("O"))}" +
            $"&userId={Uri.EscapeDataString(authResponse.User.Id)}" +
            $"&displayName={Uri.EscapeDataString(authResponse.User.DisplayName)}" +
            $"&email={Uri.EscapeDataString(authResponse.User.Email)}" +
            $"&avatarUrl={Uri.EscapeDataString(authResponse.User.AvatarUrl ?? "")}" +
            $"&roles={Uri.EscapeDataString(string.Join(",", authResponse.User.Roles))}";

        return Redirect(redirectUrl);
    }
    catch (Exception ex)
    {
        // Log exception
        return Redirect($"/login?error={Uri.EscapeDataString("Đăng nhập Google thất bại. Vui lòng thử lại.")}");
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add src/CulinaryBlog.API/Controllers/AuthController.cs
git commit -m "feat(backend): thêm endpoint Google OAuth callback

- GET /api/v1/auth/google-signin: redirect đến Google consent page
- GET /api/v1/auth/google-callback: xử lý callback, tạo tokens, redirect về frontend
- Sử dụng URL fragment để truyền tokens an toàn"
```

---

## Task 6: Tạo Google Callback Page trên Frontend

**Files:**
- Create: `frontend/app/auth/google-callback/page.tsx`

**Interfaces:**
- Consumes: URL query params (accessToken, refreshToken, user info)
- Produces: Store auth data, redirect to dashboard

- [ ] **Step 1: Tạo Google callback page**

```tsx
"use client";

import { useEffect, useRef } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { useApp } from "../../context/AppContext";
import { Loader2 } from "lucide-react";

export default function GoogleCallbackPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { loginWithExternalToken, showToast } = useApp();
  const hasProcessed = useRef(false);

  useEffect(() => {
    if (hasProcessed.current) return;
    hasProcessed.current = true;

    const processCallback = async () => {
      // Check for error from backend
      const error = searchParams.get("error");
      if (error) {
        showToast("error", `Đăng nhập Google thất bại: ${error}`);
        router.push("/login");
        return;
      }

      // Extract tokens and user info from URL
      const accessToken = searchParams.get("accessToken");
      const refreshToken = searchParams.get("refreshToken");
      const expiresAt = searchParams.get("expiresAt");
      const userId = searchParams.get("userId");
      const displayName = searchParams.get("displayName");
      const email = searchParams.get("email");
      const avatarUrl = searchParams.get("avatarUrl");
      const roles = searchParams.get("roles");

      if (!accessToken || !refreshToken || !userId) {
        showToast("error", "Đăng nhập Google thất bại: Thiếu thông tin xác thực.");
        router.push("/login");
        return;
      }

      // Build user object
      const user = {
        id: userId,
        displayName: displayName || "User",
        userName: email?.split("@")[0] || "user",
        email: email || "",
        avatarUrl: avatarUrl || undefined,
        bio: undefined,
        roles: roles ? roles.split(",") : ["Author"],
      };

      // Login with tokens
      loginWithExternalToken(accessToken, refreshToken, expiresAt || "", user);

      // Redirect to dashboard
      showToast("success", `Chào mừng ${user.displayName}! Đăng nhập thành công.`);
      router.push("/dashboard");
    };

    processCallback();
  }, [searchParams, router, showToast, loginWithExternalToken]);

  return (
    <div className="min-h-[80dvh] flex items-center justify-center">
      <div className="flex flex-col items-center gap-4">
        <Loader2 className="w-8 h-8 animate-spin text-[#C98F7D]" />
        <p className="text-[#8A817C]">Đang xử lý đăng nhập Google...</p>
      </div>
    </div>
  );
}
```

- [ ] **Step 2: Commit**

```bash
git add frontend/app/auth/google-callback/page.tsx
git commit -m "feat(frontend): tạo Google callback page

- Xử lý tokens từ URL query params
- Lưu tokens và user info vào localStorage
- Redirect về dashboard sau khi đăng nhập thành công"
```

---

## Task 7: Cập nhật AppContext với loginWithExternalToken

**Files:**
- Modify: `frontend/app/context/AppContext.tsx`

**Interfaces:**
- Consumes: accessToken, refreshToken, expiresAt, user object
- Produces: Updated auth state

- [ ] **Step 1: Thêm loginWithExternalToken method**

Thêm method vào context:

```tsx
interface AppContextType {
  // ... existing methods
  loginWithExternalToken: (
    accessToken: string,
    refreshToken: string,
    expiresAt: string,
    user: AuthUser
  ) => void;
}

// Implementation
const loginWithExternalToken = (
  accessToken: string,
  refreshToken: string,
  expiresAt: string,
  user: AuthUser
) => {
  setToken(accessToken);
  setRefreshToken(refreshToken);
  setCurrentUser(user);
  setStoredUser(user);
};
```

- [ ] **Step 2: Commit**

```bash
git add frontend/app/context/AppContext.tsx
git commit -m "feat(frontend): thêm loginWithExternalToken vào AppContext

- Method để xử lý đăng nhập từ external provider (Google)
- Lưu tokens và user info vào state và localStorage"
```

---

## Task 8: Cập nhật Login Page với Real Google Redirect

**Files:**
- Modify: `frontend/app/login/page.tsx`

**Interfaces:**
- Consumes: API_BASE, redirect URL
- Produces: Redirect to backend Google OAuth endpoint

- [ ] **Step 1: Cập nhật handleGoogleLogin function**

```tsx
const handleGoogleLogin = () => {
  // Redirect to backend Google OAuth endpoint
  const apiBase = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5058";
  const returnUrl = encodeURIComponent("/dashboard");
  window.location.href = `${apiBase}/api/v1/auth/google-signin?returnUrl=${returnUrl}`;
};
```

- [ ] **Step 2: Commit**

```bash
git add frontend/app/login/page.tsx
git commit -m "feat(frontend): implement real Google login redirect trên login page

- Redirect đến /api/v1/auth/google-signin trên backend
- Backend sẽ xử lý OAuth flow và redirect về frontend callback page"
```

---

## Task 9: Cập nhật Register Page với Real Google Redirect

**Files:**
- Modify: `frontend/app/register/page.tsx`

**Interfaces:**
- Consumes: API_BASE, redirect URL
- Produces: Redirect to backend Google OAuth endpoint

- [ ] **Step 1: Tìm và cập nhật handleGoogleSignup function**

```tsx
const handleGoogleSignup = () => {
  // Redirect to backend Google OAuth endpoint
  const apiBase = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5058";
  const returnUrl = encodeURIComponent("/dashboard");
  window.location.href = `${apiBase}/api/v1/auth/google-signin?returnUrl=${returnUrl}`;
};
```

- [ ] **Step 2: Commit**

```bash
git add frontend/app/register/page.tsx
git commit -m "feat(frontend): implement real Google signup redirect trên register page

- Redirect đến /api/v1/auth/google-signin trên backend
- Backend sẽ tự động tạo account mới nếu email chưa tồn tại"
```

---

## Task 10: Thêm User Secrets cho Development

**Files:**
- Modify: `~/.microsoft/usersecrets/<project-id>/secrets.json` (hoặc dùng dotnet user-secrets)

**Interfaces:**
- Produces: Google credentials available via configuration

- [ ] **Step 1: Set user secrets cho development**

```bash
cd /media/thanhhien/DATA/PTUDWNC_Nhom06_CulinaryBlog/src/CulinaryBlog.API
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_CLIENT_ID.apps.googleusercontent.com"
dotnet user-secrets set "Authentication:Google:ClientSecret" "GOCSPX-YOUR_SECRET"
```

> **Lưu ý:** Thay thế bằng credentials thực từ Google Cloud Console.

- [ ] **Step 2: Commit**

```bash
git add src/CulinaryBlog.API/appsettings.Development.json
git commit -m "chore: thêm placeholder credentials cho Google OAuth trong appsettings

- Thêm Authentication:Google section với placeholder values
- Cần thay thế bằng credentials thực từ Google Cloud Console
- Khuyến nghị dùng dotnet user-secrets cho development"
```

---

## Task 11: Hướng dẫn tạo Google Cloud Console Credentials

**Files:**
- Create: `docs/specs/fr-auth/FR-AUTH-003/google-oauth-setup.md`

**Interfaces:**
- Produces: Documentation cho việc tạo OAuth credentials

- [ ] **Step 1: Tạo documentation file**

```markdown
# Hướng dẫn tạo Google OAuth 2.0 Credentials

## Bước 1: Tạo Project trên Google Cloud Console

1. Truy cập [Google Cloud Console](https://console.cloud.google.com/)
2. Tạo project mới hoặc chọn project hiện có
3. Đặt tên project: "Culinary Blog"

## Bước 2: Enable Google+ API

1. Vào **APIs & Services** > **Library**
2. Tìm "Google+ API"
3. Click **Enable**

## Bước 3: Configure OAuth Consent Screen

1. Vào **APIs & Services** > **OAuth consent screen**
2. Chọn **External**
3. Điền thông tin:
   - App name: Culinary Blog
   - User support email: your-email@gmail.com
   - Developer contact: your-email@gmail.com
4. Click **Save and Continue**

## Bước 4: Tạo OAuth 2.0 Credentials

1. Vào **APIs & Services** > **Credentials**
2. Click **Create Credentials** > **OAuth client ID**
3. Application type: **Web application**
4. Name: "Culinary Blog Web Client"
5. Authorized redirect URIs:
   - Development: `http://localhost:5058/api/v1/auth/google-callback`
   - Production: `https://api.culinaryblog.com/api/v1/auth/google-callback`
6. Click **Create**
7. Copy **Client ID** và **Client Secret**

## Bước 5: Cấu hình trong ứng dụng

### Development (dotnet user-secrets)
```bash
cd src/CulinaryBlog.API
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_CLIENT_ID.apps.googleusercontent.com"
dotnet user-secrets set "Authentication:Google:ClientSecret" "GOCSPX-YOUR_SECRET"
```

### Production (Environment Variables)
```bash
export Authentication__Google__ClientId="YOUR_CLIENT_ID.apps.googleusercontent.com"
export Authentication__Google__ClientSecret="GOCSPX-YOUR_SECRET"
```

## Troubleshooting

### Lỗi "invalid_client"
- Kiểm tra Client ID và Client Secret đã đúng chưa
- Đảm bảo đã enable Google+ API

### Lỗi "redirect_uri_mismatch"
- Kiểm tra redirect URI trong Google Cloud Console khớp với callback URL
- Development: `http://localhost:5058/api/v1/auth/google-callback`

### Lỗi "access_denied"
- User đã từ chối cấp quyền
- Thử đăng nhập lại
```

- [ ] **Step 2: Commit**

```bash
git add docs/specs/fr-auth/FR-AUTH-003/google-oauth-setup.md
git commit -m "docs: thêm hướng dẫn tạo Google OAuth credentials

- Chi tiết các bước tạo project trên Google Cloud Console
- Cách enable Google+ API và tạo OAuth credentials
- Cấu hình redirect URIs cho development và production"
```

---

## Task 12: Kiểm thử End-to-End

**Files:**
- Test: Manual testing checklist

**Interfaces:**
- Consumes: Running backend và frontend
- Produces: Verified functionality

- [ ] **Step 1: Kiểm thử Happy Path**

1. Mở trình duyệt ở `http://localhost:3000/login`
2. Click "Đăng nhập bằng Google"
3. Redirect đến Google consent page
4. Đăng nhập và cấp quyền
5. Redirect về `/auth/google-callback`
6. Tokens được lưu, redirect đến `/dashboard`
7. Kiểm tra user info hiển thị đúng

- [ ] **Step 2: Kiểm thử New User Creation**

1. Đăng nhập với Google account mới (chưa có trong hệ thống)
2. Verify account được tạo tự động
3. Verify role "Author" được gán
4. Verify Avatar URL từ Google được lưu

- [ ] **Step 3: Kiểm thử Account Linking**

1. Đăng ký account local với email X
2. Logout
3. Đăng nhập Google với cùng email X
4. Verify: Login thành công với account đã tồn tại
5. Verify: Không tạo account mới

- [ ] **Step 4: Kiểm thử Error Handling**

1. Test với Google account không có email
2. Test với credentials sai
3. Verify error messages hiển thị đúng

- [ ] **Step 5: Commit test results**

```bash
git commit --allow-empty -m "test(fr-auth-003): kiểm thử Google OAuth end-to-end

- Happy path: đăng nhập thành công
- New user: tạo account tự động với role Author
- Account linking: link Google với account local
- Error handling: các trường hợp lỗi được xử lý đúng"
```

---

## Summary

| Task | Description | Files Modified/Created |
|------|-------------|----------------------|
| 1 | Cài đặt NuGet Package | `*.csproj`, `appsettings.Development.json` |
| 2 | Cấu hình Google Middleware | `Program.cs` |
| 3 | Mở rộng UserRepository | `UserRepository.cs` |
| 4 | Tạo GoogleLoginCommand/Handler | `GoogleLoginCommand.cs`, `GoogleLoginCommandHandler.cs` |
| 5 | Tạo API Endpoint | `AuthController.cs` |
| 6 | Tạo Frontend Callback Page | `auth/google-callback/page.tsx` |
| 7 | Cập nhật AppContext | `AppContext.tsx` |
| 8 | Cập nhật Login Page | `login/page.tsx` |
| 9 | Cập nhật Register Page | `register/page.tsx` |
| 10 | User Secrets Setup | Documentation |
| 11 | Google Cloud Setup Guide | `google-oauth-setup.md` |
| 12 | E2E Testing | Manual verification |

---

## Post-Implementation Checklist

- [ ] Backend build thành công
- [ ] Frontend build thành công
- [ ] Google credentials đã được cấu hình đúng
- [ ] Đăng nhập Google hoạt động
- [ ] Tạo account mới tự động hoạt động
- [ ] Link account với email tồn tại hoạt động
- [ ] Tokens được lưu và sử dụng đúng
- [ ] Redirect về dashboard sau login thành công
