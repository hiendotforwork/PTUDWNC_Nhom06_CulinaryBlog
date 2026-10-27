# Password Change and Reset Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let email/password users change a password while signed in or reset it through email, then revoke every refresh-token session.

**Architecture:** Keep password operations in ASP.NET Identity behind `IUserRepository`. Persist reset-email intents in the existing outbox with only the user ID; a dedicated job generates the Identity token when sending the email. MediatR handlers own password flows and transactions; `AuthController` and the Next.js frontend expose them.

**Tech Stack:** .NET 10, ASP.NET Identity, EF Core/PostgreSQL, MediatR, FluentValidation, Hangfire-backed outbox, Next.js 16, React 19, pnpm.

**Spec:** `docs/superpowers/specs/2026-10-27-password-change-reset-design.md`

## Global Constraints

- Use ASP.NET Identity's password-reset token and password policy.
- For a confirmed email account with a local password, enqueue a durable `password-reset-email` task. Do not enqueue for unknown, unconfirmed, or Google-only accounts.
- Always return the same HTTP status and response body whether the account exists or not; do not include account-existence details.
- Apply a separate 60-second cooldown keyed by the normalized email before looking up the account. A cooldown hit or limiter failure skips enqueueing and still returns the same accepted response; use a distinct key from email-confirmation resends.
- Put `userId` and token in the URL fragment, following the existing email-confirmation link pattern; the frontend reads the fragment and submits the values in the reset POST body.
- Run the Identity password update and refresh-token revocation in one serializable database transaction; commit only if both succeed.
- On success, the frontend clears the current browser's stored session and returns to login.
- Already-issued access JWTs may remain valid until expiry (currently 15 minutes); response timing is not normalized in this scope.

## Review Focus

- Google-only, unconfirmed, or unknown email; the forgot handler must not enqueue mail and must return the same accepted response. Pin this in `ForgotPasswordCommandHandlerTests` and `AuthControllerTests`.
- Repeated normalized email or unavailable cooldown service; no second task is enqueued, lookup is skipped, and the public response stays the same. Pin this in `ForgotPasswordCommandHandlerTests` and `PasswordResetRequestLimiterTests`.
- Wrong current password or policy-invalid new password; reject without revoking refresh tokens. Pin this in `ChangePasswordCommandHandlerTests` and `ResetPasswordCommandHandlerTests`.
- Unknown user, invalid/expired/reused reset token; return the generic invalid-link error without changing the password or revoking sessions. Pin this in `ResetPasswordCommandHandlerTests` and `AuthControllerTests`.
- Email delivery failure or encoded link values; leave the outbox task retryable and place the valid Identity token only in the URL fragment. Pin this in `PasswordResetEmailJobTests`.

---

## File Map

- `src/CulinaryBlog.Application/Interfaces/IUserRepository.cs` and `src/CulinaryBlog.Infrastructure/Services/UserRepository.cs`: expose Identity password, token-generation, and local-password operations.
- `src/CulinaryBlog.Application/DTOs/Auth/UserProfileDto.cs`, `GetCurrentUserQueryHandler.cs`, and `UpdateProfileCommandHandler.cs`: return `HasPassword` for the profile UI.
- `src/CulinaryBlog.Application/Interfaces/IBackgroundTaskQueue.cs`, `src/CulinaryBlog.API/Services/BackgroundTaskQueue.cs`, `BackgroundJobs.cs`, `RuntimeRegistration.cs`, `IEmailService.cs`, and `EmailService.cs`: enqueue, dispatch, and send reset email through the current outbox/job pattern.
- `src/CulinaryBlog.Infrastructure/Migrations/`: extend the allowed background-task kind in a new forward migration.
- `src/CulinaryBlog.Application/Commands/Auth/ChangePassword/`, `ForgotPassword/`, and `ResetPassword/`: request records, validators, and handlers.
- `src/CulinaryBlog.Application/Interfaces/IPasswordResetRequestLimiter.cs` and `src/CulinaryBlog.API/Services/PasswordResetRequestLimiter.cs`: separate normalized-email cooldown, Redis and in-memory implementations.
- `src/CulinaryBlog.API/Controllers/AuthController.cs` and `Program.cs`: three endpoints, validation, and endpoint rate limit.
- `frontend/app/lib/api.ts`, `frontend/app/lib/types.ts`, `frontend/app/login/page.tsx`, `frontend/app/profile/page.tsx`, and new `frontend/app/forgot-password/page.tsx` and `reset-password/page.tsx`: frontend API and forms.

## Task 1: Identity Adapter and Profile Capability

**Files:**
- Modify: `src/CulinaryBlog.Application/Interfaces/IUserRepository.cs`
- Modify: `src/CulinaryBlog.Infrastructure/Services/UserRepository.cs`
- Modify: `src/CulinaryBlog.Application/DTOs/Auth/UserProfileDto.cs`
- Modify: `src/CulinaryBlog.Application/Queries/Auth/GetCurrentUserQueryHandler.cs`
- Modify: `src/CulinaryBlog.Application/Commands/Auth/UpdateProfile/UpdateProfileCommandHandler.cs`
- Modify: `frontend/app/lib/types.ts`
- Test: `tests/CulinaryBlog.Application.Tests/Queries/Auth/GetCurrentUserQueryHandlerTests.cs`
- Test: `tests/CulinaryBlog.Application.Tests/Commands/UpdateProfileCommandHandlerTests.cs`

**Interfaces:**
- Produces `Task<bool> HasPasswordAsync(ApplicationUser user, CancellationToken cancellationToken = default)`.
- Produces `Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user, CancellationToken cancellationToken = default)`.
- Produces `Task<UserOperationResult> ChangePasswordAsync(ApplicationUser user, string currentPassword, string newPassword, CancellationToken cancellationToken = default)` and `Task<UserOperationResult> ResetPasswordAsync(ApplicationUser user, string token, string newPassword, CancellationToken cancellationToken = default)`. `UserOperationResult` carries Identity error codes and descriptions so reset-token errors can be separated from password-policy errors.
- Define `UserOperationError(string Code, string Description)` and `UserOperationResult(bool Succeeded, IReadOnlyList<UserOperationError> Errors)` alongside `SignInResult` in `IUserRepository.cs`.
- Produces `bool HasPassword` on `UserProfileDto` and `UserProfileResponse`.

- [ ] **Step 1: Write failing profile-handler tests** asserting `HasPassword` is mapped for local-password and passwordless users, including the profile returned by `UpdateProfileCommandHandler`.
- [ ] **Step 2: Run the focused tests** with `rtk dotnet test tests/CulinaryBlog.Application.Tests/CulinaryBlog.Application.Tests.csproj --filter "FullyQualifiedName~GetCurrentUserQueryHandlerTests|FullyQualifiedName~UpdateProfileCommandHandlerTests"`; confirm the new assertions fail.
- [ ] **Step 3: Add the Identity adapter methods and `UserOperationResult`** in `IUserRepository.cs`/`UserRepository.cs`, using `UserManager.HasPasswordAsync`, `GeneratePasswordResetTokenAsync`, `ChangePasswordAsync`, and `ResetPasswordAsync`; map each `IdentityError` to code and description.
- [ ] **Step 4: Add and map `HasPassword`** in `UserProfileDto`, both profile handlers, and the TypeScript `UserProfileResponse` type.
- [ ] **Step 5: Rerun the focused application tests** and confirm the profile results include the correct password capability.
- [ ] **Step 6: Commit** as `feat(auth): expose local password capability`.

## Task 2: Reset Email Outbox and Job

**Files:**
- Modify: `src/CulinaryBlog.Application/Interfaces/IBackgroundTaskQueue.cs`
- Modify: `src/CulinaryBlog.API/Services/BackgroundTaskQueue.cs`
- Modify: `src/CulinaryBlog.API/Services/BackgroundJobs.cs`
- Modify: `src/CulinaryBlog.API/Services/RuntimeRegistration.cs`
- Modify: `src/CulinaryBlog.Application/Interfaces/IEmailService.cs`
- Modify: `src/CulinaryBlog.Infrastructure/Services/Email/EmailService.cs`
- Create: `src/CulinaryBlog.Infrastructure/Migrations/20261027090000_AddPasswordResetEmailTask.cs`
- Modify: `tests/CulinaryBlog.Integration.Tests/BackgroundTaskQueueTests.cs`
- Modify: `tests/CulinaryBlog.Integration.Tests/EmailVerificationJobTests.cs`
- Modify: `tests/CulinaryBlog.Integration.Tests/JobsObservabilityDatabaseTests.cs`
- Create: `tests/CulinaryBlog.Integration.Tests/PasswordResetEmailJobTests.cs`

**Interfaces:**
- Consumes Task 1's `IUserRepository.HasPasswordAsync` and `GeneratePasswordResetTokenAsync`.
- Produces `Task PasswordResetEmailAsync(string userId, CancellationToken ct)` and task kind `password-reset-email`.
- Produces `PasswordResetEmailJob.RunAsync(Guid taskId, CancellationToken ct)`.
- Produces `Task SendPasswordResetAsync(string email, string displayName, string link, CancellationToken cancellationToken = default)`.

- [ ] **Step 1: Add failing tests** for queue kind, dispatcher routing, and a reset job that sends a usable fragment link; test an ineligible/missing user sends no email and completes the task, and a mail failure leaves it retryable.
- [ ] **Step 2: Run focused tests** with `rtk dotnet test tests/CulinaryBlog.Integration.Tests/CulinaryBlog.Integration.Tests.csproj --filter "FullyQualifiedName~BackgroundTaskQueueTests|FullyQualifiedName~PasswordResetEmailJobTests"`; confirm the new cases fail.
- [ ] **Step 3: Add the outbox contract and routing**: implement `PasswordResetEmailAsync` in `DatabaseBackgroundTaskQueue` and `NoBackgroundTaskQueue`; add an explicit `password-reset-email` branch in `BackgroundTaskDispatcher` and register `PasswordResetEmailJob` in `RuntimeRegistration`.
- [ ] **Step 4: Add the forward migration** extending `BackgroundTasks_Kind_check` with `password-reset-email`; use the next migration timestamp after the current latest migration and preserve existing kinds/indexes.
- [ ] **Step 5: Implement the job and email service method**. Recheck that the user still exists, is email-confirmed, and has a local password; generate the Identity reset token at send time; build a `Site:BaseUrl` `/reset-password#userId=...&token=...` link with `Uri.EscapeDataString`; preserve production HTTPS validation and existing retry/logging behavior. Do not store the raw token in the outbox.
- [ ] **Step 6: Update all `IEmailService` fakes** in `EmailVerificationJobTests.cs` and `JobsObservabilityDatabaseTests.cs`, then run `rtk dotnet test tests/CulinaryBlog.Integration.Tests/CulinaryBlog.Integration.Tests.csproj --filter "FullyQualifiedName~BackgroundTaskQueueTests|FullyQualifiedName~PasswordResetEmailJobTests"`.
- [ ] **Step 7: Verify relational kind constraint and dispatch** using `rtk pwsh -File scripts/test-jobs-observability.ps1`; confirm a PostgreSQL outbox row accepts the new kind and dispatches to `PasswordResetEmailJob`, not `ImageResizeJob`.
- [ ] **Step 8: Commit** as `feat(auth): queue password reset emails`.

## Task 3: Signed-In Password Change

**Files:**
- Create: `src/CulinaryBlog.Application/Commands/Auth/ChangePassword/ChangePasswordCommand.cs`
- Create: `src/CulinaryBlog.Application/Commands/Auth/ChangePassword/ChangePasswordCommandHandler.cs`
- Create: `src/CulinaryBlog.Application/Commands/Auth/ChangePassword/ChangePasswordCommandValidator.cs`
- Test: `tests/CulinaryBlog.Application.Tests/Commands/ChangePasswordCommandHandlerTests.cs`
- Test: `tests/CulinaryBlog.Application.Tests/Commands/ChangePasswordCommandValidatorTests.cs`
- Test: `tests/CulinaryBlog.Integration.Tests/JobsObservabilityDatabaseTests.cs` (PostgreSQL transaction proof)

**Interfaces:**
- Consumes Task 1 `IUserRepository` methods, `IRefreshTokenRepository.RevokeByUserIdAsync`, and `IUnitOfWork.BeginTransactionAsync(IsolationLevel, CancellationToken)`.
- Produces `ChangePasswordCommand(string UserId, string CurrentPassword, string NewPassword) : IRequest<Unit>`.

- [ ] **Step 1: Write failing handler tests** for valid change + revoke + commit, wrong current password + rollback without revoke, Identity policy errors + rollback, user missing, and refresh-revocation exception + rollback.
- [ ] **Step 2: Run** `rtk dotnet test tests/CulinaryBlog.Application.Tests/CulinaryBlog.Application.Tests.csproj --filter FullyQualifiedName~ChangePasswordCommandHandlerTests`; confirm failures.
- [ ] **Step 3: Implement `ChangePasswordCommandHandler.Handle`**: find user by the authenticated user ID supplied by the controller; begin a serializable transaction; call Identity change; on failed Identity result throw `IdentityOperationException`; revoke all refresh tokens; commit only after both succeed; rollback and rethrow on failure.
- [ ] **Step 4: Add `ChangePasswordCommandValidator`** requiring non-empty current and new passwords; leave password-policy enforcement to Identity.
- [ ] **Step 5: Run handler and validator tests**; assert the transaction begins with `IsolationLevel.Serializable` and invalid operations never revoke tokens.
- [ ] **Step 6: Add a PostgreSQL-backed transaction test** in `JobsObservabilityDatabaseTests.cs`: use a real Identity user and `ApplicationDbContext`, make refresh revocation save then throw, and assert the previous password and refresh-token state remain unchanged after rollback.
- [ ] **Step 7: Commit** as `feat(auth): change signed-in password`.

## Task 4: Forgot and Reset Password Handlers plus Cooldown

**Files:**
- Create: `src/CulinaryBlog.Application/Commands/Auth/ForgotPassword/ForgotPasswordCommand.cs`
- Create: `src/CulinaryBlog.Application/Commands/Auth/ForgotPassword/ForgotPasswordCommandHandler.cs`
- Create: `src/CulinaryBlog.Application/Commands/Auth/ForgotPassword/ForgotPasswordCommandValidator.cs`
- Create: `src/CulinaryBlog.Application/Commands/Auth/ResetPassword/ResetPasswordCommand.cs`
- Create: `src/CulinaryBlog.Application/Commands/Auth/ResetPassword/ResetPasswordCommandHandler.cs`
- Create: `src/CulinaryBlog.Application/Commands/Auth/ResetPassword/ResetPasswordCommandValidator.cs`
- Create: `src/CulinaryBlog.Application/Interfaces/IPasswordResetRequestLimiter.cs`
- Create: `src/CulinaryBlog.API/Services/PasswordResetRequestLimiter.cs`
- Modify: `src/CulinaryBlog.API/Program.cs`
- Modify: `src/CulinaryBlog.Application/Exceptions/AuthException.cs`
- Test: `tests/CulinaryBlog.Application.Tests/Commands/ForgotPasswordCommandHandlerTests.cs`
- Test: `tests/CulinaryBlog.Application.Tests/Commands/ResetPasswordCommandHandlerTests.cs`
- Test: `tests/CulinaryBlog.Integration.Tests/PasswordResetRequestLimiterTests.cs`
- Modify: `tests/CulinaryBlog.Integration.Tests/JobsObservabilityDatabaseTests.cs` (PostgreSQL transaction proof)

**Interfaces:**
- Produces `ForgotPasswordCommand(string Email) : IRequest<Unit>` and `ResetPasswordCommand(string UserId, string Token, string NewPassword) : IRequest<Unit>`.
- Produces `IPasswordResetRequestLimiter.TryAcquireAsync(string normalizedEmail, CancellationToken ct = default)`.
- Uses Task 2 `IBackgroundTaskQueue.PasswordResetEmailAsync` and Task 1 `UserOperationResult`.

- [ ] **Step 1: Write failing forgot-handler tests** for normalized-email cooldown before account lookup, unknown/unconfirmed/passwordless accounts not queued, eligible account queued once, cooldown hit skipping lookup/enqueue, and limiter failure skipping lookup/enqueue.
- [ ] **Step 2: Write failing reset-handler tests** for valid reset + refresh revocation + serializable commit, Identity `InvalidToken` mapped to a generic reset-link `AuthException`, weak-password Identity errors, unknown user, and refresh-revocation failure rollback.
- [ ] **Step 3: Run** `rtk dotnet test tests/CulinaryBlog.Application.Tests/CulinaryBlog.Application.Tests.csproj --filter "FullyQualifiedName~ForgotPasswordCommandHandlerTests|FullyQualifiedName~ResetPasswordCommandHandlerTests"`; confirm failures.
- [ ] **Step 4: Implement the cooldown** with a Redis key `password_reset_request:{sha256(normalizedEmail)}`, `SET NX` expiry 60 seconds, and a separate in-memory implementation for tests. Fail closed on Redis errors. Register the interface next to the email-confirmation limiter in `Program.cs`.
- [ ] **Step 5: Implement forgot handler and validator**: trim/lowercase email, acquire cooldown before lookup, then queue only confirmed local-password accounts; every ineligible/cooldown/limiter-failure case returns `Unit` so the controller can use one response.
- [ ] **Step 6: Add `AuthException.InvalidPasswordResetToken()`** with code `AUTH_PASSWORD_RESET_INVALID` and status `400`; implement reset handler/validator so missing user or Identity error code `InvalidToken` maps to that same response, other Identity errors map through `IdentityOperationException`, and password update + refresh revocation commit only together.
- [ ] **Step 7: Add a PostgreSQL-backed reset transaction test** in `JobsObservabilityDatabaseTests.cs`: force refresh revocation to fail after save and assert the password reset and token changes both roll back.
- [ ] **Step 8: Run focused handler/validator/limiter tests** and confirm cooldown uses an independent key and does not share email-confirmation state.
- [ ] **Step 9: Commit** as `feat(auth): handle password reset requests`.

## Task 5: Auth API Endpoints and Integration Coverage

**Files:**
- Create: `src/CulinaryBlog.Application/DTOs/Auth/ChangePasswordRequest.cs`
- Create: `src/CulinaryBlog.Application/DTOs/Auth/ForgotPasswordRequest.cs`
- Create: `src/CulinaryBlog.Application/DTOs/Auth/ResetPasswordRequest.cs`
- Create: `src/CulinaryBlog.Application/DTOs/Auth/PasswordResetRequestAcceptedResponse.cs`
- Modify: `src/CulinaryBlog.API/Controllers/AuthController.cs`
- Modify: `src/CulinaryBlog.API/Program.cs`
- Modify: `tests/CulinaryBlog.Integration.Tests/CustomWebApplicationFactory.cs`
- Modify: `tests/CulinaryBlog.Integration.Tests/AuthControllerTests.cs`
- Modify: `tests/CulinaryBlog.Integration.Tests/AuthControllerProfileUnitTests.cs`

**Interfaces:**
- Exposes `POST /api/v1/auth/password/change` (authorized, `204`), `POST /api/v1/auth/password/forgot` (anonymous, `202` plus fixed message), and `POST /api/v1/auth/password/reset` (anonymous, `204` on success).
- Request records are `ChangePasswordRequest(string CurrentPassword, string NewPassword)`, `ForgotPasswordRequest(string Email)`, and `ResetPasswordRequest(string UserId, string Token, string NewPassword)`.
- Forgot response message: `Nếu email này thuộc tài khoản hợp lệ, hướng dẫn đặt lại mật khẩu sẽ được gửi.`

- [ ] **Step 1: Add failing integration tests** for unauthenticated change (`401`), valid change, same accepted status/body for known and unknown forgot emails, invalid reset link, and valid reset that makes the old password fail and new password succeed.
- [ ] **Step 2: Add a rate-limit test** using a dedicated fresh test factory configured with a small permit limit (for example, two requests); assert the third request receives `429`.
- [ ] **Step 3: Run** `rtk dotnet test tests/CulinaryBlog.Integration.Tests/CulinaryBlog.Integration.Tests.csproj --filter "FullyQualifiedName~AuthControllerTests|FullyQualifiedName~AuthControllerProfileUnitTests"`; confirm the new cases fail.
- [ ] **Step 4: Add endpoint request/response records and controller actions**. Validate commands with FluentValidation; derive the change-password user ID from claims (never request input); mark only change as `[Authorize]`; apply `[AllowAnonymous]` and `PasswordResetRateLimit` to forgot; return identical `202` body for all forgot-handler results.
- [ ] **Step 5: Configure `PasswordResetRateLimit`** in `Program.cs` using the existing auth endpoint rate-limit conventions, with a configuration override for the permit limit so `CustomWebApplicationFactory` can set a small test-only value while production keeps its normal limit; keep the per-email cooldown in the application handler.
- [ ] **Step 6: Run the focused integration tests** and verify missing/invalid reset tokens use one generic invalid-link error while password-policy errors remain Identity errors.
- [ ] **Step 7: Commit** as `feat(auth): expose password change and reset endpoints`.

## Task 6: Frontend Password Forms

**Files:**
- Modify: `frontend/app/lib/api.ts`
- Modify: `frontend/app/lib/types.ts`
- Modify: `frontend/app/login/page.tsx`
- Modify: `frontend/app/profile/page.tsx`
- Create: `frontend/app/forgot-password/page.tsx`
- Create: `frontend/app/reset-password/page.tsx`

**Interfaces:**
- Produces `requestPasswordReset(email)`, `resetPassword({ userId, token, newPassword })`, and `changePassword({ currentPassword, newPassword })` API helpers.
- Uses `UserProfileResponse.hasPassword` from Task 1.

- [ ] **Step 1: Add typed API functions and request types** for all three endpoints; send forgot/reset requests without auth headers, and send change requests through `authFetch`. For change/reset `204` success, skip `handleResponse` JSON parsing and follow `deleteAvatar`'s no-content pattern.
- [ ] **Step 2: Add the login recovery link and forgot form**. Show the fixed generic confirmation only after an accepted response; display network/server errors without changing the generic account-existence message.
- [ ] **Step 3: Add the reset page**. Parse `userId` and token from `window.location.hash` using `URLSearchParams`, clear the fragment from browser history after reading it, submit only in the JSON POST body, and show a generic invalid/expired-link state for reset-token failures.
- [ ] **Step 4: Add the profile change form** using shared `Input`/`Button` components; render it only when the fetched profile has `hasPassword === true`. On success, clear auth state and navigate to `/login`.
- [ ] **Step 5: Run `rtk pnpm --dir frontend lint` and `rtk pnpm --dir frontend build`**; fix only issues introduced by this feature.
- [ ] **Step 6: Commit** as `feat(auth): add password recovery forms`.

## Final Verification

- [ ] Run `rtk dotnet test tests/CulinaryBlog.Application.Tests/CulinaryBlog.Application.Tests.csproj`.
- [ ] Run `rtk dotnet test tests/CulinaryBlog.Integration.Tests/CulinaryBlog.Integration.Tests.csproj`.
- [ ] Run `rtk pwsh -File scripts/test-jobs-observability.ps1` for the PostgreSQL outbox constraint and dispatcher path.
- [ ] Run `rtk pnpm --dir frontend lint` and `rtk pnpm --dir frontend build`.
- [ ] Review the final diff against the spec; confirm no reset token is persisted or sent outside the URL fragment/POST body, all successful password mutations revoke refresh tokens transactionally, and frontend clears its current session.
