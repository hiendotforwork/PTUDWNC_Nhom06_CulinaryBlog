"use client";

import React, { useState, useMemo } from "react";
import Image from "next/image";
import Link from "next/link";
import {
  Sparkles,
  ArrowRight,
  SlidersHorizontal,
  Grid,
  List,
  ChevronLeft,
  ChevronRight,
  Flame,
} from "lucide-react";
import { useApp } from "./context/AppContext";
import { CategoryStrip } from "./components/recipe/CategoryStrip";
import { RecipeGrid } from "./components/recipe/RecipeGrid";
import { Button } from "./components/ui/Button";
import { Badge } from "./components/ui/Badge";

export default function HomePage() {
  const { recipes, categories } = useApp();

  const [selectedCategory, setSelectedCategory] = useState<string>("all");
  const [selectedDifficulty, setSelectedDifficulty] = useState<string>("all");
  const [selectedTime, setSelectedTime] = useState<string>("all");
  const [sortBy, setSortBy] = useState<string>("newest");
  const [viewMode, setViewMode] = useState<"grid" | "list">("grid");
  const [currentPage, setCurrentPage] = useState<number>(1);
  const itemsPerPage = 6;

  // Hero Featured Recipe: the first published recipe
  const featuredRecipe = recipes.find((r) => r.status === "Published") || recipes[0];

  // Filtering recipes
  const filteredRecipes = useMemo(() => {
    return recipes.filter((r) => {
      // Must be published for homepage
      if (r.status !== "Published") return false;

      // Category filter
      if (selectedCategory !== "all" && r.category.slug !== selectedCategory) {
        return false;
      }

      // Difficulty filter
      if (selectedDifficulty !== "all" && r.difficultyLevel !== selectedDifficulty) {
        return false;
      }

      // Time filter
      const totalTime = r.prepTime + r.cookTime;
      if (selectedTime === "under30" && totalTime >= 30) return false;
      if (selectedTime === "30to60" && (totalTime < 30 || totalTime > 60)) return false;
      if (selectedTime === "over60" && totalTime <= 60) return false;

      return true;
    });
  }, [recipes, selectedCategory, selectedDifficulty, selectedTime]);

  // Sorting
  const sortedRecipes = useMemo(() => {
    const list = [...filteredRecipes];
    if (sortBy === "newest") {
      list.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
    } else if (sortBy === "oldest") {
      list.sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime());
    } else if (sortBy === "title-asc") {
      list.sort((a, b) => a.title.localeCompare(b.title, "vi"));
    } else if (sortBy === "popular") {
      list.sort((a, b) => b.viewsCount - a.viewsCount);
    }
    return list;
  }, [filteredRecipes, sortBy]);

  // Pagination
  const totalPages = Math.ceil(sortedRecipes.length / itemsPerPage) || 1;
  const paginatedRecipes = useMemo(() => {
    const start = (currentPage - 1) * itemsPerPage;
    return sortedRecipes.slice(start, start + itemsPerPage);
  }, [sortedRecipes, currentPage]);

  const handleClearFilters = () => {
    setSelectedCategory("all");
    setSelectedDifficulty("all");
    setSelectedTime("all");
    setSortBy("newest");
    setCurrentPage(1);
  };

  return (
    <div className="flex flex-col gap-10 pb-16">
      {/* 1. Hero Section (Anti-slop: Split layout with editorial food focus) */}
      <section className="relative overflow-hidden bg-[#FAF9F6] dark:bg-[#1E1B19] border-b border-[#DCD8D2]/70 dark:border-[#3D3934] pt-8 pb-12 sm:pb-16">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 lg:gap-12 items-center">
            {/* Left Content */}
            <div className="lg:col-span-6 flex flex-col gap-5">
              <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-[#F3DDD8]/60 border border-[#E0AFA0]/40 text-xs font-semibold text-[#8A5548] w-max">
                <Sparkles className="w-3.5 h-3.5 text-[#C98F7D]" />
                <span>Nền tảng ẩm thực gia đình chuẩn vị Việt</span>
              </div>

              <h1 className="font-serif text-3xl sm:text-5xl lg:text-6xl font-bold tracking-tight text-[#463F3A] dark:text-[#F5F3EF] leading-[1.15]">
                Khơi dậy cảm hứng <br className="hidden sm:inline" />
                <span className="text-[#C98F7D] italic font-normal">từ gian bếp ấm</span>
              </h1>

              <p className="text-sm sm:text-base text-[#8A817C] dark:text-[#A8A29E] leading-relaxed max-w-lg">
                Khám phá kho tàng công thức nấu ăn được biên soạn tỉ mỉ, từ mâm cơm bình dị thân quen đến những món ngon đãi tiệc cuối tuần.
              </p>

              <div className="flex items-center gap-3 pt-2">
                <Link href="/recipes/create">
                  <Button
                    variant="primary"
                    size="lg"
                    nestedIcon={<ArrowRight className="w-4 h-4" />}
                  >
                    Chia sẻ món ngon
                  </Button>
                </Link>
                <Link href="#recipe-feed">
                  <Button variant="secondary" size="lg">
                    Khám phá công thức
                  </Button>
                </Link>
              </div>

              {/* Trust & Stats Micro-bar */}
              <div className="flex items-center gap-6 pt-4 border-t border-[#DCD8D2]/60 dark:border-[#3D3934] text-xs text-[#8A817C]">
                <div>
                  <strong className="block text-base font-serif font-bold text-[#463F3A] dark:text-[#F5F3EF]">
                    500+
                  </strong>
                  <span>Công thức món</span>
                </div>
                <div className="w-px h-8 bg-[#DCD8D2]" />
                <div>
                  <strong className="block text-base font-serif font-bold text-[#463F3A] dark:text-[#F5F3EF]">
                    100%
                  </strong>
                  <span>Chuẩn vị gia đình</span>
                </div>
                <div className="w-px h-8 bg-[#DCD8D2]" />
                <div>
                  <strong className="block text-base font-serif font-bold text-[#463F3A] dark:text-[#F5F3EF]">
                    25k+
                  </strong>
                  <span>Người yêu bếp</span>
                </div>
              </div>
            </div>

            {/* Right Featured Card Showcase */}
            {featuredRecipe && (
              <div className="lg:col-span-6">
                <div className="double-bezel relative">
                  <div className="double-bezel-inner p-4 sm:p-5 flex flex-col gap-4">
                    <div className="relative aspect-video w-full rounded-2xl overflow-hidden bg-[#EFECE6]">
                      <Image
                        src={
                          featuredRecipe.images[0]?.url ||
                          "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=1200&auto=format&fit=crop&q=80"
                        }
                        alt={featuredRecipe.title}
                        fill
                        priority
                        className="object-cover"
                      />
                      <div className="absolute top-3 left-3 flex items-center gap-2">
                        <span className="px-3 py-1 rounded-full bg-[#E0AFA0] text-[#60413A] text-xs font-bold flex items-center gap-1 shadow-sm">
                          <Flame className="w-3.5 h-3.5 text-[#60413A]" />
                          Món nổi bật tuần
                        </span>
                      </div>
                    </div>

                    <div>
                      <div className="flex items-center gap-2 mb-2 flex-wrap">
                        <Badge variant="category">{featuredRecipe.category.name}</Badge>
                        <Badge
                          variant="difficulty"
                          difficulty={featuredRecipe.difficultyLevel}
                        />
                        <Badge variant="time">
                          {featuredRecipe.prepTime + featuredRecipe.cookTime} phút
                        </Badge>
                      </div>

                      <Link href={`/recipes/${featuredRecipe.slug}`}>
                        <h3 className="font-serif text-xl sm:text-2xl font-bold text-[#463F3A] dark:text-[#F5F3EF] hover:text-[#C98F7D] transition-colors leading-snug line-clamp-1">
                          {featuredRecipe.title}
                        </h3>
                      </Link>

                      <p className="text-xs sm:text-sm text-[#8A817C] dark:text-[#A8A29E] line-clamp-2 mt-1 leading-relaxed">
                        {featuredRecipe.description}
                      </p>
                    </div>

                    <div className="flex items-center justify-between pt-3 border-t border-[#DCD8D2]/60 dark:border-[#3D3934]">
                      <div className="flex items-center gap-2">
                        <div className="w-7 h-7 rounded-full overflow-hidden relative">
                          <Image
                            src={featuredRecipe.author.avatarUrl}
                            alt={featuredRecipe.author.displayName}
                            fill
                            className="object-cover"
                          />
                        </div>
                        <span className="text-xs font-semibold text-[#463F3A] dark:text-[#EAE6DF]">
                          {featuredRecipe.author.displayName}
                        </span>
                      </div>

                      <Link href={`/recipes/${featuredRecipe.slug}`}>
                        <span className="text-xs font-bold text-[#C98F7D] hover:underline flex items-center gap-1">
                          Xem công thức <ArrowRight className="w-3.5 h-3.5" />
                        </span>
                      </Link>
                    </div>
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>
      </section>

      {/* 2. Main Content Container */}
      <div id="recipe-feed" className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 w-full flex flex-col gap-8">
        {/* Category Horizontal Strip */}
        <div className="flex flex-col gap-2">
          <div className="flex items-center justify-between">
            <h2 className="font-serif text-xl sm:text-2xl font-bold text-[#463F3A] dark:text-[#F5F3EF]">
              Khám phá theo danh mục
            </h2>
          </div>
          <CategoryStrip
            categories={categories}
            selectedCategory={selectedCategory}
            onSelectCategory={(slug) => {
              setSelectedCategory(slug);
              setCurrentPage(1);
            }}
          />
        </div>

        {/* Filter, Sort and View Switcher Bar */}
        <div className="p-4 rounded-2xl bg-white dark:bg-[#24211E] border border-[#DCD8D2] dark:border-[#3D3934] shadow-xs flex flex-wrap items-center justify-between gap-4">
          <div className="flex items-center gap-3 flex-wrap">
            <div className="flex items-center gap-1.5 text-xs font-bold text-[#8A817C] uppercase tracking-wider">
              <SlidersHorizontal className="w-3.5 h-3.5" />
              <span>Bộ lọc:</span>
            </div>

            {/* Difficulty Selector */}
            <select
              value={selectedDifficulty}
              onChange={(e) => {
                setSelectedDifficulty(e.target.value);
                setCurrentPage(1);
              }}
              aria-label="Lọc theo độ khó"
              className="h-9 px-3 text-xs rounded-full bg-[#FAF9F6] dark:bg-[#2F2B27] border border-[#DCD8D2] dark:border-[#3D3934] text-[#463F3A] dark:text-[#EAE6DF] focus:border-[#C98F7D] focus:outline-none cursor-pointer"
            >
              <option value="all">Tất cả độ khó</option>
              <option value="Easy">Dễ làm</option>
              <option value="Medium">Trung bình</option>
              <option value="Hard">Kỳ công</option>
            </select>

            {/* Prep Time Selector */}
            <select
              value={selectedTime}
              onChange={(e) => {
                setSelectedTime(e.target.value);
                setCurrentPage(1);
              }}
              aria-label="Lọc theo thời gian nấu"
              className="h-9 px-3 text-xs rounded-full bg-[#FAF9F6] dark:bg-[#2F2B27] border border-[#DCD8D2] dark:border-[#3D3934] text-[#463F3A] dark:text-[#EAE6DF] focus:border-[#C98F7D] focus:outline-none cursor-pointer"
            >
              <option value="all">Mọi thời gian</option>
              <option value="under30">Dưới 30 phút</option>
              <option value="30to60">30 - 60 phút</option>
              <option value="over60">Trên 60 phút</option>
            </select>

            {(selectedDifficulty !== "all" || selectedTime !== "all" || selectedCategory !== "all") && (
              <button
                onClick={handleClearFilters}
                className="text-xs text-[#B85C5C] hover:underline font-medium cursor-pointer"
              >
                Đặt lại
              </button>
            )}
          </div>

          <div className="flex items-center gap-3">
            {/* Sort Dropdown */}
            <div className="flex items-center gap-1.5">
              <span className="text-xs text-[#8A817C] hidden sm:inline">Sắp xếp:</span>
              <select
                value={sortBy}
                onChange={(e) => setSortBy(e.target.value)}
                aria-label="Sắp xếp danh sách công thức"
                className="h-9 px-3 text-xs rounded-full bg-[#FAF9F6] dark:bg-[#2F2B27] border border-[#DCD8D2] dark:border-[#3D3934] text-[#463F3A] dark:text-[#EAE6DF] focus:border-[#C98F7D] focus:outline-none cursor-pointer"
              >
                <option value="newest">Mới nhất</option>
                <option value="popular">Xem nhiều nhất</option>
                <option value="title-asc">Tên (A-Z)</option>
                <option value="oldest">Cũ nhất</option>
              </select>
            </div>

            {/* View Mode Switcher */}
            <div className="flex items-center p-0.5 rounded-full bg-[#FAF9F6] dark:bg-[#2F2B27] border border-[#DCD8D2] dark:border-[#3D3934]">
              <button
                onClick={() => setViewMode("grid")}
                aria-label="Hiển thị dạng lưới"
                className={`p-1.5 rounded-full transition-colors ${
                  viewMode === "grid"
                    ? "bg-white dark:bg-[#1E1B19] text-[#C98F7D] shadow-2xs"
                    : "text-[#8A817C] hover:text-[#463F3A]"
                }`}
              >
                <Grid className="w-4 h-4" />
              </button>
              <button
                onClick={() => setViewMode("list")}
                aria-label="Hiển thị dạng danh sách"
                className={`p-1.5 rounded-full transition-colors ${
                  viewMode === "list"
                    ? "bg-white dark:bg-[#1E1B19] text-[#C98F7D] shadow-2xs"
                    : "text-[#8A817C] hover:text-[#463F3A]"
                }`}
              >
                <List className="w-4 h-4" />
              </button>
            </div>
          </div>
        </div>

        {/* Counter */}
        <div className="flex items-center justify-between text-xs text-[#8A817C]">
          <span>
            Hiển thị <strong>{paginatedRecipes.length}</strong> trên{" "}
            <strong>{sortedRecipes.length}</strong> công thức
          </span>
        </div>

        {/* Recipe Grid Feed */}
        <RecipeGrid
          recipes={paginatedRecipes}
          viewMode={viewMode}
          onClearFilters={handleClearFilters}
          emptyTitle="Không tìm thấy công thức nào"
          emptyDescription="Hãy thử nới lỏng các bộ lọc độ khó hoặc thời gian nấu để xem thêm công thức."
        />

        {/* Pagination Bar */}
        {totalPages > 1 && (
          <div className="flex items-center justify-center gap-2 pt-6">
            <button
              onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
              disabled={currentPage === 1}
              aria-label="Trang trước"
              className="w-9 h-9 rounded-full bg-white dark:bg-[#24211E] border border-[#DCD8D2] dark:border-[#3D3934] flex items-center justify-center text-[#463F3A] dark:text-[#EAE6DF] disabled:opacity-40 disabled:cursor-not-allowed hover:bg-[#F4F3EE] transition-colors"
            >
              <ChevronLeft className="w-4 h-4" />
            </button>

            {Array.from({ length: totalPages }).map((_, i) => {
              const page = i + 1;
              const isActive = currentPage === page;
              return (
                <button
                  key={page}
                  onClick={() => setCurrentPage(page)}
                  className={`w-9 h-9 rounded-full text-xs font-semibold transition-all ${
                    isActive
                      ? "bg-[#C98F7D] text-white shadow-sm ring-2 ring-[#C98F7D]/30"
                      : "bg-white dark:bg-[#24211E] text-[#463F3A] dark:text-[#EAE6DF] border border-[#DCD8D2] dark:border-[#3D3934] hover:border-[#C98F7D]"
                  }`}
                >
                  {page}
                </button>
              );
            })}

            <button
              onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
              disabled={currentPage === totalPages}
              aria-label="Trang sau"
              className="w-9 h-9 rounded-full bg-white dark:bg-[#24211E] border border-[#DCD8D2] dark:border-[#3D3934] flex items-center justify-center text-[#463F3A] dark:text-[#EAE6DF] disabled:opacity-40 disabled:cursor-not-allowed hover:bg-[#F4F3EE] transition-colors"
            >
              <ChevronRight className="w-4 h-4" />
            </button>
          </div>
        )}
      </div>
    </div>
  );
}
