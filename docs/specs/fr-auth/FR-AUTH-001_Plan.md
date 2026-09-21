# FR-AUTH-001: User Registration — Implementation Plan

**Version:** 1.2.0
**Date:** 2026-09-21
**Author:** Nguyen Ngoc Thanh Hien
**Status:** Ready for Implementation

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
│  ├── Entities/ApplicationUser.cs (extends IdentityUser<Guid>)   │
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

## 3. Decisions Made

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Domain-Identity coupling | Accept | Standard ASP.NET Core pattern; can be refactored later |
| Uniqueness checks | Handler | Validation stays pure; handler returns 409 for conflicts |
| Migration strategy | EF Core Migrations | Built-in, version-controlled schema changes |
| Test coverage | Full scope | All unit + E2E tests included |
| Email service | Stub deferred | Not needed for FR-AUTH-001; ready for FR-AUTH-006 |

---

## 4. Required NuGet Packages

### 4.1 Infrastructure Layer

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 10.0.x | ASP.NET Core Identity with EF Core |
| `Microsoft.EntityFrameworkCore` | 10.0.x | EF Core (already present) |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.x | PostgreSQL provider (already present) |
| `FluentValidation` | 11.x | Input validation |
| `FluentValidation.DependencyInjectionExtensions` | 11.x | DI for FluentValidation |

### 4.2 API Layer

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.x | JWT Bearer authentication |
| `Serilog.AspNetCore` | 8.x | Structured logging |
| `Serilog.Sinks.Console` | 6.x | Console sink for Serilog |
| `AspNetCoreRateLimit` | 5.x | Rate limiting |

### 4.3 Application Layer

| Package | Version | Purpose |
|---------|---------|---------|
| `MediatR` | 14.x | CQRS pattern (already present) |
| `FluentValidation` | 11.x | Input validation |

---

## 5. Files to Create

### 5.1 Domain Layer (2 files)

| File | Purpose |
|------|---------|
| `Domain/Entities/ApplicationUser.cs` | Extended IdentityUser<Guid> with DisplayName, AvatarUrl, Bio, IsActive, CreatedAt |
| `Domain/Entities/RefreshToken.cs` | Refresh token entity with rotation support |

### 5.2 Application Layer (8 files)

| File | Purpose |
|------|---------|
| `Application/Commands/Auth/Register/RegisterCommand.cs` | MediatR command |
| `Application/Commands/Auth/Register/RegisterCommandValidator.cs` | FluentValidation validator (format only) |
| `Application/Commands/Auth/Register/RegisterCommandHandler.cs` | Command handler (uniqueness checks here) |
| `Application/DTOs/Auth/RegisterRequest.cs` | Request DTO |
| `Application/DTOs/Auth/AuthResponse.cs` | Response DTO |
| `Application/Interfaces/ITokenService.cs` | Token service interface |
| `Application/Interfaces/IUserRepository.cs` | User repository interface |
| `Application/Interfaces/IRefreshTokenRepository.cs` | Refresh token repository interface |

### 5.3 Infrastructure Layer (7 files)

| File | Purpose |
|------|---------|
| `Infrastructure/Data/ApplicationDbContext.cs` | EF Core DbContext with Identity |
| `Infrastructure/Configurations/ApplicationUserConfiguration.cs` | Entity configuration |
| `Infrastructure/Configurations/RefreshTokenConfiguration.cs` | Entity configuration |
| `Infrastructure/Services/TokenService.cs` | JWT + Refresh token generation |
| `Infrastructure/Services/UserRepository.cs` | User repository implementation |
| `Infrastructure/Services/RefreshTokenRepository.cs` | Refresh token repository implementation |
| `Infrastructure/Services/Email/EmailService.cs` | Email service stub (logging only) |

### 5.4 API Layer (3 files)

| File | Purpose |
|------|---------|
| `API/Controllers/AuthController.cs` | Auth endpoints |
| `API/Program.cs` | DI registration, middleware, rate limiting |
| `API/Middleware/ExceptionHandlingMiddleware.cs` | Global exception handler |

### 5.5 Tests (4 files)

| File | Purpose |
|------|---------|
| `tests/CulinaryBlog.Application.Tests/Commands/RegisterCommandValidatorTests.cs` | Validator unit tests (12 cases) |
| `tests/CulinaryBlog.Application.Tests/Commands/RegisterCommandHandlerTests.cs` | Handler unit tests (4 cases) |
| `tests/CulinaryBlog.Application.Tests/Services/TokenServiceTests.cs` | Token service unit tests (3 cases) |
| `tests/CulinaryBlog.Integration.Tests/AuthControllerTests.cs` | E2E tests (6 user flows) |

---

## 6. Implementation Details

### 6.1 Register Flow

```
1. User POST /api/v1/auth/register
2. AuthController receives RegisterRequest
3. MediatR dispatches RegisterCommand
4. RegisterCommandValidator validates FORMAT ONLY:
   - Email: valid email format
   - UserName: 3-30 chars, alphanumeric + underscore
   - DisplayName: 2-100 chars
   - Password: 8+ chars, 1 uppercase, 1 lowercase, 1 digit, 1 special char
5. RegisterCommandHandler:
   a. Check email uniqueness via IUserRepository → 409 if exists
   b. Check username uniqueness via IUserRepository → 409 if exists
   c. Create ApplicationUser with hashed password (via UserManager)
   d. Assign "Author" role (via RoleManager)
   e. Generate JWT access token (15 min) via TokenService
   f. Generate refresh token (7 days), store hash via RefreshTokenRepository
   g. Return AuthResponse
6. Controller returns 201 Created with AuthResponse
7. Rate limiting applied (10 req/min/IP)
```

### 6.2 Password Requirements

- Minimum 8 characters
- At least 1 uppercase letter
- At least 1 lowercase letter
- At least 1 digit
- At least 1 special character (!@#$%^&*...)

### 6.3 JWT Token Structure

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

### 6.4 Refresh Token Storage

- SHA-256 hash stored in DB (not plaintext)
- 7-day expiration
- Tracks: UserId, CreatedAt, CreatedByIp, ExpiresAt
- Rotation support via ReplacedByTokenHash

---

## 7. Error Handling

| Scenario | HTTP Status | Error Code |
|----------|-------------|------------|
| Email already exists | 409 Conflict | AUTH_EMAIL_EXISTS |
| Username already exists | 409 Conflict | AUTH_USERNAME_EXISTS |
| Validation failed | 422 Unprocessable | VALIDATION_ERROR |
| Rate limit exceeded | 429 Too Many Requests | RATE_LIMIT_EXCEEDED |
| Internal error | 500 Internal Server Error | INTERNAL_ERROR |

---

## 8. Security Considerations

1. **Password Hashing**: Use ASP.NET Core Identity's PasswordHasher (PBKDF2)
2. **Email Validation**: RFC 5322 compliant
3. **Username Validation**: Alphanumeric + underscore only, case-insensitive uniqueness
4. **Rate Limiting**: 10 requests/minute/IP for register endpoint
5. **No Password in Response**: Never return password or hash in API response
6. **Request Logging**: Log registration attempts (without sensitive data)

---

## 9. Database Migrations

### Initial Migration: `InitialCreate`

Creates tables:
- `AspNetUsers` (extended with ApplicationUser fields)
- `AspNetRoles`
- `AspNetUserRoles`
- `AspNetUserClaims`
- `AspNetRoleClaims`
- `AspNetUserLogins`
- `AspNetUserTokens`
- `RefreshTokens`

### Commands

```bash
dotnet ef migrations add InitialCreate --project src/CulinaryBlog.Infrastructure --startup-project src/CulinaryBlog.API
dotnet ef database update --project src/CulinaryBlog.Infrastructure --startup-project src/CulinaryBlog.API
```

---

## 10. Testing Strategy

### 10.1 Unit Tests

**RegisterCommandValidatorTests (12 cases):**
- Valid request passes
- Invalid email format fails
- Email already exists (handled in handler)
- Username too short (< 3 chars) fails
- Username too long (> 30 chars) fails
- Username with invalid characters fails
- Username with valid underscore passes
- DisplayName too short (< 2 chars) fails
- DisplayName too long (> 100 chars) fails
- Password too short (< 8 chars) fails
- Password missing uppercase fails
- Password missing lowercase fails
- Password missing digit fails
- Password missing special char fails

**RegisterCommandHandlerTests (4 cases):**
- Success: returns AuthResponse with tokens
- Duplicate email: returns ConflictResult
- Duplicate username: returns ConflictResult
- User creation failure: returns ErrorResult

**TokenServiceTests (3 cases):**
- GenerateAccessToken: correct claims + 15 min expiration
- GenerateRefreshToken: valid format + 7 day expiration
- Token format validation

### 10.2 Integration Tests

**AuthControllerTests (6 user flows):**
- Valid registration → 201 with tokens
- Invalid email → 422 with error
- Duplicate email → 409 with AUTH_EMAIL_EXISTS
- Duplicate username → 409 with AUTH_USERNAME_EXISTS
- Rate limit exceeded → 429
- JWT token structure validation

---

## 11. Acceptance Criteria

- [ ] POST /api/v1/auth/register returns 201 with valid tokens
- [ ] Duplicate email returns 409
- [ ] Duplicate username returns 409
- [ ] Invalid password format returns 422
- [ ] Refresh token stored in DB (hashed)
- [ ] User assigned "Author" role
- [ ] Rate limiting works (10 req/min/IP)
- [ ] Unit tests pass
- [ ] Integration tests pass
- [ ] Code compiles without errors

---

## 12. Implementation Order

### Lane A (Domain + Application):
1. Create Domain entities (ApplicationUser, RefreshToken)
2. Create Application interfaces (ITokenService, IUserRepository, IRefreshTokenRepository)
3. Create Application DTOs (RegisterRequest, AuthResponse)
4. Create Application commands and validators (RegisterCommand, RegisterCommandValidator, RegisterCommandHandler)

### Lane B (Infrastructure - depends on Domain + Application):
5. Create Infrastructure DbContext and configurations
6. Create Infrastructure repositories
7. Create Infrastructure services (TokenService, EmailService stub)

### Lane C (API - depends on all):
8. Create API controller and middleware
9. Update Program.cs with DI
10. Add NuGet packages

### Lane D (Tests - depends on all):
11. Write unit tests (Validator, Handler, TokenService)
12. Write integration tests

### Sequential Step:
13. Run migrations
14. Build and verify

---

## 13. Effort Estimate

| Layer | Files | Complexity |
|-------|-------|------------|
| NuGet Packages | 3 configs | Low |
| Domain | 2 | Low |
| Application | 8 | Medium |
| Infrastructure | 7 | Medium |
| API | 3 | Medium |
| Tests | 4 | Medium |
| **Total** | **27** | ~5-6 hours |

---

## 14. NOT in Scope

| Item | Rationale |
|------|----------|
| Email verification | Deferred to FR-AUTH-006 |
| Login endpoint | FR-AUTH-002 |
| Password reset | FR-AUTH-FUTURE |
| Google OAuth | FR-AUTH-003 |
| Refresh token rotation | FR-AUTH-004 |
| Account lockout | FR-AUTH-002 |

---

## 15. What Already Exists

| Component | Status | Reused |
|-----------|--------|--------|
| MediatR 14.2.0 | ✅ In Application.csproj | Yes — no new package needed |
| EF Core + Npgsql | ✅ In Infrastructure.csproj | Yes — no new package needed |
| Next.js frontend | ✅ In `/frontend` | Yes — API contract only |

---

## 16. Failure Modes

| Codepath | Failure Mode | Test | Error Handling | Silent? |
|----------|-------------|------|----------------|---------|
| RegisterCommandValidator | Invalid input | ✅ | 422 response | No |
| RegisterCommandHandler (email exists) | DB conflict | ✅ | 409 response | No |
| RegisterCommandHandler (user create fails) | DB error | ✅ | 500 response | No |
| TokenService.GenerateAccessToken | Invalid claims | ✅ | Throws exception | No |
| TokenService.GenerateRefreshToken | Format error | ✅ | Throws exception | No |
| Rate limiting | DoS attempt | ✅ | 429 response | No |
| ApplicationDbContext | Connection failure | ❌ | 500 response | No |

---

## 17. ASCII Diagrams

### ApplicationUser Entity

```
ApplicationUser (extends IdentityUser<Guid>)
├── Id: Guid (PK)
├── Email: string
├── EmailConfirmed: bool
├── PasswordHash: string
├── UserName: string
├── NormalizedEmail: string
├── NormalizedUserName: string
├── SecurityStamp: string
├── ConcurrencyStamp: string
├── LockoutEnd: DateTimeOffset?
├── LockoutEnabled: bool
├── AccessFailedCount: int
├── ─── Custom Fields ───
├── DisplayName: string (max 100)
├── AvatarUrl: string? (nullable)
├── Bio: string? (nullable)
├── IsActive: bool (default true)
└── CreatedAt: DateTime
```

### Register Flow State Machine

```
[Initial]
    │
    ▼
[Validate Input] ──invalid──→ [Return 422]
    │valid
    ▼
[Check Email Uniqueness] ──exists──→ [Return 409 AUTH_EMAIL_EXISTS]
    │not exists
    ▼
[Check Username Uniqueness] ──exists──→ [Return 409 AUTH_USERNAME_EXISTS]
    │not exists
    ▼
[Create User] ──error──→ [Return 500]
    │success
    ▼
[Assign Author Role] ──error──→ [Return 500]
    │success
    ▼
[Generate JWT] ──error──→ [Return 500]
    │success
    ▼
[Generate Refresh Token] ──error──→ [Return 500]
    │success
    ▼
[Return 201 AuthResponse]
```

---

## GSTACK REVIEW REPORT

| Review | Trigger | Why | Runs | Status | Findings |
|--------|---------|-----|------|--------|----------|
| CEO Review | `/plan-ceo-review` | Scope & strategy | 1 | issues_open | 3 scope decisions made |
| Outside Review | Codex | Independent 2nd opinion | 1 | pending | — |
| Eng Review | `/plan-eng-review` | Architecture & tests | 1 | issues_open | 4 issues found, 4 decisions made |
| Design Review | — | UI/UX gaps | 0 | — | — |
| DX Review | — | Developer experience gaps | 0 | — | — |

**OUTSIDE COVERAGE:** Pending — Codex review in progress.

**VERDICT:** CEO + ENG REVIEW PASSED — 4 scope decisions made, awaiting Codex review.

**UNRESOLVED DECISIONS:** None — all decisions confirmed via AskUserQuestion.

---
