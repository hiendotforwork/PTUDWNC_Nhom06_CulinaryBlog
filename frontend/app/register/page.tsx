"use client";

import React, { useState } from "react";
import Link from "next/link";
import { ChefHat, User, Mail, AtSign, Lock, ArrowRight } from "lucide-react";
import { useApp } from "../context/AppContext";
import { Input } from "../components/ui/Input";
import { Button } from "../components/ui/Button";
import { resendEmailConfirmation } from "../lib/api";

export default function RegisterPage() {
  const { register, showToast } = useApp();

  const [displayName, setDisplayName] = useState("");
  const [email, setEmail] = useState("");
  const [userName, setUserName] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [registeredEmail, setRegisteredEmail] = useState<string | null>(null);
  const [isResending, setIsResending] = useState(false);
  const [resendCooldown, setResendCooldown] = useState(0);

  const handleRegister = async (e: React.FormEvent) => {
    e.preventDefault();
    const newErrors: Record<string, string> = {};

    // Client-side validation (mirrors backend)
    if (!displayName.trim() || displayName.length < 2) {
      newErrors.displayName = "Tên hiển thị phải có ít nhất 2 ký tự.";
    } else if (displayName.length > 100) {
      newErrors.displayName = "Tên hiển thị không quá 100 ký tự.";
    }

    if (!email.trim() || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
      newErrors.email = "Email không hợp lệ.";
    }

    if (!userName.trim() || userName.length < 3) {
      newErrors.userName = "Tên đăng nhập phải từ 3 ký tự trở lên.";
    } else if (userName.length > 30) {
      newErrors.userName = "Tên đăng nhập không quá 30 ký tự.";
    } else if (!/^[a-zA-Z0-9_]+$/.test(userName)) {
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
      setRegisteredEmail(email);
    } catch (err: unknown) {
      const error = err as { fieldErrors?: Record<string, string>; field?: string; message?: string };
      if (error.fieldErrors && Object.keys(error.fieldErrors).length > 0) {
        setErrors(error.fieldErrors);
      } else if (error.field && error.message) {
        setErrors({ [error.field]: error.message });
      }
    } finally {
      setIsLoading(false);
    }
  };

  const handleResend = async () => {
    if (!registeredEmail || resendCooldown > 0) return;
    setIsResending(true);
    try {
      await resendEmailConfirmation(registeredEmail);
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

  const handleGoogleSignup = () => {
    const apiBase = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5058";
    const returnUrl = encodeURIComponent("/");
    window.location.href = `${apiBase}/api/v1/auth/google-signin?returnUrl=${returnUrl}`;
  };

  if (registeredEmail) {
    return (
      <div className="min-h-[85dvh] flex items-center justify-center px-4 py-12 sm:px-6">
        <div className="w-full max-w-md double-bezel">
          <div className="double-bezel-inner p-8 sm:p-10 flex flex-col gap-6 text-center items-center">
            <div className="w-14 h-14 rounded-2xl bg-[#E0AFA0] text-[#60413A] flex items-center justify-center shadow-inner">
              <Mail className="w-7 h-7" />
            </div>

            <div className="flex flex-col gap-2">
              <h1 className="font-serif text-2xl sm:text-3xl font-bold text-[#463F3A] dark:text-[#F5F3EF]">
                Kiểm tra email của bạn
              </h1>
              <p className="text-xs sm:text-sm text-[#8A817C] dark:text-[#A8A29E] leading-relaxed">
                Chúng tôi đã gửi liên kết kích hoạt tài khoản đến địa chỉ:
              </p>
              <p className="font-semibold text-sm sm:text-base text-[#463F3A] dark:text-[#F5F3EF] bg-[#F5F3EF] dark:bg-[#2A2624] py-2 px-3 rounded-xl border border-[#DCD8D2] dark:border-[#3D3934]">
                {registeredEmail}
              </p>
              <p className="text-xs text-[#8A817C] dark:text-[#A8A29E] mt-1">
                Vui lòng mở email và nhấn vào liên kết xác nhận để kích hoạt tài khoản. Liên kết có hiệu lực trong 24 giờ.
              </p>
            </div>

            <div className="flex flex-col gap-3 w-full">
              <Button
                type="button"
                variant="secondary"
                size="md"
                isLoading={isResending}
                disabled={isResending || resendCooldown > 0}
                onClick={handleResend}
                className="w-full"
              >
                {resendCooldown > 0
                  ? `Gửi lại sau ${resendCooldown}s`
                  : "Gửi lại email xác nhận"}
              </Button>

              <Link href="/login" className="w-full">
                <Button variant="primary" size="md" className="w-full">
                  Quay lại đăng nhập
                </Button>
              </Link>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-[85dvh] flex items-center justify-center px-4 py-12 sm:px-6">
      <div className="w-full max-w-md double-bezel">
        <div className="double-bezel-inner p-8 sm:p-10 flex flex-col gap-6">
          {/* Header */}
          <div className="flex flex-col items-center text-center gap-2">
            <Link href="/" className="w-12 h-12 rounded-2xl bg-[#E0AFA0] text-[#60413A] flex items-center justify-center shadow-inner mb-2">
              <ChefHat className="w-7 h-7" />
            </Link>
            <h1 className="font-serif text-2xl sm:text-3xl font-bold text-[#463F3A] dark:text-[#F5F3EF]">
              Đăng ký tài khoản
            </h1>
            <p className="text-xs sm:text-sm text-[#8A817C] dark:text-[#A8A29E]">
              Gia nhập cộng đồng Culinary Blog và chia sẻ công thức của riêng bạn.
            </p>
          </div>

          {/* Form */}
          <form onSubmit={handleRegister} className="flex flex-col gap-4">
            <Input
              label="Tên hiển thị (Display Name)"
              placeholder="VD: Hoàng Linh, Mai Phương..."
              required
              value={displayName}
              onChange={(e) => setDisplayName(e.target.value)}
              error={errors.displayName}
              leftIcon={<User className="w-4 h-4" />}
            />

            <Input
              label="Địa chỉ Email"
              type="email"
              placeholder="email@culinaryblog.vn"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              error={errors.email}
              leftIcon={<Mail className="w-4 h-4" />}
            />

            <Input
              label="Tên đăng nhập (Username)"
              placeholder="VD: hoanglinh_chef"
              required
              value={userName}
              onChange={(e) => setUserName(e.target.value)}
              error={errors.userName}
              leftIcon={<AtSign className="w-4 h-4" />}
            />

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

            <Input
              label="Xác nhận mật khẩu"
              type="password"
              placeholder="Nhập lại mật khẩu..."
              required
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              error={errors.confirmPassword}
              leftIcon={<Lock className="w-4 h-4" />}
            />

            <Button
              type="submit"
              variant="primary"
              size="lg"
              isLoading={isLoading}
              className="w-full mt-2"
              rightIcon={<ArrowRight className="w-4 h-4" />}
            >
              Đăng ký ngay
            </Button>
          </form>

          {/* Divider */}
          <div className="flex items-center gap-3">
            <div className="flex-1 h-px bg-[#DCD8D2] dark:bg-[#3D3934]" />
            <span className="text-xs text-[#8A817C] uppercase tracking-wider">Hoặc</span>
            <div className="flex-1 h-px bg-[#DCD8D2] dark:bg-[#3D3934]" />
          </div>

          {/* Google Signup */}
          <Button
            type="button"
            variant="secondary"
            size="md"
            onClick={handleGoogleSignup}
            className="w-full flex items-center justify-center gap-3"
          >
            <span>Đăng ký bằng Google</span>
          </Button>

          {/* Login Link */}
          <div className="text-center text-xs text-[#8A817C] pt-2 border-t border-[#DCD8D2]/60 dark:border-[#3D3934]">
            Đã có tài khoản?{" "}
            <Link href="/login" className="font-bold text-[#C98F7D] hover:underline">
              Đăng nhập ngay
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}
