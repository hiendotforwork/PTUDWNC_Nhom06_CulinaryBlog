"use client";

import React, { useEffect, useMemo, useState, useSyncExternalStore } from "react";
import Link from "next/link";
import { Lock } from "lucide-react";
import { Input } from "../components/ui/Input";
import { Button } from "../components/ui/Button";
import { resetPassword } from "../lib/api";

const subscribeToHash = (callback: () => void) => {
  window.addEventListener("hashchange", callback);
  return () => window.removeEventListener("hashchange", callback);
};

export default function ResetPasswordPage() {
  const hash = useSyncExternalStore(subscribeToHash, () => window.location.hash, () => "");
  const { userId, token } = useMemo(() => {
    const params = new URLSearchParams(hash.slice(1));
    return { userId: params.get("userId") || "", token: params.get("token") || "" };
  }, [hash]);
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [invalidToken, setInvalidToken] = useState(false);
  const [completed, setCompleted] = useState(false);
  const [error, setError] = useState("");
  const invalidLink = invalidToken || !userId || !token;

  useEffect(() => {
    if (hash) window.history.replaceState(null, "", `${window.location.pathname}${window.location.search}`);
  }, [hash]);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (newPassword !== confirmPassword) {
      setError("Mật khẩu xác nhận không khớp.");
      return;
    }
    setError("");
    setIsLoading(true);
    try {
      await resetPassword({ userId, token, newPassword });
      setCompleted(true);
    } catch (err) {
      const apiError = err as { errorCode?: string; message?: string };
      if (apiError.errorCode === "AUTH_PASSWORD_RESET_INVALID") setInvalidToken(true);
      else setError(apiError.message || "Không thể đặt lại mật khẩu. Vui lòng thử lại sau.");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-[80dvh] flex items-center justify-center px-4 py-12 sm:px-6">
      <div className="w-full max-w-md double-bezel">
        <div className="double-bezel-inner p-8 sm:p-10 flex flex-col gap-6">
          <div className="text-center flex flex-col gap-2">
            <h1 className="font-serif text-2xl font-bold text-[#463F3A] dark:text-[#F5F3EF]">Đặt lại mật khẩu</h1>
            {invalidLink ? (
              <p role="alert" className="text-sm text-[#8A817C]">Liên kết không hợp lệ hoặc đã hết hạn. Hãy yêu cầu một liên kết mới.</p>
            ) : completed ? (
              <p role="status" className="text-sm text-[#385938] dark:text-green-300">Mật khẩu đã được cập nhật. Bạn có thể đăng nhập bằng mật khẩu mới.</p>
            ) : (
              <p className="text-sm text-[#8A817C]">Tạo mật khẩu mới cho tài khoản của bạn.</p>
            )}
          </div>
          {!invalidLink && !completed && (
            <form onSubmit={handleSubmit} className="flex flex-col gap-4">
              <Input
                label="Mật khẩu mới"
                type="password"
                required
                minLength={8}
                value={newPassword}
                onChange={(event) => setNewPassword(event.target.value)}
                leftIcon={<Lock className="w-4 h-4" />}
              />
              <Input
                label="Xác nhận mật khẩu mới"
                type="password"
                required
                minLength={8}
                value={confirmPassword}
                onChange={(event) => setConfirmPassword(event.target.value)}
                leftIcon={<Lock className="w-4 h-4" />}
              />
              {error && <p role="alert" className="text-sm text-[#B85C5C]">{error}</p>}
              <Button type="submit" size="lg" isLoading={isLoading} className="w-full">Đặt lại mật khẩu</Button>
            </form>
          )}
          <Link href="/login" className="text-center text-sm text-[#C98F7D] hover:underline">Đến trang đăng nhập</Link>
        </div>
      </div>
    </div>
  );
}
