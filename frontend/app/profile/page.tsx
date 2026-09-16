"use client";

import React, { Suspense, useState } from "react";
import Image from "next/image";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import {
  User as UserIcon,
  BookOpen,
  Heart,
  PlusCircle,
  Calendar,
  Mail,
  AtSign,
  ChevronRight,
  Save,
} from "lucide-react";
import { useApp } from "../context/AppContext";
import { Input, Textarea } from "../components/ui/Input";
import { Button } from "../components/ui/Button";
import { RecipeGrid } from "../components/recipe/RecipeGrid";

function ProfileContent() {
  const searchParams = useSearchParams();
  const initialTab = searchParams.get("tab") === "recipes" ? "recipes" : "profile";

  const { currentUser, recipes, favorites, updateProfile, showToast } = useApp();

  const [activeTab, setActiveTab] = useState<"profile" | "recipes" | "favorites">(initialTab);
  const [displayName, setDisplayName] = useState(currentUser?.displayName || "");
  const [bio, setBio] = useState(currentUser?.bio || "");
  const [avatarUrl, setAvatarUrl] = useState(currentUser?.avatarUrl || "");
  const [selectedStatus, setSelectedStatus] = useState<string>("all");
  const [isSaving, setIsSaving] = useState(false);

  if (!currentUser) {
    return (
      <div className="max-w-md mx-auto py-16 px-4 text-center">
        <div className="double-bezel">
          <div className="double-bezel-inner p-8 flex flex-col items-center gap-4">
            <UserIcon className="w-12 h-12 text-[#8A817C]" />
            <h2 className="font-serif text-xl font-bold">Bạn chưa đăng nhập</h2>
            <p className="text-xs text-[#8A817C]">
              Vui lòng đăng nhập hoặc đăng ký tài khoản để xem và quản lý hồ sơ cá nhân của bạn.
            </p>
            <div className="flex gap-3 mt-2">
              <Link href="/login">
                <Button variant="primary" size="md">
                  Đăng nhập
                </Button>
              </Link>
              <Link href="/register">
                <Button variant="secondary" size="md">
                  Đăng ký
                </Button>
              </Link>
            </div>
          </div>
        </div>
      </div>
    );
  }

  // Filter recipes created by current user
  const myRecipes = recipes.filter((r) => r.author.id === currentUser.id);

  // Status filtered recipes
  const filteredMyRecipes = myRecipes.filter((r) => {
    if (selectedStatus === "all") return true;
    return r.status === selectedStatus;
  });

  // Favorite recipes
  const favoriteRecipes = recipes.filter((r) => favorites.includes(r.id));

  const handleSaveProfile = (e: React.FormEvent) => {
    e.preventDefault();
    if (!displayName.trim()) {
      showToast("error", "Tên hiển thị không được để trống.");
      return;
    }
    setIsSaving(true);
    setTimeout(() => {
      updateProfile({ displayName, bio, avatarUrl });
      setIsSaving(false);
    }, 400);
  };

  const formattedDate = new Date(currentUser.createdAt).toLocaleDateString("vi-VN", {
    month: "long",
    year: "numeric",
  });

  return (
    <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-8 sm:py-12 flex flex-col gap-8">
      {/* Breadcrumb */}
      <nav className="flex items-center gap-2 text-xs text-[#8A817C]">
        <Link href="/" className="hover:text-[#C98F7D] transition-colors">
          Trang chủ
        </Link>
        <ChevronRight className="w-3.5 h-3.5" />
        <span className="text-[#463F3A] dark:text-[#F5F3EF] font-medium">Hồ sơ cá nhân</span>
      </nav>

      {/* Profile Header Card */}
      <div className="double-bezel">
        <div className="double-bezel-inner p-6 sm:p-8 flex flex-col sm:flex-row items-center sm:items-start gap-6">
          <div className="w-24 h-24 sm:w-28 sm:h-28 rounded-3xl overflow-hidden relative border-4 border-[#E0AFA0] shadow-md shrink-0">
            <Image
              src={currentUser.avatarUrl}
              alt={currentUser.displayName}
              fill
              className="object-cover"
            />
          </div>

          <div className="flex flex-col text-center sm:text-left gap-2 flex-1">
            <div className="flex items-center justify-center sm:justify-start gap-3 flex-wrap">
              <h1 className="font-serif text-2xl sm:text-3xl font-bold text-[#463F3A] dark:text-[#F5F3EF]">
                {currentUser.displayName}
              </h1>
              <span className="px-3 py-0.5 rounded-full bg-[#E8EFE8] text-[#385938] text-xs font-bold border border-[#B3CFB3]">
                Thành viên Bếp
              </span>
            </div>

            <div className="flex items-center justify-center sm:justify-start gap-4 text-xs text-[#8A817C] flex-wrap">
              <span className="flex items-center gap-1">
                <AtSign className="w-3.5 h-3.5" />
                {currentUser.userName}
              </span>
              <span className="flex items-center gap-1">
                <Mail className="w-3.5 h-3.5" />
                {currentUser.email}
              </span>
              <span className="flex items-center gap-1">
                <Calendar className="w-3.5 h-3.5" />
                Gia nhập {formattedDate}
              </span>
            </div>

            {currentUser.bio && (
              <p className="text-xs sm:text-sm text-[#463F3A] dark:text-[#EAE6DF] leading-relaxed mt-1 max-w-xl">
                {currentUser.bio}
              </p>
            )}
          </div>

          <div className="shrink-0">
            <Link href="/recipes/create">
              <Button variant="primary" size="sm" leftIcon={<PlusCircle className="w-4 h-4" />}>
                Tạo công thức mới
              </Button>
            </Link>
          </div>
        </div>
      </div>

      {/* Tabs Switcher */}
      <div className="flex items-center gap-2 border-b border-[#DCD8D2] dark:border-[#3D3934] pb-px">
        <button
          onClick={() => setActiveTab("profile")}
          className={`flex items-center gap-2 px-5 py-3 text-xs sm:text-sm font-bold border-b-2 transition-all cursor-pointer ${
            activeTab === "profile"
              ? "border-[#C98F7D] text-[#C98F7D]"
              : "border-transparent text-[#8A817C] hover:text-[#463F3A]"
          }`}
        >
          <UserIcon className="w-4 h-4" />
          <span>Thông tin hồ sơ</span>
        </button>

        <button
          onClick={() => setActiveTab("recipes")}
          className={`flex items-center gap-2 px-5 py-3 text-xs sm:text-sm font-bold border-b-2 transition-all cursor-pointer ${
            activeTab === "recipes"
              ? "border-[#C98F7D] text-[#C98F7D]"
              : "border-transparent text-[#8A817C] hover:text-[#463F3A]"
          }`}
        >
          <BookOpen className="w-4 h-4" />
          <span>Công thức của tôi ({myRecipes.length})</span>
        </button>

        <button
          onClick={() => setActiveTab("favorites")}
          className={`flex items-center gap-2 px-5 py-3 text-xs sm:text-sm font-bold border-b-2 transition-all cursor-pointer ${
            activeTab === "favorites"
              ? "border-[#C98F7D] text-[#C98F7D]"
              : "border-transparent text-[#8A817C] hover:text-[#463F3A]"
          }`}
        >
          <Heart className="w-4 h-4" />
          <span>Món đã lưu ({favoriteRecipes.length})</span>
        </button>
      </div>

      {/* Tab 1: Edit Profile */}
      {activeTab === "profile" && (
        <div className="double-bezel max-w-2xl">
          <div className="double-bezel-inner p-6 sm:p-8 flex flex-col gap-6">
            <div className="pb-3 border-b border-[#DCD8D2]/60 dark:border-[#3D3934]">
              <h3 className="font-serif text-lg font-bold text-[#463F3A] dark:text-[#F5F3EF]">
                Chỉnh sửa thông tin cá nhân
              </h3>
              <p className="text-xs text-[#8A817C] mt-0.5">
                Cập nhật tên hiển thị, hình đại diện và đôi dòng giới thiệu về niềm yêu thích ẩm thực.
              </p>
            </div>

            <form onSubmit={handleSaveProfile} className="flex flex-col gap-4">
              <Input
                label="Tên hiển thị"
                required
                value={displayName}
                onChange={(e) => setDisplayName(e.target.value)}
                helperText="Tên này sẽ xuất hiện trên tất cả các công thức bạn đăng tải."
              />

              <Textarea
                label="Tiểu sử / Giới thiệu bản thân"
                rows={3}
                value={bio}
                onChange={(e) => setBio(e.target.value)}
                helperText="Tối đa 500 ký tự."
              />

              <Input
                label="Đường dẫn ảnh đại diện (URL)"
                type="url"
                value={avatarUrl}
                onChange={(e) => setAvatarUrl(e.target.value)}
                helperText="Nhập đường dẫn ảnh trực tuyến (HTTPS)."
              />

              {/* Readonly fields as per SRS requirement */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 pt-2 border-t border-[#DCD8D2]/40">
                <Input label="Địa chỉ Email" value={currentUser.email} disabled />
                <Input label="Tên đăng nhập (Username)" value={currentUser.userName} disabled />
              </div>

              <div className="pt-2">
                <Button
                  type="submit"
                  variant="primary"
                  size="md"
                  isLoading={isSaving}
                  leftIcon={<Save className="w-4 h-4" />}
                >
                  Lưu thay đổi
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Tab 2: My Recipes */}
      {activeTab === "recipes" && (
        <div className="flex flex-col gap-6">
          {/* Status Sub-filter Bar */}
          <div className="flex items-center justify-between gap-4 flex-wrap">
            <div className="flex items-center gap-2">
              <span className="text-xs font-bold text-[#8A817C] uppercase tracking-wider">
                Trạng thái:
              </span>
              <div className="flex gap-1.5">
                {["all", "Published", "Draft", "Archived"].map((st) => (
                  <button
                    key={st}
                    onClick={() => setSelectedStatus(st)}
                    className={`px-3 py-1 rounded-full text-xs font-semibold transition-all cursor-pointer ${
                      selectedStatus === st
                        ? "bg-[#C98F7D] text-white"
                        : "bg-white dark:bg-[#24211E] text-[#463F3A] dark:text-[#EAE6DF] border border-[#DCD8D2]"
                    }`}
                  >
                    {st === "all"
                      ? "Tất cả"
                      : st === "Published"
                      ? "Đã xuất bản"
                      : st === "Draft"
                      ? "Bản nháp"
                      : "Lưu trữ"}
                  </button>
                ))}
              </div>
            </div>

            <Link href="/recipes/create">
              <Button variant="secondary" size="sm" leftIcon={<PlusCircle className="w-3.5 h-3.5" />}>
                Thêm món mới
              </Button>
            </Link>
          </div>

          <RecipeGrid
            recipes={filteredMyRecipes}
            showOwnerActions={true}
            emptyTitle="Bạn chưa có công thức nào trong mục này"
            emptyDescription="Hãy tạo món ăn thơm ngon đầu tiên của bạn để chia sẻ cho mọi người cùng nấu!"
          />
        </div>
      )}

      {/* Tab 3: Saved Favorites */}
      {activeTab === "favorites" && (
        <div className="flex flex-col gap-6">
          <RecipeGrid
            recipes={favoriteRecipes}
            emptyTitle="Chưa có món ăn nào trong danh sách yêu thích"
            emptyDescription="Khám phá các công thức trên trang chủ và bấm vào biểu tượng trái tim để lưu lại nấu sau!"
          />
        </div>
      )}
    </div>
  );
}

export default function ProfilePage() {
  return (
    <Suspense fallback={<div className="p-12 text-center text-xs text-[#8A817C]">Đang tải hồ sơ...</div>}>
      <ProfileContent />
    </Suspense>
  );
}
