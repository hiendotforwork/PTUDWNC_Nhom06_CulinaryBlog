import { RecipeCardSkeleton } from "./components/ui/Skeleton";

export default function Loading() {
  return (
    <div
      role="status"
      aria-label="Đang tải trang"
      className="mx-auto grid max-w-7xl grid-cols-1 gap-6 px-4 py-12 sm:grid-cols-2 sm:px-6 lg:grid-cols-3 lg:px-8"
    >
      {Array.from({ length: 6 }).map((_, index) => (
        <RecipeCardSkeleton key={index} />
      ))}
      <span className="sr-only">Đang tải nội dung...</span>
    </div>
  );
}
