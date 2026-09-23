# FR-AUTH-001: User Registration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Triển khai hoàn chỉnh tính năng đăng ký tài khoản (FR-AUTH-001) cho Culinary Blog, bao gồm Application layer (CQRS/MediatR), Infrastructure services/repositories, API endpoint POST /api/v1/auth/register, rate limiting, exception handling, và trọn bộ Unit/Integration tests theo đặc tả FR-AUTH-001_Plan.md.

**Architecture:** Áp dụng Clean Architecture (Domain -> Application -> Infrastructure -> API). Application Layer sử dụng MediatR Command + FluentValidation, Infrastructure triển khai ASP.NET Core Identity + JWT TokenService + EF Core Repositories, API Layer đóng gói Controller v1, Middleware xử lý lỗi tập trung và Rate Limiting.

**Tech Stack:** .NET 10, C# 13, ASP.NET Core Identity, Entity Framework Core, PostgreSQL (Npgsql), MediatR 14, FluentValidation 12, JWT Bearer (Microsoft.IdentityModel), Serilog, xUnit, FluentAssertions, Moq/NSubstitute, Microsoft.AspNetCore.Mvc.Testing.

**Spec:** `/media/thanhhien/DATA/PTUDWNC_Nhom06_CulinaryBlog/docs/specs/fr-auth/FR-AUTH-001_Plan.md`

## Global Constraints

- Password rules: Tối thiểu 8 ký tự, ít nhất 1 chữ hoa, 1 chữ thường, 1 số, 1 ký tự đặc biệt (!@#$%^&*...).
- Username rules: 3-30 ký tự, chỉ gồm ký tự chữ cái (a-z, A-Z), số (0-9) và dấu gạch dưới (_).
- Display name rules: 2-100 ký tự.
- JWT Access token expiry: 15 phút, chứa claims: sub (UserId), email, name (DisplayName), roles (Author).
- Refresh token: SHA-256 hash lưu DB, hạn 7 ngày, chuỗi raw 64 hex / base64 cryptographically secure.
- Role mặc định khi đăng ký: "Author".
- Rate limit: 10 requests / phút / IP cho endpoint register.
- Error codes: 409 AUTH_EMAIL_EXISTS, 409 AUTH_USERNAME_EXISTS, 422 VALIDATION_ERROR, 429 RATE_LIMIT_EXCEEDED, 500 INTERNAL_ERROR.

## Review Focus

1. Duplicate email/username race condition: Handled gracefully via Unique constraint & handler check returning 409 Conflict.
2. Case-insensitivity in uniqueness: Email and username comparisons must be normalized / case-insensitive.
3. Refresh token hashing: Raw token is never stored in DB plaintext; only SHA-256 hash is persisted.
4. Password secrecy: Password and PasswordHash must never leak into AuthResponse or logging.
5. Role assignment failure: If role assignment or token generation fails, transaction or error handling ensures clean state.

---

### Task 1: Test Project Setup, DTOs & Validation (Lane A)

**Files:**
- Create: `tests/CulinaryBlog.Application.Tests/CulinaryBlog.Application.Tests.csproj`
- Create: `src/CulinaryBlog.Application/DTOs/Auth/RegisterRequest.cs`
- Create: `src/CulinaryBlog.Application/DTOs/Auth/AuthResponse.cs`
- Create: `src/CulinaryBlog.Application/Commands/Auth/Register/RegisterCommand.cs`
- Create: `tests/CulinaryBlog.Application.Tests/Commands/RegisterCommandValidatorTests.cs`
- Create: `src/CulinaryBlog.Application/Commands/Auth/Register/RegisterCommandValidator.cs`
- Modify: `CulinaryBlog.slnx`

- [ ] **Step 1: Create Application.Tests project and add to solution**
  Create test project with xUnit, FluentAssertions, Moq and project references to `CulinaryBlog.Application` and `CulinaryBlog.Domain`.
  Add project to `CulinaryBlog.slnx`.

- [ ] **Step 2: Create DTOs and RegisterCommand**
  Define `RegisterRequest(string Email, string UserName, string DisplayName, string Password)`.
  Define `AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt, UserDto User)`.
  Define `RegisterCommand(string Email, string UserName, string DisplayName, string Password) : IRequest<AuthResponse>`.

- [ ] **Step 3: Write failing unit tests for RegisterCommandValidator (RED)**
  Create `RegisterCommandValidatorTests.cs` covering all 12 cases specified in Section 10.1 of spec:
  1. Valid request passes
  2. Invalid email format fails
  3. Username < 3 chars fails
  4. Username > 30 chars fails
  5. Username with special chars (e.g. `user@name`) fails
  6. Username with valid underscore (`user_name123`) passes
  7. DisplayName < 2 chars fails
  8. DisplayName > 100 chars fails
  9. Password < 8 chars fails
  10. Password missing uppercase fails
  11. Password missing lowercase fails
  12. Password missing digit fails
  13. Password missing special char fails

- [ ] **Step 4: Run validator tests to verify RED**
  Command: `dotnet test tests/CulinaryBlog.Application.Tests --filter FullyQualifiedName~RegisterCommandValidatorTests`
  Expected: Compilation failure or failed tests because validator is not yet implemented.

- [ ] **Step 5: Implement RegisterCommandValidator (GREEN)**
  Create `RegisterCommandValidator.cs` inheriting `AbstractValidator<RegisterCommand>` with rules matching spec 6.1 and 6.2.

- [ ] **Step 6: Run validator tests to verify GREEN**
  Command: `dotnet test tests/CulinaryBlog.Application.Tests --filter FullyQualifiedName~RegisterCommandValidatorTests`
  Expected: All 12+ tests PASS.

- [ ] **Step 7: Commit Task 1**
  Commit message: `feat(application): cài đặt DTOs, RegisterCommand và RegisterCommandValidator với unit tests`

---

### Task 2: Application Interfaces & TokenService (Lane A & B)

**Files:**
- Create: `src/CulinaryBlog.Application/Interfaces/ITokenService.cs`
- Create: `src/CulinaryBlog.Application/Interfaces/IUserRepository.cs`
- Create: `src/CulinaryBlog.Application/Interfaces/IRefreshTokenRepository.cs`
- Create: `tests/CulinaryBlog.Application.Tests/Services/TokenServiceTests.cs`
- Create: `src/CulinaryBlog.Infrastructure/Services/TokenService.cs`

- [ ] **Step 1: Create Application interfaces**
  Define `ITokenService` (`GenerateAccessToken(ApplicationUser user, IList<string> roles)`, `GenerateRefreshToken()`).
  Define `IUserRepository` (`FindByEmailAsync(string email)`, `FindByUserNameAsync(string userName)`, `CreateAsync(ApplicationUser user, string password)`, `AddToRoleAsync(ApplicationUser user, string role)`).
  Define `IRefreshTokenRepository` (`AddAsync(RefreshToken token)`, `GetByTokenHashAsync(string hash)`, `SaveChangesAsync()`).

- [ ] **Step 2: Write failing unit tests for TokenService (RED)**
  Create `TokenServiceTests.cs` in `CulinaryBlog.Application.Tests` testing:
  1. `GenerateAccessToken`: returns valid JWT with claims (`sub`, `email`, `name`, `roles`), valid signature, 15 min expiry.
  2. `GenerateRefreshToken`: returns cryptographically random token string (minimum 32 bytes / 64 hex characters).
  3. Validate token generation handling when configuration is valid.

- [ ] **Step 3: Run TokenService tests to verify RED**
  Command: `dotnet test tests/CulinaryBlog.Application.Tests --filter FullyQualifiedName~TokenServiceTests`
  Expected: Fails because `TokenService` is not implemented.

- [ ] **Step 4: Implement TokenService in Infrastructure (GREEN)**
  Create `src/CulinaryBlog.Infrastructure/Services/TokenService.cs` using `IConfiguration` for JWT settings (Secret, Issuer, Audience, ExpiryMinutes), `JwtSecurityTokenHandler`, and `RandomNumberGenerator`.

- [ ] **Step 5: Run TokenService tests to verify GREEN**
  Command: `dotnet test tests/CulinaryBlog.Application.Tests --filter FullyQualifiedName~TokenServiceTests`
  Expected: All TokenService unit tests PASS.

- [ ] **Step 6: Commit Task 2**
  Commit message: `feat(infrastructure): cài đặt TokenService và các interface repository với unit tests`

---

### Task 3: Infrastructure Repositories & Email Service Stub (Lane B)

**Files:**
- Create: `src/CulinaryBlog.Infrastructure/Services/UserRepository.cs`
- Create: `src/CulinaryBlog.Infrastructure/Services/RefreshTokenRepository.cs`
- Create: `src/CulinaryBlog.Infrastructure/Services/Email/EmailService.cs`

- [ ] **Step 1: Implement UserRepository**
  Create `UserRepository.cs` using `UserManager<ApplicationUser>` to implement `IUserRepository`.
  Ensure case-insensitive checks for email (`FindByEmailAsync`) and username (`FindByNameAsync`).

- [ ] **Step 2: Implement RefreshTokenRepository**
  Create `RefreshTokenRepository.cs` using `ApplicationDbContext` to implement `IRefreshTokenRepository`.

- [ ] **Step 3: Implement EmailService Stub**
  Create `EmailService.cs` in `Infrastructure/Services/Email/` implementing a basic logging stub for registration emails (deferred to FR-AUTH-006).

- [ ] **Step 4: Verify build and compile**
  Command: `dotnet build`
  Expected: Build succeeds with 0 errors.

- [ ] **Step 5: Commit Task 3**
  Commit message: `feat(infrastructure): cài đặt UserRepository, RefreshTokenRepository và EmailService stub`

---

### Task 4: Register Command Handler & Business Logic (Lane A)

**Files:**
- Create: `src/CulinaryBlog.Application/Exceptions/AuthConflictException.cs`
- Create: `tests/CulinaryBlog.Application.Tests/Commands/RegisterCommandHandlerTests.cs`
- Create: `src/CulinaryBlog.Application/Commands/Auth/Register/RegisterCommandHandler.cs`

- [ ] **Step 1: Define AuthConflictException and Domain/Application error codes**
  Create `AuthConflictException` carrying error codes: `AUTH_EMAIL_EXISTS` and `AUTH_USERNAME_EXISTS`.

- [ ] **Step 2: Write failing unit tests for RegisterCommandHandler (RED)**
  Create `RegisterCommandHandlerTests.cs` covering 4 cases in spec Section 10.1:
  1. Success: creates user, assigns "Author" role, generates tokens, persists refresh token, returns `AuthResponse`.
  2. Duplicate email: throws `AuthConflictException` with code `AUTH_EMAIL_EXISTS`.
  3. Duplicate username: throws `AuthConflictException` with code `AUTH_USERNAME_EXISTS`.
  4. User creation failure: throws error when `UserManager.CreateAsync` fails.

- [ ] **Step 3: Run RegisterCommandHandler tests to verify RED**
  Command: `dotnet test tests/CulinaryBlog.Application.Tests --filter FullyQualifiedName~RegisterCommandHandlerTests`
  Expected: Fails because `RegisterCommandHandler` is not yet implemented.

- [ ] **Step 4: Implement RegisterCommandHandler (GREEN)**
  Implement `RegisterCommandHandler` implementing `IRequestHandler<RegisterCommand, AuthResponse>`.
  Follow flow:
  1. Check email uniqueness via `IUserRepository` -> if exists throw `AUTH_EMAIL_EXISTS`.
  2. Check username uniqueness via `IUserRepository` -> if exists throw `AUTH_USERNAME_EXISTS`.
  3. Create `ApplicationUser` with `ApplicationUser.Create(...)`.
  4. Call `IUserRepository.CreateAsync(user, request.Password)`.
  5. Assign "Author" role via `IUserRepository.AddToRoleAsync(user, AppRoles.Author)`.
  6. Generate JWT via `ITokenService.GenerateAccessToken`.
  7. Generate raw refresh token via `ITokenService.GenerateRefreshToken`.
  8. Save `RefreshToken.Create(user.Id, rawRefreshToken)` via `IRefreshTokenRepository`.
  9. Return `AuthResponse`.

- [ ] **Step 5: Run all Application unit tests to verify GREEN**
  Command: `dotnet test tests/CulinaryBlog.Application.Tests`
  Expected: All unit tests PASS (~19/19 tests).

- [ ] **Step 6: Commit Task 4**
  Commit message: `feat(application): cài đặt RegisterCommandHandler và AuthConflictException với unit tests`

---

### Task 5: API Layer, Middlewares, Rate Limiting & Program.cs Setup (Lane C)

**Files:**
- Create: `src/CulinaryBlog.API/Middleware/ExceptionHandlingMiddleware.cs`
- Create: `src/CulinaryBlog.API/Controllers/AuthController.cs`
- Modify: `src/CulinaryBlog.API/Program.cs`
- Modify: `src/CulinaryBlog.API/appsettings.json`
- Modify: `src/CulinaryBlog.API/appsettings.Development.json`

- [ ] **Step 1: Implement ExceptionHandlingMiddleware**
  Handle:
  - `ValidationException` -> 422 Unprocessable Entity with `VALIDATION_ERROR` and detailed error list.
  - `AuthConflictException` -> 409 Conflict with `AUTH_EMAIL_EXISTS` or `AUTH_USERNAME_EXISTS`.
  - Unhandled exceptions -> 500 Internal Server Error with `INTERNAL_ERROR`.

- [ ] **Step 2: Implement AuthController**
  Create `AuthController` at `/api/v1/auth`:
  - `POST /api/v1/auth/register`: accepts `RegisterRequest`, maps to `RegisterCommand`, sends via MediatR, returns 201 Created with `AuthResponse`.
  - Decorated with rate limiting policy.

- [ ] **Step 3: Update Program.cs and appsettings.json**
  - Add JWT Bearer authentication options and token validation parameters.
  - Add MediatR handlers from `CulinaryBlog.Application`.
  - Add FluentValidation validators.
  - Add DI for `IUserRepository`, `IRefreshTokenRepository`, `ITokenService`.
  - Add ASP.NET Core built-in Rate Limiter: partition by Client IP, 10 requests per minute for register.
  - Add `ExceptionHandlingMiddleware` before routing/controllers.
  - Add `builder.Services.AddControllers()` and `app.MapControllers()`.
  - Update `appsettings.json` with `Jwt:Secret`, `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryMinutes`.

- [ ] **Step 4: Verify build and compile**
  Command: `dotnet build`
  Expected: Solution compiles with 0 errors.

- [ ] **Step 5: Commit Task 5**
  Commit message: `feat(api): cài đặt AuthController, ExceptionHandlingMiddleware, rate limiting và cấu hình DI`

---

### Task 6: Integration Tests & Acceptance Criteria Verification (Lane D)

**Files:**
- Create: `tests/CulinaryBlog.Integration.Tests/CulinaryBlog.Integration.Tests.csproj`
- Create: `tests/CulinaryBlog.Integration.Tests/CustomWebApplicationFactory.cs`
- Create: `tests/CulinaryBlog.Integration.Tests/AuthControllerTests.cs`
- Modify: `CulinaryBlog.slnx`

- [ ] **Step 1: Create Integration.Tests project and add to solution**
  Add `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.EntityFrameworkCore.InMemory` or SQLite/TestContainers, `xunit`, `FluentAssertions`.
  Register project in `CulinaryBlog.slnx`.

- [ ] **Step 2: Create CustomWebApplicationFactory**
  Configure test WebApplicationFactory overriding DbContext with In-Memory or isolated test database and test JWT config.

- [ ] **Step 3: Write E2E integration tests in AuthControllerTests**
  Cover 6 user flows from spec Section 10.2:
  1. Valid registration -> returns 201 Created with tokens & user info.
  2. Invalid email format -> returns 422 Unprocessable Entity.
  3. Duplicate email -> returns 409 Conflict (`AUTH_EMAIL_EXISTS`).
  4. Duplicate username -> returns 409 Conflict (`AUTH_USERNAME_EXISTS`).
  5. JWT token structure -> verify claims (`sub`, `email`, `roles`).
  6. Invalid password format -> returns 422 Unprocessable Entity.

- [ ] **Step 4: Run integration tests to verify GREEN**
  Command: `dotnet test tests/CulinaryBlog.Integration.Tests`
  Expected: All integration tests PASS.

- [ ] **Step 5: Run full test suite across entire solution**
  Command: `dotnet test`
  Expected: All unit and integration tests PASS.

- [ ] **Step 6: Commit Task 6**
  Commit message: `test(integration): cài đặt trọn bộ Integration Tests cho AuthController và nghiệm thu luồng đăng ký`
