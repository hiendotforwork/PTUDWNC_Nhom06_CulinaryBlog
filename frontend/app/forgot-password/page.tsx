"use client";

import React, { useState } from "react";
import Link from "next/link";
import { ArrowLeft, Mail } from "lucide-react";
import { Input } from "../components/ui/Input";
import { Button } from "../components/ui/Button";
import { requestPasswordReset } from "../lib/api";

export default function ForgotPasswordPage() {
  const [email, setEmail] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [accepted, setAccepted] = useState(false);
  const [error, setError] = useState("");

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setIsLoading(true);
    setError("");
    try {
      await requestPasswordReset(email);
      setAccepted(true);
    } catch (err) {
      setError((err as { message?: string }).message || "Không thể gửi yêu cầu. Vui lòng thử lại sau.");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-[80dvh] flex items-center justify-center px-4 py-12 sm:px-6">
      <div className="w-full max-w-md double-bezel">
        <div className="double-bezel-inner p-8 sm:p-10 flex flex-col gap-6">
          <div className="text-center flex flex-col gap-2">
            <h1 className="font-serif text-2xl font-bold text-[#463F3A] dark:text-[#F5F3EF]">Quên mật khẩu</h1>
            <p className="text-sm text-[#8A817C]">Nhập email đăng ký để nhận hướng dẫn đặt lại mật khẩu.</p>
          </div>
          {accepted ? (
            <p role="status" className="text-sm text-center text-[#385938] dark:text-green-300">
              Nếu email này thuộc tài khoản hợp lệ, hướng dẫn đặt lại mật khẩu sẽ được gửi đến hộp thư của bạn.
            </p>
          ) : (
            <form onSubmit={handleSubmit} className="flex flex-col gap-4">
              <Input
                label="Địa chỉ Email"
                type="email"
                required
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                leftIcon={<Mail className="w-4 h-4" />}
              />
              {error && <p role="alert" className="text-sm text-[#B85C5C]">{error}</p>}
              <Button type="submit" size="lg" isLoading={isLoading} className="w-full">
                Gửi hướng dẫn
              </Button>
            </form>
          )}
          <Link href="/login" className="inline-flex justify-center items-center gap-2 text-sm text-[#C98F7D] hover:underline">
            <ArrowLeft className="w-4 h-4" /> Quay lại đăng nhập
          </Link>
        </div>
      </div>
    </div>
  );
}
