# FR-AUTH-008 Email Verification Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Require local accounts to confirm email before any authenticated use, then automatically sign them in; trust verified Google OAuth email without sending a separate confirmation email.

**Architecture:** Reuse ASP.NET Core Identity confirmation tokens and the existing durable background-task outbox/SMTP sender. Add confirm and resend commands, change registration/login/refresh behavior, and enforce current confirmation state centrally for all protected API requests. Add `/verify-email` and update the existing register/login flows to match.

**Tech Stack:** ASP.NET Core Identity, MediatR, EF Core/PostgreSQL, existing background task dispatcher and SMTP `EmailService`, Next.js App Router, React, TypeScript.

**Spec:** `docs/superpowers/specs/2026-10-19-fr-auth-008-email-verification-design.md`

## Global Constraints

- Confirmation link validity is **24 hours**.
- Local registration returns pending-confirmation state and issues no access or refresh token.
- Successful first confirmation returns the normal `AuthResponse`; replay does not issue another session.
- The link carries the confirmation token in the URL fragment, never query or path; confirmation state changes only through `POST`.
- Google accounts skip separate confirmation only when validated Google data asserts verified email; missing/false assertion fails closed.
- Every protected API checks current server-side `EmailConfirmed`; anonymous confirmation/resend/logout and public Guest routes remain available.
- Legacy local accounts are not bulk-marked confirmed; existing Google accounts must reauthenticate with verified Google email.
- Reuse Identity tokens, SMTP, and the durable outbox; add no dependency or custom confirmation-token table.
- Never log raw confirmation tokens or full tokenized links; use configured frontend origin and HTTPS in production.

## Review Focus

- A confirmation email delayed in the outbox must not arrive with an already-expired token; generate the 24-hour token when the email job sends it, and test token generation at job execution.
- Concurrent confirmation requests for the same account/token must not create multiple sessions; test a parallel first-confirmation race.
- Repeated resend for one account must enqueue another email without weakening deduplication for welcome/resize jobs; test both behaviors.
- An absent or false Google verified-email assertion must neither confirm nor link an account; cover callback claim parsing and handler behavior.
- Legacy bearer tokens must fail on every protected endpoint, including `/api/v1/auth/me`, while public and anonymous endpoints remain usable; test both sides.

---

### Task 1: Identity token configuration and verification email outbox

**Files:**
- Modify: `src/CulinaryBlog.API/Program.cs`
- Modify: `src/CulinaryBlog.Application/Interfaces/IEmailService.cs`
- Modify: `src/CulinaryBlog.Application/Interfaces/IBackgroundTaskQueue.cs`
- Modify: `src/CulinaryBlog.API/Services/BackgroundTaskQueue.cs`
- Modify: `src/CulinaryBlog.API/Services/BackgroundJobs.cs`
- Modify: `src/CulinaryBlog.API/Services/RuntimeRegistration.cs`
- Modify: `src/CulinaryBlog.Infrastructure/Services/Email/EmailService.cs`
- Create: new EF migration under `src/CulinaryBlog.Infrastructure/Migrations/` to allow repeated verification tasks while retaining welcome/resize deduplication
- Test: `tests/CulinaryBlog.Integration.Tests/EmailVerificationJobTests.cs`
- Test: `tests/CulinaryBlog.Integration.Tests/BackgroundTaskQueueTests.cs`
- Modify: `tests/CulinaryBlog.Integration.Tests/JobsObservabilityDatabaseTests.cs` (`RecordingEmail` implements the extended email interface)

**Interfaces:**
- Consumes: `IBackgroundTaskQueue.WelcomeAsync(string userId, CancellationToken ct)`; existing `EmailService` SMTP configuration `Email:*` and `Site:BaseUrl`.
- Produces: `IEmailService.SendEmailConfirmationAsync(string email, string displayName, string link, CancellationToken cancellationToken = default) -> Task`; `IBackgroundTaskQueue.EmailConfirmationAsync(string userId, CancellationToken ct) -> Task`; `EmailVerificationJob.RunAsync(Guid taskId, CancellationToken ct) -> Task`.

- [x] **Step 1: Add verification-job tests** for a pending user: job creates an Identity confirmation token at send time, builds a URL using configured frontend origin and a URL fragment, and calls the email service. Also assert confirmed/missing users do not receive confirmation email, and missing/malformed `Site:BaseUrl` or an HTTP URL in production fails the job clearly without sending email.
- [x] **Step 2: Run the focused test** with `dotnet test tests/CulinaryBlog.Integration.Tests --filter FullyQualifiedName~EmailVerificationJobTests`; expect failures because the job and email method do not exist.
- [x] **Step 3: Add the email-service method and implement its HTML email** using the existing SMTP config; HTML-encode user-controlled content and do not log the link/token.
- [x] **Step 4: Register Identity token providers in `Program.cs` and configure a 24-hour data-protection token lifetime explicitly.** Keep the current default Identity provider; document its shared lifetime effect in code/configuration notes if future Identity token purposes are added.
- [x] **Step 5: Validate the configured frontend origin and build the verification link.** Require a valid absolute `Site:BaseUrl`; in production require HTTPS and fail the job clearly when the setting is missing or invalid. Never silently fall back to a development URL. Generate the Identity token when the job runs, then create `/verify-email#userId={id}&token={Uri.EscapeDataString(token)}` from that trusted origin; do not use request `Host`. Register the job in `RuntimeRegistration` and route the new task kind to it in `BackgroundTaskDispatcher`.
- [x] **Step 6: Add the outbox migration and queue insert path.** Extend the `Kind` check constraint with `email-verification`; replace the table-wide unique constraint with a partial unique index for `welcome` and `resize`; use general `ON CONFLICT DO NOTHING` for queue inserts. This allows repeat verification tasks without changing existing welcome/resize deduplication.
- [x] **Step 7: Test outbox semantics:** two verification requests for one user create two pending jobs, while duplicate welcome/resize intents remain deduplicated. Run the focused integration tests and require PASS.
- [x] **Step 8: Commit** as `feat(auth): queue email verification messages`.

### Task 2: Confirm and resend email API

**Files:**
- Create: `src/CulinaryBlog.Application/Commands/Auth/ConfirmEmail/ConfirmEmailCommand.cs`
- Create: `src/CulinaryBlog.Application/Commands/Auth/ConfirmEmail/ConfirmEmailCommandHandler.cs`
- Create: `src/CulinaryBlog.Application/Commands/Auth/ConfirmEmail/ConfirmEmailCommandValidator.cs`
- Create: `src/CulinaryBlog.Application/Commands/Auth/ResendEmailConfirmation/ResendEmailConfirmationCommand.cs`
- Create: `src/CulinaryBlog.Application/Commands/Auth/ResendEmailConfirmation/ResendEmailConfirmationCommandHandler.cs`
- Modify: `src/CulinaryBlog.Application/Interfaces/IUserRepository.cs`
- Create: `src/CulinaryBlog.Application/Interfaces/IEmailConfirmationResendLimiter.cs`
- Modify: `src/CulinaryBlog.Infrastructure/Services/UserRepository.cs`
- Modify: `src/CulinaryBlog.API/Controllers/AuthController.cs`
- Modify: `src/CulinaryBlog.API/Program.cs` for resend rate-limit policy
- Create: `src/CulinaryBlog.API/Services/EmailConfirmationResendLimiter.cs`
- Test: new command tests under `tests/CulinaryBlog.Application.Tests/Commands/`
- Test: `tests/CulinaryBlog.Integration.Tests/AuthControllerTests.cs`

**Interfaces:**
- Consumes: Task 1 `EmailConfirmationAsync(string userId, CancellationToken)`.
- Produces: `ConfirmEmailCommand(string UserId, string Token) : IRequest<AuthResponse>`; `ResendEmailConfirmationCommand(string Email) : IRequest<Unit>`; `IEmailConfirmationResendLimiter.TryAcquireAsync(string normalizedEmail, CancellationToken ct) -> Task<bool>`; anonymous `POST /api/v1/auth/email/confirm` and `POST /api/v1/auth/email/resend`.
- Repository additions: `GenerateEmailConfirmationTokenAsync(ApplicationUser user, CancellationToken ct) -> Task<string>` and `ConfirmEmailAsync(ApplicationUser user, string token, CancellationToken ct) -> Task<(bool Succeeded, IEnumerable<string> Errors)>`.

- [x] **Step 1: Add handler tests** for valid, invalid, expired, already-confirmed and concurrent confirmation; assert only the first successful transition returns `AuthResponse` and persists one refresh token.
- [x] **Step 2: Add resend tests** asserting pending account queues a job; unknown/already-confirmed addresses return the same result and queue nothing; the Redis email cooldown is 60 seconds and the ASP.NET policy allows 5 requests per minute per client IP.
- [x] **Step 3: Run the focused command tests** with `dotnet test tests/CulinaryBlog.Application.Tests --filter "FullyQualifiedName~ConfirmEmail|FullyQualifiedName~ResendEmailConfirmation"`; expect missing command failures.
- [x] **Step 4: Add `IUserRepository.GenerateEmailConfirmationTokenAsync(ApplicationUser user, CancellationToken ct) -> Task<string>` and `IUserRepository.ConfirmEmailAsync(ApplicationUser user, string token, CancellationToken ct) -> Task<(bool Succeeded, IEnumerable<string> Errors)>`** backed by `UserManager`; retain `FindByIdAsync` for account lookup.
- [x] **Step 5: Implement `ConfirmEmailCommandHandler.Handle(ConfirmEmailCommand, CancellationToken) -> Task<AuthResponse>`.** Confirm under a transaction/serialized state transition, reject invalid token, and issue/store the standard access/refresh pair only for the first successful confirmation.
- [x] **Step 6: Implement `EmailConfirmationResendLimiter`** for `IEmailConfirmationResendLimiter` using the existing `IConnectionMultiplexer`: SHA-256 the normalized email for the Redis key and acquire a 60-second `SET NX EX` cooldown. Keep the endpoint's ASP.NET rate limit at 5 requests per minute per IP; fail closed if the distributed email limiter is unavailable.
- [x] **Step 7: Implement `ResendEmailConfirmationCommandHandler.Handle(ResendEmailConfirmationCommand, CancellationToken) -> Task<Unit>`.** Normalize the address and consume the hashed-email cooldown for every request before checking account state; return the same accepted result for all addresses and enqueue only for a pending account when not rate limited.
- [x] **Step 8: Add anonymous POST actions to `AuthController`** with request DTOs and validation. Confirmation accepts `userId` and `token`, returns `AuthResponse` on first success; resend returns generic `202`.
- [x] **Step 9: Add API integration tests** for success, invalid/expired token, replay, no-email-enumeration resend, rate limiting, and no state change on GET. Run the focused tests and require PASS.
- [x] **Step 10: Commit** as `feat(auth): confirm and resend email tokens`.

### Task 3: Registration pending state and local login/refresh gate

**Files:**
- Modify: `src/CulinaryBlog.Application/Commands/Auth/Register/RegisterCommand.cs`
- Modify: `src/CulinaryBlog.Application/Commands/Auth/Register/RegisterCommandHandler.cs`
- Modify: `src/CulinaryBlog.API/Controllers/AuthController.cs`
- Modify: `src/CulinaryBlog.Application/Commands/Auth/Login/LoginCommandHandler.cs`
- Modify: `src/CulinaryBlog.Application/Commands/Auth/RefreshToken/RefreshTokenCommandHandler.cs`
- Modify: `src/CulinaryBlog.Application/Exceptions/AuthException.cs`
- Create: `src/CulinaryBlog.Application/DTOs/Auth/RegistrationPendingResponse.cs` with `RegistrationPendingResponse(string Email, bool EmailConfirmationRequired)`
- Test: `tests/CulinaryBlog.Application.Tests/Commands/RegisterCommandHandlerTests.cs`
- Test: `tests/CulinaryBlog.Application.Tests/Commands/LoginCommandHandlerTests.cs`
- Test: `tests/CulinaryBlog.Application.Tests/Commands/RefreshTokenCommandHandlerTests.cs`
- Test: `tests/CulinaryBlog.Integration.Tests/AuthControllerTests.cs`
- Modify: `tests/CulinaryBlog.Integration.Tests/AvatarTests.cs`
- Modify: `tests/CulinaryBlog.Integration.Tests/CategoryTests.cs`
- Modify: `tests/CulinaryBlog.Integration.Tests/JobsObservabilityDatabaseTests.cs`
- Modify: `tests/CulinaryBlog.Integration.Tests/SearchAndFileTests.cs`
- Modify: `tests/CulinaryBlog.Integration.Tests/RecipeLifecycleTests.cs`
- Create: `tests/CulinaryBlog.Integration.Tests/Helpers/AuthTestHelpers.cs`

**Interfaces:**
- Consumes: Task 1 email queue method and Task 2 resend/confirm result contracts.
- Produces: registration result `{ email, emailConfirmationRequired: true }` with no tokens; HTTP `403` with stable `EMAIL_NOT_CONFIRMED` error for correct credentials on a pending account.
- Test helper: `RegisterConfirmedUserAsync(HttpClient client, IServiceProvider services, RegisterRequest request) -> Task<AuthResponse>` for existing integration scenarios that need an authenticated user; the registration endpoint test continues to assert the pending response directly.

- [x] **Step 1: Update registration handler tests** so valid registration creates `EmailConfirmed=false`, queues welcome and confirmation tasks transactionally, and returns no access/refresh tokens.
- [x] **Step 2: Run `dotnet test tests/CulinaryBlog.Application.Tests --filter FullyQualifiedName~RegisterCommandHandlerTests`; verify changed assertions fail against current behavior.**
- [x] **Step 3: Change `RegisterCommand` result and handler** to return a pending-confirmation DTO, stop generating/storing tokens, and queue the verification task with the user in the existing unit-of-work transaction.
- [x] **Step 4: Update `AuthControllerTests`** for the `201` pending response and absence of token fields; preserve validation/conflict/rate-limit behavior.
- [x] **Step 5: Add `RegisterConfirmedUserAsync` test helper** that registers through the API, obtains an Identity confirmation token from the test service scope, confirms through the API, and returns the resulting `AuthResponse`.
- [x] **Step 6: Update integration tests that currently deserialize registration as `AuthResponse`** (`AvatarTests`, `CategoryTests`, `JobsObservabilityDatabaseTests`, `SearchAndFileTests`, and `RecipeLifecycleTests`) to use the helper when they need authenticated users. Keep pending-registration tests on the raw endpoint.
- [x] **Step 7: Add login tests** proving correct password + `EmailConfirmed=false` returns `EMAIL_NOT_CONFIRMED`, invalid password remains generic, and confirmed login retains current rotation behavior.
- [x] **Step 8: Add `AuthException.EmailNotConfirmed()`** returning HTTP `403` and code `EMAIL_NOT_CONFIRMED`; implement the login gate after password verification so only correct credentials receive this response and no tokens or refresh-token changes occur for pending accounts.
- [x] **Step 9: Add refresh tests** proving unconfirmed accounts cannot rotate tokens and confirmed accounts keep current behavior; implement the check before issuing a replacement token.
- [x] **Step 10: Run the focused register/login/refresh application and integration tests; require PASS.**
- [x] **Step 11: Commit** as `feat(auth): require email confirmation for local login`.

### Task 4: Enforce confirmation for all protected API requests

**Files:**
- Modify: `src/CulinaryBlog.API/Program.cs` JWT bearer validation configuration
- Test: `tests/CulinaryBlog.Integration.Tests/AuthControllerTests.cs`
- Test: `tests/CulinaryBlog.Integration.Tests/RecipeLifecycleTests.cs`

**Interfaces:**
- Consumes: persisted Identity `EmailConfirmed` state from Task 3.
- Produces: JWT validation that rejects an unconfirmed/missing user for every endpoint requiring authentication, while `AllowAnonymous` confirmation/resend/logout and public Guest routes remain usable.

- [x] **Step 1: Add integration tests** with a valid JWT for an unconfirmed user and assert protected `GET /api/v1/auth/me` and an Author mutation are rejected; assert public category/recipe reads and anonymous confirmation/resend/logout remain reachable.
- [x] **Step 2: Add a regression test** that a token issued before a user's `EmailConfirmed` becomes false is rejected after the state change.
- [x] **Step 3: Implement a central server-side confirmation check in JWT `OnTokenValidated`** by loading the token subject's current user record and failing authentication when missing or unconfirmed. Do not trust a JWT `emailConfirmed` claim alone.
- [x] **Step 4: Run the focused authorization integration tests; require protected routes to reject and public/anonymous routes to pass.**
- [x] **Step 5: Commit** as `fix(auth): reject unconfirmed protected requests`.

### Task 5: Trust Google verified email and re-confirm legacy OAuth users

**Files:**
- Modify: `src/CulinaryBlog.API/Program.cs` Google OAuth claim mapping/event
- Modify: `src/CulinaryBlog.API/Controllers/AuthController.cs` Google callback
- Modify: `src/CulinaryBlog.Application/Commands/Auth/GoogleLogin/GoogleLoginCommand.cs`
- Modify: `src/CulinaryBlog.Application/Commands/Auth/GoogleLogin/GoogleLoginCommandHandler.cs`
- Modify: `src/CulinaryBlog.Application/Interfaces/IUserRepository.cs`
- Modify: `src/CulinaryBlog.Infrastructure/Services/UserRepository.cs`
- Test: `tests/CulinaryBlog.Application.Tests/Commands/GoogleLoginCommandHandlerTests.cs`
- Test: `tests/CulinaryBlog.Integration.Tests/GoogleOAuthCallbackTests.cs` using a fake external auth scheme

**Interfaces:**
- Produces: `GoogleLoginCommand(string Provider, string ProviderKey, string Email, string DisplayName, string? AvatarUrl, bool EmailVerified) : IRequest<AuthResponse>`, with `EmailVerified` sourced only from validated Google user-info/OpenID data.

- [x] **Step 1: Add Google handler tests** for new verified identity, linked verified identity, existing local account verified and linked, missing/false verification, and no auto-link on unverified matching email.
- [x] **Step 2: Run `dotnet test tests/CulinaryBlog.Application.Tests --filter FullyQualifiedName~GoogleLoginCommandHandlerTests`; verify unverified cases fail under current behavior.**
- [x] **Step 3: Map the provider's verified-email assertion in `Program.cs`/Google callback** from the validated provider principal or trusted user-info response; fail closed if absent, and never read verification state from request input.
- [x] **Step 4: Extend `GoogleLoginCommand` with `EmailVerified` and update the handler** to set `EmailConfirmed=true` for newly created and matched legacy Google accounts only when verified; issue existing token response and do not queue confirmation email.
- [x] **Step 5: Add callback integration coverage** proving unverified Google identity returns a safe login error and verified Google identity produces the existing token callback flow.
- [x] **Step 6: Run Google handler/controller tests; require PASS.**
- [x] **Step 7: Commit** as `fix(auth): require verified Google email`.

### Task 6: Frontend registration, login, and verification page

**Files:**
- Modify: `frontend/app/lib/types.ts`
- Modify: `frontend/app/lib/api.ts`
- Modify: `frontend/app/context/AppContext.tsx`
- Modify: `frontend/app/register/page.tsx`
- Modify: `frontend/app/login/page.tsx`
- Create: `frontend/app/verify-email/page.tsx`
- Modify: `frontend/next.config.ts` to set the verification page response header
- Modify: `frontend/app/auth/google-callback/page.tsx` only if required for verified callback/error response compatibility

**Interfaces:**
- Consumes: pending registration response, `POST` confirm returning `AuthResponse`, and generic resend response from Tasks 2–5.
- Produces: `confirmEmail(userId: string, token: string): Promise<AuthResponse>` and `resendEmailConfirmation(email: string): Promise<void>` in `api.ts`.

- [x] **Step 1: Add the API and frontend type helpers** for pending registration, confirm, and resend; preserve existing `authFetch` behavior for authenticated APIs and use public `fetch` for anonymous auth endpoints.
- [x] **Step 2: Update register context/page** to stop storing tokens after registration and show a check-email state with resend, retaining the submitted email for display.
- [x] **Step 3: Update login context/page** to show a specific confirmation-required message and resend action only for `EMAIL_NOT_CONFIRMED`; keep invalid-credential copy generic.
- [x] **Step 4: Build `/verify-email` as a client page** that reads `userId`/token from `location.hash`, immediately strips the fragment with `history.replaceState`, displays explicit confirmation action, then calls confirm `POST` only after the user presses it.
- [x] **Step 5: Match the existing frontend style** by following the layout, spacing, typography, colors, and shared `Button`/`Input` components used by the current register/login pages; avoid introducing new colors or one-off UI patterns.
- [x] **Step 6: On confirmation success, store access/refresh/user through the existing auth helpers and redirect only to a validated same-origin path or home.** Show already-confirmed, expired/invalid, and retryable network states; expired state offers resend.
- [x] **Step 7: Set `Referrer-Policy: no-referrer` for `/verify-email` in `next.config.ts` and ensure the page loads no third-party analytics/resources while handling the token.**
- [x] **Step 8: Run `pnpm --dir frontend lint` and `pnpm --dir frontend exec tsc --noEmit`; require both PASS.**
- [x] **Step 9: Manually verify registration pending state, valid link auto-login, expired link/resend, login with unconfirmed user, safe return path, Google callback sign-in, and the `/verify-email` response header. Compare the page visually with the existing register/login pages on desktop and mobile for matching layout, spacing, typography, colors, and shared components.**
- [x] **Step 10: Commit** as `feat(frontend): verify email before local login`.

### Task 7: Align FR-AUTH contracts and acceptance documents

**Files:**
- Modify: `docs/specs/fr-auth/FR-AUTH_APIContract.md`
- Modify: `docs/specs/fr-auth/FR-AUTH_BaoCao.md`
- Modify: `docs/specs/fr-auth/FR-AUTH_Database_Design.md`
- Modify: `docs/specs/fr-auth/FR-AUTH_SecuritySpec.md`
- Modify: `docs/specs/fr-auth/FR-AUTH_ErrorCodes.md`
- Modify: `docs/specs/fr-auth/FR-AUTH_TestPlan.md`
- Modify: `docs/specs/fr-auth/FR-AUTH-001_EndUser_Test_Scenarios.md`
- Modify: `docs/specs/fr-auth/FR-AUTH-001_Design.md`
- Modify: `docs/specs/fr-auth/FR-AUTH-004_EndUser_Test_Scenarios.md` if its login/refresh expectations mention unconfirmed accounts
- Modify: `docs/specs/fr-auth/FR-AUTH-001_Plan.md`
- Modify: `docs/superpowers/plans/2026-09-23-fr-auth-001-implementation.md`
- Modify: `docs/superpowers/plans/2026-09-26-FR-AUTH-002-login.md`
- Modify: `docs/superpowers/plans/2026-09-26-FR-AUTH-002-frontend-integration.md`
- Modify: `docs/superpowers/plans/2025-12-14-fr-auth-003-google-oauth-implementation-plan.md`

**Interfaces:**
- Consumes: shipped behavior from Tasks 1–6.
- Produces: consistent API, requirement, test, and implementation documentation with no remaining claim that local registration returns tokens immediately or that confirmation mutates state through `GET`.

- [x] **Step 1: Update API contract and error-code catalog** with pending registration response, confirm/resend POST bodies and results, HTTP 403 `EMAIL_NOT_CONFIRMED`, 24-hour fragment link, verified Google behavior, rate limits, and authorization behavior.
- [x] **Step 2: Update FR-AUTH report, database, and security specs** for registration without auto-login, Identity-protected token instead of a custom token table, local login gate, Google verified-email bypass, confirmed-email requirement on protected APIs, and legacy account rollout.
- [x] **Step 3: Update FR-AUTH-001 design, test/UAT, and affected plans** so descriptions and old tests expecting local registration to return `AuthResponse` tokens and auto-login are explicitly superseded, and the new pending-confirmation outcomes are listed.
- [x] **Step 4: Search FR-AUTH docs/plans for stale `auto-login`, `GET /auth/verify-email`, GUID token table, and immediate registration token claims; resolve every relevant match.**
- [x] **Step 5: Review `git diff --check` and the documentation diff; commit** as `docs(auth): align email verification contracts`.

## Final Verification

- [x] Run `dotnet test tests/CulinaryBlog.Application.Tests`.
- [x] Run `dotnet test tests/CulinaryBlog.Integration.Tests`.
- [x] Run `pnpm --dir frontend lint` and `pnpm --dir frontend exec tsc --noEmit`.
- [x] Review all acceptance criteria in the spec, especially legacy token rejection and Guest access.
