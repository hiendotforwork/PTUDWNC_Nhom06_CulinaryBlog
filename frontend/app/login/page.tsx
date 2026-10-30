"use client";

import React, { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { ChefHat, Lock, Mail, ArrowRight } from "lucide-react";
import { useApp } from "../context/AppContext";
import { Input } from "../components/ui/Input";
import { Button } from "../components/ui/Button";
import { resendEmailConfirmation } from "../lib/api";

export default function LoginPage() {
  const router = useRouter();
  const { login, showToast } = useApp();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [unconfirmedEmail, setUnconfirmedEmail] = useState<string | null>(null);
  const [isResending, setIsResending] = useState(false);
  const [resendCooldown, setResendCooldown] = useState(0);

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
    setUnconfirmedEmail(null);
    setIsLoading(true);

    try {
      const success = await login(email, password);
      if (success) {
        router.push("/");
      }
    } catch (err: unknown) {
      if ((err as { errorCode?: string }).errorCode === "EMAIL_NOT_CONFIRMED") {
        setUnconfirmedEmail(email);
      }
    } finally {
      setIsLoading(false);
    }
  };

  const handleResendConfirmation = async () => {
    if (!unconfirmedEmail || resendCooldown > 0) return;
    setIsResending(true);
    try {
      await resendEmailConfirmation(unconfirmedEmail);
      showToast("success", "Email xác nhận mới đã được gửi!");
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
      showToast("error", "Không thể gửi lại email xác nhận. Vui lòng thử lại sau.");
    } finally {
      setIsResending(false);
    }
  };

  const handleGoogleLogin = () => {
    const apiBase = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5058";
    const returnUrl = encodeURIComponent("/");
    window.location.href = `${apiBase}/api/v1/auth/google-signin?returnUrl=${returnUrl}`;
  };

  return (
    <div className="min-h-[80dvh] flex items-center justify-center px-4 py-12 sm:px-6">
      <div className="w-full max-w-md double-bezel">
        <div className="double-bezel-inner p-8 sm:p-10 flex flex-col gap-6">
          {/* Header */}
          <div className="flex flex-col items-center text-center gap-2">
            <Link href="/" className="w-12 h-12 rounded-2xl bg-[#E0AFA0] text-[#60413A] flex items-center justify-center shadow-inner mb-2">
              <ChefHat className="w-7 h-7" />
            </Link>
            <h1 className="font-serif text-2xl sm:text-3xl font-bold text-[#463F3A] dark:text-[#F5F3EF]">
              Đăng nhập tài khoản
            </h1>
            <p className="text-xs sm:text-sm text-[#8A817C] dark:text-[#A8A29E]">
              Chào mừng bạn trở lại với căn bếp Culinary Blog!
            </p>
          </div>

          {/* Form */}
          <form onSubmit={handleLogin} className="flex flex-col gap-4">
            {unconfirmedEmail && (
              <div className="p-4 rounded-xl bg-amber-50 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-800 text-left flex flex-col gap-2">
                <p className="text-xs sm:text-sm text-amber-800 dark:text-amber-200 font-medium">
                  Tài khoản <strong>{unconfirmedEmail}</strong> chưa được xác nhận email. Vui lòng kiểm tra hộp thư hoặc nhấn nút bên dưới để nhận liên kết mới.
                </p>
                <Button
                  type="button"
                  variant="secondary"
                  size="sm"
                  isLoading={isResending}
                  disabled={isResending || resendCooldown > 0}
                  onClick={handleResendConfirmation}
                  className="self-start text-xs"
                >
                  {resendCooldown > 0 ? `Gửi lại sau ${resendCooldown}s` : "Gửi lại email xác nhận"}
                </Button>
              </div>
            )}

            <Input
              label="Địa chỉ Email"
              type="email"
              placeholder="example@culinaryblog.vn"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              error={errors.email}
              leftIcon={<Mail className="w-4 h-4" />}
            />

            <Input
              label="Mật khẩu"
              type="password"
              placeholder="Nhập mật khẩu..."
              required
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              error={errors.password}
              leftIcon={<Lock className="w-4 h-4" />}
            />

            <div className="flex items-center justify-end">
              <Link
                href="/forgot-password"
                className="text-xs text-[#C98F7D] hover:underline cursor-pointer"
              >
                Quên mật khẩu?
              </Link>
            </div>

            <Button
              type="submit"
              variant="primary"
              size="lg"
              isLoading={isLoading}
              className="w-full mt-2"
              rightIcon={<ArrowRight className="w-4 h-4" />}
            >
              Đăng nhập
            </Button>
          </form>

          {/* Divider */}
          <div className="flex items-center gap-3">
            <div className="flex-1 h-px bg-[#DCD8D2] dark:bg-[#3D3934]" />
            <span className="text-xs text-[#8A817C] uppercase tracking-wider">Hoặc</span>
            <div className="flex-1 h-px bg-[#DCD8D2] dark:bg-[#3D3934]" />
          </div>

          {/* Social Google Login Button */}
          <Button
            type="button"
            variant="secondary"
            size="md"
            onClick={handleGoogleLogin}
            className="w-full flex items-center justify-center gap-3"
          >
            <span>Đăng nhập bằng Google</span>
          </Button>

          {/* Register Link */}
          <div className="text-center text-xs text-[#8A817C] pt-2 border-t border-[#DCD8D2]/60 dark:border-[#3D3934]">
            Chưa có tài khoản?{" "}
            <Link href="/register" className="font-bold text-[#C98F7D] hover:underline">
              Đăng ký ngay
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}
