"use client";

import React, { useRef } from "react";
import { Category } from "../../lib/types";
import { Utensils, Sunrise, Sun, Moon, Cake, Coffee, Cookie, ChevronLeft, ChevronRight } from "lucide-react";

export interface CategoryStripProps {
  categories: Category[];
  selectedCategory: string;
  onSelectCategory: (slug: string) => void;
}

export const CategoryStrip: React.FC<CategoryStripProps> = ({
  categories,
  selectedCategory,
  onSelectCategory,
}) => {
  const scrollContainerRef = useRef<HTMLDivElement>(null);

  const getIcon = (iconName: string) => {
    switch (iconName) {
      case "Sunrise":
        return <Sunrise className="w-4 h-4" />;
      case "Sun":
        return <Sun className="w-4 h-4" />;
      case "Moon":
        return <Moon className="w-4 h-4" />;
      case "Cake":
        return <Cake className="w-4 h-4" />;
      case "Coffee":
        return <Coffee className="w-4 h-4" />;
      case "Cookie":
        return <Cookie className="w-4 h-4" />;
      default:
        return <Utensils className="w-4 h-4" />;
    }
  };

  const scroll = (direction: "left" | "right") => {
    if (scrollContainerRef.current) {
      const offset = direction === "left" ? -240 : 240;
      scrollContainerRef.current.scrollBy({ left: offset, behavior: "smooth" });
    }
  };

  return (
    <div className="w-full relative py-2 select-none group">
      {/* Scroll Left Button */}
      <button
        onClick={() => scroll("left")}
        aria-label="Cuộn sang trái"
        className="hidden sm:flex absolute -left-3 top-1/2 -translate-y-1/2 z-10 w-8 h-8 rounded-full bg-white dark:bg-[#24211E] shadow-md border border-[#DCD8D2] dark:border-[#3D3934] items-center justify-center text-[#463F3A] dark:text-[#EAE6DF] hover:bg-[#F4F3EE] opacity-0 group-hover:opacity-100 transition-opacity cursor-pointer"
      >
        <ChevronLeft className="w-4 h-4" />
      </button>

      {/* Scrollable Container */}
      <div
        ref={scrollContainerRef}
        className="flex items-center gap-2.5 overflow-x-auto no-scrollbar py-1 px-1 scroll-smooth"
      >
        {categories.map((category) => {
          const isActive = selectedCategory === category.slug;
          return (
            <button
              key={category.id}
              onClick={() => onSelectCategory(category.slug)}
              className={`flex items-center gap-2 px-4 py-2.5 rounded-full text-xs font-semibold whitespace-nowrap transition-all duration-200 cursor-pointer ${
                isActive
                  ? "bg-[#C98F7D] text-white shadow-sm ring-2 ring-[#C98F7D]/30"
                  : "bg-white dark:bg-[#24211E] text-[#463F3A] dark:text-[#EAE6DF] border border-[#DCD8D2] dark:border-[#3D3934] hover:border-[#C98F7D] hover:bg-[#FDF8F6] dark:hover:bg-[#2E2A26]"
              }`}
            >
              <span className={isActive ? "text-white" : "text-[#8A817C]"}>
                {getIcon(category.icon)}
              </span>
              <span>{category.name}</span>
              {category.recipeCount !== undefined && (
                <span
                  className={`text-[10px] px-1.5 py-0.2 rounded-full ${
                    isActive
                      ? "bg-white/20 text-white"
                      : "bg-[#F4F3EE] dark:bg-[#332F2B] text-[#8A817C]"
                  }`}
                >
                  {category.recipeCount}
                </span>
              )}
            </button>
          );
        })}
      </div>

      {/* Scroll Right Button */}
      <button
        onClick={() => scroll("right")}
        aria-label="Cuộn sang phải"
        className="hidden sm:flex absolute -right-3 top-1/2 -translate-y-1/2 z-10 w-8 h-8 rounded-full bg-white dark:bg-[#24211E] shadow-md border border-[#DCD8D2] dark:border-[#3D3934] items-center justify-center text-[#463F3A] dark:text-[#EAE6DF] hover:bg-[#F4F3EE] opacity-0 group-hover:opacity-100 transition-opacity cursor-pointer"
      >
        <ChevronRight className="w-4 h-4" />
      </button>
    </div>
  );
};
