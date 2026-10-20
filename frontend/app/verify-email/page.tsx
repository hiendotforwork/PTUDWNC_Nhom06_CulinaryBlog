"use client";

import React, { useCallback, useEffect, useRef, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { ChefHat, CheckCircle2, AlertCircle, Mail, ArrowRight, RefreshCw } from "lucide-react";
import { useApp } from "../context/AppContext";
import { Button } from "../components/ui/Button";
import { Input } from "../components/ui/Input";
import { confirmEmail, getEmailConfirmationStatus, resendEmailConfirmation } from "../lib/api";
import { getSafeReturnUrl } from "../lib/safeReturnUrl";

type VerifyState =
  | { type: "loading" }
  | { type: "ready"; userId: string; token: string }
  | { type: "success"; displayName: string }
  | { type: "already-confirmed" }
  | { type: "invalid-or-expired" }
  | { type: "network-error"; userId: string; token: string; action: "check" | "confirm" };

export default function VerifyEmailPage() {
  const router = useRouter();
  const { loginWithExternalToken, showToast } = useApp();

  const [state, setState] = useState<VerifyState>({ type: "loading" });
  const hasReadConfirmationLink = useRef(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [resendEmail, setResendEmail] = useState("");
  const [isResending, setIsResending] = useState(false);
  const [resendCooldown, setResendCooldown] = useState(0);

  const checkStatus = useCallback(async (userId: string, token: string) => {
    setState({ type: "loading" });
    try {
      const { confirmed } = await getEmailConfirmationStatus(userId, token);
      setState(confirmed ? { type: "already-confirmed" } : { type: "ready", userId, token });
    } catch (err: unknown) {
      const error = err as { statusCode?: number; errorCode?: string };
      if (error.statusCode === 400 || error.errorCode === "EMAIL_CONFIRMATION_FAILED") {
        setState({ type: "invalid-or-expired" });
      } else {
        setState({ type: "network-error", userId, token, action: "check" });
      }
    }
  }, []);

  useEffect(() => {
    // 1. Read userId and token from location.hash (#userId=...&token=...)
    if (typeof window === "undefined") return;
    // Strict Mode replays effects in development after the first pass strips the fragment.
    if (hasReadConfirmationLink.current) return;
    hasReadConfirmationLink.current = true;

    const hash = window.location.hash.startsWith("#")
      ? window.location.hash.slice(1)
      : window.location.hash;

    const searchParams = new URLSearchParams(hash);
    const userId = searchParams.get("userId");
    const token = searchParams.get("token");

    // 2. Immediately strip sensitive token fragment from browser address bar and history
    window.history.replaceState(null, "", `${window.location.pathname}${window.location.search}`);

    if (userId && token) {
      queueMicrotask(() => void checkStatus(userId, token));
    } else {
      queueMicrotask(() => setState({ type: "invalid-or-expired" }));
    }
  }, [checkStatus]);

  const handleConfirm = async () => {
    if (state.type !== "ready" && (state.type !== "network-error" || state.action !== "confirm")) return;
    const { userId, token } = state;

    setIsSubmitting(true);
    try {
      const response = await confirmEmail(userId, token);

      // Store tokens and set current user
      loginWithExternalToken(
        response.accessToken,
        response.refreshToken,
        response.expiresAt,
        response.user
      );

      setState({ type: "success", displayName: response.user.displayName });

      // Redirect to safe return URL or home after brief delay
      const safeReturnUrl = getSafeReturnUrl(new URLSearchParams(window.location.search).get("returnUrl"), window.location.origin);
      setTimeout(() => {
        router.push(safeReturnUrl);
      }, 1500);
    } catch (err: unknown) {
      const error = err as { statusCode?: number; errorCode?: string };
      if (error.statusCode === 409 || error.errorCode === "EMAIL_ALREADY_CONFIRMED") {
        setState({ type: "already-confirmed" });
      } else if (error.statusCode === 400 || error.errorCode === "EMAIL_CONFIRMATION_FAILED") {
        setState({ type: "invalid-or-expired" });
      } else {
        setState({ type: "network-error", userId, token, action: "confirm" });
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleResend = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!resendEmail.trim() || resendCooldown > 0) return;

    setIsResending(true);
    try {
      await resendEmailConfirmation(resendEmail.trim());
      showToast("success", "Email xác nhận mới đã được gửi nếu tài khoản tồn tại!");
      setResendCooldown(60);
      const timer = setInterval(() => {
        setResendCooldown((prev) => {
          if (prev <= 1) {
            clearInterval(timer);
            return 0;
          }
          return prev - 1;
        });
      }, 1000);
    } catch {
      showToast("error", "Không thể gửi email xác nhận. Vui lòng thử lại sau.");
    } finally {
      setIsResending(false);
    }
  };

  return (
    <div className="min-h-[85dvh] flex items-center justify-center px-4 py-12 sm:px-6">
      <div className="w-full max-w-md double-bezel">
        <div className="double-bezel-inner p-8 sm:p-10 flex flex-col gap-6 text-center items-center">
          {/* Header Icon */}
          <Link href="/" className="w-14 h-14 rounded-2xl bg-[#E0AFA0] text-[#60413A] flex items-center justify-center shadow-inner mb-1">
            <ChefHat className="w-7 h-7" />
          </Link>

          {/* 1. Loading State */}
          {state.type === "loading" && (
            <div className="flex flex-col items-center gap-3 py-6">
              <RefreshCw className="w-8 h-8 text-[#C98F7D] animate-spin" />
              <p className="text-sm text-[#8A817C] dark:text-[#A8A29E]">Đang kiểm tra liên kết xác thực...</p>
            </div>
          )}

          {/* 2. Ready State (Explicit POST Button) */}
          {state.type === "ready" && (
            <div className="flex flex-col items-center gap-4 w-full">
              <div className="flex flex-col gap-2">
                <h1 className="font-serif text-2xl sm:text-3xl font-bold text-[#463F3A] dark:text-[#F5F3EF]">
                  Kích hoạt tài khoản
                </h1>
                <p className="text-xs sm:text-sm text-[#8A817C] dark:text-[#A8A29E] leading-relaxed">
                  Cảm ơn bạn đã đăng ký với Culinary Blog. Nhấn vào nút bên dưới để hoàn tất việc xác thực email và kích hoạt tài khoản của bạn.
                </p>
              </div>

              <Button
                type="button"
                variant="primary"
                size="lg"
                isLoading={isSubmitting}
                onClick={handleConfirm}
                className="w-full mt-2"
                rightIcon={<ArrowRight className="w-4 h-4" />}
              >
                Xác nhận và Đăng nhập
              </Button>
            </div>
          )}

          {/* 3. Success State */}
          {state.type === "success" && (
            <div className="flex flex-col items-center gap-4 py-4 w-full">
              <div className="w-12 h-12 rounded-full bg-emerald-100 dark:bg-emerald-950/40 text-emerald-600 dark:text-emerald-400 flex items-center justify-center">
                <CheckCircle2 className="w-7 h-7" />
              </div>
              <div className="flex flex-col gap-1">
                <h2 className="font-serif text-xl sm:text-2xl font-bold text-[#463F3A] dark:text-[#F5F3EF]">
                  Xác nhận thành công!
                </h2>
                <p className="text-xs sm:text-sm text-[#8A817C] dark:text-[#A8A29E]">
                  Chào mừng <strong>{state.displayName}</strong>. Đang chuyển hướng bạn đến trang chủ...
                </p>
              </div>
            </div>
          )}

          {/* 4. Already Confirmed State */}
          {state.type === "already-confirmed" && (
            <div className="flex flex-col items-center gap-4 py-2 w-full">
              <div className="w-12 h-12 rounded-full bg-blue-100 dark:bg-blue-950/40 text-blue-600 dark:text-blue-400 flex items-center justify-center">
                <CheckCircle2 className="w-7 h-7" />
              </div>
              <div className="flex flex-col gap-1">
                <h2 className="font-serif text-xl sm:text-2xl font-bold text-[#463F3A] dark:text-[#F5F3EF]">
                  Email đã được xác nhận
                </h2>
                <p className="text-xs sm:text-sm text-[#8A817C] dark:text-[#A8A29E]">
                  Tài khoản của bạn đã được kích hoạt trước đó. Bạn có thể đăng nhập ngay bây giờ.
                </p>
              </div>

              <Link href="/login" className="w-full mt-2">
                <Button variant="primary" size="md" className="w-full">
                  Đăng nhập ngay
                </Button>
              </Link>
            </div>
          )}

          {/* 5. Invalid or Expired State */}
          {state.type === "invalid-or-expired" && (
            <div className="flex flex-col items-center gap-4 py-2 w-full">
              <div className="w-12 h-12 rounded-full bg-amber-100 dark:bg-amber-950/40 text-amber-600 dark:text-amber-400 flex items-center justify-center">
                <AlertCircle className="w-7 h-7" />
              </div>
              <div className="flex flex-col gap-1">
                <h2 className="font-serif text-xl sm:text-2xl font-bold text-[#463F3A] dark:text-[#F5F3EF]">
                  Liên kết không hợp lệ hoặc đã hết hạn
                </h2>
                <p className="text-xs sm:text-sm text-[#8A817C] dark:text-[#A8A29E]">
                  Liên kết xác nhận chỉ có hiệu lực trong 24 giờ. Vui lòng nhập email bên dưới để nhận liên kết xác nhận mới.
                </p>
              </div>

              <form onSubmit={handleResend} className="flex flex-col gap-3 w-full mt-2">
                <Input
                  type="email"
                  placeholder="example@culinaryblog.vn"
                  required
                  value={resendEmail}
                  onChange={(e) => setResendEmail(e.target.value)}
                  leftIcon={<Mail className="w-4 h-4" />}
                />
                <Button
                  type="submit"
                  variant="primary"
                  size="md"
                  isLoading={isResending}
                  disabled={isResending || resendCooldown > 0}
                  className="w-full"
                >
                  {resendCooldown > 0 ? `Gửi lại sau ${resendCooldown}s` : "Gửi lại email xác nhận"}
                </Button>
              </form>

              <Link href="/login" className="text-xs text-[#C98F7D] hover:underline mt-1">
                Quay lại đăng nhập
              </Link>
            </div>
          )}

          {/* 6. Network Error State */}
          {state.type === "network-error" && (
            <div className="flex flex-col items-center gap-4 py-2 w-full">
              <div className="w-12 h-12 rounded-full bg-rose-100 dark:bg-rose-950/40 text-rose-600 dark:text-rose-400 flex items-center justify-center">
                <AlertCircle className="w-7 h-7" />
              </div>
              <div className="flex flex-col gap-1">
                <h2 className="font-serif text-xl sm:text-2xl font-bold text-[#463F3A] dark:text-[#F5F3EF]">
                  Lỗi kết nối
                </h2>
                <p className="text-xs sm:text-sm text-[#8A817C] dark:text-[#A8A29E]">
                  Không thể kết nối đến máy chủ. Vui lòng kiểm tra lại đường truyền mạng.
                </p>
              </div>

              <Button
                type="button"
                variant="secondary"
                size="md"
                onClick={() => {
                  if (state.action === "check") {
                    void checkStatus(state.userId, state.token);
                  } else {
                    void handleConfirm();
                  }
                }}
                isLoading={isSubmitting}
                className="w-full mt-2"
              >
                Thử lại
              </Button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
