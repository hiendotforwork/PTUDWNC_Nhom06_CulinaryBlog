import React from "react";
import { clsx } from "clsx";
import { twMerge } from "tailwind-merge";
import { DifficultyLevel, RecipeStatus } from "../../lib/types";
import { Clock, ChefHat, Sparkles } from "lucide-react";

export interface BadgeProps extends React.HTMLAttributes<HTMLSpanElement> {
  variant?: "category" | "difficulty" | "time" | "status" | "default";
  difficulty?: DifficultyLevel;
  status?: RecipeStatus;
  size?: "sm" | "md";
}

export const Badge: React.FC<BadgeProps> = ({
  className,
  variant = "default",
  difficulty,
  status,
  size = "sm",
  children,
  ...props
}) => {
  const sizeStyles = {
    sm: "text-[11px] px-2.5 py-0.5 font-medium rounded-full gap-1",
    md: "text-xs px-3 py-1 font-semibold rounded-full gap-1.5",
  };

  let variantStyle = "bg-[#FAF9F6] text-[#463F3A] border border-[#DCD8D2]";

  if (variant === "category") {
    variantStyle = "bg-[#F3DDD8]/50 text-[#8A5548] border border-[#E0AFA0]/40";
  } else if (variant === "time") {
    variantStyle = "bg-[#EBF1F5] text-[#3F5B6E] border border-[#BACCD9]";
  } else if (variant === "difficulty" && difficulty) {
    if (difficulty === "Easy") {
      variantStyle = "bg-[#E8EFE8] text-[#385938] border border-[#B3CFB3]";
    } else if (difficulty === "Medium") {
      variantStyle = "bg-[#FBF4E4] text-[#876214] border border-[#E8D19D]";
    } else if (difficulty === "Hard") {
      variantStyle = "bg-[#F9EBEB] text-[#8A3737] border border-[#E4B5B5]";
    }
  } else if (variant === "status" && status) {
    if (status === "Published") {
      variantStyle = "bg-[#E8EFE8] text-[#385938] border border-[#B3CFB3]";
    } else if (status === "Draft") {
      variantStyle = "bg-[#EFECE6] text-[#6B6560] border border-[#DCD8D2]";
    } else if (status === "Archived") {
      variantStyle = "bg-[#EBF1F5] text-[#4A6070] border border-[#BACCD9]";
    }
  }

  const renderIcon = () => {
    if (variant === "time") return <Clock className="w-3 h-3 shrink-0" />;
    if (variant === "difficulty") return <ChefHat className="w-3 h-3 shrink-0" />;
    if (variant === "category") return <Sparkles className="w-2.5 h-2.5 shrink-0 opacity-75" />;
    return null;
  };

  const getLabel = () => {
    if (children) return children;
    if (difficulty) {
      if (difficulty === "Easy") return "Dễ";
      if (difficulty === "Medium") return "Trung bình";
      if (difficulty === "Hard") return "Kỳ công";
    }
    if (status) {
      if (status === "Published") return "Đã xuất bản";
      if (status === "Draft") return "Bản nháp";
      if (status === "Archived") return "Lưu trữ";
    }
    return null;
  };

  return (
    <span
      className={twMerge(
        clsx(
          "inline-flex items-center select-none shrink-0 transition-colors",
          sizeStyles[size],
          variantStyle,
          className
        )
      )}
      {...props}
    >
      {renderIcon()}
      <span>{getLabel()}</span>
    </span>
  );
};
