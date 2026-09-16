"use client";

import React, { use } from "react";
import { notFound } from "next/navigation";
import { useApp } from "../../context/AppContext";
import { RecipeDetailView } from "../../components/recipe/RecipeDetailView";

interface PageProps {
  params: Promise<{ slug: string }>;
}

export default function RecipePage({ params }: PageProps) {
  const resolvedParams = use(params);
  const { slug } = resolvedParams;
  const { recipes } = useApp();

  const recipe = recipes.find((r) => r.slug === slug || r.id === slug);

  if (!recipe) {
    notFound();
  }

  return <RecipeDetailView recipe={recipe} />;
}
