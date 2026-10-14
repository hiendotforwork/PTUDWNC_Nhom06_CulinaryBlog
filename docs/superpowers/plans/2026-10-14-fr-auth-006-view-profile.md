# FR-AUTH-006 View Profile Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement authenticated `GET /api/v1/auth/me` and load the server-backed current-user profile on `/profile`.

**Architecture:** Follow the existing MediatR and repository pattern for the read endpoint. The frontend calls it through the existing authenticated fetch helper only on the profile page, then refreshes shared user state and shows load/error states.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, ASP.NET Core Identity, xUnit, Moq, FluentAssertions, Next.js 16, React 19, TypeScript.

**Spec:** `docs/superpowers/specs/2026-10-14-fr-auth-006-view-profile-design.md`

## Global Constraints

- `GET /api/v1/auth/me` requires `Authorization: Bearer <access-token>`.
- Use `displayName` and include `bio`; the original SRS `fullName` wording is superseded by the detailed FR-AUTH report and API contract.
- `avatarUrl` and `bio` are nullable; `roles` is a non-null array and is `[]` when the user has no roles.
- `id`, `displayName`, `email`, and `userName` are non-null strings. Persisted profile `email` and `userName` must be non-empty; invalid stored data is a data-integrity failure (generic 500), never an empty-string or null response.
- Role ordering is not part of the API contract.
- Resolve the user only from token claims; do not accept a user ID in the route, query string, or body.
- If neither `ClaimTypes.NameIdentifier` nor `sub` contains a usable user ID, return 401 without dispatching the query.
- Return 404 if the identified user no longer exists. Never return password hashes, security stamps, or credential material.
- Preserve API `roles` in frontend shared user state; keep `role` as a separate derived Admin/User UI flag.
- Do not change FR-AUTH-007 save behavior, load this profile on unrelated pages, or refactor unrelated authentication code.

## Review Focus

- Claim handling: whitespace `NameIdentifier` falls back to valid `sub`; when both are unusable, return 401 without a MediatR call. Pin both cases with controller unit tests.
- A valid principal whose user was deleted must return 404; pin with an API integration test.
- Null `avatarUrl` and `bio` must remain JSON `null`; pin with an API integration test using a newly registered user.
- A user with no roles must return `roles: []`; pin with a handler test. Do not assert role ordering.
- Missing or whitespace email/username on a persisted user must fail as a data-integrity error; pin with handler tests.

---

### Task 1: Application profile query and user lookup

**Files:**
- Create: `src/CulinaryBlog.Application/DTOs/Auth/UserProfileDto.cs`
- Create: `src/CulinaryBlog.Application/Queries/Auth/GetCurrentUserQuery.cs`
- Create: `src/CulinaryBlog.Application/Queries/Auth/GetCurrentUserQueryHandler.cs`
- Modify: `src/CulinaryBlog.Application/Interfaces/IUserRepository.cs`
- Modify: `src/CulinaryBlog.Infrastructure/Services/UserRepository.cs`
- Test: `tests/CulinaryBlog.Application.Tests/Queries/Auth/GetCurrentUserQueryHandlerTests.cs`

**Interfaces:**
- Produces `GetCurrentUserQuery(string UserId) : IRequest<UserProfileDto?>`.
- Produces `UserProfileDto(string Id, string DisplayName, string Email, string UserName, string? AvatarUrl, string? Bio, string[] Roles, bool EmailConfirmed, DateTime CreatedAt)`.
- Adds `Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken = default)` to `IUserRepository`.

- [ ] **Step 1: Write handler tests**

  Test `Handle_UserFound_MapsPublicProfileAndRoles` for every DTO field, nullable avatar/bio, and role mapping. Test `Handle_UserWithNoRoles_ReturnsEmptyRoles` for `Roles == []`. Test `Handle_UserWithoutEmailOrUserName_ThrowsInvalidOperationException` for null, empty, and whitespace email/username. Test `Handle_UserNotFound_ReturnsNull` and verify roles are not queried.

- [ ] **Step 2: Run the focused test to verify it fails**

  Run: `dotnet test tests/CulinaryBlog.Application.Tests/CulinaryBlog.Application.Tests.csproj --filter FullyQualifiedName~GetCurrentUserQueryHandlerTests`

  Expected: FAIL because the query, DTO, handler, and repository lookup do not exist yet.

- [ ] **Step 3: Add the profile DTO, query, and repository lookup contract**

  Define the DTO and query signatures above. Add `FindByIdAsync` to `IUserRepository`; implement it in `UserRepository` with `UserManager<ApplicationUser>.FindByIdAsync(userId)`.

- [ ] **Step 4: Implement the query handler**

  Find the user by `request.UserId`; return `null` when absent. Otherwise require non-whitespace `Email` and `UserName` (throw `InvalidOperationException` if persisted data violates the invariant), get roles through `IUserRepository.GetRolesAsync`, and map explicitly to `UserProfileDto`. Materialize roles as an array; do not substitute empty values for missing email or username.

- [ ] **Step 5: Run the focused test to verify it passes**

  Run: `dotnet test tests/CulinaryBlog.Application.Tests/CulinaryBlog.Application.Tests.csproj --filter FullyQualifiedName~GetCurrentUserQueryHandlerTests`

  Expected: PASS.

- [ ] **Step 6: Commit the application read model**

  ```bash
  git add src/CulinaryBlog.Application/DTOs/Auth/UserProfileDto.cs src/CulinaryBlog.Application/Queries/Auth/GetCurrentUserQuery.cs src/CulinaryBlog.Application/Queries/Auth/GetCurrentUserQueryHandler.cs src/CulinaryBlog.Application/Interfaces/IUserRepository.cs src/CulinaryBlog.Infrastructure/Services/UserRepository.cs tests/CulinaryBlog.Application.Tests/Queries/Auth/GetCurrentUserQueryHandlerTests.cs
  git commit -m "feat(auth): add current user profile query"
  ```

### Task 2: Authenticated profile endpoint

**Files:**
- Modify: `src/CulinaryBlog.API/Controllers/AuthController.cs`
- Create: `tests/CulinaryBlog.Integration.Tests/AuthControllerProfileUnitTests.cs`
- Modify: `tests/CulinaryBlog.Integration.Tests/AuthControllerTests.cs`
- Modify: `tests/CulinaryBlog.Integration.Tests/CulinaryBlog.Integration.Tests.csproj` (add test-only Moq 4.20.72)

**Interfaces:**
- Consumes Task 1's `GetCurrentUserQuery` and `UserProfileDto`.
- Produces `GET /api/v1/auth/me` as `Task<ActionResult<UserProfileDto>>`.

- [ ] **Step 1: Write controller and API tests before adding the endpoint**

  In `AuthControllerProfileUnitTests`, test that no usable `NameIdentifier` or `sub` returns `UnauthorizedResult` and verifies `IMediator.Send` was never called; also test that whitespace `NameIdentifier` with a valid `sub` dispatches `GetCurrentUserQuery` with the `sub` value. In `AuthControllerTests`, add `GetCurrentUser_WithoutToken_Returns401`, `GetCurrentUser_WithValidToken_ReturnsProfile`, `GetCurrentUser_WithoutUserIdClaims_Returns401`, and `GetCurrentUser_WhenUserDoesNotExist_Returns404`. For the last case, register a user, delete it through `UserManager`, then request with the still-valid access token. Add a valid test-JWT helper using the issuer, audience, and signing key from `CreateExpiredJwt`; the no-ID token contains an unrelated claim only. Add test-only Moq 4.20.72 to the integration test project for the controller unit tests.

- [ ] **Step 2: Run the focused tests to verify they fail**

  Run: `dotnet test tests/CulinaryBlog.Integration.Tests/CulinaryBlog.Integration.Tests.csproj --filter "FullyQualifiedName~AuthControllerProfileUnitTests|FullyQualifiedName~AuthControllerTests.GetCurrentUser"`

  Expected: FAIL because the profile action is not implemented.

- [ ] **Step 3: Add the authorized profile action**

  Add `[HttpGet("me")]` and `[Authorize]`. Use the first non-whitespace value from `ClaimTypes.NameIdentifier` and `sub`; return 401 without calling MediatR if neither is usable. Dispatch `GetCurrentUserQuery(userId)` and return 404 for a null result, otherwise 200 with the DTO.

- [ ] **Step 4: Run the focused tests to verify they pass**

  Run: `dotnet test tests/CulinaryBlog.Integration.Tests/CulinaryBlog.Integration.Tests.csproj --filter "FullyQualifiedName~AuthControllerProfileUnitTests|FullyQualifiedName~AuthControllerTests.GetCurrentUser"`

  Expected: PASS for controller claim handling and all profile HTTP cases. The valid registered-user case asserts public fields, JSON nulls for avatar/bio, and absence of `passwordHash`/`securityStamp`; do not assert role ordering.

- [ ] **Step 5: Commit the endpoint**

  ```bash
  git add src/CulinaryBlog.API/Controllers/AuthController.cs tests/CulinaryBlog.Integration.Tests/AuthControllerProfileUnitTests.cs tests/CulinaryBlog.Integration.Tests/AuthControllerTests.cs tests/CulinaryBlog.Integration.Tests/CulinaryBlog.Integration.Tests.csproj
  git commit -m "feat(auth): expose current user profile endpoint"
  ```

### Task 3: Load profile data on `/profile`

**Files:**
- Modify: `frontend/app/lib/types.ts`
- Modify: `frontend/app/lib/api.ts`
- Modify: `frontend/app/context/AppContext.tsx`
- Modify: `frontend/app/profile/page.tsx`

**Interfaces:**
- Produces `UserProfileResponse` with `id`, `displayName`, `email`, `userName`, `avatarUrl: string | null`, `bio: string | null`, `roles: string[]`, `emailConfirmed`, and `createdAt`; `email` and `userName` are required non-empty strings.
- Adds optional `roles?: string[]` to the shared `User` type so recipe-author/mock users without auth role data remain valid.
- Produces `getCurrentUser(): Promise<UserProfileResponse>` using the existing `authFetch` and `handleResponse` helpers.

- [ ] **Step 1: Add the profile response type and API function**

  Keep nullable fields explicit and `roles` non-null. Implement `getCurrentUser` with `authFetch(`${API_BASE}/api/v1/auth/me`)` and `handleResponse<UserProfileResponse>`.

- [ ] **Step 2: Load and map the response on the profile page**

  Fetch only when the authenticated profile page opens. Preserve `profile.roles` in `User.roles`; derive the existing `User.role` UI flag separately (`Admin` only when roles include `Admin`, otherwise `User`). Keep `roles` in all four existing AppContext mappings: local-storage restore, login, register, and external-token login. Map null avatar to the current default avatar and null bio to undefined, call `setCurrentUser`, and update the three form field states from the profile response. Do not change `updateProfile` or its save behavior.

- [ ] **Step 3: Add visible loading, error, and retry states**

  Keep request errors distinct from the unauthenticated screen. Show a not-found message for 404; show a retry action for network or other API errors. Reuse the existing loading and button components.

- [ ] **Step 4: Run frontend static validation**

  Run: `pnpm --dir frontend lint`

  Expected: PASS with no lint errors.

- [ ] **Step 5: Type-check the frontend**

  Run: `pnpm --dir frontend exec tsc --noEmit`

  Expected: PASS with no TypeScript errors.

- [ ] **Step 6: Manually verify the profile flow**

  Sign in as an Author, open `/profile`, and confirm server values replace cached values while `currentUser.roles` still contains `Author`; confirm loading is visible while the request is pending; exercise 404 and network failure to check the message and retry action; confirm unauthenticated users still see the sign-in state and profile edit save behavior is unchanged.

- [ ] **Step 7: Commit the frontend integration**

  ```bash
  git add frontend/app/lib/types.ts frontend/app/lib/api.ts frontend/app/context/AppContext.tsx frontend/app/profile/page.tsx
  git commit -m "feat(frontend): load profile from auth API"
  ```
