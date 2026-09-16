"use client";

import React, { useState } from "react";
import Image from "next/image";
import Link from "next/link";
import { Heart, Users, MoreVertical, Edit3, Trash2, Globe, Archive } from "lucide-react";
import { Recipe } from "../../lib/types";
import { Badge } from "../ui/Badge";
import { useApp } from "../../context/AppContext";

export interface RecipeCardProps {
  recipe: Recipe;
  viewMode?: "grid" | "list";
  showOwnerActions?: boolean;
}

export const RecipeCard: React.FC<RecipeCardProps> = ({
  recipe,
  viewMode = "grid",
  showOwnerActions = false,
}) => {
  const { favorites, toggleFavorite, currentUser, deleteRecipe, publishRecipe, archiveRecipe } = useApp();
  const [isActionsOpen, setIsActionsOpen] = useState(false);

  const isFav = favorites.includes(recipe.id);
  const isOwner = currentUser?.id === recipe.author.id;
  const primaryImage =
    recipe.images.find((img) => img.isPrimary)?.url ||
    recipe.images[0]?.url ||
    "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=800&auto=format&fit=crop&q=80";

  const totalTime = recipe.prepTime + recipe.cookTime;

  if (viewMode === "list") {
    return (
      <div className="double-bezel group transition-all duration-300 hover:-translate-y-0.5">
        <div className="double-bezel-inner p-3 sm:p-4 flex flex-col sm:flex-row gap-4 sm:gap-6 items-center">
          {/* Image */}
          <Link
            href={`/recipes/${recipe.slug}`}
            className="w-full sm:w-60 aspect-video rounded-xl overflow-hidden relative shrink-0"
          >
            <Image
              src={primaryImage}
              alt={recipe.title}
              fill
              className="object-cover group-hover:scale-105 transition-transform duration-500"
            />
            {/* Status Pill for non-published */}
            {recipe.status !== "Published" && (
              <div className="absolute top-2.5 left-2.5">
                <Badge status={recipe.status} size="sm" />
              </div>
            )}
          </Link>

          {/* Content */}
          <div className="flex-1 flex flex-col justify-between w-full">
            <div>
              <div className="flex items-center justify-between gap-2 mb-2">
                <div className="flex items-center gap-1.5 flex-wrap">
                  <Badge variant="category">{recipe.category.name}</Badge>
                  <Badge variant="difficulty" difficulty={recipe.difficultyLevel} />
                  <Badge variant="time">{totalTime} phút</Badge>
                </div>
                <button
                  onClick={(e) => {
                    e.preventDefault();
                    toggleFavorite(recipe.id);
                  }}
                  aria-label={isFav ? "Bỏ yêu thích" : "Yêu thích"}
                  className="p-1.5 rounded-full bg-[#FAF9F6] dark:bg-[#2F2B27] text-[#8A817C] hover:text-[#B85C5C] transition-colors cursor-pointer"
                >
                  <Heart
                    className={`w-4 h-4 ${
                      isFav ? "fill-[#B85C5C] text-[#B85C5C]" : "text-[#8A817C]"
                    }`}
                  />
                </button>
              </div>

              <Link href={`/recipes/${recipe.slug}`} className="group-hover:text-[#C98F7D] transition-colors">
                <h3 className="font-serif text-lg font-bold text-[#463F3A] dark:text-[#F5F3EF] leading-snug mb-1.5 line-clamp-1">
                  {recipe.title}
                </h3>
              </Link>
              <p className="text-xs text-[#8A817C] dark:text-[#A8A29E] line-clamp-2 leading-relaxed">
                {recipe.description}
              </p>
            </div>

            {/* Author */}
            <div className="flex items-center justify-between pt-3 mt-3 border-t border-[#DCD8D2]/60 dark:border-[#3D3934]">
              <div className="flex items-center gap-2">
                <div className="w-6 h-6 rounded-full overflow-hidden relative">
                  <Image
                    src={recipe.author.avatarUrl}
                    alt={recipe.author.displayName}
                    fill
                    className="object-cover"
                  />
                </div>
                <span className="text-xs font-medium text-[#463F3A] dark:text-[#EAE6DF]">
                  {recipe.author.displayName}
                </span>
              </div>
              <div className="flex items-center gap-3 text-xs text-[#8A817C]">
                <span className="flex items-center gap-1">
                  <Users className="w-3.5 h-3.5" />
                  {recipe.servings} phần
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="double-bezel group transition-all duration-300 hover:-translate-y-1">
      <div className="double-bezel-inner p-3.5 flex flex-col h-full">
        {/* Image Container with 16:9 ratio */}
        <div className="relative aspect-video w-full rounded-xl overflow-hidden mb-3.5 bg-[#EFECE6] shrink-0">
          <Link href={`/recipes/${recipe.slug}`} className="block w-full h-full relative">
            <Image
              src={primaryImage}
              alt={recipe.title}
              fill
              sizes="(max-width: 768px) 100vw, (max-width: 1200px) 50vw, 33vw"
              className="object-cover group-hover:scale-105 transition-transform duration-500 ease-out"
            />
          </Link>

          {/* Top Badges Overlay */}
          <div className="absolute top-2.5 left-2.5 flex items-center gap-1.5 z-10 pointer-events-none">
            <Badge variant="category" size="sm">
              {recipe.category.name}
            </Badge>
            {recipe.status !== "Published" && (
              <Badge status={recipe.status} size="sm" />
            )}
          </div>

          {/* Favorite Button */}
          <button
            onClick={(e) => {
              e.preventDefault();
              toggleFavorite(recipe.id);
            }}
            aria-label={isFav ? "Bỏ yêu thích" : "Yêu thích"}
            className="absolute top-2.5 right-2.5 z-10 p-2 rounded-full bg-white/90 dark:bg-[#181614]/90 backdrop-blur-xs text-[#8A817C] hover:text-[#B85C5C] shadow-sm transition-transform active:scale-90 cursor-pointer"
          >
            <Heart
              className={`w-4 h-4 transition-colors ${
                isFav ? "fill-[#B85C5C] text-[#B85C5C]" : "text-[#8A817C]"
              }`}
            />
          </button>
        </div>

        {/* Card Body */}
        <div className="flex-1 flex flex-col justify-between">
          <div>
            {/* Meta Tags: Difficulty & Prep Time */}
            <div className="flex items-center gap-2 mb-2 flex-wrap">
              <Badge variant="difficulty" difficulty={recipe.difficultyLevel} size="sm" />
              <Badge variant="time" size="sm">
                {totalTime} phút
              </Badge>
              <span className="text-[11px] text-[#8A817C] flex items-center gap-1 ml-auto">
                <Users className="w-3 h-3" />
                {recipe.servings} người
              </span>
            </div>

            {/* Title */}
            <Link href={`/recipes/${recipe.slug}`}>
              <h3 className="font-serif text-base sm:text-lg font-bold text-[#463F3A] dark:text-[#F5F3EF] leading-snug line-clamp-2 group-hover:text-[#C98F7D] transition-colors mb-1.5">
                {recipe.title}
              </h3>
            </Link>

            {/* Description Snippet */}
            <p className="text-xs text-[#8A817C] dark:text-[#A8A29E] line-clamp-2 leading-relaxed mb-4">
              {recipe.description}
            </p>
          </div>

          {/* Card Footer: Author Info & Owner Actions */}
          <div className="flex items-center justify-between pt-3 border-t border-[#DCD8D2]/60 dark:border-[#3D3934] relative">
            <div className="flex items-center gap-2 min-w-0">
              <div className="w-7 h-7 rounded-full overflow-hidden relative shrink-0 border border-[#E0AFA0]/40">
                <Image
                  src={recipe.author.avatarUrl}
                  alt={recipe.author.displayName}
                  fill
                  className="object-cover"
                />
              </div>
              <span className="text-xs font-medium text-[#463F3A] dark:text-[#EAE6DF] truncate">
                {recipe.author.displayName}
              </span>
            </div>

            {/* Owner Actions Dropdown */}
            {(showOwnerActions || isOwner) && (
              <div className="relative">
                <button
                  onClick={() => setIsActionsOpen(!isActionsOpen)}
                  aria-label="Tùy chọn công thức"
                  className="p-1 rounded-md text-[#8A817C] hover:text-[#463F3A] hover:bg-[#F4F3EE] dark:hover:bg-[#2F2B27] transition-colors cursor-pointer"
                >
                  <MoreVertical className="w-4 h-4" />
                </button>

                {isActionsOpen && (
                  <div className="absolute right-0 bottom-8 w-44 bg-white dark:bg-[#24211E] rounded-xl shadow-lg border border-[#DCD8D2] dark:border-[#3D3934] py-1.5 z-20 animate-in fade-in">
                    <Link
                      href={`/recipes/${recipe.slug}/edit`}
                      onClick={() => setIsActionsOpen(false)}
                      className="flex items-center gap-2 px-3 py-1.5 text-xs text-[#463F3A] dark:text-[#EAE6DF] hover:bg-[#F4F3EE] dark:hover:bg-[#2F2B27]"
                    >
                      <Edit3 className="w-3.5 h-3.5" />
                      <span>Chỉnh sửa</span>
                    </Link>
                    {recipe.status !== "Published" ? (
                      <button
                        onClick={() => {
                          setIsActionsOpen(false);
                          publishRecipe(recipe.id);
                        }}
                        className="w-full flex items-center gap-2 px-3 py-1.5 text-xs text-[#6B8E6B] hover:bg-[#F4F3EE] dark:hover:bg-[#2F2B27]"
                      >
                        <Globe className="w-3.5 h-3.5" />
                        <span>Xuất bản</span>
                      </button>
                    ) : (
                      <button
                        onClick={() => {
                          setIsActionsOpen(false);
                          archiveRecipe(recipe.id);
                        }}
                        className="w-full flex items-center gap-2 px-3 py-1.5 text-xs text-[#C89B3C] hover:bg-[#F4F3EE] dark:hover:bg-[#2F2B27]"
                      >
                        <Archive className="w-3.5 h-3.5" />
                        <span>Lưu trữ</span>
                      </button>
                    )}
                    <button
                      onClick={() => {
                        setIsActionsOpen(false);
                        deleteRecipe(recipe.id);
                      }}
                      className="w-full flex items-center gap-2 px-3 py-1.5 text-xs text-[#B85C5C] hover:bg-[#F9EBEB]"
                    >
                      <Trash2 className="w-3.5 h-3.5" />
                      <span>Xóa công thức</span>
                    </button>
                  </div>
                )}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};
