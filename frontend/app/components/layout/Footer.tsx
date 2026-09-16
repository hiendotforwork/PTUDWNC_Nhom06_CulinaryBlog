"use client";

import React, { useState } from "react";
import Link from "next/link";
import { ChefHat, Send, Heart } from "lucide-react";
import { useApp } from "../../context/AppContext";

export const Footer: React.FC = () => {
  const { categories, showToast } = useApp();
  const [email, setEmail] = useState("");

  const handleNewsletterSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (email.trim()) {
      showToast("success", "Cảm ơn bạn đã đăng ký! Bản tin công thức sẽ được gửi đến email.");
      setEmail("");
    }
  };

  return (
    <footer className="bg-[#EFECE6] dark:bg-[#181614] border-t border-[#DCD8D2] dark:border-[#3D3934] mt-auto transition-colors">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12 sm:py-16">
        <div className="grid grid-cols-1 md:grid-cols-12 gap-10 lg:gap-12">
          {/* Brand Column */}
          <div className="md:col-span-4 flex flex-col gap-4">
            <Link href="/" className="flex items-center gap-3 group">
              <div className="w-10 h-10 rounded-2xl bg-[#E0AFA0] text-[#60413A] flex items-center justify-center shadow-inner">
                <ChefHat className="w-6 h-6" />
              </div>
              <div className="flex flex-col">
                <span className="font-serif text-xl font-bold tracking-tight text-[#463F3A] dark:text-[#F5F3EF]">
                  Culinary Blog
                </span>
                <span className="text-[11px] font-medium text-[#8A817C] uppercase tracking-wider">
                  Bếp Ẩm Thực & Nấu Ăn
                </span>
              </div>
            </Link>
            <p className="text-sm text-[#8A817C] dark:text-[#A8A29E] leading-relaxed max-w-sm">
              Không gian chia sẻ đam mê nấu nướng gia đình, gìn giữ tinh hoa ẩm thực truyền thống Việt và kết nối những người yêu bếp.
            </p>
            <div className="flex items-center gap-2 text-xs text-[#8A817C]">
              <span>Được thiết kế với tình yêu ẩm thực</span>
              <Heart className="w-3.5 h-3.5 text-[#B85C5C] fill-[#B85C5C]" />
            </div>
          </div>

          {/* Quick Categories */}
          <div className="md:col-span-3 flex flex-col gap-3">
            <h4 className="font-serif text-sm font-bold uppercase tracking-wider text-[#463F3A] dark:text-[#F5F3EF]">
              Danh mục công thức
            </h4>
            <ul className="flex flex-col gap-2 text-xs text-[#8A817C] dark:text-[#A8A29E]">
              {categories.slice(1, 6).map((cat) => (
                <li key={cat.id}>
                  <Link
                    href={`/categories/${cat.slug}`}
                    className="hover:text-[#C98F7D] transition-colors"
                  >
                    {cat.name}
                  </Link>
                </li>
              ))}
            </ul>
          </div>

          {/* About Links */}
          <div className="md:col-span-2 flex flex-col gap-3">
            <h4 className="font-serif text-sm font-bold uppercase tracking-wider text-[#463F3A] dark:text-[#F5F3EF]">
              Về chúng tôi
            </h4>
            <ul className="flex flex-col gap-2 text-xs text-[#8A817C] dark:text-[#A8A29E]">
              <li>
                <Link href="/" className="hover:text-[#C98F7D] transition-colors">
                  Giới thiệu
                </Link>
              </li>
              <li>
                <Link href="/recipes/create" className="hover:text-[#C98F7D] transition-colors">
                  Đóng góp công thức
                </Link>
              </li>
              <li>
                <Link href="/search" className="hover:text-[#C98F7D] transition-colors">
                  Tra cứu nguyên liệu
                </Link>
              </li>
            </ul>
          </div>

          {/* Newsletter Column */}
          <div className="md:col-span-3 flex flex-col gap-3">
            <h4 className="font-serif text-sm font-bold uppercase tracking-wider text-[#463F3A] dark:text-[#F5F3EF]">
              Bản tin Bếp Ấm
            </h4>
            <p className="text-xs text-[#8A817C] dark:text-[#A8A29E] leading-relaxed">
              Nhận công thức nấu ăn mới nhất, mẹo vặt nhà bếp vào mỗi sáng thứ Bảy.
            </p>
            <form onSubmit={handleNewsletterSubmit} className="flex flex-col gap-2 mt-1">
              <div className="relative">
                <input
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="Nhập email của bạn..."
                  required
                  className="w-full h-10 px-3.5 pr-10 text-xs rounded-xl bg-white dark:bg-[#24211E] border border-[#DCD8D2] dark:border-[#3D3934] focus:border-[#C98F7D] focus:outline-none"
                />
                <button
                  type="submit"
                  aria-label="Gửi đăng ký nhận bản tin"
                  className="absolute right-2 top-1/2 -translate-y-1/2 p-1.5 rounded-lg text-[#C98F7D] hover:bg-[#FDF8F6] transition-colors cursor-pointer"
                >
                  <Send className="w-3.5 h-3.5" />
                </button>
              </div>
            </form>
          </div>
        </div>

        {/* Bottom Bar */}
        <div className="mt-12 pt-6 border-t border-[#DCD8D2] dark:border-[#3D3934] flex flex-col sm:flex-row items-center justify-between gap-4 text-xs text-[#8A817C]">
          <p>© 2026 Culinary Blog. Nền tảng blog ẩm thực và chia sẻ công thức nấu ăn.</p>
          <div className="flex items-center gap-4">
            <Link href="/" className="hover:text-[#C98F7D] transition-colors">
              Chính sách bảo mật
            </Link>
            <Link href="/" className="hover:text-[#C98F7D] transition-colors">
              Điều khoản sử dụng
            </Link>
          </div>
        </div>
      </div>
    </footer>
  );
};
