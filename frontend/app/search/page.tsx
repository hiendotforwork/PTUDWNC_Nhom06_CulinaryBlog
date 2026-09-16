"use client";

import React, { Suspense, useState, useMemo } from "react";
import { useSearchParams, useRouter } from "next/navigation";
import Link from "next/link";
import { Search as SearchIcon, SlidersHorizontal, Grid, List, ChevronRight, RotateCcw } from "lucide-react";
import { useApp } from "../context/AppContext";
import { RecipeGrid } from "../components/recipe/RecipeGrid";

function SearchContent() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const initialQuery = searchParams.get("q") || "";

  const { recipes, categories } = useApp();

  const [query, setQuery] = useState(initialQuery);
  const [selectedCategory, setSelectedCategory] = useState<string>("all");
  const [selectedDifficulty, setSelectedDifficulty] = useState<string>("all");
  const [selectedTime, setSelectedTime] = useState<string>("all");
  const [sortBy, setSortBy] = useState<string>("newest");
  const [viewMode, setViewMode] = useState<"grid" | "list">("grid");

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    router.push(`/search?q=${encodeURIComponent(query)}`);
  };

  const clearAllFilters = () => {
    setQuery("");
    setSelectedCategory("all");
    setSelectedDifficulty("all");
    setSelectedTime("all");
    setSortBy("newest");
    router.push("/search");
  };

  // Filter recipes matching query and filters
  const searchResults = useMemo(() => {
    const qLower = query.trim().toLowerCase();

    return recipes.filter((recipe) => {
      if (recipe.status !== "Published") return false;

      // Text match (title, description, ingredients)
      if (qLower) {
        const titleMatch = recipe.title.toLowerCase().includes(qLower);
        const descMatch = recipe.description.toLowerCase().includes(qLower);
        const ingMatch = recipe.ingredients.some((ing) =>
          ing.name.toLowerCase().includes(qLower)
        );
        if (!titleMatch && !descMatch && !ingMatch) return false;
      }

      // Category filter
      if (selectedCategory !== "all" && recipe.category.slug !== selectedCategory) {
        return false;
      }

      // Difficulty filter
      if (selectedDifficulty !== "all" && recipe.difficultyLevel !== selectedDifficulty) {
        return false;
      }

      // Time filter
      const totalTime = recipe.prepTime + recipe.cookTime;
      if (selectedTime === "under30" && totalTime >= 30) return false;
      if (selectedTime === "30to60" && (totalTime < 30 || totalTime > 60)) return false;
      if (selectedTime === "over60" && totalTime <= 60) return false;

      return true;
    });
  }, [recipes, query, selectedCategory, selectedDifficulty, selectedTime]);

  // Sort results
  const sortedResults = useMemo(() => {
    const list = [...searchResults];
    if (sortBy === "newest") {
      list.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
    } else if (sortBy === "popular") {
      list.sort((a, b) => b.viewsCount - a.viewsCount);
    } else if (sortBy === "title-asc") {
      list.sort((a, b) => a.title.localeCompare(b.title, "vi"));
    }
    return list;
  }, [searchResults, sortBy]);

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 sm:py-12 flex flex-col gap-8">
      {/* Breadcrumb */}
      <nav className="flex items-center gap-2 text-xs text-[#8A817C]">
        <Link href="/" className="hover:text-[#C98F7D] transition-colors">
          Trang chủ
        </Link>
        <ChevronRight className="w-3.5 h-3.5" />
        <span className="text-[#463F3A] dark:text-[#F5F3EF] font-medium">Tìm kiếm công thức</span>
      </nav>

      {/* Search Header Banner */}
      <div className="double-bezel">
        <div className="double-bezel-inner p-6 sm:p-8 flex flex-col gap-4">
          <h1 className="font-serif text-2xl sm:text-3xl font-bold text-[#463F3A] dark:text-[#F5F3EF]">
            {query.trim() ? (
              <>
                Kết quả tìm kiếm cho: <span className="text-[#C98F7D]">&ldquo;{query}&rdquo;</span>
              </>
            ) : (
              "Tra cứu & Tìm kiếm công thức"
            )}
          </h1>
          <p className="text-xs sm:text-sm text-[#8A817C] dark:text-[#A8A29E]">
            Tìm thấy <strong>{sortedResults.length}</strong> công thức phù hợp với tiêu chí của bạn.
          </p>

          {/* Search Bar inside Page */}
          <form onSubmit={handleSearchSubmit} className="relative max-w-xl mt-1">
            <input
              type="search"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder="Nhập tên món, nguyên liệu (VD: thịt bò, phở, bơ chanh...)"
              className="w-full h-12 pl-11 pr-28 text-sm rounded-2xl bg-white dark:bg-[#1E1B19] border border-[#DCD8D2] dark:border-[#3D3934] focus:border-[#C98F7D] focus:outline-none"
            />
            <SearchIcon className="w-5 h-5 text-[#8A817C] absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" />
            <button
              type="submit"
              className="absolute right-2 top-1/2 -translate-y-1/2 h-8 px-4 rounded-xl bg-[#C98F7D] text-white text-xs font-semibold hover:bg-[#B0705E] transition-colors cursor-pointer"
            >
              Tìm kiếm
            </button>
          </form>
        </div>
      </div>

      {/* Filter and Sort Toolbar */}
      <div className="p-4 rounded-2xl bg-white dark:bg-[#24211E] border border-[#DCD8D2] dark:border-[#3D3934] shadow-xs flex flex-wrap items-center justify-between gap-4">
        <div className="flex items-center gap-2.5 flex-wrap">
          <div className="flex items-center gap-1 text-xs font-bold text-[#8A817C] uppercase tracking-wider">
            <SlidersHorizontal className="w-3.5 h-3.5" />
            <span>Bộ lọc:</span>
          </div>

          {/* Category Filter */}
          <select
            value={selectedCategory}
            onChange={(e) => setSelectedCategory(e.target.value)}
            className="h-9 px-3 text-xs rounded-full bg-[#FAF9F6] dark:bg-[#2F2B27] border border-[#DCD8D2] dark:border-[#3D3934] text-[#463F3A] dark:text-[#EAE6DF] focus:border-[#C98F7D] focus:outline-none cursor-pointer"
          >
            <option value="all">Mọi danh mục</option>
            {categories
              .filter((c) => c.slug !== "all")
              .map((c) => (
                <option key={c.id} value={c.slug}>
                  {c.name}
                </option>
              ))}
          </select>

          {/* Difficulty Filter */}
          <select
            value={selectedDifficulty}
            onChange={(e) => setSelectedDifficulty(e.target.value)}
            className="h-9 px-3 text-xs rounded-full bg-[#FAF9F6] dark:bg-[#2F2B27] border border-[#DCD8D2] dark:border-[#3D3934] text-[#463F3A] dark:text-[#EAE6DF] focus:border-[#C98F7D] focus:outline-none cursor-pointer"
          >
            <option value="all">Mọi độ khó</option>
            <option value="Easy">Dễ làm</option>
            <option value="Medium">Trung bình</option>
            <option value="Hard">Kỳ công</option>
          </select>

          {/* Time Filter */}
          <select
            value={selectedTime}
            onChange={(e) => setSelectedTime(e.target.value)}
            className="h-9 px-3 text-xs rounded-full bg-[#FAF9F6] dark:bg-[#2F2B27] border border-[#DCD8D2] dark:border-[#3D3934] text-[#463F3A] dark:text-[#EAE6DF] focus:border-[#C98F7D] focus:outline-none cursor-pointer"
          >
            <option value="all">Mọi thời gian</option>
            <option value="under30">&lt; 30 phút</option>
            <option value="30to60">30 - 60 phút</option>
            <option value="over60">&gt; 60 phút</option>
          </select>

          {(selectedCategory !== "all" || selectedDifficulty !== "all" || selectedTime !== "all") && (
            <button
              onClick={clearAllFilters}
              className="text-xs text-[#B85C5C] hover:underline flex items-center gap-1 font-medium cursor-pointer"
            >
              <RotateCcw className="w-3 h-3" />
              <span>Xóa bộ lọc</span>
            </button>
          )}
        </div>

        {/* Sort & Mode */}
        <div className="flex items-center gap-3">
          <select
            value={sortBy}
            onChange={(e) => setSortBy(e.target.value)}
            className="h-9 px-3 text-xs rounded-full bg-[#FAF9F6] dark:bg-[#2F2B27] border border-[#DCD8D2] dark:border-[#3D3934] text-[#463F3A] dark:text-[#EAE6DF] focus:border-[#C98F7D] focus:outline-none cursor-pointer"
          >
            <option value="newest">Mới nhất</option>
            <option value="popular">Xem nhiều nhất</option>
            <option value="title-asc">Tên món (A-Z)</option>
          </select>

          <div className="flex items-center p-0.5 rounded-full bg-[#FAF9F6] dark:bg-[#2F2B27] border border-[#DCD8D2] dark:border-[#3D3934]">
            <button
              onClick={() => setViewMode("grid")}
              aria-label="Dạng lưới"
              className={`p-1.5 rounded-full ${
                viewMode === "grid" ? "bg-white dark:bg-[#1E1B19] text-[#C98F7D]" : "text-[#8A817C]"
              }`}
            >
              <Grid className="w-4 h-4" />
            </button>
            <button
              onClick={() => setViewMode("list")}
              aria-label="Dạng danh sách"
              className={`p-1.5 rounded-full ${
                viewMode === "list" ? "bg-white dark:bg-[#1E1B19] text-[#C98F7D]" : "text-[#8A817C]"
              }`}
            >
              <List className="w-4 h-4" />
            </button>
          </div>
        </div>
      </div>

      {/* Results Grid */}
      <RecipeGrid
        recipes={sortedResults}
        viewMode={viewMode}
        onClearFilters={clearAllFilters}
        emptyTitle="Không tìm thấy công thức phù hợp"
        emptyDescription="Thử tìm kiếm với từ khóa khác hoặc xóa bớt các bộ lọc đang áp dụng."
      />
    </div>
  );
}

function SearchPageFallback() {
  return <div className="p-12 text-center text-xs text-[#8A817C]">Đang tải dữ liệu tìm kiếm...</div>;
}

function SearchWrapper() {
  const searchParams = useSearchParams();
  const q = searchParams.get("q") || "";
  return <SearchContent key={q} />;
}

export default function SearchPage() {
  return (
    <Suspense fallback={<SearchPageFallback />}>
      <SearchWrapper />
    </Suspense>
  );
}
