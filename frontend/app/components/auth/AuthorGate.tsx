"use client";

import React, { useEffect, useState } from "react";
import Link from "next/link";
import { getCurrentUser } from "../../lib/api";
import { Button } from "../ui/Button";

export function AuthorGate({ children }: { children: React.ReactNode }) {
  const [access, setAccess] = useState<"checking" | "allowed" | "denied" | "error">("checking");
  const [retryCount, setRetryCount] = useState(0);

  useEffect(() => {
    let active = true;
    getCurrentUser()
      .then((user) => {
        if (active) setAccess(user.roles.some((role) => role === "Author" || role === "Admin") ? "allowed" : "denied");
      })
      .catch((error: unknown) => {
        if (!active) return;
        const status = (error as { statusCode?: number }).statusCode;
        setAccess(status === 401 || status === 403 ? "denied" : "error");
      });
    return () => { active = false; };
  }, [retryCount]);

  if (access === "allowed") return <>{children}</>;

  return (
    <div className="double-bezel max-w-md mx-auto mt-12">
      <div className="double-bezel-inner p-8 flex flex-col items-center gap-4 text-center">
        <h1 className="font-serif text-xl font-bold text-[#463F3A] dark:text-[#F5F3EF]">
          {access === "checking" ? "Đang kiểm tra quyền truy cập..." : access === "error" ? "Không thể kiểm tra phiên đăng nhập" : "Vui lòng đăng nhập"}
        </h1>
        {access !== "checking" && (
          <>
            <p className="text-sm text-[#8A817C] dark:text-[#A8A29E]">
              {access === "error" ? "Không thể kết nối đến máy chủ. Vui lòng thử lại." : "Bạn cần xác nhận email và đăng nhập để tạo hoặc sửa công thức."}
            </p>
            {access === "error" ? (
              <Button variant="primary" onClick={() => { setAccess("checking"); setRetryCount((count) => count + 1); }}>Thử lại</Button>
            ) : (
              <Link href="/login"><Button variant="primary">Đăng nhập</Button></Link>
            )}
          </>
        )}
      </div>
    </div>
  );
}
