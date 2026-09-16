"use client";

import React, { use } from "react";
import Link from "next/link";
import { notFound } from "next/navigation";
import { ChevronRight } from "lucide-react";
import { useApp } from "../../../context/AppContext";
import { RecipeForm } from "../../../components/recipe/RecipeForm";

interface PageProps {
  params: Promise<{ slug: string }>;
}

export default function EditRecipePage({ params }: PageProps) {
  const resolvedParams = use(params);
  const { slug } = resolvedParams;
  const { recipes } = useApp();

  const recipe = recipes.find((r) => r.slug === slug || r.id === slug);

  if (!recipe) {
    notFound();
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
      {/* Breadcrumb */}
      <nav className="flex items-center gap-2 text-xs text-[#8A817C] mb-4">
        <Link href="/" className="hover:text-[#C98F7D] transition-colors">
          Trang chủ
        </Link>
        <ChevronRight className="w-3.5 h-3.5" />
        <Link href={`/recipes/${recipe.slug}`} className="hover:text-[#C98F7D] transition-colors">
          {recipe.title}
        </Link>
        <ChevronRight className="w-3.5 h-3.5" />
        <span className="text-[#463F3A] dark:text-[#F5F3EF] font-medium">Chỉnh sửa</span>
      </nav>

      <RecipeForm initialData={recipe} isEditing={true} />
    </div>
  );
}
