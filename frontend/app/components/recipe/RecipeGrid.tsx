"use client";

import React from "react";
import { Recipe } from "../../lib/types";
import { RecipeCard } from "./RecipeCard";
import { RecipeCardSkeleton } from "../ui/Skeleton";
import { EmptyState } from "./EmptyState";

export interface RecipeGridProps {
  recipes: Recipe[];
  isLoading?: boolean;
  viewMode?: "grid" | "list";
  emptyTitle?: string;
  emptyDescription?: string;
  onClearFilters?: () => void;
  showOwnerActions?: boolean;
}

export const RecipeGrid: React.FC<RecipeGridProps> = ({
  recipes,
  isLoading = false,
  viewMode = "grid",
  emptyTitle,
  emptyDescription,
  onClearFilters,
  showOwnerActions = false,
}) => {
  if (isLoading) {
    return (
      <div
        className={
          viewMode === "grid"
            ? "grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6"
            : "flex flex-col gap-4"
        }
      >
        {Array.from({ length: 6 }).map((_, i) => (
          <RecipeCardSkeleton key={i} />
        ))}
      </div>
    );
  }

  if (recipes.length === 0) {
    return (
      <EmptyState
        title={emptyTitle}
        description={emptyDescription}
        onActionClick={onClearFilters}
        type={onClearFilters ? "search" : "recipes"}
      />
    );
  }

  if (viewMode === "list") {
    return (
      <div className="flex flex-col gap-4">
        {recipes.map((recipe) => (
          <RecipeCard
            key={recipe.id}
            recipe={recipe}
            viewMode="list"
            showOwnerActions={showOwnerActions}
          />
        ))}
      </div>
    );
  }

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
      {recipes.map((recipe) => (
        <RecipeCard
          key={recipe.id}
          recipe={recipe}
          viewMode="grid"
          showOwnerActions={showOwnerActions}
        />
      ))}
    </div>
  );
};
