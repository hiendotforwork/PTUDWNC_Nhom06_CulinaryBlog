# FR-AUTH-006 View Profile Design

## Purpose

Let an authenticated user view the current, server-backed profile on `/profile`.
The user identity comes from the access token; clients cannot choose another
user's ID.

## Current state

- `AuthController` has no `GET /api/v1/auth/me` action.
- The profile page reads `currentUser` from `AppContext`; on login or page
  restoration this can be data copied from the authentication response or
  local storage rather than a fresh profile query.
- The frontend API client already has `authFetch`, which adds the Bearer token
  and retries once after refreshing an expired access token.
- `IUserRepository` supports email and username lookup, but not lookup by ID.

## Scope

### In scope

- Add authenticated `GET /api/v1/auth/me`.
- Resolve the user from the authenticated principal and return a safe profile
  DTO, including roles.
- Load that profile from the API when `/profile` is opened and update the
  shared current-user state.
- Show loading, load-error, retry, and unauthenticated states as appropriate.
- Add focused application and API tests for the profile read path.

### Out of scope

- Persisting profile edits; that is FR-AUTH-007.
- Email or username changes, avatar upload, profile caching, or loading the
  profile on every application page.
- Refactoring unrelated authentication or profile code.

## Contract

`GET /api/v1/auth/me` requires `Authorization: Bearer <access-token>`.

Successful response (`200 OK`):

```json
{
  "id": "user-id",
  "displayName": "Nguyễn Văn A",
  "email": "user@example.com",
  "userName": "nguyenvana",
  "avatarUrl": null,
  "bio": null,
  "roles": ["Author"],
  "emailConfirmed": true,
  "createdAt": "2026-06-01T10:00:00Z"
}
```

Response nullability and collection rules:

- `avatarUrl`: string or `null`.
- `bio`: string or `null`.
- `roles`: non-null array; use `[]` when the user has no roles.
- Role ordering is not part of the API contract.

Never include password hashes, security stamps, or other credential material.

The detailed FR-AUTH report and API contract record the later field decision:
use `displayName` and include `bio`. This supersedes the original SRS wording
that says `fullName` and omits `bio`; it also matches the existing frontend
model.

## Backend design

Keep the existing CQRS/MediatR pattern:

1. Add a `GetCurrentUserQuery` and handler in the auth application area.
2. Extend `IUserRepository` and `UserRepository` with lookup by user ID; reuse
   the existing role lookup for the response.
3. Map explicitly to `UserProfileDto`, so identity internals cannot leak into
   the response.
4. Add an `[Authorize]` GET action to `AuthController`. Read the user ID from
   `ClaimTypes.NameIdentifier` with `sub` as fallback and dispatch the query.
   Do not accept a user ID in the route, query string, or body.
5. Error responsibilities:
   - `[Authorize]` returns 401 for anonymous requests and invalid tokens.
   - If neither `ClaimTypes.NameIdentifier` nor `sub` contains a usable user
     ID, the controller returns 401 and does not dispatch the query.
   - The query/handler returns a not-found outcome if the user does not exist;
     the controller maps that outcome to 404.

## Frontend design

Add a `getCurrentUser` API function using `authFetch`. Call it when the profile
page is opened, update `AppContext.currentUser` with the response, and render
the refreshed values. Do not request the profile on other pages. The API
client's existing refresh behavior handles an expired access token; if
authentication still fails, its existing auth clearing behavior applies.

Keep the profile page's existing edit controls, but do not change their save
behavior in this feature. The response should populate the visible profile
and edit fields. A request failure should not be shown as an unauthenticated
state: show a useful error and a retry action. A 404 should explain that the
profile no longer exists.

## Verification plan

- Application handler: maps all public profile fields and roles; returns the
  not-found outcome when lookup by ID fails; preserves nullable `avatarUrl` and
  `bio`, and returns `[]` when no roles exist.
- API integration: valid token returns 200 and expected fields; no token
  returns 401; authenticated token without a usable user-ID claim returns 401
  without dispatching the query; missing user returns 404; response contains
  no credential fields. Do not assert role ordering.
- Frontend: authenticated profile page displays the API response; loading,
  network/API error, retry, and unauthenticated states behave as specified.

## Acceptance criteria

1. A signed-in user sees profile data returned by `GET /api/v1/auth/me` on
   `/profile`, including the current database values.
2. A user cannot select another account's profile by changing request input.
3. Anonymous requests receive 401; a deleted account receives 404.
4. The response includes only the agreed profile fields and never includes
   credential data.
5. FR-AUTH-007 save behavior and unrelated application pages are unchanged.
