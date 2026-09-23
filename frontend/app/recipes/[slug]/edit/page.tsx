"use client";

import React, { use, useEffect, useState } from "react";
import Link from "next/link";
import { notFound } from "next/navigation";
import { ChevronRight } from "lucide-react";
import { RecipeForm } from "../../../components/recipe/RecipeForm";
import { getRecipe } from "../../../lib/api";
import { Recipe } from "../../../lib/types";

interface PageProps {
  params: Promise<{ slug: string }>;
}

export default function EditRecipePage({ params }: PageProps) {
  const { slug } = use(params);
  const [recipe, setRecipe] = useState<Recipe | null>();

  useEffect(() => {
    getRecipe(slug).then(setRecipe).catch(() => setRecipe(null));
  }, [slug]);

  if (recipe === undefined) {
    return <div className="max-w-7xl mx-auto px-4 py-16 text-center text-sm text-[#8A817C]">Đang tải công thức...</div>;
  }
  if (recipe === null) notFound();

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
      <nav className="flex items-center gap-2 text-xs text-[#8A817C] mb-4">
        <Link href="/">Trang chủ</Link>
        <ChevronRight className="w-3.5 h-3.5" />
        <Link href={`/recipes/${recipe.slug}`}>{recipe.title}</Link>
        <ChevronRight className="w-3.5 h-3.5" />
        <span>Chỉnh sửa</span>
      </nav>
      <RecipeForm initialData={recipe} isEditing />
    </div>
  );
}