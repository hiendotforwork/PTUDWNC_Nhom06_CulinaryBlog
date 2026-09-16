"use client";

import React from "react";
import Link from "next/link";
import { ArrowLeft, Search, UtensilsCrossed } from "lucide-react";
import { Button } from "./components/ui/Button";

export default function NotFound() {
  return (
    <div className="min-h-[70dvh] flex items-center justify-center px-4 py-16">
      <div className="double-bezel max-w-md w-full text-center">
        <div className="double-bezel-inner p-8 sm:p-12 flex flex-col items-center gap-4">
          <div className="w-20 h-20 rounded-full bg-[#FAF9F6] dark:bg-[#2A2723] border border-[#DCD8D2] dark:border-[#3D3934] flex items-center justify-center text-[#C98F7D] shadow-inner mb-2">
            <UtensilsCrossed className="w-10 h-10" />
          </div>

          <span className="text-xs font-bold text-[#C98F7D] uppercase tracking-widest">
            Mã lỗi 404
          </span>

          <h1 className="font-serif text-2xl sm:text-3xl font-bold text-[#463F3A] dark:text-[#F5F3EF]">
            Trang không tìm thấy
          </h1>

          <p className="text-xs sm:text-sm text-[#8A817C] dark:text-[#A8A29E] leading-relaxed max-w-xs">
            Có vẻ như công thức hoặc trang bạn đang tìm kiếm không tồn tại hoặc đã được chuyển sang địa chỉ khác.
          </p>

          <div className="flex flex-col sm:flex-row items-center gap-3 mt-4 w-full justify-center">
            <Link href="/" className="w-full sm:w-auto">
              <Button variant="primary" size="md" leftIcon={<ArrowLeft className="w-4 h-4" />}>
                Về trang chủ
              </Button>
            </Link>
            <Link href="/search" className="w-full sm:w-auto">
              <Button variant="secondary" size="md" leftIcon={<Search className="w-4 h-4" />}>
                Tìm công thức
              </Button>
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}
