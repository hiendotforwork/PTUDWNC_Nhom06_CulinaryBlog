# FR-AUTH-001 Frontend Integration Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace mock auth in AppContext with real API calls to FR-AUTH-001 backend.

**Architecture:** Simple service layer + token storage in localStorage. No Redux/Zustand — extend AppContext to handle auth state. Token stored in localStorage for SSR compatibility.

**Tech Stack:** Next.js 16, React 19, fetch API (no axios needed for single endpoint)

**Spec:** `docs/specs/fr-auth/FR-AUTH-001_Plan.md`

---

## Global Constraints

- API base URL: `http://localhost:5058`
- Register endpoint: `POST /api/v1/auth/register`
- Request: `{ email, userName, displayName, password }`
- Success response 201: `{ accessToken, refreshToken, expiresAt, user: UserDto }`
- Error 409: `{ errorCode: "AUTH_EMAIL_EXISTS" | "AUTH_USERNAME_EXISTS", message: string }`
- Error 422: `{ errorCode: "VALIDATION_ERROR", message: string, errors: [{ field: string, message: string }] }`
- Password requirements: 8+ chars, 1 uppercase, 1 lowercase, 1 digit, 1 special char
- UserDto: `{ id, email, userName, displayName, avatarUrl?, bio?, roles: string[] }`

---

## Review Focus

| Failure Mode | Expected Behavior | Owner |
|--------------|------------------|-------|
| Network error | Show toast "Không thể kết nối server" | Task 2 |
| 409 duplicate email/username | Show specific error under field | Task 3 |
| 422 validation | Show backend validation errors | Task 3 |
| 500 server error | Show toast "Lỗi server, thử lại sau" | Task 3 |
| Token missing | Redirect to login | Task 1 |
| Expired token | Silent refresh or redirect to login | Task 1 (future) |

---
## File Structure

```
frontend/app/
├── lib/
│   ├── api.ts          # API client + auth endpoints
│   ├── auth.ts         # Token storage helpers
│   └── types.ts        # Add AuthResponse, RegisterRequest types
├── context/
│   └── AppContext.tsx  # Modify: replace mock register/login with API calls
└── register/
    └── page.tsx        # Modify: update password validation to match backend
```

---

## Tasks

### Task 1: Create API Client & Auth Helpers

**Files:**
- Create: `frontend/app/lib/api.ts`
- Create: `frontend/app/lib/auth.ts`
- Modify: `frontend/app/lib/types.ts:1-83` (add auth types)

- [ ] **Step 1: Add auth types to types.ts**

```typescript
// Add after existing types
export interface AuthUser {
  id: string;
  displayName: string;
  userName: string;
  email: string;
  avatarUrl?: string;
  bio?: string;
  roles: string[];  // e.g. ["Author"]
  // Note: createdAt not returned by backend, use Date.now() on frontend
}

export interface RegisterRequest {
  email: string;
  userName: string;
  displayName: string;
  password: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: AuthUser;
}

export interface ApiError {
  errorCode?: string;  // e.g. "AUTH_EMAIL_EXISTS", "AUTH_USERNAME_EXISTS"
  message?: string;
  errors?: Array<{ field: string; message: string }>;
}
```

- [ ] **Step 2: Create auth.ts - token storage helpers**

```typescript
const TOKEN_KEY = "auth_access_token";
const REFRESH_KEY = "auth_refresh_token";
const USER_KEY = "auth_user";

export function getToken(): string | null {
  if (typeof window === "undefined") return null;
  return localStorage.getItem(TOKEN_KEY);
}

export function setToken(token: string): void {
  if (typeof window === "undefined") return;
  localStorage.setItem(TOKEN_KEY, token);
}

export function getRefreshToken(): string | null {
  if (typeof window === "undefined") return null;
  return localStorage.getItem(REFRESH_KEY);
}

export function setRefreshToken(token: string): void {
  if (typeof window === "undefined") return;
  localStorage.setItem(REFRESH_KEY, token);
}

export function getStoredUser(): (AuthUser & { createdAt: string }) | null {
  if (typeof window === "undefined") return null;
  const data = localStorage.getItem(USER_KEY);
  return data ? JSON.parse(data) : null;
}

export function setStoredUser(user: AuthUser): void {
  if (typeof window === "undefined") return;
  // Add createdAt since backend doesn't return it
  const userWithTimestamp = { ...user, createdAt: new Date().toISOString() };
  localStorage.setItem(USER_KEY, JSON.stringify(userWithTimestamp));
}

export function clearAuth(): void {
  if (typeof window === "undefined") return;
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(REFRESH_KEY);
  localStorage.removeItem(USER_KEY);
}
```

- [ ] **Step 3: Create api.ts - API client**

```typescript
import { AuthResponse, RegisterRequest, ApiError } from "./types";

const API_BASE = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5058";

async function handleResponse<T>(res: Response): Promise<T> {
  if (!res.ok) {
    const error: ApiError = await res.json().catch(() => ({ message: "Lỗi không xác định" }));
    throw error;
  }
  return res.json();
}

export async function register(data: RegisterRequest): Promise<AuthResponse> {
  const res = await fetch(`${API_BASE}/api/v1/auth/register`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
  return handleResponse<AuthResponse>(res);
}
```

- [ ] **Step 4: Commit**

```bash
git add frontend/app/lib/api.ts frontend/app/lib/auth.ts frontend/app/lib/types.ts
git commit -m "feat(frontend): add API client and auth helpers"
```

---

### Task 2: Update AppContext to Use Real API

**Files:**
- Modify: `frontend/app/context/AppContext.tsx:1-248`

- [ ] **Step 1: Update imports**

```typescript
import React, { useState, useEffect, ReactNode } from "react";
import { Recipe, User, ToastMessage, ToastType, Category, AuthUser, AuthResponse } from "../lib/types";
import * as auth from "../lib/auth";
```

- [ ] **Step 2: Update register function to call API**

Replace the mock `register` function (lines 181-195):

```typescript
const register = async (data: { displayName: string; email: string; userName: string; password: string }) => {
  try {
    const response: AuthResponse = await apiRegister({
      email: data.email,
      userName: data.userName,
      displayName: data.displayName,
      password: data.password,
    });

    // Store tokens and user
    auth.setToken(response.accessToken);
    auth.setRefreshToken(response.refreshToken);
    auth.setStoredUser(response.user);

    // Convert AuthUser to User for AppContext
    const user: User = {
      id: response.user.id,
      displayName: response.user.displayName,
      userName: response.user.userName,
      email: response.user.email,
      avatarUrl: response.user.avatarUrl || "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=200&auto=format&fit=crop&q=80",
      bio: response.user.bio,
      role: (response.user.roles.includes("Author") ? "User" : response.user.roles[0]) as "User" | "Admin",
      createdAt: new Date().toISOString(),
    };

    setCurrentUser(user);
    showToast("success", `Đăng ký thành công! Chào mừng ${user.displayName}`);
    return true;
  } catch (error: unknown) {
    // Handle network errors (fetch throws TypeError)
    if (error instanceof TypeError && error.message.includes("fetch")) {
      showToast("error", "Không thể kết nối server. Vui lòng kiểm tra kết nối mạng.");
      return false;
    }
    const apiError = error as { errorCode?: string; errors?: Array<{ field: string; message: string }>; message?: string };
    if (apiError.errorCode === "AUTH_EMAIL_EXISTS") {
      throw { field: "email", message: "Email đã được sử dụng" };
    }
    if (apiError.errorCode === "AUTH_USERNAME_EXISTS") {
      throw { field: "userName", message: "Tên đăng nhập đã được sử dụng" };
    }
    if (apiError.errors && apiError.errors.length > 0) {
      const firstError = apiError.errors[0];
      throw { field: firstError.field.toLowerCase(), message: firstError.message };
    }
    showToast("error", apiError.message || "Đăng ký thất bại. Vui lòng thử lại.");
    return false;
  }
};
```

- [ ] **Step 3: Update login function to call API (mock for now)**

Replace the mock `login` function (lines 164-179):

```typescript
const login = (email: string) => {
  // FR-AUTH-002: Login endpoint not implemented yet
  // Fallback to mock for demo
  const found = MOCK_USERS.find((u) => u.email.toLowerCase() === email.toLowerCase());
  const user = found || {
    id: `usr-${Date.now()}`,
    displayName: email.split("@")[0],
    userName: email.split("@")[0].toLowerCase(),
    email,
    avatarUrl: "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=200&auto=format&fit=crop&q=80",
    bio: "Đầu bếp gia đình nhiệt huyết.",
    role: "User",
    createdAt: new Date().toISOString(),
  };
  setCurrentUser(user);
  showToast("success", `Chào mừng trở lại, ${user.displayName}!`);
  return true;
};
```

- [ ] **Step 4: Initialize currentUser from localStorage**

Replace the initial state line 49 (`const [currentUser, setCurrentUser] = useState<User | null>(MOCK_USERS[0])`):

```typescript
// Start with null; restore from localStorage via useEffect below
const [currentUser, setCurrentUser] = useState<User | null>(null);
```

Add new useEffect after the state declarations (around line 73):

```typescript
// Restore auth state from localStorage on mount
useEffect(() => {
  const stored = auth.getStoredUser();
  if (stored) {
    setCurrentUser({
      id: stored.id,
      displayName: stored.displayName,
      userName: stored.userName,
      email: stored.email,
      avatarUrl: stored.avatarUrl || "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=200&auto=format&fit=crop&q=80",
      bio: stored.bio,
      role: (stored.roles.includes("Author") ? "User" : stored.roles[0]) as "User" | "Admin",
      createdAt: new Date().toISOString(), // not stored, use current time
    });
  }
}, []);
```

- [ ] **Step 5: Update logout to clear tokens**

Replace the `logout` function (lines 197-200):

```typescript
const logout = () => {
  auth.clearAuth();
  setCurrentUser(null);
  showToast("info", "Đã đăng xuất");
};
```

- [ ] **Step 6: Commit**

```bash
git add frontend/app/context/AppContext.tsx
git commit -m "feat(frontend): connect AppContext to real auth API"
```

---

### Task 3: Update Register Page to Handle API Errors

**Files:**
- Modify: `frontend/app/register/page.tsx:1-206`

- [ ] **Step 1: Update handleRegister to catch errors**

Replace `handleRegister` function:

```typescript
const handleRegister = async (e: React.FormEvent) => {
  e.preventDefault();
  const newErrors: Record<string, string> = {};

  // Client-side validation (mirrors backend)
  if (!displayName.trim() || displayName.length < 2) {
    newErrors.displayName = "Tên hiển thị phải có ít nhất 2 ký tự.";
  }
  if (displayName.length > 100) {
    newErrors.displayName = "Tên hiển thị không quá 100 ký tự.";
  }

  if (!email.trim() || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
    newErrors.email = "Email không hợp lệ.";
  }

  if (!userName.trim() || userName.length < 3) {
    newErrors.userName = "Tên đăng nhập phải từ 3 ký tự trở lên.";
  }
  if (userName.length > 30) {
    newErrors.userName = "Tên đăng nhập không quá 30 ký tự.";
  }
  if (!/^[a-zA-Z0-9_]+$/.test(userName)) {
    newErrors.userName = "Chỉ chấp nhận chữ cái, số và dấu gạch dưới.";
  }

  // Password: 8+ chars, 1 uppercase, 1 lowercase, 1 digit, 1 special
  if (password.length < 8) {
    newErrors.password = "Mật khẩu phải từ 8 ký tự trở lên.";
  } else if (!/[A-Z]/.test(password)) {
    newErrors.password = "Mật khẩu phải có ít nhất 1 chữ hoa.";
  } else if (!/[a-z]/.test(password)) {
    newErrors.password = "Mật khẩu phải có ít nhất 1 chữ thường.";
  } else if (!/[0-9]/.test(password)) {
    newErrors.password = "Mật khẩu phải có ít nhất 1 chữ số.";
  } else if (!/[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(password)) {
    newErrors.password = "Mật khẩu phải có ít nhất 1 ký tự đặc biệt.";
  }

  if (password !== confirmPassword) {
    newErrors.confirmPassword = "Mật khẩu xác nhận không khớp.";
  }

  if (Object.keys(newErrors).length > 0) {
    setErrors(newErrors);
    return;
  }

  setErrors({});
  setIsLoading(true);

  try {
    await register({ displayName, email, userName, password });
    router.push("/");
  } catch (err: unknown) {
    const error = err as { field?: string; message?: string };
    if (error.field && error.message) {
      setErrors({ [error.field]: error.message });
    }
  } finally {
    setIsLoading(false);
  }
};
```

- [ ] **Step 2: Update password placeholder text**

Update line ~128 from "Tối thiểu 6 ký tự..." to "Tối thiểu 8 ký tự, có chữ hoa, số, ký tự đặc biệt..." and add helperText:

```tsx
<Input
  label="Mật khẩu"
  type="password"
  placeholder="Tối thiểu 8 ký tự..."
  required
  value={password}
  onChange={(e) => setPassword(e.target.value)}
  error={errors.password}
  leftIcon={<Lock className="w-4 h-4" />}
  helperText="8+ ký tự, có chữ hoa, chữ thường, số và ký tự đặc biệt"
/>
```

- [ ] **Step 3: Disable Google signup button (not implemented)**

Update `handleGoogleSignup` to show toast:

```typescript
const handleGoogleSignup = () => {
  showToast("info", "Đăng ký Google đang được phát triển.");
};
```

- [ ] **Step 4: Commit**

```bash
git add frontend/app/register/page.tsx
git commit -m "feat(frontend): update register page with real validation and API integration"
```

---

### Task 4: Build & Test

**Files:** (none - verify existing)

- [ ] **Step 1: Build frontend**

```bash
cd frontend && pnpm build
```

Expected: Build succeeds with no errors

- [ ] **Step 2: Start backend (if not running)**

```bash
dotnet run --project src/CulinaryBlog.API
```

- [ ] **Step 3: Start frontend and test registration flow**

```bash
cd frontend && pnpm dev
```

Manual test:
1. Navigate to http://localhost:3000/register
2. Fill valid data and submit
3. Should see success toast and redirect to home
4. Token should be in localStorage

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "chore: ready for integration testing"
```

---

## Acceptance Criteria

| Criteria | Verification |
|----------|--------------|
| Valid registration → success toast + redirect to home | Manual test |
| Duplicate email → error under email field | Manual test |
| Duplicate username → error under username field | Manual test |
| Weak password → client-side validation errors | Manual test |
| Token stored in localStorage | DevTools check |
| Logout clears token | Manual test |
| Page refresh persists login | Manual test |

---

## NOT in Scope

| Item | Rationale |
|------|-----------|
| Login page API integration | FR-AUTH-002 |
| JWT token in API requests | FR-AUTH-002 |
| Token refresh | FR-AUTH-004 |
| Google OAuth | FR-AUTH-003 |
