# FR-AUTH-004: Token Refresh - End-User Test Scenarios

## Prerequisites
- Backend running at http://localhost:5058
- Frontend running at http://localhost:3000
- Test user account registered and logged in

---

## TC001: Token Refresh - Successful Refresh

**Objective:** Verify user can refresh tokens when access token is about to expire

**Pre-conditions:**
- User is logged in
- Store current access token and refresh token

**Test Steps:**
1. Wait for access token to approach expiration (or decode JWT to see exp claim)
2. Call API to refresh: `POST /api/v1/auth/refresh` with `{ "refreshToken": "<current_refresh_token>" }`
3. Observe response

**Expected Results:**
- HTTP 200 OK
- Response contains new `accessToken` (15 min validity)
- Response contains new `refreshToken` (different from old one - rotation)
- Response contains `expiresAt` timestamp
- Response contains `user` object with correct user info

**Pass Criteria:** All expected results met

---

## TC002: Token Rotation - Old Token Invalidated

**Objective:** Verify old refresh token is invalidated after refresh (token rotation)

**Pre-conditions:**
- User has performed a successful token refresh (TC001)

**Test Steps:**
1. Call API to refresh using the **old** refresh token (from before TC001)
2. Observe response

**Expected Results:**
- HTTP 401 Unauthorized
- Error message indicates token reuse detected

**Pass Criteria:** Old token correctly rejected

---

## TC003: Invalid Refresh Token

**Objective:** Verify system rejects invalid refresh tokens

**Test Steps:**
1. Call API to refresh with: `{ "refreshToken": "completely-invalid-token" }`
2. Observe response

**Expected Results:**
- HTTP 401 Unauthorized
- Appropriate error message

**Pass Criteria:** Invalid token rejected

---

## TC004: Expired Refresh Token

**Objective:** Verify system rejects expired refresh tokens

**Pre-conditions:**
- Have a refresh token that has passed its expiration (7+ days old)

**Test Steps:**
1. Call API to refresh with expired token
2. Observe response

**Expected Results:**
- HTTP 401 Unauthorized
- Appropriate error message indicating token expired

**Pass Criteria:** Expired token rejected

---

## TC005: Rate Limiting

**Objective:** Verify refresh endpoint respects rate limits

**Test Steps:**
1. Make 30+ rapid refresh requests in 1 minute
2. Observe response after limit exceeded

**Expected Results:**
- First 30 requests: HTTP 200 (successful refresh)
- 31st+ request: HTTP 429 Too Many Requests
- Response includes `Retry-After` header

**Pass Criteria:** Rate limiting enforced

---

## TC006: Frontend Auto-Refresh on Page Load

**Objective:** Verify frontend auto-refreshes token when page is refreshed

**Pre-conditions:**
- User is logged in
- Access token is about to expire

**Test Steps:**
1. Open browser DevTools > Application > Local Storage
2. Note current `auth_refresh_token` value
3. Wait for token to approach expiration
4. Refresh the page (F5)
5. Check Local Storage for new token values

**Expected Results:**
- Page loads successfully without login prompt
- `auth_access_token` is updated (new token)
- `auth_refresh_token` is updated (rotated)
- No login modal appears

**Pass Criteria:** Auto-refresh works on page load

---

## TC007: Frontend Manual Refresh

**Objective:** Verify frontend exposes manual refresh capability

**Test Steps:**
1. User is logged in
2. Trigger manual refresh (if exposed in UI, e.g., a refresh button or API call)
3. Check Local Storage for updated tokens

**Expected Results:**
- `auth_access_token` updated
- `auth_refresh_token` updated (rotated)
- User remains logged in

**Pass Criteria:** Manual refresh works

---

## TC008: Session Persistence After Token Expiry

**Objective:** Verify user stays logged in after token expiry via refresh

**Pre-conditions:**
- User is logged in

**Test Steps:**
1. Wait for access token to expire (15 minutes)
2. On the frontend, make a protected API call
3. Observe if system auto-refreshes and completes the request

**Expected Results:**
- Request succeeds (auto-refresh triggered)
- User remains logged in
- No login prompt appears

**Pass Criteria:** Session persists via refresh

---

## TC009: Login Required After All Tokens Expired

**Objective:** Verify user must re-login when refresh token also expires

**Pre-conditions:**
- User has been inactive for 7+ days (or manually clear tokens)

**Test Steps:**
1. Clear all auth data from Local Storage
2. Attempt to access protected route

**Expected Results:**
- Login/register page appears
- User must authenticate again

**Pass Criteria:** Correctly requires re-authentication

---

## TC010: Concurrent Refresh Requests

**Objective:** Verify system handles concurrent refresh requests correctly

**Test Steps:**
1. User is logged in
2. Send multiple refresh requests simultaneously (e.g., via Postman or curl with `&` background jobs)
3. Observe responses

**Expected Results:**
- Only one refresh succeeds
- Other requests return 401 (token already rotated)
- No errors or data corruption

**Pass Criteria:** Concurrent requests handled safely

---

## Summary

| Test Case | Description | Priority |
|-----------|-------------|----------|
| TC001 | Successful token refresh | P0 |
| TC002 | Token rotation (old invalidated) | P0 |
| TC003 | Invalid token rejection | P0 |
| TC004 | Expired token rejection | P1 |
| TC005 | Rate limiting | P1 |
| TC006 | Frontend auto-refresh | P0 |
| TC007 | Frontend manual refresh | P1 |
| TC008 | Session persistence | P0 |
| TC009 | Re-login after expiry | P1 |
| TC010 | Concurrent requests | P2 |
