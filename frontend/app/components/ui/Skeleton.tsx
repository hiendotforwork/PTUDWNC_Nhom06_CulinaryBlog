import React from "react";
import { clsx } from "clsx";
import { twMerge } from "tailwind-merge";

export type SkeletonProps = React.HTMLAttributes<HTMLDivElement>;

export const Skeleton: React.FC<SkeletonProps> = ({ className, ...props }) => {
  return (
    <div
      className={twMerge(
        clsx("animate-pulse bg-[#EFECE6] dark:bg-[#332F2B] rounded-lg", className)
      )}
      {...props}
    />
  );
};

export const RecipeCardSkeleton: React.FC = () => {
  return (
    <div className="double-bezel overflow-hidden">
      <div className="double-bezel-inner p-4 flex flex-col gap-3">
        {/* Image placeholder 16:9 */}
        <Skeleton className="w-full aspect-video rounded-xl" />

        {/* Badges placeholder */}
        <div className="flex gap-2">
          <Skeleton className="w-16 h-5 rounded-full" />
          <Skeleton className="w-14 h-5 rounded-full" />
        </div>

        {/* Title placeholder */}
        <Skeleton className="w-5/6 h-6 rounded-md" />
        <Skeleton className="w-3/4 h-4 rounded-md" />

        {/* Meta / Author */}
        <div className="flex items-center gap-3 pt-3 border-t border-[#DCD8D2]/50 mt-1">
          <Skeleton className="w-8 h-8 rounded-full" />
          <div className="flex flex-col gap-1.5 flex-1">
            <Skeleton className="w-24 h-3.5 rounded" />
            <Skeleton className="w-16 h-2.5 rounded" />
          </div>
        </div>
      </div>
    </div>
  );
};
