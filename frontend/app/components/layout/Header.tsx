"use client";

import React, { useState, useRef, useEffect } from "react";
import Link from "next/link";
import Image from "next/image";
import { useRouter } from "next/navigation";
import {
  Search,
  PlusCircle,
  Menu,
  X,
  User as UserIcon,
  LogOut,
  BookOpen,
  ChevronDown,
  Flame,
  ChefHat,
  Sparkles,
} from "lucide-react";
import { useApp } from "../../context/AppContext";
import { Button } from "../ui/Button";

export const Header: React.FC = () => {
  const router = useRouter();
  const { currentUser, logout, login, recipes, categories } = useApp();

  const [searchQuery, setSearchQuery] = useState("");
  const [isSearchOpen, setIsSearchOpen] = useState(false);
  const [isUserMenuOpen, setIsUserMenuOpen] = useState(false);
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const [isCategoryMenuOpen, setIsCategoryMenuOpen] = useState(false);

  const searchRef = useRef<HTMLDivElement>(null);
  const userMenuRef = useRef<HTMLDivElement>(null);
  const categoryMenuRef = useRef<HTMLDivElement>(null);

  // Close menus on outside click
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (searchRef.current && !searchRef.current.contains(e.target as Node)) {
        setIsSearchOpen(false);
      }
      if (userMenuRef.current && !userMenuRef.current.contains(e.target as Node)) {
        setIsUserMenuOpen(false);
      }
      if (categoryMenuRef.current && !categoryMenuRef.current.contains(e.target as Node)) {
        setIsCategoryMenuOpen(false);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (searchQuery.trim()) {
      setIsSearchOpen(false);
      router.push(`/search?q=${encodeURIComponent(searchQuery.trim())}`);
    }
  };

  const handleSuggestionClick = (term: string) => {
    setSearchQuery(term);
    setIsSearchOpen(false);
    router.push(`/search?q=${encodeURIComponent(term)}`);
  };

  // Top searches from UI spec
  const topSearches = ["Phở", "Cá hồi", "Bún chả", "Cơm tấm", "Trà đào"];

  // Filter matched recipe suggestions
  const recipeSuggestions = searchQuery.trim()
    ? recipes.filter((r) =>
        r.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
        r.description.toLowerCase().includes(searchQuery.toLowerCase())
      ).slice(0, 4)
    : [];

  return (
    <header className="sticky top-0 z-40 w-full bg-[#F4F3EE]/95 dark:bg-[#181614]/95 backdrop-blur-md border-b border-[#DCD8D2]/80 dark:border-[#3D3934] transition-colors">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-18 sm:h-20 flex items-center justify-between gap-4">
        {/* Brand Logo */}
        <Link href="/" className="flex items-center gap-3 group shrink-0">
          <div className="w-10 h-10 sm:w-11 sm:h-11 rounded-2xl bg-[#E0AFA0] text-[#60413A] flex items-center justify-center shadow-inner group-hover:scale-105 group-hover:bg-[#C98F7D] group-hover:text-white transition-all duration-200">
            <ChefHat className="w-6 h-6" />
          </div>
          <div className="flex flex-col">
            <span className="font-serif text-lg sm:text-xl font-bold tracking-tight text-[#463F3A] dark:text-[#F5F3EF] leading-tight">
              Culinary Blog
            </span>
            <span className="text-[10px] sm:text-[11px] font-medium tracking-wider text-[#8A817C] uppercase">
              Bếp Ẩm Thực Việt
            </span>
          </div>
        </Link>

        {/* Center Search Bar (Desktop) */}
        <div ref={searchRef} className="hidden md:flex flex-1 max-w-md relative mx-4">
          <form onSubmit={handleSearchSubmit} className="w-full relative">
            <input
              type="search"
              value={searchQuery}
              onChange={(e) => {
                setSearchQuery(e.target.value);
                setIsSearchOpen(true);
              }}
              onFocus={() => setIsSearchOpen(true)}
              placeholder="Tìm kiếm công thức, nguyên liệu..."
              aria-label="Tìm kiếm công thức"
              className="w-full h-10 pl-10 pr-4 text-xs sm:text-sm rounded-full bg-white dark:bg-[#24211E] border border-[#DCD8D2] dark:border-[#3D3934] focus:border-[#C98F7D] focus:ring-2 focus:ring-[#C98F7D]/20 focus:outline-none transition-all placeholder:text-[#8A817C]/70 shadow-2xs"
            />
            <Search className="w-4 h-4 text-[#8A817C] absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" />
          </form>

          {/* Autocomplete Dropdown */}
          {isSearchOpen && (
            <div className="absolute top-12 left-0 right-0 bg-white dark:bg-[#24211E] rounded-2xl shadow-xl border border-[#DCD8D2] dark:border-[#3D3934] p-4 flex flex-col gap-3 animate-in fade-in z-50">
              <div>
                <div className="flex items-center gap-1.5 text-xs font-semibold text-[#8A817C] uppercase tracking-wider mb-2">
                  <Flame className="w-3.5 h-3.5 text-[#C89B3C]" />
                  <span>Tìm kiếm phổ biến</span>
                </div>
                <div className="flex flex-wrap gap-1.5">
                  {topSearches.map((term) => (
                    <button
                      key={term}
                      onClick={() => handleSuggestionClick(term)}
                      className="text-xs px-3 py-1 rounded-full bg-[#FAF9F6] dark:bg-[#2F2B27] border border-[#DCD8D2] text-[#463F3A] dark:text-[#EAE6DF] hover:border-[#C98F7D] hover:text-[#C98F7D] transition-colors cursor-pointer"
                    >
                      {term}
                    </button>
                  ))}
                </div>
              </div>

              {recipeSuggestions.length > 0 && (
                <div className="border-t border-[#DCD8D2]/60 pt-2.5">
                  <div className="flex items-center gap-1.5 text-xs font-semibold text-[#8A817C] uppercase tracking-wider mb-2">
                    <Sparkles className="w-3.5 h-3.5 text-[#C98F7D]" />
                    <span>Gợi ý công thức</span>
                  </div>
                  <div className="flex flex-col gap-1">
                    {recipeSuggestions.map((recipe) => (
                      <Link
                        key={recipe.id}
                        href={`/recipes/${recipe.slug}`}
                        onClick={() => setIsSearchOpen(false)}
                        className="flex items-center gap-2.5 p-1.5 rounded-lg hover:bg-[#F4F3EE] dark:hover:bg-[#2F2B27] transition-colors text-xs text-[#463F3A] dark:text-[#EAE6DF]"
                      >
                        <div className="w-8 h-8 rounded-md overflow-hidden relative shrink-0">
                          <Image
                            src={recipe.images[0]?.url || "https://picsum.photos/100"}
                            alt={recipe.title}
                            fill
                            className="object-cover"
                          />
                        </div>
                        <span className="font-medium truncate">{recipe.title}</span>
                      </Link>
                    ))}
                  </div>
                </div>
              )}
            </div>
          )}
        </div>

        {/* Right Navigation & Actions */}
        <div className="flex items-center gap-2.5 sm:gap-3">
          {/* Categories Dropdown */}
          <div ref={categoryMenuRef} className="relative hidden lg:block">
            <button
              onClick={() => setIsCategoryMenuOpen(!isCategoryMenuOpen)}
              className="h-10 px-3 text-xs font-semibold rounded-full text-[#463F3A] dark:text-[#EAE6DF] hover:bg-[#EFECE6] dark:hover:bg-[#2A2723] flex items-center gap-1.5 transition-colors cursor-pointer"
            >
              <span>Danh mục</span>
              <ChevronDown className={`w-3.5 h-3.5 transition-transform ${isCategoryMenuOpen ? "rotate-180" : ""}`} />
            </button>

            {isCategoryMenuOpen && (
              <div className="absolute right-0 top-12 w-56 bg-white dark:bg-[#24211E] rounded-2xl shadow-xl border border-[#DCD8D2] dark:border-[#3D3934] py-2 z-50 animate-in fade-in">
                {categories.map((cat) => (
                  <Link
                    key={cat.id}
                    href={cat.slug === "all" ? "/" : `/categories/${cat.slug}`}
                    onClick={() => setIsCategoryMenuOpen(false)}
                    className="flex items-center justify-between px-4 py-2 text-xs font-medium text-[#463F3A] dark:text-[#EAE6DF] hover:bg-[#FAF9F6] dark:hover:bg-[#2E2A26] transition-colors"
                  >
                    <span>{cat.name}</span>
                    <span className="text-[10px] text-[#8A817C] bg-[#F4F3EE] dark:bg-[#332F2B] px-2 py-0.5 rounded-full">
                      {cat.recipeCount}
                    </span>
                  </Link>
                ))}
              </div>
            )}
          </div>

          {/* Quick Demo Switcher (Author vs Guest) */}
          <button
            onClick={() => {
              if (currentUser) {
                logout();
              } else {
                login("hoanglinh@culinaryblog.vn");
              }
            }}
            title="Nhấp để đổi nhanh giữa vai trò Người dùng đã đăng nhập và Khách xem"
            className="hidden xl:flex items-center gap-1 text-[11px] font-medium px-2.5 py-1 rounded-full bg-[#FAF9F6] dark:bg-[#2A2723] border border-[#DCD8D2] text-[#8A817C] hover:border-[#C98F7D] hover:text-[#C98F7D] transition-colors cursor-pointer"
          >
            <span className="w-2 h-2 rounded-full bg-[#6B8E6B]" />
            <span>Vai trò: {currentUser ? "Đầu bếp (Auth)" : "Khách xem (Guest)"}</span>
          </button>

          {/* Create Recipe Button */}
          <Link href="/recipes/create">
            <Button
              variant="primary"
              size="sm"
              nestedIcon={<PlusCircle className="w-3.5 h-3.5" />}
              className="h-9 sm:h-10 text-xs sm:text-sm shadow-xs"
            >
              Tạo công thức
            </Button>
          </Link>

          {/* Auth State */}
          {currentUser ? (
            <div ref={userMenuRef} className="relative">
              <button
                onClick={() => setIsUserMenuOpen(!isUserMenuOpen)}
                aria-label="Menu tài khoản"
                className="flex items-center gap-2 p-1 rounded-full hover:ring-2 hover:ring-[#C98F7D]/40 transition-all cursor-pointer"
              >
                <div className="w-9 h-9 sm:w-10 sm:h-10 rounded-full overflow-hidden relative border-2 border-[#E0AFA0]">
                  <Image
                    src={currentUser.avatarUrl}
                    alt={currentUser.displayName}
                    fill
                    className="object-cover"
                  />
                </div>
              </button>

              {/* User Dropdown */}
              {isUserMenuOpen && (
                <div className="absolute right-0 top-12 w-64 bg-white dark:bg-[#24211E] rounded-2xl shadow-xl border border-[#DCD8D2] dark:border-[#3D3934] p-3 z-50 animate-in fade-in">
                  <div className="px-3 py-2 border-b border-[#DCD8D2]/60 dark:border-[#3D3934]">
                    <p className="text-sm font-bold text-[#463F3A] dark:text-[#F5F3EF] truncate">
                      {currentUser.displayName}
                    </p>
                    <p className="text-xs text-[#8A817C] truncate">{currentUser.email}</p>
                  </div>
                  <div className="py-2 flex flex-col gap-1">
                    <Link
                      href="/profile"
                      onClick={() => setIsUserMenuOpen(false)}
                      className="flex items-center gap-2.5 px-3 py-2 rounded-xl text-xs font-medium text-[#463F3A] dark:text-[#EAE6DF] hover:bg-[#FAF9F6] dark:hover:bg-[#2E2A26] transition-colors"
                    >
                      <UserIcon className="w-4 h-4 text-[#8A817C]" />
                      <span>Hồ sơ cá nhân</span>
                    </Link>
                    <Link
                      href="/profile?tab=recipes"
                      onClick={() => setIsUserMenuOpen(false)}
                      className="flex items-center gap-2.5 px-3 py-2 rounded-xl text-xs font-medium text-[#463F3A] dark:text-[#EAE6DF] hover:bg-[#FAF9F6] dark:hover:bg-[#2E2A26] transition-colors"
                    >
                      <BookOpen className="w-4 h-4 text-[#8A817C]" />
                      <span>Công thức của tôi</span>
                    </Link>
                  </div>
                  <div className="pt-2 border-t border-[#DCD8D2]/60 dark:border-[#3D3934]">
                    <button
                      onClick={() => {
                        setIsUserMenuOpen(false);
                        logout();
                      }}
                      className="w-full flex items-center gap-2.5 px-3 py-2 rounded-xl text-xs font-medium text-[#B85C5C] hover:bg-[#F9EBEB] transition-colors cursor-pointer"
                    >
                      <LogOut className="w-4 h-4" />
                      <span>Đăng xuất</span>
                    </button>
                  </div>
                </div>
              )}
            </div>
          ) : (
            <div className="flex items-center gap-2">
              <Link href="/login">
                <Button variant="ghost" size="sm" className="hidden sm:inline-flex text-xs">
                  Đăng nhập
                </Button>
              </Link>
              <Link href="/register">
                <Button variant="secondary" size="sm" className="text-xs">
                  Đăng ký
                </Button>
              </Link>
            </div>
          )}

          {/* Mobile Menu Trigger */}
          <button
            onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
            aria-label="Mở menu điều hướng"
            className="md:hidden p-2 rounded-xl text-[#463F3A] dark:text-[#F5F3EF] hover:bg-[#EFECE6] dark:hover:bg-[#2A2723] transition-colors"
          >
            {isMobileMenuOpen ? <X className="w-6 h-6" /> : <Menu className="w-6 h-6" />}
          </button>
        </div>
      </div>

      {/* Mobile Drawer Navigation */}
      {isMobileMenuOpen && (
        <div className="md:hidden border-t border-[#DCD8D2] dark:border-[#3D3934] bg-[#F4F3EE] dark:bg-[#181614] px-4 pt-3 pb-6 flex flex-col gap-4 animate-in slide-in-from-top-2">
          {/* Mobile Search Input */}
          <form onSubmit={handleSearchSubmit} className="relative w-full">
            <input
              type="search"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="Tìm kiếm công thức..."
              className="w-full h-10 pl-10 pr-4 text-xs rounded-full bg-white dark:bg-[#24211E] border border-[#DCD8D2] dark:border-[#3D3934] focus:outline-none"
            />
            <Search className="w-4 h-4 text-[#8A817C] absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" />
          </form>

          {/* Mobile Category Links */}
          <div>
            <p className="text-[11px] font-bold text-[#8A817C] uppercase tracking-wider mb-2">
              Danh mục món ăn
            </p>
            <div className="grid grid-cols-2 gap-2">
              {categories.map((cat) => (
                <Link
                  key={cat.id}
                  href={cat.slug === "all" ? "/" : `/categories/${cat.slug}`}
                  onClick={() => setIsMobileMenuOpen(false)}
                  className="px-3 py-2 rounded-xl bg-white dark:bg-[#24211E] border border-[#DCD8D2] text-xs font-medium text-[#463F3A] dark:text-[#EAE6DF]"
                >
                  {cat.name}
                </Link>
              ))}
            </div>
          </div>

          {/* Auth in Mobile */}
          <div className="pt-2 border-t border-[#DCD8D2]/60 dark:border-[#3D3934] flex flex-col gap-2">
            {currentUser ? (
              <>
                <Link
                  href="/profile"
                  onClick={() => setIsMobileMenuOpen(false)}
                  className="text-xs font-semibold py-2 px-3 rounded-lg hover:bg-white dark:hover:bg-[#24211E]"
                >
                  Hồ sơ cá nhân ({currentUser.displayName})
                </Link>
                <button
                  onClick={() => {
                    setIsMobileMenuOpen(false);
                    logout();
                  }}
                  className="text-xs font-semibold text-[#B85C5C] text-left py-2 px-3 rounded-lg hover:bg-[#F9EBEB]"
                >
                  Đăng xuất
                </button>
              </>
            ) : (
              <div className="flex gap-2">
                <Link href="/login" onClick={() => setIsMobileMenuOpen(false)} className="flex-1">
                  <Button variant="outline" size="sm" className="w-full text-xs">
                    Đăng nhập
                  </Button>
                </Link>
                <Link href="/register" onClick={() => setIsMobileMenuOpen(false)} className="flex-1">
                  <Button variant="primary" size="sm" className="w-full text-xs">
                    Đăng ký
                  </Button>
                </Link>
              </div>
            )}
          </div>
        </div>
      )}
    </header>
  );
};
