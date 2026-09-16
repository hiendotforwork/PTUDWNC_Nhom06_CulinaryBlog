"use client";

import React, { useState } from "react";
import Image from "next/image";
import Link from "next/link";
import { useRouter } from "next/navigation";
import {
  ChevronRight,
  Heart,
  Share2,
  Edit3,
  Trash2,
  Globe,
  Archive,
  ChevronDown,
  ChevronUp,
  CheckCircle2,
  Sparkles,
  Flame,
  Printer,
  Minus,
  Plus,
} from "lucide-react";
import { Recipe } from "../../lib/types";
import { Badge } from "../ui/Badge";
import { Button } from "../ui/Button";
import { Modal } from "../ui/Modal";
import { RecipeCard } from "./RecipeCard";
import { useApp } from "../../context/AppContext";

export interface RecipeDetailViewProps {
  recipe: Recipe;
}

export const RecipeDetailView: React.FC<RecipeDetailViewProps> = ({ recipe }) => {
  const router = useRouter();
  const {
    currentUser,
    favorites,
    toggleFavorite,
    deleteRecipe,
    publishRecipe,
    archiveRecipe,
    recipes,
    showToast,
  } = useApp();

  const [selectedImageIndex, setSelectedImageIndex] = useState(0);
  const [isNutritionOpen, setIsNutritionOpen] = useState(true);
  const [checkedIngredients, setCheckedIngredients] = useState<Record<string, boolean>>({});
  const [servingsMultiplier, setServingsMultiplier] = useState(1);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);

  const isOwner = currentUser?.id === recipe.author.id;
  const isFav = favorites.includes(recipe.id);

  const imagesList =
    recipe.images && recipe.images.length > 0
      ? recipe.images
      : [
          {
            id: "default-img",
            url: "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=1200&auto=format&fit=crop&q=80",
            isPrimary: true,
          },
        ];

  const currentImage = imagesList[selectedImageIndex] || imagesList[0];

  const toggleIngredient = (id: string) => {
    setCheckedIngredients((prev) => ({
      ...prev,
      [id]: !prev[id],
    }));
  };

  const handleShare = () => {
    if (typeof window !== "undefined") {
      navigator.clipboard.writeText(window.location.href);
      showToast("success", "Đã sao chép liên kết công thức vào clipboard!");
    }
  };

  const handleDelete = () => {
    deleteRecipe(recipe.id);
    setIsDeleteModalOpen(false);
    router.push("/");
  };

  const relatedRecipes = recipes
    .filter((r) => r.id !== recipe.id && r.category.id === recipe.category.id)
    .slice(0, 3);

  // If no same category, grab other recipes
  const displayRelated =
    relatedRecipes.length > 0
      ? relatedRecipes
      : recipes.filter((r) => r.id !== recipe.id).slice(0, 3);

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6 sm:py-10">
      {/* Breadcrumb Navigation */}
      <nav className="flex items-center gap-2 text-xs text-[#8A817C] mb-6 flex-wrap">
        <Link href="/" className="hover:text-[#C98F7D] transition-colors">
          Trang chủ
        </Link>
        <ChevronRight className="w-3.5 h-3.5" />
        <Link
          href={`/categories/${recipe.category.slug}`}
          className="hover:text-[#C98F7D] transition-colors"
        >
          {recipe.category.name}
        </Link>
        <ChevronRight className="w-3.5 h-3.5" />
        <span className="text-[#463F3A] dark:text-[#F5F3EF] font-medium truncate max-w-xs">
          {recipe.title}
        </span>
      </nav>

      {/* Title & Top Action Bar */}
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-4 pb-6 border-b border-[#DCD8D2]/60 dark:border-[#3D3934] mb-8">
        <div>
          <div className="flex items-center gap-2 mb-2 flex-wrap">
            <Badge variant="category" size="md">
              {recipe.category.name}
            </Badge>
            <Badge variant="difficulty" difficulty={recipe.difficultyLevel} size="md" />
            <Badge status={recipe.status} size="md" />
          </div>
          <h1 className="font-serif text-2xl sm:text-4xl lg:text-5xl font-bold text-[#463F3A] dark:text-[#F5F3EF] tracking-tight leading-tight">
            {recipe.title}
          </h1>
        </div>

        <div className="flex items-center gap-2 shrink-0">
          <Button
            variant="outline"
            size="sm"
            onClick={() => toggleFavorite(recipe.id)}
            leftIcon={
              <Heart
                className={`w-4 h-4 ${
                  isFav ? "fill-[#B85C5C] text-[#B85C5C]" : "text-[#8A817C]"
                }`}
              />
            }
          >
            {isFav ? "Đã lưu" : "Lưu món"}
          </Button>

          <Button
            variant="secondary"
            size="sm"
            onClick={handleShare}
            leftIcon={<Share2 className="w-4 h-4" />}
          >
            Chia sẻ
          </Button>

          <Button
            variant="ghost"
            size="sm"
            onClick={() => window.print()}
            leftIcon={<Printer className="w-4 h-4" />}
            className="hidden sm:inline-flex"
          >
            In
          </Button>
        </div>
      </div>

      {/* Owner Action Strip (Visible if recipe author) */}
      {isOwner && (
        <div className="p-4 rounded-2xl bg-[#FDF8F6] dark:bg-[#2A2320] border border-[#E0AFA0]/60 mb-8 flex flex-wrap items-center justify-between gap-4">
          <div className="flex items-center gap-2">
            <span className="w-2.5 h-2.5 rounded-full bg-[#C98F7D] animate-pulse" />
            <span className="text-xs sm:text-sm font-semibold text-[#8A5548] dark:text-[#E0AFA0]">
              Bạn là tác giả của công thức này
            </span>
          </div>
          <div className="flex items-center gap-2">
            <Link href={`/recipes/${recipe.slug}/edit`}>
              <Button variant="secondary" size="sm" leftIcon={<Edit3 className="w-3.5 h-3.5" />}>
                Chỉnh sửa
              </Button>
            </Link>

            {recipe.status !== "Published" ? (
              <Button
                variant="primary"
                size="sm"
                onClick={() => publishRecipe(recipe.id)}
                leftIcon={<Globe className="w-3.5 h-3.5" />}
              >
                Xuất bản
              </Button>
            ) : (
              <Button
                variant="outline"
                size="sm"
                onClick={() => archiveRecipe(recipe.id)}
                leftIcon={<Archive className="w-3.5 h-3.5" />}
              >
                Lưu trữ
              </Button>
            )}

            <Button
              variant="destructive"
              size="sm"
              onClick={() => setIsDeleteModalOpen(true)}
              leftIcon={<Trash2 className="w-3.5 h-3.5" />}
            >
              Xóa
            </Button>
          </div>
        </div>
      )}

      {/* Main Recipe Header Section: Hero Image + Sidebar */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 mb-12">
        {/* Left Column: Hero 16:9 Image & Thumbnails */}
        <div className="lg:col-span-8 flex flex-col gap-4">
          <div className="double-bezel">
            <div className="double-bezel-inner p-2">
              <div className="relative aspect-video w-full rounded-2xl overflow-hidden bg-[#EFECE6]">
                <Image
                  src={currentImage.url}
                  alt={recipe.title}
                  fill
                  priority
                  className="object-cover transition-all duration-300"
                />
              </div>
            </div>
          </div>

          {/* Thumbnail Gallery */}
          {imagesList.length > 1 && (
            <div className="flex items-center gap-3 overflow-x-auto pb-2">
              {imagesList.map((img, idx) => (
                <button
                  key={img.id}
                  onClick={() => setSelectedImageIndex(idx)}
                  className={`relative w-20 sm:w-24 aspect-video rounded-xl overflow-hidden border-2 transition-all cursor-pointer shrink-0 ${
                    selectedImageIndex === idx
                      ? "border-[#C98F7D] ring-2 ring-[#C98F7D]/30 scale-105"
                      : "border-transparent opacity-70 hover:opacity-100"
                  }`}
                >
                  <Image src={img.url} alt={`Thumbnail ${idx + 1}`} fill className="object-cover" />
                </button>
              ))}
            </div>
          )}
        </div>

        {/* Right Column: Recipe Meta & Author Card Sidebar */}
        <div className="lg:col-span-4 flex flex-col gap-6">
          {/* Quick Info Specs Bento Card */}
          <div className="double-bezel">
            <div className="double-bezel-inner p-6 flex flex-col gap-5">
              <h3 className="font-serif text-lg font-bold text-[#463F3A] dark:text-[#F5F3EF] pb-3 border-b border-[#DCD8D2]/60 dark:border-[#3D3934]">
                Tổng quan chế biến
              </h3>

              <div className="grid grid-cols-2 gap-4">
                <div className="p-3.5 rounded-xl bg-[#FAF9F6] dark:bg-[#201D1B] border border-[#DCD8D2]/60 dark:border-[#3D3934] flex flex-col">
                  <span className="text-[11px] font-medium text-[#8A817C] uppercase tracking-wider">
                    Chuẩn bị
                  </span>
                  <span className="text-lg font-bold text-[#463F3A] dark:text-[#F5F3EF] mt-0.5">
                    {recipe.prepTime} phút
                  </span>
                </div>

                <div className="p-3.5 rounded-xl bg-[#FAF9F6] dark:bg-[#201D1B] border border-[#DCD8D2]/60 dark:border-[#3D3934] flex flex-col">
                  <span className="text-[11px] font-medium text-[#8A817C] uppercase tracking-wider">
                    Nấu chín
                  </span>
                  <span className="text-lg font-bold text-[#463F3A] dark:text-[#F5F3EF] mt-0.5">
                    {recipe.cookTime} phút
                  </span>
                </div>

                <div className="p-3.5 rounded-xl bg-[#FAF9F6] dark:bg-[#201D1B] border border-[#DCD8D2]/60 dark:border-[#3D3934] flex flex-col">
                  <span className="text-[11px] font-medium text-[#8A817C] uppercase tracking-wider">
                    Khẩu phần
                  </span>
                  <span className="text-lg font-bold text-[#463F3A] dark:text-[#F5F3EF] mt-0.5">
                    {recipe.servings} người
                  </span>
                </div>

                <div className="p-3.5 rounded-xl bg-[#FAF9F6] dark:bg-[#201D1B] border border-[#DCD8D2]/60 dark:border-[#3D3934] flex flex-col">
                  <span className="text-[11px] font-medium text-[#8A817C] uppercase tracking-wider">
                    Độ khó
                  </span>
                  <span className="text-base font-bold text-[#C98F7D] mt-1">
                    {recipe.difficultyLevel === "Easy"
                      ? "Dễ làm"
                      : recipe.difficultyLevel === "Medium"
                      ? "Trung bình"
                      : "Kỳ công"}
                  </span>
                </div>
              </div>
            </div>
          </div>

          {/* Author Card */}
          <div className="double-bezel">
            <div className="double-bezel-inner p-6 flex flex-col gap-4">
              <span className="text-[11px] font-bold text-[#8A817C] uppercase tracking-wider">
                Người chia sẻ công thức
              </span>
              <div className="flex items-center gap-3">
                <div className="w-14 h-14 rounded-full overflow-hidden relative border-2 border-[#E0AFA0] shrink-0">
                  <Image
                    src={recipe.author.avatarUrl}
                    alt={recipe.author.displayName}
                    fill
                    className="object-cover"
                  />
                </div>
                <div>
                  <h4 className="font-serif font-bold text-base text-[#463F3A] dark:text-[#F5F3EF]">
                    {recipe.author.displayName}
                  </h4>
                  <p className="text-xs text-[#8A817C]">@{recipe.author.userName}</p>
                </div>
              </div>
              {recipe.author.bio && (
                <p className="text-xs text-[#8A817C] dark:text-[#A8A29E] leading-relaxed">
                  {recipe.author.bio}
                </p>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Recipe Content Body */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-10">
        <div className="lg:col-span-8 flex flex-col gap-10">
          {/* Mô tả */}
          <section className="flex flex-col gap-3">
            <h2 className="font-serif text-2xl font-bold text-[#463F3A] dark:text-[#F5F3EF]">
              Giới thiệu món ăn
            </h2>
            <p className="text-base text-[#463F3A] dark:text-[#EAE6DF] leading-relaxed">
              {recipe.description}
            </p>
          </section>

          {/* Collapsible Nutrition Accordion */}
          {recipe.nutrition && (
            <section className="double-bezel">
              <div className="double-bezel-inner p-5 sm:p-6">
                <button
                  onClick={() => setIsNutritionOpen(!isNutritionOpen)}
                  className="w-full flex items-center justify-between text-left cursor-pointer"
                >
                  <div className="flex items-center gap-2">
                    <Flame className="w-5 h-5 text-[#C89B3C]" />
                    <h3 className="font-serif text-lg font-bold text-[#463F3A] dark:text-[#F5F3EF]">
                      Thông tin dinh dưỡng (ước tính)
                    </h3>
                  </div>
                  {isNutritionOpen ? (
                    <ChevronUp className="w-5 h-5 text-[#8A817C]" />
                  ) : (
                    <ChevronDown className="w-5 h-5 text-[#8A817C]" />
                  )}
                </button>

                {isNutritionOpen && (
                  <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 mt-5 pt-4 border-t border-[#DCD8D2]/60 dark:border-[#3D3934] animate-in fade-in">
                    <div className="p-3 rounded-xl bg-[#FAF9F6] dark:bg-[#201D1B] text-center">
                      <p className="text-xs text-[#8A817C]">Calories</p>
                      <p className="text-lg font-bold text-[#463F3A] dark:text-[#F5F3EF]">
                        {recipe.nutrition.calories} kcal
                      </p>
                    </div>
                    <div className="p-3 rounded-xl bg-[#FAF9F6] dark:bg-[#201D1B] text-center">
                      <p className="text-xs text-[#8A817C]">Chất đạm (Protein)</p>
                      <p className="text-lg font-bold text-[#463F3A] dark:text-[#F5F3EF]">
                        {recipe.nutrition.protein} g
                      </p>
                    </div>
                    <div className="p-3 rounded-xl bg-[#FAF9F6] dark:bg-[#201D1B] text-center">
                      <p className="text-xs text-[#8A817C]">Tinh bột (Carbs)</p>
                      <p className="text-lg font-bold text-[#463F3A] dark:text-[#F5F3EF]">
                        {recipe.nutrition.carbs} g
                      </p>
                    </div>
                    <div className="p-3 rounded-xl bg-[#FAF9F6] dark:bg-[#201D1B] text-center">
                      <p className="text-xs text-[#8A817C]">Chất béo (Fat)</p>
                      <p className="text-lg font-bold text-[#463F3A] dark:text-[#F5F3EF]">
                        {recipe.nutrition.fat} g
                      </p>
                    </div>
                  </div>
                )}
              </div>
            </section>
          )}

          {/* Ingredients with Servings Adjuster */}
          <section className="double-bezel">
            <div className="double-bezel-inner p-6 sm:p-8 flex flex-col gap-6">
              <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-4 border-b border-[#DCD8D2]/60 dark:border-[#3D3934]">
                <div>
                  <h2 className="font-serif text-2xl font-bold text-[#463F3A] dark:text-[#F5F3EF]">
                    Nguyên liệu chuẩn bị
                  </h2>
                  <p className="text-xs text-[#8A817C] mt-0.5">
                    Nhấp vào nguyên liệu để đánh dấu đã mua hoặc chuẩn bị xong
                  </p>
                </div>

                {/* Servings Adjuster */}
                <div className="flex items-center gap-2 p-1 rounded-full bg-[#FAF9F6] dark:bg-[#201D1B] border border-[#DCD8D2] dark:border-[#3D3934]">
                  <button
                    onClick={() => setServingsMultiplier(Math.max(0.5, servingsMultiplier - 0.5))}
                    aria-label="Giảm khẩu phần"
                    className="w-7 h-7 rounded-full bg-white dark:bg-[#2D2925] flex items-center justify-center text-[#463F3A] dark:text-white shadow-xs hover:bg-[#F4F3EE] transition-colors cursor-pointer"
                  >
                    <Minus className="w-3.5 h-3.5" />
                  </button>
                  <span className="text-xs font-bold px-2 text-[#463F3A] dark:text-white">
                    {Math.round(recipe.servings * servingsMultiplier)} người
                  </span>
                  <button
                    onClick={() => setServingsMultiplier(servingsMultiplier + 0.5)}
                    aria-label="Tăng khẩu phần"
                    className="w-7 h-7 rounded-full bg-white dark:bg-[#2D2925] flex items-center justify-center text-[#463F3A] dark:text-white shadow-xs hover:bg-[#F4F3EE] transition-colors cursor-pointer"
                  >
                    <Plus className="w-3.5 h-3.5" />
                  </button>
                </div>
              </div>

              {/* Ingredient List */}
              <ul className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                {recipe.ingredients.map((ing) => {
                  const isChecked = checkedIngredients[ing.id];
                  // If amount is numeric, scale it
                  const numAmount = parseFloat(ing.amount);
                  const displayAmount = !isNaN(numAmount)
                    ? `${(numAmount * servingsMultiplier).toFixed(
                        numAmount * servingsMultiplier % 1 === 0 ? 0 : 1
                      )}`
                    : ing.amount;

                  return (
                    <li
                      key={ing.id}
                      onClick={() => toggleIngredient(ing.id)}
                      className={`flex items-center gap-3 p-3 rounded-xl border transition-all cursor-pointer select-none ${
                        isChecked
                          ? "bg-[#FAF9F6] dark:bg-[#201D1B] border-[#DCD8D2]/50 opacity-60 line-through"
                          : "bg-white dark:bg-[#24211E] border-[#DCD8D2] dark:border-[#3D3934] hover:border-[#C98F7D]"
                      }`}
                    >
                      <div
                        className={`w-5 h-5 rounded-md flex items-center justify-center shrink-0 transition-colors ${
                          isChecked
                            ? "bg-[#6B8E6B] text-white"
                            : "border border-[#8A817C]/40 text-transparent"
                        }`}
                      >
                        <CheckCircle2 className="w-4 h-4" />
                      </div>
                      <span className="text-xs sm:text-sm text-[#463F3A] dark:text-[#EAE6DF] font-medium flex-1">
                        <strong className="text-[#C98F7D] font-bold">
                          {displayAmount} {ing.unit}
                        </strong>{" "}
                        {ing.name}
                      </span>
                    </li>
                  );
                })}
              </ul>
            </div>
          </section>

          {/* Step-by-Step Instructions */}
          <section className="flex flex-col gap-6">
            <h2 className="font-serif text-2xl font-bold text-[#463F3A] dark:text-[#F5F3EF]">
              Các bước thực hiện
            </h2>

            <div className="flex flex-col gap-6">
              {recipe.steps.map((step) => (
                <div key={step.id} className="double-bezel">
                  <div className="double-bezel-inner p-6 flex flex-col gap-4">
                    <div className="flex items-center gap-3">
                      <span className="w-8 h-8 rounded-full bg-[#E0AFA0] text-[#60413A] font-serif font-bold text-sm flex items-center justify-center shrink-0">
                        {step.stepNumber}
                      </span>
                      <h3 className="font-serif text-base font-bold text-[#463F3A] dark:text-[#F5F3EF]">
                        Bước {step.stepNumber}
                      </h3>
                    </div>

                    <p className="text-sm sm:text-base text-[#463F3A] dark:text-[#EAE6DF] leading-relaxed pl-1">
                      {step.description}
                    </p>

                    {step.imageUrl && (
                      <div className="relative aspect-video max-w-xl rounded-xl overflow-hidden mt-2 bg-[#EFECE6]">
                        <Image
                          src={step.imageUrl}
                          alt={`Bước ${step.stepNumber}`}
                          fill
                          className="object-cover"
                        />
                      </div>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </section>
        </div>

        {/* Sticky Aside / Related Column */}
        <div className="lg:col-span-4 flex flex-col gap-8">
          <div className="sticky top-24 flex flex-col gap-6">
            <h3 className="font-serif text-xl font-bold text-[#463F3A] dark:text-[#F5F3EF] flex items-center gap-2">
              <Sparkles className="w-4 h-4 text-[#C98F7D]" />
              <span>Gợi ý cùng chuyên mục</span>
            </h3>

            <div className="flex flex-col gap-4">
              {displayRelated.map((rel) => (
                <RecipeCard key={rel.id} recipe={rel} viewMode="grid" />
              ))}
            </div>
          </div>
        </div>
      </div>

      {/* Delete Confirmation Modal */}
      <Modal
        isOpen={isDeleteModalOpen}
        onClose={() => setIsDeleteModalOpen(false)}
        title="Xác nhận xóa công thức"
        size="sm"
        footer={
          <>
            <Button variant="secondary" size="sm" onClick={() => setIsDeleteModalOpen(false)}>
              Hủy bỏ
            </Button>
            <Button variant="destructive" size="sm" onClick={handleDelete}>
              Xác nhận xóa
            </Button>
          </>
        }
      >
        <p className="text-xs text-[#8A817C] leading-relaxed">
          Bạn có chắc chắn muốn xóa công thức <strong>{recipe.title}</strong>? Thao tác này không thể hoàn tác.
        </p>
      </Modal>
    </div>
  );
};
