# FR-AUTH-001: User Registration — Design Doc

**Version:** 1.1.0
**Date:** 2026-09-11
**Author:** Nguyen Ngoc Thanh Hien
**Status:** Draft
**Review:** Updated from plan-ceo-review v1.1.0

---

## 1. Problem Statement

Triển khai chức năng đăng ký tài khoản (FR-AUTH-001) cho Culinary Blog. User đăng ký với email, username, display name, và password. Sau khi đăng ký thành công, user nhận được JWT tokens và tự động đăng nhập.

---

## 2. Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        API Layer                                 │
│  CulinaryBlog.API/Controllers/AuthController.cs                  │
│  - POST /api/v1/auth/register                                   │
└────────────────────────┬────────────────────────────────────────┘
                         │ MediatR Command
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Application Layer                               │
│  CulinaryBlog.Application/                                       │
│  ├── Commands/Auth/Register/                                    │
│  │   ├── RegisterCommand.cs                                     │
│  │   ├── RegisterCommandValidator.cs                           │
│  │   └── RegisterCommandHandler.cs                             │
│  ├── DTOs/Auth/                                                │
│  │   ├── RegisterRequest.cs                                    │
│  │   └── AuthResponse.cs                                       │
│  └── Interfaces/                                                │
│      ├── ITokenService.cs                                      │
│      ├── IUserRepository.cs                                    │
│      └── IRefreshTokenRepository.cs                            │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│                     Domain Layer                                 │
│  CulinaryBlog.Domain/                                           │
│  ├── Entities/ApplicationUser.cs                               │
│  └── Entities/RefreshToken.cs                                  │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│                  Infrastructure Layer                             │
│  CulinaryBlog.Infrastructure/                                    │
│  ├── Data/ApplicationDbContext.cs                              │
│  ├── Configurations/                                            │
│  │   ├── ApplicationUserConfiguration.cs                       │
│  │   └── RefreshTokenConfiguration.cs                         │
│  ├── Services/                                                  │
│  │   ├── TokenService.cs                                      │
│  │   ├── UserRepository.cs                                    │
│  │   └── RefreshTokenRepository.cs                            │
│  └── Services/Email/                                            │
│      └── EmailService.cs (stub)                                │
└─────────────────────────────────────────────────────────────────┘
```

---

## 3. Required NuGet Packages

### 3.1 Infrastructure Layer

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 10.0.x | ASP.NET Core Identity with EF Core |
| `Microsoft.EntityFrameworkCore` | 10.0.x | EF Core (already present) |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.x | PostgreSQL provider (already present) |
| `FluentValidation` | 11.x | Input validation |
| `FluentValidation.DependencyInjectionExtensions` | 11.x | DI for FluentValidation |

### 3.2 API Layer

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.x | JWT Bearer authentication |
| `Serilog.AspNetCore` | 8.x | Structured logging |
| `Serilog.Sinks.Console` | 6.x | Console sink for Serilog |
| `AspNetCoreRateLimit` | 5.x | Rate limiting |

### 3.3 Application Layer

| Package | Version | Purpose |
|---------|---------|---------|
| `MediatR` | 14.x | CQRS pattern (already present) |
| `FluentValidation` | 11.x | Input validation |

---

## 4. Files to Create

### 4.1 Domain Layer

| File | Purpose |
|------|---------|
| `Domain/Entities/ApplicationUser.cs` | Extended IdentityUser with DisplayName, AvatarUrl, Bio, IsActive, CreatedAt |
| `Domain/Entities/RefreshToken.cs` | Refresh token entity with rotation support |

### 4.2 Application Layer

| File | Purpose |
|------|---------|
| `Application/Commands/Auth/Register/RegisterCommand.cs` | MediatR command |
| `Application/Commands/Auth/Register/RegisterCommandValidator.cs` | FluentValidation validator |
| `Application/Commands/Auth/Register/RegisterCommandHandler.cs` | Command handler |
| `Application/DTOs/Auth/RegisterRequest.cs` | Request DTO |
| `Application/DTOs/Auth/AuthResponse.cs` | Response DTO |
| `Application/Interfaces/ITokenService.cs` | Token service interface |
| `Application/Interfaces/IUserRepository.cs` | User repository interface |
| `Application/Interfaces/IRefreshTokenRepository.cs` | Refresh token repository interface |

### 4.3 Infrastructure Layer

| File | Purpose |
|------|---------|
| `Infrastructure/Data/ApplicationDbContext.cs` | EF Core DbContext with Identity |
| `Infrastructure/Configurations/ApplicationUserConfiguration.cs` | Entity configuration |
| `Infrastructure/Configurations/RefreshTokenConfiguration.cs` | Entity configuration |
| `Infrastructure/Services/TokenService.cs` | JWT + Refresh token generation |
| `Infrastructure/Services/UserRepository.cs` | User repository implementation |
| `Infrastructure/Services/RefreshTokenRepository.cs` | Refresh token repository implementation |
| `Infrastructure/Services/Email/EmailService.cs` | Email service stub (for future) |

### 4.4 API Layer

| File | Purpose |
|------|---------|
| `API/Controllers/AuthController.cs` | Auth endpoints |
| `API/Program.cs` | DI registration, middleware, rate limiting |
| `API/Middleware/ExceptionHandlingMiddleware.cs` | Global exception handler |

### 4.5 Tests

| File | Purpose |
|------|---------|
| `tests/CulinaryBlog.Application.Tests/Commands/RegisterCommandValidatorTests.cs` | Validator unit tests |
| `tests/CulinaryBlog.Application.Tests/Commands/RegisterCommandHandlerTests.cs` | Handler unit tests |

---

## 5. Implementation Details

### 5.1 Register Flow

```
1. User POST /api/v1/auth/register
2. AuthController receives request
3. MediatR dispatches RegisterCommand
4. RegisterCommandValidator validates:
   - Email: valid format, not already exists
   - UserName: 3-30 chars, alphanumeric + underscore, not already exists
   - DisplayName: 2-100 chars
   - Password: 8+ chars, 1 uppercase, 1 lowercase, 1 digit, 1 special char
5. RegisterCommandHandler:
   a. Check email uniqueness → 409 if exists
   b. Check username uniqueness → 409 if exists
   c. Create ApplicationUser with hashed password (via UserManager)
   d. Assign "Author" role (via RoleManager)
   e. Generate JWT access token (15 min) via TokenService
   f. Generate refresh token (7 days), store hash in DB via RefreshTokenRepository
   g. Return AuthResponse
6. Controller returns 201 Created with AuthResponse
7. Email verification queued (stub) — deferred to FR-AUTH-006
```

### 5.2 Password Requirements

- Minimum 8 characters
- At least 1 uppercase letter
- At least 1 lowercase letter
- At least 1 digit
- At least 1 special character (!@#$%^&*...)

### 5.3 JWT Token Structure

```json
{
  "sub": "user-uuid",
  "email": "user@example.com",
  "name": "Display Name",
  "roles": ["Author"],
  "iat": 1234567890,
  "exp": 1234568790  // 15 minutes
}
```

### 5.4 Refresh Token Storage

- SHA-256 hash stored in DB (not plaintext)
- 7-day expiration
- Tracks: UserId, CreatedAt, CreatedByIp, ExpiresAt
- Rotation support via ReplacedByTokenHash

---

## 6. Error Handling

| Scenario | HTTP Status | Error Code |
|----------|-------------|------------|
| Email already exists | 409 Conflict | AUTH_EMAIL_EXISTS |
| Username already exists | 409 Conflict | AUTH_USERNAME_EXISTS |
| Validation failed | 422 Unprocessable | VALIDATION_ERROR |
| Rate limit exceeded | 429 Too Many Requests | RATE_LIMIT_EXCEEDED |
| Internal error | 500 Internal Server Error | INTERNAL_ERROR |

---

## 7. Security Considerations

1. **Password Hashing**: Use ASP.NET Core Identity's PasswordHasher (PBKDF2)
2. **Email Validation**: RFC 5322 compliant
3. **Username Validation**: Alphanumeric + underscore only, case-insensitive uniqueness
4. **Rate Limiting**: 10 requests/minute/IP for register endpoint
5. **No Password in Response**: Never return password or hash in API response
6. **Request Logging**: Log registration attempts (without sensitive data)

---

## 8. Deferred Scope

### 8.1 Email Verification (Deferred to FR-AUTH-006)

The spec mentions "sends welcome and verification emails asynchronously" but this is **deferred** because:
- Email infrastructure requires SMTP config or third-party service (SendGrid, AWS SES)
- Testing email delivery requires mock SMTP server
- Can be implemented independently after core registration works

**Stub implementation:** `EmailService.cs` with `SendWelcomeEmailAsync()` and `SendVerificationEmailAsync()` methods that log instead of sending. Ready to implement when email provider is configured.

### 8.2 Future Enhancements

| Feature | FR | Priority |
|---------|-----|----------|
| Email verification | FR-AUTH-006 | High |
| Account lockout | FR-AUTH-002 | Medium |
| Google OAuth | FR-AUTH-003 | Medium |
| Refresh token rotation | FR-AUTH-004 | Medium |

---

## 9. Testing Strategy

### 9.1 Unit Tests

**RegisterCommandValidatorTests:**
- Valid request passes
- Invalid email format fails
- Email already exists fails
- Username too short/long fails
- Username with invalid characters fails
- Username already exists fails
- DisplayName too short/long fails
- Password too short fails
- Password missing uppercase fails
- Password missing lowercase fails
- Password missing digit fails
- Password missing special char fails

**RegisterCommandHandlerTests:**
- Success: returns AuthResponse with tokens
- Duplicate email: returns conflict result
- Duplicate username: returns conflict result
- User creation failure: returns error result

### 9.2 Integration Tests (Future)

- Full endpoint with in-memory database
- Rate limiting behavior
- JWT token validation

---

## 10. Acceptance Criteria

- [ ] POST /api/v1/auth/register returns 201 with valid tokens
- [ ] Duplicate email returns 409
- [ ] Duplicate username returns 409
- [ ] Invalid password format returns 422
- [ ] Refresh token stored in DB (hashed)
- [ ] User assigned "Author" role
- [ ] Rate limiting works (10 req/min/IP)
- [ ] Unit tests pass
- [ ] Code compiles without errors

---

## 11. Effort Estimate

| Layer | Files | Complexity |
|-------|-------|------------|
| NuGet Packages | 3 configs | Low |
| Domain | 2 | Low |
| Application | 8 | Medium |
| Infrastructure | 7 | Medium |
| API | 3 | Medium |
| Tests | 2 | Low |
| **Total** | **25** | ~4-5 hours |

---

## 12. Implementation Order

1. Add NuGet packages to project files
2. Create Domain entities
3. Create Application interfaces
4. Create Application DTOs
5. Create Application commands and validators
6. Create Infrastructure DbContext and configurations
7. Create Infrastructure repositories
8. Create Infrastructure services
9. Create API controller and middleware
10. Update Program.cs with DI
11. Write unit tests
12. Build and verify

---

**Document Status:** Draft
**Last Updated:** 2026-09-11
**Reviewed by:** ________________
**Date:** ________________
