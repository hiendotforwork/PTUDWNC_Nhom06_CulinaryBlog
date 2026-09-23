// Tệp này là trang xem chi tiết công thức theo slug.
// Chức năng: tải dữ liệu và render chi tiết công thức (RecipePage).

"use client";

import React, { use, useEffect, useState } from "react";
import { notFound } from "next/navigation";
import { RecipeDetailView } from "../../components/recipe/RecipeDetailView";
import { getRecipe } from "../../lib/api";
import { Recipe } from "../../lib/types";

interface PageProps {
  params: Promise<{ slug: string }>;
}

// Chức năng: tải công thức theo tham số URL và hiển thị RecipeDetailView.
// Input: params chứa slug. Output: React component trang chi tiết hoặc trang 404.
export default function RecipePage({ params }: PageProps) {
  const { slug } = use(params);
  const [recipe, setRecipe] = useState<Recipe | null>();

  useEffect(() => {
    getRecipe(slug).then(setRecipe).catch(() => setRecipe(null));
  }, [slug]);

  if (recipe === undefined) {
    return <div className="max-w-7xl mx-auto px-4 py-16 text-center text-sm text-[#8A817C]">Đang tải công thức...</div>;
  }
  if (recipe === null) notFound();

  return <RecipeDetailView recipe={recipe} />;
}
