"use client";

import React from "react";
import Link from "next/link";
import { ChevronRight } from "lucide-react";
import { RecipeForm } from "../../components/recipe/RecipeForm";

export default function CreateRecipePage() {
  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
      {/* Breadcrumb */}
      <nav className="flex items-center gap-2 text-xs text-[#8A817C] mb-4">
        <Link href="/" className="hover:text-[#C98F7D] transition-colors">
          Trang chủ
        </Link>
        <ChevronRight className="w-3.5 h-3.5" />
        <span className="text-[#463F3A] dark:text-[#F5F3EF] font-medium">Tạo công thức mới</span>
      </nav>

      <RecipeForm />
    </div>
  );
}
