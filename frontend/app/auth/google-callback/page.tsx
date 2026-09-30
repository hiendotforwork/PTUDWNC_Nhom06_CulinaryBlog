"use client";

import { Suspense, useEffect, useRef } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { useApp } from "../../context/AppContext";
import { Loader2 } from "lucide-react";

function GoogleCallbackContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { loginWithExternalToken, showToast } = useApp();
  const hasProcessed = useRef(false);

  useEffect(() => {
    if (hasProcessed.current) return;
    hasProcessed.current = true;

    const processCallback = () => {
      // Extract tokens and user info from URL hash fragment (secure) or query params
      const hash = typeof window !== "undefined" && window.location.hash.startsWith("#")
        ? window.location.hash.substring(1)
        : "";
      const hashParams = new URLSearchParams(hash);

      // Check for error from backend
      const error = hashParams.get("error") || searchParams.get("error");
      if (error) {
        showToast("error", `Đăng nhập Google thất bại: ${error}`);
        router.push("/login");
        return;
      }

      // Extract tokens and user info from hash or searchParams
      const accessToken = hashParams.get("accessToken") || searchParams.get("accessToken");
      const refreshToken = hashParams.get("refreshToken") || searchParams.get("refreshToken");
      const expiresAt = hashParams.get("expiresAt") || searchParams.get("expiresAt");
      const userId = hashParams.get("userId") || searchParams.get("userId");
      const displayName = hashParams.get("displayName") || searchParams.get("displayName");
      const email = hashParams.get("email") || searchParams.get("email");
      const avatarUrl = hashParams.get("avatarUrl") || searchParams.get("avatarUrl");
      const roles = hashParams.get("roles") || searchParams.get("roles");
      const returnUrl = hashParams.get("returnUrl") || searchParams.get("returnUrl");

      if (!accessToken || !refreshToken || !userId) {
        showToast("error", "Đăng nhập Google thất bại: Thiếu thông tin xác thực.");
        router.push("/login");
        return;
      }

      // Build user object
      const user = {
        id: userId,
        displayName: displayName || "User",
        userName: email?.split("@")[0] || "user",
        email: email || "",
        avatarUrl: avatarUrl || undefined,
        bio: undefined,
        roles: roles ? roles.split(",") : ["Author"],
      };

      // Login with tokens
      loginWithExternalToken(accessToken, refreshToken, expiresAt || "", user);

      // Redirect to home/returnUrl
      showToast("success", `Chào mừng ${user.displayName}! Đăng nhập thành công.`);
      const destination = returnUrl && returnUrl !== "/dashboard" ? returnUrl : "/";
      router.push(destination);
    };

    processCallback();
  }, [searchParams, router, showToast, loginWithExternalToken]);

  return (
    <div className="min-h-[80dvh] flex items-center justify-center">
      <div className="flex flex-col items-center gap-4">
        <Loader2 className="w-8 h-8 animate-spin text-[#C98F7D]" />
        <p className="text-[#8A817C]">Đang xử lý đăng nhập Google...</p>
      </div>
    </div>
  );
}

export default function GoogleCallbackPage() {
  return (
    <Suspense
      fallback={
        <div className="min-h-[80dvh] flex items-center justify-center">
          <div className="flex flex-col items-center gap-4">
            <Loader2 className="w-8 h-8 animate-spin text-[#C98F7D]" />
            <p className="text-[#8A817C]">Đang xử lý đăng nhập Google...</p>
          </div>
        </div>
      }
    >
      <GoogleCallbackContent />
    </Suspense>
  );
}
