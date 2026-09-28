# FR-AUTH-002 Frontend Integration — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Connect the backend login endpoint (FR-AUTH-002) to the frontend login page, replacing mock authentication.

**Architecture:** Frontend calls `POST /api/v1/auth/login`, stores JWT tokens in localStorage, and updates AppContext with the authenticated user.

**Tech Stack:** Next.js 16, React 19, TypeScript, Tailwind CSS

**Spec:** `docs/specs/fr-auth/FR-AUTH_APIContract.md` (section 2.2), `docs/specs/fr-auth/FR-AUTH_ErrorCodes.md` (section 2.2)

---

## Global Constraints

- API Base URL: `http://localhost:5058` (from existing api.ts)
- Error handling must show user-friendly Vietnamese messages
- Account locked (423) should show unlock time
- Rate limited (429) should show retry countdown
- Keep existing login page UI unchanged

---

## Review Focus

1. **401 vs 423 response** — 401 for wrong password, 423 for locked account
2. **Rate limit handling** — 429 response shows retry countdown
3. **Token storage** — JWT stored in localStorage via existing auth.ts
4. **User state sync** — AppContext updates with real backend user data
5. **Error mapping** — API error codes mapped to Vietnamese messages

---

## File Structure Overview

```
frontend/app/
├── lib/
│   ├── types.ts        [MODIFY - add LoginRequest]
│   └── api.ts          [MODIFY - add login API function]
└── context/
    └── AppContext.tsx  [MODIFY - update login() to call API]
```

---

## Tasks

### Task 1: Add LoginRequest type to types.ts

**Files:**
- Modify: `frontend/app/lib/types.ts`

**Interfaces:**
- Consumes: Backend LoginRequest schema
- Produces: LoginRequest type for API calls

- [x] **Step 1: Add LoginRequest and LoginResponse types**

```typescript
// Add after RegisterRequest (around line 107)
export interface LoginRequest {
  email: string;
  password: string;
}
```

- [x] **Step 2: Commit**

```bash
cd /media/thanhhien/DATA/PTUDWNC_Nhom06_CulinaryBlog
git add frontend/app/lib/types.ts
git commit -m "feat(frontend): add LoginRequest type for FR-AUTH-002"
```

---

### Task 2: Add login API function to api.ts

**Files:**
- Modify: `frontend/app/lib/api.ts`

**Interfaces:**
- Consumes: LoginRequest (email, password)
- Produces: AuthResponse with tokens and user info

- [x] **Step 1: Add login function after register function**

```typescript
// Add after register function (around line 62)
export async function login(data: LoginRequest): Promise<AuthResponse> {
  const res = await fetch(`${API_BASE}/api/v1/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
  return handleResponse<AuthResponse>(res);
}
```

- [x] **Step 2: Add LoginRequest to imports**

```typescript
// Update imports at top of file (around line 7-17)
import {
  ApiError,
  AuthResponse,
  DifficultyLevel,
  Ingredient,
  LoginRequest,  // ADD THIS
  Recipe,
  RecipeImage,
  RecipeStatus,
  RecipeStep,
  RegisterRequest,
} from "./types";
```

- [x] **Step 3: Commit**

```bash
git add frontend/app/lib/api.ts
git commit -m "feat(frontend): add login API function for FR-AUTH-002"
```

---

### Task 3: Update AppContext.login() to call API

**Files:**
- Modify: `frontend/app/context/AppContext.tsx`

**Interfaces:**
- Consumes: LoginRequest (email, password)
- Produces: Updates currentUser state, stores tokens

- [x] **Step 1: Update imports to add login**

```typescript
// Update import from api.ts (around line 10-27)
import {
  register as apiRegister,
  login as apiLogin,  // ADD THIS
  getRecipes,
  getRecipe,
  // ... rest unchanged
} from "../lib/api";
```

- [x] **Step 2: Replace mock login function (around line 299-314)**

```typescript
// Replace the existing login function with this:
const login = async (email: string, password?: string): Promise<boolean> => {
  if (!password) {
    showToast("error", "Vui lòng nhập mật khẩu");
    return false;
  }

  try {
    const response = await apiLogin({ email, password });

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
      role: (response.user.roles?.includes("Author") ? "User" : (response.user.roles?.[0] as "User" | "Admin")) || "User",
      createdAt: new Date().toISOString(),
    };

    setCurrentUser(user);
    await reloadRecipes();
    showToast("success", `Chào mừng trở lại, ${user.displayName}!`);
    return true;
  } catch (error: unknown) {
    // Handle network errors (fetch throws TypeError)
    if (error instanceof TypeError && error.message.includes("fetch")) {
      showToast("error", "Không thể kết nối server");
      return false;
    }

    // Handle API errors
    const apiError = error as {
      statusCode?: number;
      errorCode?: string;
      extensions?: { code?: string; retryAfterSeconds?: number; unlockAt?: string };
      message?: string;
    };

    // Account locked (423)
    if (apiError.statusCode === 423) {
      const unlockAt = apiError.extensions?.unlockAt;
      const retryAfter = apiError.extensions?.retryAfterSeconds;
      let message = "Tài khoản đã bị khóa do đăng nhập sai nhiều lần.";
      if (retryAfter) {
        const minutes = Math.ceil(retryAfter / 60);
        message = `Tài khoản đã bị khóa. Vui lòng thử lại sau ${minutes} phút.`;
      } else if (unlockAt) {
        const unlockTime = new Date(unlockAt).toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" });
        message = `Tài khoản đã bị khóa. Vui lòng thử lại sau ${unlockTime}.`;
      }
      showToast("error", message);
      return false;
    }

    // Rate limited (429)
    if (apiError.statusCode === 429) {
      const retryAfter = apiError.extensions?.retryAfterSeconds;
      if (retryAfter) {
        const seconds = Math.ceil(retryAfter);
        showToast("warning", `Quá nhiều yêu cầu. Vui lòng chờ ${seconds} giây.`);
      } else {
        showToast("warning", "Quá nhiều yêu cầu. Vui lòng thử lại sau.");
      }
      return false;
    }

    // Invalid credentials (401) - generic message for security
    if (apiError.statusCode === 401 || apiError.errorCode === "AUTH_INVALID_CREDENTIALS") {
      showToast("error", "Email hoặc mật khẩu không đúng.");
      return false;
    }

    // Validation error (422)
    if (apiError.statusCode === 422) {
      showToast("error", "Thông tin đăng nhập không hợp lệ.");
      return false;
    }

    // Default error
    showToast("error", apiError.message || "Đăng nhập thất bại. Vui lòng thử lại.");
    return false;
  }
};
```

- [x] **Step 3: Update AppContextType interface**

```typescript
// Update login signature in AppContextType (around line 42)
login: (email: string, password?: string) => Promise<boolean>;
```

- [x] **Step 4: Update login page to handle async login**

```typescript
// Update login/page.tsx - change handleLogin to async
const handleLogin = async (e: React.FormEvent) => {
  e.preventDefault();
  const newErrors: Record<string, string> = {};

  if (!email.trim()) {
    newErrors.email = "Vui lòng nhập địa chỉ email.";
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
    newErrors.email = "Địa chỉ email không hợp lệ.";
  }

  if (!password) {
    newErrors.password = "Vui lòng nhập mật khẩu.";
  }

  if (Object.keys(newErrors).length > 0) {
    setErrors(newErrors);
    return;
  }

  setErrors({});
  setIsLoading(true);

  const success = await login(email, password);
  setIsLoading(false);

  if (success) {
    router.push("/");
  }
};
```

- [x] **Step 5: Update handleGoogleLogin similarly**

```typescript
// Update handleGoogleLogin in login/page.tsx
const handleGoogleLogin = async () => {
  setIsLoading(true);
  // TODO: Implement Google OAuth login (FR-AUTH-003)
  showToast("info", "Đăng nhập Google đang được phát triển.");
  setIsLoading(false);
};
```

- [x] **Step 6: Commit**

```bash
git add frontend/app/context/AppContext.tsx frontend/app/login/page.tsx
git commit -m "feat(frontend): connect login to backend API for FR-AUTH-002"
```

---

### Task 4: Update handleResponse to preserve error extensions

**Files:**
- Modify: `frontend/app/lib/api.ts`

**Interfaces:**
- Consumes: HTTP response (error case)
- Produces: Error object with extensions preserved

- [x] **Step 1: Update handleResponse to include extensions**

```typescript
// Replace handleResponse error handling (around line 41-51)
async function handleResponse<T>(res: Response): Promise<T> {
  if (!res.ok) {
    const errorData = await res.json().catch(() => ({}));
    const error: ApiError = {
      statusCode: res.status,
      errorCode: errorData.extensions?.code,
      message: errorData.detail || errorData.title || (res.status === 500 ? "Lỗi server, thử lại sau." : "Lỗi không xác định từ máy chủ."),
      extensions: errorData.extensions,
    };
    throw error;
  }
  return res.json() as Promise<T>;
}
```

- [x] **Step 2: Update ApiError type in types.ts**

```typescript
// Update ApiError interface (around line 126-132)
export interface ApiError {
  errorCode?: string;
  error?: string;
  statusCode?: number;
  message?: string;
  errors?: Array<{ field: string; message: string }>;
  extensions?: {
    code?: string;
    retryAfterSeconds?: number;
    unlockAt?: string;
    [key: string]: unknown;
  };
}
```

- [x] **Step 3: Commit**

```bash
git add frontend/app/lib/api.ts frontend/app/lib/types.ts
git commit -m "feat(frontend): preserve API error extensions for error handling"
```

---

### Task 5: Build and verify

- [x] **Step 1: Build frontend**

```bash
cd /media/thanhhien/DATA/PTUDWNC_Nhom06_CulinaryBlog/frontend
pnpm build
```

- [x] **Step 2: Verify no build errors**

Expected: Build succeeded with no errors.

- [x] **Step 3: Start backend and test login**

```bash
# Terminal 1: Start backend
cd /media/thanhhien/DATA/PTUDWNC_Nhom06_CulinaryBlog
dotnet run --project src/CulinaryBlog.API

# Terminal 2: Start frontend
cd /media/thanhhien/DATA/PTUDWNC_Nhom06_CulinaryBlog/frontend
pnpm dev
```

- [x] **Step 4: Commit if needed**

```bash
git add -A && git commit -m "fix(frontend): resolve build errors in FR-AUTH-002 integration"
```

---

## Acceptance Criteria

- [x] Login page calls `POST /api/v1/auth/login`
- [x] Successful login stores tokens in localStorage
- [x] Successful login updates AppContext with user data
- [x] Failed login (401) shows "Email hoặc mật khẩu không đúng"
- [x] Locked account (423) shows unlock time
- [x] Rate limited (429) shows retry countdown
- [x] Network error shows "Không thể kết nối server"
- [x] Frontend builds without errors

---

## NOT in Scope

| Item | Rationale |
|------|----------|
| Unit tests | Deferred |
| Google OAuth login | FR-AUTH-003 |
| Token refresh | FR-AUTH-004 |
| Logout API | FR-AUTH-005 |
| Protected route middleware | Separate task |

---

## Effort Estimate

| Task | Files | Complexity |
|------|-------|------------|
| 1 | 1 | Low |
| 2 | 1 | Low |
| 3 | 2 | Medium |
| 4 | 2 | Low |
| 5 | 1 | Low |
| **Total** | **7** | ~1 hour |

---

## File Summary

| Type | Count |
|------|-------|
| Modify | 5 |
| **Total** | **5** |
