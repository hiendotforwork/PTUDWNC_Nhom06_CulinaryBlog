# FR-AUTH-005 Logout Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Revoke the submitted refresh token through `POST /api/v1/auth/logout` and always clear the browser's local authentication state.

**Architecture:** Logout is authorized by possession of the refresh token, not by a JWT, so it remains available after an access token expires. The application handler hashes the submitted token and revokes it only when the matching persisted token is active; every semantic token outcome is a no-op or success from the client’s perspective and maps to HTTP 204. The frontend attempts the revoke, then clears browser state in `finally`.

**Tech Stack:** ASP.NET Core 10, MediatR, EF Core, xUnit/Moq/FluentAssertions, Next.js 16, TypeScript.

**Spec:** `docs/specs/SRS_Culinary_Blog_v1.0.0.md` (FR-AUTH-005, lines 953-985); `docs/specs/fr-auth/FR-AUTH_BaoCao.md` (FR-AUTH-005, lines 380-415).

## Global Constraints

- Do not add packages, configuration, schema migrations, or new database indexes; `RefreshTokens.TokenHash` and `RefreshToken.HashToken` already exist.
- `POST /api/v1/auth/logout` accepts a JSON body containing `refreshToken`; semantic outcomes for absent, invalid, expired, revoked, or unknown tokens return `204 No Content`.
- The endpoint must work with no access token or an expired/invalid `Authorization` header; use `[AllowAnonymous]` and never identify a user from a JWT.
- Revoke only the one matching active refresh token; do not revoke a user's other sessions.
- Never return token existence, ownership, expiry, or revocation status; malformed JSON and unexpected server failures retain normal framework error behavior.
- The client must clear access token, refresh token, stored user, and `currentUser` even if the network request fails.

## Review Focus

- Empty or omitted `refreshToken` must produce `204` without hashing or saving; covered by Task 1.
- A token that is already revoked or expired must remain untouched and return `204`; covered by Tasks 1 and 2.
- A raw token must be SHA-256 hashed before repository lookup; covered by Task 1.
- A valid refresh token must be revoked without a usable access token; covered by Task 2.
- A failed logout request must not leave frontend credentials in `localStorage`; covered by Task 3's manual test.

---

## File Structure

| File | Responsibility |
|---|---|
| `src/CulinaryBlog.Application/DTOs/Auth/LogoutRequest.cs` | Transport body containing the optional raw refresh token. |
| `src/CulinaryBlog.Application/Commands/Auth/Logout/LogoutCommand.cs` | MediatR command carrying the raw token to the application layer. |
| `src/CulinaryBlog.Application/Commands/Auth/Logout/LogoutCommandHandler.cs` | Hashes, finds, and conditionally revokes the one active token. |
| `tests/CulinaryBlog.Application.Tests/Commands/LogoutCommandHandlerTests.cs` | Unit proofs of revoke/no-op behavior. |
| `src/CulinaryBlog.API/Controllers/AuthController.cs` | Exposes anonymous `POST /logout` and translates every semantic result to 204. |
| `tests/CulinaryBlog.Integration.Tests/AuthControllerTests.cs` | Verifies the endpoint contract and persisted revocation. |
| `frontend/app/lib/api.ts` | Sends the logout request without a Bearer header. |
| `frontend/app/context/AppContext.tsx` | Revokes remotely, then always clears local auth state. |
| `frontend/app/components/layout/Header.tsx` | Invokes the now-async context logout without leaving unhandled promises. |

### Task 1: Application logout command and unit tests

**Files:**
- Create: `src/CulinaryBlog.Application/DTOs/Auth/LogoutRequest.cs`
- Create: `src/CulinaryBlog.Application/Commands/Auth/Logout/LogoutCommand.cs`
- Create: `src/CulinaryBlog.Application/Commands/Auth/Logout/LogoutCommandHandler.cs`
- Create: `tests/CulinaryBlog.Application.Tests/Commands/LogoutCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `IRefreshTokenRepository.GetByTokenHashAsync(string, CancellationToken)`, `IRefreshTokenRepository.SaveChangesAsync(CancellationToken)`, and `RefreshToken.HashToken(string)` / `RefreshToken.Revoke()`.
- Produces: `public record LogoutRequest(string? RefreshToken);`, `public record LogoutCommand(string? RefreshToken) : IRequest;`, and `LogoutCommandHandler : IRequestHandler<LogoutCommand>`.

**Acceptance Criteria:**

- Raw non-empty input is hashed before exactly one repository lookup.
- Only an active matching token gets `RevokedAt` set and triggers exactly one save.
- Null, whitespace, missing, unknown, expired, and previously revoked tokens complete successfully without a save or exception.
- The handler returns no token-state information.

**Test Cases:**

| Name | Setup | Expected assertion |
|---|---|---|
| `Handle_ActiveToken_RevokesAndSaves` | Repository returns `RefreshToken.Create(...)` for the SHA-256 hash. | `IsRevoked` is true; lookup uses `RefreshToken.HashToken(raw)`; save once. |
| `Handle_MissingOrWhitespaceToken_IsNoOp` | `null`, `""`, and whitespace commands. | No lookup, no save, no exception. |
| `Handle_UnknownExpiredOrRevokedToken_IsNoOp` | Repository returns null, expired token, then revoked token. | No save and no exception in each case. |

- [ ] **Step 1: Write failing handler tests**

Create `LogoutCommandHandlerTests` with the three cases above. Set up `IRefreshTokenRepository` using Moq; create expired tokens by setting `ExpiresAt` to a past UTC timestamp and revoked tokens via `Revoke()`.

- [ ] **Step 2: Run the handler tests to verify RED**

Run: `dotnet test tests/CulinaryBlog.Application.Tests --filter FullyQualifiedName~LogoutCommandHandlerTests`

Expected: compilation failure because the logout DTO, command, and handler do not exist.

- [ ] **Step 3: Implement the minimal command handler**

Create the DTO and command with the signatures in **Interfaces**. Implement `Task Handle(LogoutCommand request, CancellationToken cancellationToken)` to return immediately for null/whitespace input; otherwise hash, look up, and call `Revoke()` plus `SaveChangesAsync()` only when `token?.IsActive == true`.

- [ ] **Step 4: Run the handler tests to verify GREEN**

Run: `dotnet test tests/CulinaryBlog.Application.Tests --filter FullyQualifiedName~LogoutCommandHandlerTests`

Expected: all three test cases pass.

- [ ] **Step 5: Commit the application slice**

```bash
git add src/CulinaryBlog.Application/DTOs/Auth/LogoutRequest.cs src/CulinaryBlog.Application/Commands/Auth/Logout tests/CulinaryBlog.Application.Tests/Commands/LogoutCommandHandlerTests.cs
git commit -m "feat: add refresh-token logout command"
```

### Task 2: HTTP endpoint and integration contract

**Files:**
- Modify: `src/CulinaryBlog.API/Controllers/AuthController.cs`
- Modify: `tests/CulinaryBlog.Integration.Tests/AuthControllerTests.cs`

**Interfaces:**
- Consumes: `LogoutRequest` and `LogoutCommand` from Task 1 via `IMediator.Send(LogoutCommand, CancellationToken)`.
- Produces: anonymous `POST /api/v1/auth/logout` returning `204 No Content` for all semantic refresh-token outcomes.

**Acceptance Criteria:**

- The action is explicitly `[AllowAnonymous]`, accepts `LogoutRequest?`, and passes `request?.RefreshToken` to the command.
- A registered user's refresh token is revoked after the endpoint returns `204`.
- The endpoint returns `204` with no Authorization header for unknown, expired, and already-revoked tokens; it exposes no response body.
- No endpoint requires or attempts to validate a bearer token for logout.

**Test Cases:**

| Name | Setup | Expected assertion |
|---|---|---|
| `Logout_WithActiveRefreshToken_Returns204AndRevokesToken` | Register a unique user; POST its returned refresh token without Authorization. | Status 204; `ApplicationDbContext.RefreshTokens` contains the token hash with non-null `RevokedAt`. |
| `Logout_WithUnknownRefreshToken_Returns204` | POST a random token without Authorization. | Status 204 and empty body. |
| `Logout_WithExpiredOrRevokedRefreshToken_Returns204` | Seed an expired token and a revoked token, then POST each raw token. | Each response is 204 and neither token is changed. |
| `Logout_WithoutBody_Returns204` | POST with no content. | Status 204 and empty body. |
| `Logout_WithMissingNullOrBlankRefreshToken_Returns204` | POST `{}`, `null`, empty, and whitespace `refreshToken` JSON values. | Each response is 204 and empty. |
| `Logout_WithInvalidAuthorizationHeader_Returns204AndRevokesToken` | POST an active refresh token with a valid-but-expired JWT, then malformed Bearer credentials. | Each response is 204/empty and each submitted refresh token is revoked. |

- [ ] **Step 1: Write failing integration tests**

Append the six cases above to `AuthControllerTests`. Use `_factory.Services.CreateScope()` and `ApplicationDbContext` to seed/read refresh tokens. Use `RefreshToken.HashToken(raw)` when asserting persisted state. Send missing-body and JSON null/blank cases as real HTTP payloads, and sign the expired JWT with the test JWT secret so the test demonstrates expiry rather than a malformed-token path.

- [ ] **Step 2: Run integration tests to verify RED**

Run: `dotnet test tests/CulinaryBlog.Integration.Tests --filter FullyQualifiedName~AuthControllerTests`

Expected: failing route/404 because `/api/v1/auth/logout` is not implemented.

- [ ] **Step 3: Add the endpoint**

Import `CulinaryBlog.Application.Commands.Auth.Logout` and `Microsoft.AspNetCore.Authorization`. Add `Logout([FromBody] LogoutRequest? request, CancellationToken cancellationToken)` to `AuthController`, decorate it with `[HttpPost("logout")]` and `[AllowAnonymous]`, send `new LogoutCommand(request?.RefreshToken)`, and return `NoContent()`.

- [ ] **Step 4: Run integration tests to verify GREEN**

Run: `dotnet test tests/CulinaryBlog.Integration.Tests --filter FullyQualifiedName~AuthControllerTests`

Expected: all existing auth tests and the three logout cases pass.

- [ ] **Step 5: Commit the API slice**

```bash
git add src/CulinaryBlog.API/Controllers/AuthController.cs tests/CulinaryBlog.Integration.Tests/AuthControllerTests.cs
git commit -m "feat: expose idempotent logout endpoint"
```

### Task 3: Frontend revoke-and-clear behavior

**Files:**
- Modify: `frontend/app/lib/api.ts`
- Modify: `frontend/app/context/AppContext.tsx`
- Modify: `frontend/app/components/layout/Header.tsx`

**Interfaces:**
- Consumes: backend `POST /api/v1/auth/logout` and `auth.getRefreshToken()` / `auth.clearAuth()`.
- Produces: `export async function logout(refreshToken: string | null): Promise<void>` in `api.ts`, and `logout: () => Promise<void>` from `AppContext`.

**Acceptance Criteria:**

- `api.logout` sends JSON `{ refreshToken }` to `/api/v1/auth/logout` without an Authorization header and accepts `204` without attempting JSON parsing.
- `AppContext.logout` awaits the remote call when a refresh token exists, then clears auth storage and `currentUser` in `finally`.
- A network or unexpected HTTP failure still clears client credentials and shows the existing logout toast; it must not create an unhandled promise rejection.
- The three Header callers use `void logout()` after their UI-menu state updates.

**Test Cases:**

| Name | Setup | Expected assertion |
|---|---|---|
| `logout request` | Logged-in browser session; click desktop logout. | One POST with `{ refreshToken }`, no Bearer header, then page shows logged-out state. |
| `remote failure` | Block or stop API before clicking logout. | Local storage keys `auth_access_token`, `auth_refresh_token`, and `auth_user` are removed; header becomes guest state. |
| `no stored refresh token` | Seed only stored user/access token, then click logout. | No request is required; local state is cleared with no console error. |

- [ ] **Step 1: Implement the API client function**

In `frontend/app/lib/api.ts`, add `logout(refreshToken: string | null): Promise<void>`. POST JSON to the auth endpoint; because `handleResponse` assumes JSON on success, only call it for a non-OK response so `204` completes normally.

- [ ] **Step 2: Implement the context and caller changes**

Import the new API function as `apiLogout`. Change the context interface and implementation to `logout: () => Promise<void>`. Capture `auth.getRefreshToken()`, await `apiLogout` only when it is non-null, ignore the remote failure after optionally logging it, then call `auth.clearAuth()`, `setCurrentUser(null)`, and the existing informational toast in `finally`. Replace each Header call with `void logout()`.

- [ ] **Step 3: Run frontend static verification**

Run: `pnpm --dir frontend lint && pnpm --dir frontend build`

Expected: both commands exit 0 with no TypeScript, ESLint, or Next.js build errors.

- [ ] **Step 4: Execute the manual browser test cases**

Run the three **Test Cases** above against the local frontend and API. Record whether each passed in the pull request/commit notes; do not add a frontend test framework for this one flow.

- [ ] **Step 5: Commit the frontend slice**

```bash
git add frontend/app/lib/api.ts frontend/app/context/AppContext.tsx frontend/app/components/layout/Header.tsx
git commit -m "feat: revoke refresh token on logout"
```

### Task 4: Full regression gate

**Files:**
- Modify: none unless a regression test exposes a defect in Tasks 1-3.

**Interfaces:**
- Consumes: completed Tasks 1-3.
- Produces: verified FR-AUTH-005 behavior without regressions in the existing solution.

**Acceptance Criteria:**

- Application and integration test projects pass together.
- Frontend lint and production build pass.
- The final working tree contains only the intended implementation and the plan; do not stage or change the pre-existing untracked `.codegraph/` directory.

**Test Cases:**

| Name | Command | Expected assertion |
|---|---|---|
| Backend regression | `dotnet test CulinaryBlog.slnx` | Exit 0. |
| Frontend regression | `pnpm --dir frontend lint && pnpm --dir frontend build` | Exit 0. |
| Scope audit | `git status --short` | Only planned files are modified; `.codegraph/` remains untouched. |

- [ ] **Step 1: Run the backend solution suite**

Run: `dotnet test CulinaryBlog.slnx`

Expected: exit 0.

- [ ] **Step 2: Run the frontend gate**

Run: `pnpm --dir frontend lint && pnpm --dir frontend build`

Expected: exit 0.

- [ ] **Step 3: Audit scope and commit final fixes if any**

Run: `git status --short`

Expected: no unrelated files are staged or changed. If a regression fix is needed, add its focused test and commit it with `fix: resolve logout regression`.

## Self-Review

- **Spec coverage:** Tasks 1-2 implement token lookup, single-token revocation, idempotency, and HTTP 204. Task 3 implements client-side credential removal. The approved expired-access-token behavior is implemented by the anonymous endpoint in Task 2. No SRS requirement is unassigned.
- **Step scan:** Every code step names its file and exact interface; tests specify inputs and observable outcomes. No extra migration, logout-all-devices feature, audit-log pipeline, refresh flow, or frontend test dependency is planned.
- **Type consistency:** `LogoutRequest(string? RefreshToken)` flows from controller to `LogoutCommand(string? RefreshToken)`. The frontend sends `string | null`; the backend maps it to a no-op.
- **Review focus:** All five identified input/failure modes have an owning task and an explicit test case.
- **Proportion:** Four independently reviewable tasks cover application behavior, HTTP contract, frontend behavior, and regression proof without restructuring existing auth code.
