# FR-AUTH-008: Email Verification Design

**Status:** Proposed for review  
**Date:** 2026-10-19  
**Scope:** Email verification for local registration, with Google OAuth treated as an already verified sign-in when Google asserts a verified email.

## 1. Goal

Implement FR-AUTH-008 so a new local account proves control of its email address before it can sign in or use Author/chef features. Unconfirmed users remain Guests and can use public functionality. A successful email confirmation automatically signs the user in.

Google registration and sign-in do not require a separate Culinary Blog confirmation email. A successful, validated Google OAuth flow whose identity asserts a verified email marks the account email as confirmed and signs the user in immediately.

## 2. Decisions

| Topic | Decision |
|---|---|
| Local verification method | Identity email-confirmation link; no OTP in v1.0. A successful confirmation is effective once; replay must not issue another session. |
| Link validity | 24 hours. Configure explicitly rather than relying on an implicit default. |
| Confirmation UX | Link opens `/verify-email`; user explicitly presses Confirm before the frontend submits the token. |
| Successful confirmation | Confirm the email and issue the normal access/refresh token pair in the same response. Frontend stores the session and redirects to the requested local route or home. |
| Local registration | Create account and queue confirmation email; do not issue access or refresh tokens. |
| Resend | Available without authentication; rate limited and returns a generic response that does not reveal account state. |
| Google | No separate confirmation email when validated Google identity supplies `email_verified=true`. Do not trust an email supplied by the browser. |
| Guest access | Public/Guest routes remain available to unconfirmed users. Author operations require a confirmed account. |
| Existing accounts | Do not bulk-mark legacy local accounts confirmed. Require email confirmation before they can sign in or use Author features. Legacy Google-linked accounts must complete Google sign-in again and satisfy the verified-email check. |

## 3. User Flows

### 3.1 Local registration and confirmation

1. Guest submits the existing registration form.
2. Backend validates the request and creates an Identity account with `EmailConfirmed=false`, assigning the existing default Author role.
3. Backend creates an Identity email-confirmation token with a 24-hour lifetime and enqueues a durable email job through the existing background task infrastructure.
4. Register returns `201 Created` with a pending-confirmation response, not an `AuthResponse`; no JWT or refresh token is issued.
5. Frontend displays a check-your-email state, with an action to resend confirmation.
6. User opens the email link. It navigates to `/verify-email` with the confirmation token and minimum account identifier in the URL fragment (for example, `#userId=...&token=...`); fragments are not sent in the page request.
7. The page explains which address is being confirmed where available, then waits for an explicit Confirm action. Merely opening the URL must not confirm the account because email security scanners may follow links automatically.
8. On Confirm, frontend posts the token to the API. Backend validates it with Identity, sets `EmailConfirmed=true`, and issues the normal `AuthResponse` (access token, refresh token, expiry, user) only on the first successful confirmation.
9. Frontend reads the fragment, immediately removes token material from the visible URL/history, stores the returned session using the existing auth path, refreshes authenticated app state, and redirects to the safe local return path or home.

### 3.2 Invalid, expired, repeated, or failed confirmation

- Invalid/expired token: do not change account state or issue session tokens; show an expired/invalid-link state with a resend action.
- Already confirmed account: report that it is already confirmed and offer login; do not mint another session from a replayed link.
- Email delivery failure: keep the pending account. The user can request another message; do not delete the account or claim it was confirmed.
- API/network failure: show a retryable error, retain the page state, and do not treat the user as authenticated.

### 3.3 Resend confirmation

1. User submits their email from the registration confirmation state, verification page, or login error state.
2. API always returns the same generic accepted response, whether the address is unknown, already confirmed, or pending.
3. For a known pending account, backend creates a fresh Identity token and enqueues an email. Apply limits per IP and normalized email, plus an appropriate cooldown.
4. Resend does not create an authenticated session.

### 3.4 Local login and refresh

- Valid credentials for an unconfirmed account do not produce tokens. Return a stable `EMAIL_NOT_CONFIRMED` application error so the frontend can show the resend action.
- Invalid credentials retain the existing generic invalid-credentials response.
- Refresh must refuse to mint new tokens for an account that is not confirmed.
- Confirmed local accounts continue through the existing login and refresh behavior.

### 3.5 Google registration/login

1. User completes the existing Google OAuth challenge and callback.
2. Backend derives email and provider identity from the validated provider principal and obtains the verified-email assertion from trusted Google user-info/OpenID data. Normalize the provider's assertion into a server-side boolean; never accept a client-provided email or verification flag as proof. Fail closed if the verified-email assertion is missing or false.
3. For a new Google account, set `EmailConfirmed=true`, assign the existing default Author role, link the Google identity, issue tokens, and complete the existing callback sign-in flow. Do not send a separate verification email.
4. For an existing account with the same email, link Google only after Google has asserted the email is verified. Then allow sign-in as a confirmed account.
5. For an already linked Google identity whose verified email changed, update the account email and confirmation state only if the new email is not owned by another account. If the address is in use or Identity cannot persist the update, fail authentication without issuing a session.
6. If Google does not assert a verified email, do not grant Author access or auto-link by email; return a safe authentication error.

## 4. Pages and API Contract

### Frontend

- Add `/verify-email` for pending, confirming, success, invalid/expired, already-confirmed, and retryable-error states.
- Update registration success UI to explain that confirmation is required and provide resend.
- Update login UI for `EMAIL_NOT_CONFIRMED`, with a resend action.
- Keep public Guest routes available. Do not expose Author actions as usable until signed in; backend remains the enforcement boundary.
- Validate any return URL as a local application path before redirecting to prevent open redirects.
- Strip the token fragment while preserving the query string, then validate `returnUrl` against the current origin before redirecting.

### API behavior

Names below are proposed; align them with the existing `/api/v1/auth` route convention during implementation. These behaviors supersede the current API contract, which documents immediate tokens from registration and a `GET` confirmation endpoint.

| Method / route | Purpose | Expected behavior |
|---|---|---|
| `POST /api/v1/auth/register` | Create local pending account | `201`; pending-confirmation payload; no tokens. |
| `POST /api/v1/auth/email/confirm` | Confirm email | Accept account identifier + token; on first success return `200` with `AuthResponse`. |
| `POST /api/v1/auth/email/status` | Read link status | Return confirmed for an already confirmed account, matching confirm replay behavior; for a pending account require a valid identifier + token. Never change account state. |
| `POST /api/v1/auth/email/resend` | Request another message | Generic `202` response; rate-limited. |
| `POST /api/v1/auth/login` | Local sign-in | Existing success response for confirmed accounts; stable unconfirmed error for correct credentials on pending accounts. |
| `POST /api/v1/auth/refresh` | Rotate session tokens | Reject if account is currently unconfirmed. |

Do not mutate account state from a `GET` endpoint. The confirmation token may appear in the email link fragment, but the frontend confirmation action must submit it using `POST`.

### Documentation updates required with implementation

- Update `docs/specs/fr-auth/FR-AUTH_APIContract.md`: registration's pending-confirmation response, confirm/resend `POST` endpoints, local login error, fragment-based link, and Google verified-email behavior.
- Update `docs/specs/fr-auth/FR-AUTH_BaoCao.md`: FR-AUTH-001 registration no longer auto-logs in, FR-AUTH-002 login gate, FR-AUTH-003 Google verification, FR-AUTH-008 flow, and BR-AUTH-004 and related rules.
- Update `docs/specs/fr-auth/FR-AUTH_TestPlan.md` and affected UAT/implementation documents so they no longer expect registration to return tokens and cover confirmation auto-login, resend, legacy accounts, Google verification, and rejection of existing sessions.

## 5. Architecture and Data Handling

- Reuse ASP.NET Core Identity email-confirmation token generation/validation; do not add a custom GUID token table unless implementation discovery proves Identity tokens cannot meet the requirements. Register the Identity token providers explicitly (`AddDefaultTokenProviders`) and set a 24-hour lifetime explicitly. If configuring the shared data-protection provider changes lifetimes for other token purposes, use a dedicated email-confirmation provider or document the shared effect.
- Reuse the existing SMTP-backed email service and durable background task/job pattern. Add a verification email operation/job alongside the existing welcome email operation.
- Rolling back the repeated-verification-task migration deletes all `email-verification` outbox rows before restoring the table-wide unique constraint; welcome and resize rows remain.
- Use the configured public frontend base URL to form HTTPS links in production. Validate configuration at startup or fail the job clearly; never silently send a development URL in production.
- Token generation, confirmation, resend, and failure logs must not include the raw token or full confirmation URL.
- Put the confirmation token in the URL fragment, never the query or path; fragments are not sent to the frontend server in the page request. The verification page must use a no-referrer policy, avoid third-party resources/analytics, and remove the fragment from browser history immediately after reading it.
- Do not put access/refresh tokens in the confirmation URL. Return them only from the confirmation API response over HTTPS after explicit user action.
- Keep email state on the Identity user (`EmailConfirmed`) and avoid a second confirmation-state source of truth.
- Persist the Identity confirmation and refresh-token creation consistently. If confirmation succeeds but session issuance fails, account may remain confirmed; the page should allow the user to log in normally. Do not roll back a valid confirmation solely because email/session response delivery failed.

## 6. Authorization and Existing Sessions

- Registration must never establish an authenticated session for an unconfirmed local account.
- Login and refresh checks alone do not stop already-issued access tokens. Every protected application API request must check current confirmation state server-side (or use an equally immediate revocation mechanism), not only Author endpoints. This includes profile endpoints such as `GET /api/v1/auth/me` and role-gated endpoints.
- Requests from an unconfirmed account using an old access token must be rejected on every endpoint requiring authentication. Anonymous confirmation, resend, and logout endpoints remain accessible; Guest/public endpoints remain accessible without confirmation.
- Do not trust a client-side `emailConfirmed` value or a stale JWT claim as the sole enforcement mechanism.
- Do not bulk-mark legacy local accounts confirmed. Existing local accounts must confirm the email on file before login, refresh, or any protected API access. Provide resend from the unauthenticated login/verification flow. Existing Google-linked accounts must reauthenticate through Google and pass the verified-email check before being marked confirmed. Guest/public routes remain available throughout.

## 7. Security, Abuse, and Error Handling

- Confirmation and resend endpoints are anonymous, input-validated, HTTPS-only in production, and rate limited.
- Resend responses do not disclose whether an email exists or is already confirmed.
- Login distinguishes an unconfirmed account only after valid credentials; invalid credentials remain generic.
- Confirmation tokens are time-limited (24 hours), Identity-protected, and must not be logged. Only first successful confirmation issues an auth session.
- Rate-limit resend by normalized email and IP, with a cooldown and bounded delivery volume. Avoid permanent account lockout based only on unauthenticated resend requests.
- Escape user-controlled values in email HTML and build links from trusted configured origins, not request `Host` headers.
- Use a safe, allowlisted return path. Do not redirect to arbitrary external URLs.
- Google linking requires validated OAuth callback plus provider-verified email; do not link based on an unverified matching email.

## 8. Acceptance Criteria

1. Local registration creates an unconfirmed account, queues verification email, and returns no tokens.
2. The email link opens `/verify-email`; a GET or link preview alone does not confirm the email.
3. A valid token submitted by explicit POST marks the account confirmed and returns a normal token pair exactly once.
4. Invalid and expired tokens do not confirm the account or issue tokens; the UI offers resend.
5. Replaying a used confirmation token does not issue another session.
6. Resend works for pending users, is rate limited, and does not disclose account existence/state in its response.
7. A local user with correct password but unconfirmed email cannot log in; frontend shows the resend path.
8. Refresh and every protected API reject unconfirmed accounts, including requests using access tokens issued before the policy is enforced; explicitly cover `GET /api/v1/auth/me` and an Author operation.
9. Guest/public routes remain accessible without a verified account.
10. A verified Google OAuth identity can create/link a confirmed account and sign in without a separate confirmation email.
11. Google identity without a verified-email assertion cannot create an Author session or be auto-linked by email.
12. A changed email from an already linked Google identity is synchronized only when unclaimed; collisions or update failures return an authentication error without tokens.
13. Raw verification tokens and tokenized links do not appear in logs.
14. Existing local accounts are not silently trusted; they must confirm before login or any protected API access. Existing Google-linked accounts can be confirmed by a fresh validated Google sign-in with a verified-email assertion.
15. The API contract, FR-AUTH analysis report, test plan, and affected UAT/implementation documents agree on the new behavior.

## 9. Out of Scope

- Email OTP, SMS, authenticator apps, or multi-factor authentication.
- Password reset/change flows.
- Changing an account email address and reconfirming it.
- Admin user-management UI.
- Replacing the SMTP provider or adding a third-party email SDK.

## 10. Rollout Impact

Legacy local users will be unable to sign in or use any protected API until they confirm their stored email address. Login and refresh must not issue tokens for them; existing access tokens must also be denied by authorization checks on every protected endpoint, including profile APIs. They retain Guest access and can request a confirmation email without signing in. Existing Google-linked users must sign in again through Google and pass the verified-email check. This strict rollout matches the requirement that an unconfirmed account cannot authenticate, but requires a clear release notice and working email delivery before enforcement.
