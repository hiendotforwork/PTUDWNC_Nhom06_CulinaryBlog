# Browser Testing Plan: Password Change and Reset

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Execute end-to-end browser testing for the signed-in password change, forgot-password request, email delivery, and reset-password flows on `localhost`, validating UI behavior, anti-enumeration, Mailpit email delivery, single-use token security, and refresh-token session revocation.

**Architecture:** Browser tests run against Next.js (`http://localhost:3001`), ASP.NET Core API (`http://localhost:5058`), PostgreSQL, Redis, and Mailpit (`http://localhost:8025`). Tests are executed using the `browser_subagent` tool for interactive browser validation (with screenshots/recordings) and codified into an automated Playwright E2E suite interacting directly with Mailpit's REST API for link retrieval.

**Tech Stack:** Browser Subagent (Playwright/Chromium), Next.js 16, React 19, ASP.NET Core 10 Identity, Mailpit REST API, Docker Compose, curl/jq, bash.

**Spec:** `docs/superpowers/specs/2026-10-27-password-change-reset-design.md`  
**Implementation Plan Reference:** `docs/superpowers/plans/2026-10-27-password-change-reset.md`

## Global Constraints

- Frontend application URL is `http://localhost:3001`.
- Backend API URL is `http://localhost:5058`.
- Mailpit Web UI is `http://localhost:8025` and REST API is `http://localhost:8025/api/v1/messages`.
- Anti-enumeration: `/forgot-password` must always return the exact same generic message: `"Nếu email này thuộc tài khoản hợp lệ, hướng dẫn đặt lại mật khẩu sẽ được gửi."` regardless of whether the email exists, is unconfirmed, or is Google-only.
- Ineligible accounts (unconfirmed, non-existent, Google-only) must NOT trigger any email in Mailpit.
- 60-second cooldown per normalized email must suppress duplicate emails in Mailpit while keeping the UI response unchanged.
- Password reset link must contain credentials in the URL fragment (`#userId=...&token=...`); the frontend must parse the fragment, scrub it from `window.location.hash` / browser history, and send them only in the POST body.
- Successful password change or reset must invalidate all existing refresh tokens and force sign-out.

## Review Focus

- Fragment leaking: verify the reset token is stripped from the browser URL address bar and not preserved in browser history or referrer headers. Pin in Task 4.
- Ineligible account enumeration: verify entering a nonexistent or Google-only email on the forgot-password page shows identical success UI and produces zero Mailpit emails. Pin in Task 3.
- Cooldown suppression: verify requesting reset twice within 60s for the same email sends only 1 email to Mailpit. Pin in Task 3.
- Single-use and tampered token rejection: verify reusing a consumed token or submitting a tampered token produces the generic invalid/expired error without updating the password. Pin in Task 4.
- Session revocation: verify an active browser session on Device A is logged out or blocked from refreshing after a password change/reset on Device B. Pin in Task 5.

---

## File Map

- `scripts/seed-browser-test-users.sh`: Helper bash script to prepare dedicated seed users for browser testing (confirmed local user, unconfirmed user, and Google-only user).
- `scripts/verify-mailpit-reset-link.sh`: Helper bash script to fetch the latest password reset email from Mailpit API and parse the URL fragment.
- `frontend/e2e/password-change-reset.spec.ts`: Automated Playwright E2E test suite covering all password change and recovery scenarios.
- `frontend/package.json`: Add Playwright test dependency and `test:e2e` script if needed.

---

### Task 1: Environment Setup, Mailpit Health, and Test User Provisioning

**Files:**
- Create: `scripts/seed-browser-test-users.sh`
- Create: `scripts/verify-mailpit-reset-link.sh`

**Interfaces:**
- Consumes: Docker services (`docker compose up -d postgres redis mailpit api`), Backend API at `http://localhost:5058`, Mailpit at `http://localhost:8025`.
- Produces: Test users seeded in database:
  - Local user: `e2e_user@example.com` / `OldPassword123!`, EmailConfirmed = true, DisplayName = "E2E User".
  - Unconfirmed user: `e2e_unconfirmed@example.com` / `OldPassword123!`, EmailConfirmed = false.
  - Google user: `e2e_google@example.com`, HasPassword = false.
- Produces: `scripts/verify-mailpit-reset-link.sh <email>` which outputs `{ "userId": "...", "token": "...", "link": "..." }`.

- [ ] **Step 1: Write helper script `scripts/seed-browser-test-users.sh`** to ensure Docker services are healthy and seed the test users via direct database query or API registration.

```bash
#!/usr/bin/env bash
set -euo pipefail

echo "=== 1. Checking Docker services ==="
docker compose up -d postgres redis mailpit api

echo "Waiting for API to be ready at http://localhost:5058/health or root..."
until curl -s http://localhost:5058/api/v1/recipes > /dev/null; do
  sleep 2
done

echo "Waiting for Mailpit at http://localhost:8025/api/v1/messages..."
until curl -s http://localhost:8025/api/v1/messages > /dev/null; do
  sleep 1
done

echo "=== 2. Clearing previous Mailpit messages ==="
curl -s -X DELETE http://localhost:8025/api/v1/messages || true

echo "=== 3. Seeding E2E test users in PostgreSQL ==="
docker compose exec -T postgres psql -U culinary -d culinary_blog << 'EOSQL'
-- Create or update e2e_user@example.com (confirmed, with password)
DO $$
DECLARE
    v_user_id text := 'e2e-test-user-confirmed-id';
    v_unconf_id text := 'e2e-test-user-unconfirmed-id';
    v_google_id text := 'e2e-test-user-google-only-id';
BEGIN
    -- Local confirmed user (password: OldPassword123!)
    -- ASP.NET Identity password hash for OldPassword123!
    DELETE FROM "AspNetUsers" WHERE "Email" IN ('e2e_user@example.com', 'e2e_unconfirmed@example.com', 'e2e_google@example.com');
    
    INSERT INTO "AspNetUsers" (
        "Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
        "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
        "DisplayName", "IsActive", "CreatedAt", "AccessFailedCount", "LockoutEnabled", "PhoneNumberConfirmed", "TwoFactorEnabled"
    ) VALUES (
        v_user_id, 'e2e_user', 'E2E_USER', 'e2e_user@example.com', 'E2E_USER@EXAMPLE.COM',
        true, 'AQAAAAIAAYagAAAAEGK5Z6w6rI2jW4T6jW6Z8jG4R1v9X0y1Z2a3b4c5d6e7f8g9h0==', gen_random_uuid()::text, gen_random_uuid()::text,
        'E2E Tester', true, NOW(), 0, false, false, false
    );

    -- Unconfirmed user
    INSERT INTO "AspNetUsers" (
        "Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
        "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
        "DisplayName", "IsActive", "CreatedAt", "AccessFailedCount", "LockoutEnabled", "PhoneNumberConfirmed", "TwoFactorEnabled"
    ) VALUES (
        v_unconf_id, 'e2e_unconfirmed', 'E2E_UNCONFIRMED', 'e2e_unconfirmed@example.com', 'E2E_UNCONFIRMED@EXAMPLE.COM',
        false, 'AQAAAAIAAYagAAAAEGK5Z6w6rI2jW4T6jW6Z8jG4R1v9X0y1Z2a3b4c5d6e7f8g9h0==', gen_random_uuid()::text, gen_random_uuid()::text,
        'E2E Unconfirmed', true, NOW(), 0, false, false, false
    );

    -- Google-only user (no password hash)
    INSERT INTO "AspNetUsers" (
        "Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
        "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
        "DisplayName", "IsActive", "CreatedAt", "AccessFailedCount", "LockoutEnabled", "PhoneNumberConfirmed", "TwoFactorEnabled"
    ) VALUES (
        v_google_id, 'e2e_google', 'E2E_GOOGLE', 'e2e_google@example.com', 'E2E_GOOGLE@EXAMPLE.COM',
        true, NULL, gen_random_uuid()::text, gen_random_uuid()::text,
        'E2E Google User', true, NOW(), 0, false, false, false
    );
END $$;
EOSQL

echo "Seed complete! Users created."
```

- [ ] **Step 2: Write helper script `scripts/verify-mailpit-reset-link.sh`** to parse Mailpit messages and extract the reset URL fragment.

```bash
#!/usr/bin/env bash
set -euo pipefail

TARGET_EMAIL="${1:-e2e_user@example.com}"

# Query Mailpit messages for recipient
MSG_ID=$(curl -s "http://localhost:8025/api/v1/messages" | jq -r --arg email "$TARGET_EMAIL" '.[] | select(.To[].Address == $email) | .ID' | head -n 1)

if [ -z "$MSG_ID" ] || [ "$MSG_ID" = "null" ]; then
  echo "ERROR: No email found for $TARGET_EMAIL" >&2
  exit 1
fi

BODY=$(curl -s "http://localhost:8025/api/v1/message/$MSG_ID" | jq -r '.Text // .HTML')

# Extract link: http://localhost:3001/reset-password#userId=...&token=...
LINK=$(echo "$BODY" | grep -o 'http://localhost:3001/reset-password#[^"'"'"' <>]*' | head -n 1)

if [ -z "$LINK" ]; then
  echo "ERROR: Reset link not found in message body: $BODY" >&2
  exit 2
fi

echo "$LINK"
```

- [ ] **Step 3: Make scripts executable and test environment setup**

Run: `chmod +x scripts/seed-browser-test-users.sh scripts/verify-mailpit-reset-link.sh`  
Expected: Scripts are executable.

- [ ] **Step 4: Commit setup scripts**

```bash
git add scripts/seed-browser-test-users.sh scripts/verify-mailpit-reset-link.sh
git commit -m "chore(test): add browser test seeding and mailpit inspection scripts"
```

---

### Task 2: Browser Test Suite 1 — Signed-In Password Change (`/profile`)

**Files:**
- Test via: `browser_subagent` task and recorded WebP video `test_browser_change_password`.

**Interfaces:**
- Consumes: `http://localhost:3001/login`, `http://localhost:3001/profile`.
- Validates:
  1. Profile page displays "Đổi mật khẩu" section only when `hasPassword === true`.
  2. Submitting with wrong current password displays error and preserves session.
  3. Submitting new password violating Identity policy displays validation message.
  4. Submitting valid current and new password succeeds, clears browser session, and redirects to `/login`.
  5. Subsequent login with old password fails; login with new password succeeds.
  6. Google-only user has no password change section on `/profile`.

- [ ] **Step 1: Execute Browser Subagent for Change Password Failure Cases**
  - Launch `browser_subagent` with RecordingName `change_pwd_validation`.
  - Navigate to `http://localhost:3001/login`.
  - Fill email `e2e_user@example.com` and password `OldPassword123!`, click "Đăng nhập".
  - Verify redirection to `/` or `/profile`.
  - Navigate to `/profile`.
  - Locate section "Đổi mật khẩu" (contains Current Password, New Password, Confirm New Password).
  - Test 1 (Wrong current password):
    - Enter Current Password: `WrongPassword999!`.
    - Enter New Password: `NewValidPassword456!`.
    - Click "Đổi mật khẩu" / "Lưu thay đổi".
    - Assert: Error alert/toast appears ("Mật khẩu hiện tại không đúng" or API error description).
    - Assert: User remains on `/profile`, session is still valid.
  - Test 2 (Weak new password):
    - Enter Current Password: `OldPassword123!`.
    - Enter New Password: `123`.
    - Click "Đổi mật khẩu".
    - Assert: Password policy error message is displayed ("Mật khẩu phải có ít nhất 6 ký tự...").

- [ ] **Step 2: Execute Browser Subagent for Change Password Success Flow**
  - In the same profile form:
    - Enter Current Password: `OldPassword123!`.
    - Enter New Password: `NewSecret2026!`.
    - Click "Đổi mật khẩu".
  - Assert: Success toast appears ("Đổi mật khẩu thành công. Vui lòng đăng nhập lại.").
  - Assert: Browser storage (`accessToken`, `refreshToken`, user in localStorage) is cleared.
  - Assert: Browser automatically redirects to `http://localhost:3001/login`.

- [ ] **Step 3: Verify Authentication State in Browser**
  - On `http://localhost:3001/login`:
    - Attempt login with OLD password `OldPassword123!` -> Assert: Login fails with error "Email hoặc mật khẩu không chính xác."
    - Attempt login with NEW password `NewSecret2026!` -> Assert: Login succeeds and navigates into app.

- [ ] **Step 4: Execute Browser Subagent for Google-Only Account Visibility**
  - Sign in as `e2e_google@example.com` (or mock profile with `hasPassword = false`).
  - Navigate to `/profile`.
  - Assert: The "Đổi mật khẩu" form is NOT rendered on the page (or shows explicit notice "Tài khoản liên kết Google không sử dụng mật khẩu cục bộ").

---

### Task 3: Browser Test Suite 2 — Forgot Password Request & Cooldown (`/forgot-password`)

**Files:**
- Test via: `browser_subagent` task and recorded WebP video `test_browser_forgot_password`.

**Interfaces:**
- Consumes: `http://localhost:3001/login`, `http://localhost:3001/forgot-password`, `http://localhost:8025/api/v1/messages`.
- Validates:
  1. Entry link from `/login` -> "Quên mật khẩu?" navigates to `/forgot-password`.
  2. Client-side email validation on `/forgot-password`.
  3. Anti-enumeration: nonexistent, unconfirmed, and Google-only emails display the exact same accepted message without sending mail to Mailpit.
  4. Valid user request triggers email to Mailpit with valid subject and fragment link.
  5. 60-second cooldown suppresses duplicate emails.

- [ ] **Step 1: Execute Browser Subagent for Login Entry Link and Form Validation**
  - Launch `browser_subagent` with RecordingName `forgot_pwd_form`.
  - Navigate to `http://localhost:3001/login`.
  - Click link "Quên mật khẩu?".
  - Assert: URL changes to `http://localhost:3001/forgot-password`.
  - Assert: Heading "Quên mật khẩu" or "Khôi phục mật khẩu" is visible.
  - Submit empty form -> Assert: Validation error "Vui lòng nhập địa chỉ email."
  - Enter `invalid-email-format` and submit -> Assert: Validation error "Địa chỉ email không hợp lệ."

- [ ] **Step 2: Execute Anti-Enumeration Tests**
  - Clear Mailpit messages: `curl -s -X DELETE http://localhost:8025/api/v1/messages`.
  - Test 2.1 (Non-existent email):
    - Enter `nonexistent_user_999@culinaryblog.vn` and submit.
    - Assert: UI displays message `"Nếu email này thuộc tài khoản hợp lệ, hướng dẫn đặt lại mật khẩu sẽ được gửi."`
    - Check Mailpit: `curl -s http://localhost:8025/api/v1/messages | jq 'length'` -> Output must be `0`.
  - Test 2.2 (Unconfirmed account):
    - Enter `e2e_unconfirmed@example.com` and submit.
    - Assert: UI displays the exact same accepted message.
    - Check Mailpit: Output must still be `0`.
  - Test 2.3 (Google-only account):
    - Enter `e2e_google@example.com` and submit.
    - Assert: UI displays the exact same accepted message.
    - Check Mailpit: Output must still be `0`.

- [ ] **Step 3: Execute Happy Path Forgot Password Flow**
  - Enter confirmed local user email: `e2e_user@example.com`.
  - Click "Gửi yêu cầu" / "Tiếp tục".
  - Assert: UI displays `"Nếu email này thuộc tài khoản hợp lệ, hướng dẫn đặt lại mật khẩu sẽ được gửi."`
  - Assert: Submit button shows cooldown timer or disabled state.
  - Check Mailpit via script: `bash scripts/verify-mailpit-reset-link.sh e2e_user@example.com`.
  - Assert: Script outputs link starting with `http://localhost:3001/reset-password#userId=` and containing `&token=`.

- [ ] **Step 4: Execute 60-Second Cooldown Verification**
  - Count Mailpit messages before retry: `COUNT_BEFORE=$(curl -s http://localhost:8025/api/v1/messages | jq 'length')`.
  - Within 15 seconds, enter `e2e_user@example.com` again and submit.
  - Assert: UI returns same accepted response.
  - Count Mailpit messages after retry: `COUNT_AFTER=$(curl -s http://localhost:8025/api/v1/messages | jq 'length')`.
  - Assert: `COUNT_AFTER == COUNT_BEFORE` (no duplicate email sent).

---

### Task 4: Browser Test Suite 3 — Reset Password Submission (`/reset-password`)

**Files:**
- Test via: `browser_subagent` task and recorded WebP video `test_browser_reset_password`.

**Interfaces:**
- Consumes: Reset URL extracted from Mailpit, e.g. `http://localhost:3001/reset-password#userId=<UID>&token=<TOKEN>`.
- Validates:
  1. URL fragment security: `#userId=...&token=...` is parsed and immediately stripped from the address bar.
  2. Direct navigation to `/reset-password` without fragment displays error state.
  3. Weak new password validation against Identity policy.
  4. Happy path: Submit new password -> succeeds -> redirects to `/login`.
  5. Single-use token: Reusing the same link fails with generic invalid/expired link error.
  6. Tampered token: Modifying the token fails with generic invalid/expired link error.

- [ ] **Step 1: Test Missing or Tampered URL Fragment**
  - Navigate to `http://localhost:3001/reset-password` (without hash fragment).
  - Assert: Page displays error alert: "Liên kết không hợp lệ hoặc thiếu thông tin khôi phục." and a link "Quay lại trang quên mật khẩu".
  - Navigate to `http://localhost:3001/reset-password#userId=invalid-user&token=corrupted-token`.
  - Enter New Password: `ValidPassword2026!`.
  - Submit -> Assert: Error message "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn."

- [ ] **Step 2: Test URL Fragment Sanitization and Policy Validation**
  - Retrieve fresh valid reset link for `e2e_user@example.com` via Mailpit.
  - Navigate browser to the retrieved reset link: `http://localhost:3001/reset-password#userId=...&token=...`.
  - Inspect browser URL bar:
    - Assert: `window.location.hash` is empty (`""`).
    - Assert: URL displayed in address bar is cleanly `http://localhost:3001/reset-password`.
  - Test Weak Password:
    - Enter `12345` in "Mật khẩu mới".
    - Click "Đặt lại mật khẩu".
    - Assert: Error message "Mật khẩu phải có ít nhất 6 ký tự..." is displayed.

- [ ] **Step 3: Test Reset Password Success**
  - On the same reset form:
    - Enter "Mật khẩu mới": `ResetSuperSecret2026!`.
    - Enter "Xác nhận mật khẩu": `ResetSuperSecret2026!`.
    - Click "Đặt lại mật khẩu".
  - Assert: Success notification appears ("Đặt lại mật khẩu thành công!").
  - Assert: Browser redirects to `http://localhost:3001/login`.
  - Attempt login with previous password `NewSecret2026!` -> Assert: Fails.
  - Attempt login with newly reset password `ResetSuperSecret2026!` -> Assert: Succeeds!

- [ ] **Step 4: Test Single-Use Token Invalidation**
  - Open the exact same reset link again in the browser: `http://localhost:3001/reset-password#userId=...&token=...`.
  - Enter New Password: `AnotherNewPassword2026!`.
  - Submit form.
  - Assert: Error displayed: "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn."
  - Assert: Password remains `ResetSuperSecret2026!`.

---

### Task 5: Browser Test Suite 4 — Multi-Session & Refresh Token Revocation

**Files:**
- Test via: `browser_subagent` task with dual browser contexts / private tabs.

**Interfaces:**
- Consumes: Active refresh token sessions on PostgreSQL table `RefreshTokens`.
- Validates:
  1. User signs in on Session A (Browser window 1).
  2. User changes or resets password on Session B (Browser window 2).
  3. Session A attempts to fetch protected profile or rotate refresh token.
  4. Session A is denied (HTTP 401) and forced to sign in again.

- [ ] **Step 1: Setup Active Sessions**
  - Window 1 (Context A): Log in as `e2e_user@example.com` with `ResetSuperSecret2026!`.
  - Verify tokens in localStorage (`accessToken` and `refreshToken`).
  - Query DB: `docker compose exec -T postgres psql -U culinary -d culinary_blog -c 'SELECT "Token", "IsRevoked" FROM "RefreshTokens" WHERE "UserId" = '\''e2e-test-user-confirmed-id'\'';'`
  - Assert: At least 1 active refresh token exists (`IsRevoked = false`).

- [ ] **Step 2: Trigger Password Change in Window 2**
  - Window 2 (Incognito / Context B): Log in and change password to `FinalSessionPassword2026!`.
  - Assert: Password change succeeds in Window 2.
  - Query DB:
  - Assert: ALL refresh tokens for `e2e-test-user-confirmed-id` now have `IsRevoked = true` (or are deleted).

- [ ] **Step 3: Verify Session Invalidation in Window 1**
  - Switch back to Window 1.
  - Click "Hồ sơ" or refresh page.
  - Trigger API call with expired/rotating token.
  - Assert: Backend rejects with 401 Unauthorized.
  - Assert: Window 1 clears auth state and redirects to `/login`.

---

### Task 6: Codified Playwright E2E Regression Suite

**Files:**
- Create: `frontend/e2e/password-change-reset.spec.ts`
- Modify: `frontend/package.json`

**Interfaces:**
- Produces: Executable command `pnpm --dir frontend test:e2e` for CI and headless local runs.

- [ ] **Step 1: Install Playwright if not present**

Run: `pnpm --dir frontend add -D @playwright/test`  
Expected: `@playwright/test` added to devDependencies.

- [ ] **Step 2: Create `frontend/e2e/password-change-reset.spec.ts`** with automated checks for Mailpit API and browser flows.

```typescript
import { test, expect } from '@playwright/test';

const BASE_URL = process.env.BASE_URL || 'http://localhost:3001';
const MAILPIT_URL = process.env.MAILPIT_URL || 'http://localhost:8025';

test.describe('Password Change & Reset E2E', () => {
  test.beforeEach(async ({ request }) => {
    // Clear Mailpit inbox
    await request.delete(`${MAILPIT_URL}/api/v1/messages`).catch(() => {});
  });

  test('TC01: Forgot password anti-enumeration for invalid account', async ({ page, request }) => {
    await page.goto(`${BASE_URL}/forgot-password`);
    await page.fill('input[type="email"]', 'nonexistent_account@example.com');
    await page.click('button[type="submit"]');

    await expect(page.locator('text=Nếu email này thuộc tài khoản hợp lệ')).toBeVisible();

    // Verify 0 messages in Mailpit
    const res = await request.get(`${MAILPIT_URL}/api/v1/messages`);
    const messages = await res.json();
    expect(messages.length).toBe(0);
  });

  test('TC02: Full recovery loop via Mailpit reset link', async ({ page, request }) => {
    // 1. Request reset
    await page.goto(`${BASE_URL}/forgot-password`);
    await page.fill('input[type="email"]', 'e2e_user@example.com');
    await page.click('button[type="submit"]');
    await expect(page.locator('text=Nếu email này thuộc tài khoản hợp lệ')).toBeVisible();

    // 2. Poll Mailpit for reset email
    let resetLink = '';
    for (let i = 0; i < 10; i++) {
      const res = await request.get(`${MAILPIT_URL}/api/v1/messages`);
      const messages = await res.json();
      const msg = messages.find((m: any) => m.To?.some((t: any) => t.Address === 'e2e_user@example.com'));
      if (msg) {
        const detailRes = await request.get(`${MAILPIT_URL}/api/v1/message/${msg.ID}`);
        const detail = await detailRes.json();
        const text = detail.Text || detail.HTML || '';
        const match = text.match(/http:\/\/localhost:3001\/reset-password#[^\s"'<>]+/);
        if (match) {
          resetLink = match[0];
          break;
        }
      }
      await page.waitForTimeout(1000);
    }
    expect(resetLink).toBeTruthy();

    // 3. Open reset link
    await page.goto(resetLink);
    // Fragment must be sanitized from URL bar
    await expect(page).toHaveURL(`${BASE_URL}/reset-password`);

    // 4. Fill new password
    await page.fill('input[name="newPassword"], input[placeholder*="Mật khẩu mới"]', 'AutomatedPass2026!');
    await page.click('button[type="submit"]');

    // 5. Must redirect to login
    await expect(page).toHaveURL(`${BASE_URL}/login`);

    // 6. Login with new password
    await page.fill('input[type="email"]', 'e2e_user@example.com');
    await page.fill('input[type="password"]', 'AutomatedPass2026!');
    await page.click('button[type="submit"]');
    await expect(page).not.toHaveURL(`${BASE_URL}/login`);
  });
});
```

- [ ] **Step 3: Run Playwright test suite**

Run: `pnpm --dir frontend exec playwright test e2e/password-change-reset.spec.ts`  
Expected: All tests PASS.

- [ ] **Step 4: Commit Playwright suite**

```bash
git add frontend/e2e/password-change-reset.spec.ts frontend/package.json
git commit -m "test(auth): add playwright e2e tests for password change and reset"
```

---

## Final Verification Checklist

- [ ] All Docker containers (`postgres`, `redis`, `mailpit`, `api`) and frontend server are healthy.
- [ ] Signed-in change password validates current password and policy, clears session on success, and logs in with new password.
- [ ] Forgot password form handles non-existent, unconfirmed, and Google-only emails with identical generic message and 0 Mailpit emails.
- [ ] Mailpit successfully receives reset email for eligible users with URL fragment link.
- [ ] 60-second cooldown prevents spamming reset emails for the same normalized address.
- [ ] Reset password page scrubs the URL fragment immediately upon loading.
- [ ] Reset password rejects expired/reused/tampered tokens.
- [ ] Multi-session test confirms refresh tokens are invalidated upon password change or reset.
- [ ] Browser recording videos and Playwright test logs are saved in artifacts directory.
