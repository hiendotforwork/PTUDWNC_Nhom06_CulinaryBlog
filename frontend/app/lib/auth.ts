import { AuthUser } from "./types";
import { refreshToken } from "./api";

const TOKEN_KEY = "auth_access_token";
const REFRESH_KEY = "auth_refresh_token";
const USER_KEY = "auth_user";

// Auth state change listener
type AuthChangeCallback = () => void;
let _authChangeCallback: AuthChangeCallback | null = null;

/**
 * Register a callback to be called when auth state changes (login, logout, refresh)
 */
export function onAuthChange(callback: AuthChangeCallback): () => void {
  _authChangeCallback = callback;
  return () => {
    _authChangeCallback = null;
  };
}

function _notifyAuthChange(): void {
  if (typeof window !== "undefined" && _authChangeCallback) {
    _authChangeCallback();
  }
}

export function getToken(): string | null {
  if (typeof window === "undefined") return null;
  return localStorage.getItem(TOKEN_KEY);
}

export function setToken(token: string): void {
  if (typeof window === "undefined") return;
  localStorage.setItem(TOKEN_KEY, token);
  _notifyAuthChange();
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
  try {
    return data ? JSON.parse(data) : null;
  } catch {
    return null;
  }
}

export function setStoredUser(user: AuthUser): void {
  if (typeof window === "undefined") return;
  // Add createdAt since backend doesn't return it
  const userWithTimestamp = { ...user, createdAt: new Date().toISOString() };
  localStorage.setItem(USER_KEY, JSON.stringify(userWithTimestamp));
  _notifyAuthChange();
}

export function clearAuth(): void {
  if (typeof window === "undefined") return;
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(REFRESH_KEY);
  localStorage.removeItem(USER_KEY);
  _notifyAuthChange();
}

/**
 * Decode JWT to get expiration time
 * Note: This is a minimal decode, not validation (validation is server-side)
 */
function decodeJwt(token: string): { exp: number } | null {
  if (typeof window === "undefined") return null;
  try {
    const base64Url = token.split(".")[1];
    if (!base64Url) return null;
    let base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/");
    while (base64.length % 4 !== 0) {
      base64 += "=";
    }
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split("")
        .map((c) => "%" + ("00" + c.charCodeAt(0).toString(16)).slice(-2))
        .join("")
    );
    return JSON.parse(jsonPayload);
  } catch {
    return null;
  }
}

/**
 * Check if the current access token is expiring soon (within 1 minute)
 */
export function isTokenExpiringSoon(): boolean {
  const token = getToken();
  if (!token) return true;

  const decoded = decodeJwt(token);
  if (!decoded || !decoded.exp) return true;

  const currentTime = Math.floor(Date.now() / 1000);
  const timeUntilExpiry = decoded.exp - currentTime;

  // Return true if expires within 60 seconds
  return timeUntilExpiry < 60;
}

/**
 * Get token expiration timestamp
 */
export function getTokenExpiry(): Date | null {
  const token = getToken();
  if (!token) return null;

  const decoded = decodeJwt(token);
  if (!decoded || !decoded.exp) return null;

  return new Date(decoded.exp * 1000);
}

let refreshPromise: Promise<boolean> | null = null;

/**
 * Refresh the access token using the stored refresh token.
 * Notifies auth change listeners on success or failure.
 */
export async function refreshAccessToken(): Promise<boolean> {
  if (typeof window === "undefined") return false;

  if (refreshPromise) {
    return refreshPromise;
  }

  refreshPromise = (async () => {
    const refreshTokenValue = getRefreshToken();
    if (!refreshTokenValue) {
      clearAuth();
      return false;
    }

    try {
      const response = await refreshToken(refreshTokenValue);

      // Update stored tokens
      setToken(response.accessToken);
      setRefreshToken(response.refreshToken);

      // Update user if provided
      if (response.user) {
        setStoredUser(response.user);
      }

      return true;
    } catch {
      // Refresh failed, clear auth
      clearAuth();
      return false;
    } finally {
      refreshPromise = null;
    }
  })();

  return refreshPromise;
}
