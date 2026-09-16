"use client";

import React, { use, useState, useMemo } from "react";
import Link from "next/link";
import { notFound } from "next/navigation";
import {
  Utensils,
  Sunrise,
  Sun,
  Moon,
  Cake,
  Coffee,
  Cookie,
  ChevronRight,
  SlidersHorizontal,
  Grid,
  List,
} from "lucide-react";
import { useApp } from "../../context/AppContext";
import { RecipeGrid } from "../../components/recipe/RecipeGrid";

interface PageProps {
  params: Promise<{ slug: string }>;
}

export default function CategoryDetailPage({ params }: PageProps) {
  const resolvedParams = use(params);
  const { slug } = resolvedParams;
  const { categories, recipes } = useApp();

  const [selectedDifficulty, setSelectedDifficulty] = useState<string>("all");
  const [sortBy, setSortBy] = useState<string>("newest");
  const [viewMode, setViewMode] = useState<"grid" | "list">("grid");

  const category = categories.find((c) => c.slug === slug);

  const getCategoryIcon = (iconName: string) => {
    switch (iconName) {
      case "Sunrise":
        return <Sunrise className="w-8 h-8" />;
      case "Sun":
        return <Sun className="w-8 h-8" />;
      case "Moon":
        return <Moon className="w-8 h-8" />;
      case "Cake":
        return <Cake className="w-8 h-8" />;
      case "Coffee":
        return <Coffee className="w-8 h-8" />;
      case "Cookie":
        return <Cookie className="w-8 h-8" />;
      default:
        return <Utensils className="w-8 h-8" />;
    }
  };

  const categoryRecipes = useMemo(() => {
    return recipes.filter((r) => {
      if (r.status !== "Published") return false;
      if (category && category.slug !== "all" && r.category.slug !== category.slug) {
        return false;
      }
      if (selectedDifficulty !== "all" && r.difficultyLevel !== selectedDifficulty) {
        return false;
      }
      return true;
    });
  }, [recipes, category, selectedDifficulty]);

  const sortedRecipes = useMemo(() => {
    const list = [...categoryRecipes];
    if (sortBy === "newest") {
      list.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
    } else if (sortBy === "popular") {
      list.sort((a, b) => b.viewsCount - a.viewsCount);
    } else if (sortBy === "title-asc") {
      list.sort((a, b) => a.title.localeCompare(b.title, "vi"));
    }
    return list;
  }, [categoryRecipes, sortBy]);

  if (!category && slug !== "all") {
    notFound();
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 sm:py-12 flex flex-col gap-8">
      {/* Breadcrumb */}
      <nav className="flex items-center gap-2 text-xs text-[#8A817C]">
        <Link href="/" className="hover:text-[#C98F7D] transition-colors">
          Trang chủ
        </Link>
        <ChevronRight className="w-3.5 h-3.5" />
        <span className="text-[#463F3A] dark:text-[#F5F3EF] font-medium">
          {category?.name || "Danh mục"}
        </span>
      </nav>

      {/* Category Hero Header Banner */}
      <div className="double-bezel">
        <div className="double-bezel-inner p-8 sm:p-10 flex flex-col sm:flex-row items-center sm:items-start gap-6">
          <div className="w-18 h-18 sm:w-20 sm:h-20 rounded-3xl bg-[#E0AFA0]/40 border border-[#E0AFA0] text-[#60413A] dark:text-[#E0AFA0] flex items-center justify-center shrink-0 shadow-inner">
            {getCategoryIcon(category?.icon || "Utensils")}
          </div>

          <div className="flex flex-col text-center sm:text-left gap-2 flex-1">
            <div className="flex items-center justify-center sm:justify-start gap-3 flex-wrap">
              <h1 className="font-serif text-3xl sm:text-4xl font-bold text-[#463F3A] dark:text-[#F5F3EF]">
                {category?.name}
              </h1>
              <span className="px-3 py-1 rounded-full bg-[#FAF9F6] dark:bg-[#2F2B27] border border-[#DCD8D2] dark:border-[#3D3934] text-xs font-bold text-[#C98F7D]">
                {categoryRecipes.length} công thức
              </span>
            </div>
            <p className="text-sm text-[#8A817C] dark:text-[#A8A29E] max-w-2xl leading-relaxed">
              {category?.description}
            </p>
          </div>
        </div>
      </div>

      {/* Filter and Sort Toolbar */}
      <div className="p-4 rounded-2xl bg-white dark:bg-[#24211E] border border-[#DCD8D2] dark:border-[#3D3934] shadow-xs flex flex-wrap items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <div className="flex items-center gap-1.5 text-xs font-bold text-[#8A817C] uppercase tracking-wider">
            <SlidersHorizontal className="w-3.5 h-3.5" />
            <span>Độ khó:</span>
          </div>
          <select
            value={selectedDifficulty}
            onChange={(e) => setSelectedDifficulty(e.target.value)}
            className="h-9 px-3 text-xs rounded-full bg-[#FAF9F6] dark:bg-[#2F2B27] border border-[#DCD8D2] dark:border-[#3D3934] text-[#463F3A] dark:text-[#EAE6DF] focus:border-[#C98F7D] focus:outline-none cursor-pointer"
          >
            <option value="all">Tất cả</option>
            <option value="Easy">Dễ làm</option>
            <option value="Medium">Trung bình</option>
            <option value="Hard">Kỳ công</option>
          </select>
        </div>

        <div className="flex items-center gap-3">
          <select
            value={sortBy}
            onChange={(e) => setSortBy(e.target.value)}
            className="h-9 px-3 text-xs rounded-full bg-[#FAF9F6] dark:bg-[#2F2B27] border border-[#DCD8D2] dark:border-[#3D3934] text-[#463F3A] dark:text-[#EAE6DF] focus:border-[#C98F7D] focus:outline-none cursor-pointer"
          >
            <option value="newest">Mới nhất</option>
            <option value="popular">Xem nhiều nhất</option>
            <option value="title-asc">Tên (A-Z)</option>
          </select>

          <div className="flex items-center p-0.5 rounded-full bg-[#FAF9F6] dark:bg-[#2F2B27] border border-[#DCD8D2] dark:border-[#3D3934]">
            <button
              onClick={() => setViewMode("grid")}
              aria-label="Lưới"
              className={`p-1.5 rounded-full ${
                viewMode === "grid"
                  ? "bg-white dark:bg-[#1E1B19] text-[#C98F7D]"
                  : "text-[#8A817C]"
              }`}
            >
              <Grid className="w-4 h-4" />
            </button>
            <button
              onClick={() => setViewMode("list")}
              aria-label="Danh sách"
              className={`p-1.5 rounded-full ${
                viewMode === "list"
                  ? "bg-white dark:bg-[#1E1B19] text-[#C98F7D]"
                  : "text-[#8A817C]"
              }`}
            >
              <List className="w-4 h-4" />
            </button>
          </div>
        </div>
      </div>

      {/* Recipe Grid */}
      <RecipeGrid
        recipes={sortedRecipes}
        viewMode={viewMode}
        emptyTitle={`Chưa có công thức cho danh mục ${category?.name}`}
        emptyDescription="Hãy là người đầu tiên đóng góp công thức thơm ngon vào danh mục này!"
      />
    </div>
  );
}
