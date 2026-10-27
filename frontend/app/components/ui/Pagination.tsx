import { ChevronLeft, ChevronRight } from "lucide-react";

interface PaginationProps {
  currentPage: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  disabled?: boolean;
}

export function Pagination({
  currentPage,
  totalPages,
  onPageChange,
  disabled = false,
}: PaginationProps) {
  if (totalPages <= 1) return null;

  const visiblePages = Array.from(
    new Set(
      [1, currentPage - 1, currentPage, currentPage + 1, totalPages].filter(
        (page) => page >= 1 && page <= totalPages,
      ),
    ),
  ).sort((left, right) => left - right);

  return (
    <nav
      aria-label="Phân trang công thức"
      className="flex flex-wrap items-center justify-center gap-2 pt-6"
    >
      <button
        type="button"
        onClick={() => onPageChange(currentPage - 1)}
        disabled={disabled || currentPage <= 1}
        aria-label="Trang trước"
        className="flex h-9 w-9 items-center justify-center rounded-full border border-[#DCD8D2] bg-white text-[#463F3A] transition-colors hover:bg-[#F4F3EE] disabled:cursor-not-allowed disabled:opacity-40 dark:border-[#3D3934] dark:bg-[#24211E] dark:text-[#EAE6DF]"
      >
        <ChevronLeft className="h-4 w-4" />
      </button>

      {visiblePages.map((page, index) => {
        const previousPage = visiblePages[index - 1];
        const hasGap = previousPage !== undefined && page - previousPage > 1;

        return (
          <span key={page} className="contents">
            {hasGap && (
              <span aria-hidden="true" className="px-1 text-sm text-[#8A817C]">
                …
              </span>
            )}
            <button
              type="button"
              onClick={() => onPageChange(page)}
              disabled={disabled}
              aria-current={currentPage === page ? "page" : undefined}
              aria-label={`Trang ${page}`}
              className={`h-9 min-w-9 rounded-full px-2 text-xs font-semibold transition-all disabled:cursor-not-allowed disabled:opacity-50 ${
                currentPage === page
                  ? "bg-[#C98F7D] text-white shadow-sm ring-2 ring-[#C98F7D]/30"
                  : "border border-[#DCD8D2] bg-white text-[#463F3A] hover:border-[#C98F7D] dark:border-[#3D3934] dark:bg-[#24211E] dark:text-[#EAE6DF]"
              }`}
            >
              {page}
            </button>
          </span>
        );
      })}

      <button
        type="button"
        onClick={() => onPageChange(currentPage + 1)}
        disabled={disabled || currentPage >= totalPages}
        aria-label="Trang sau"
        className="flex h-9 w-9 items-center justify-center rounded-full border border-[#DCD8D2] bg-white text-[#463F3A] transition-colors hover:bg-[#F4F3EE] disabled:cursor-not-allowed disabled:opacity-40 dark:border-[#3D3934] dark:bg-[#24211E] dark:text-[#EAE6DF]"
      >
        <ChevronRight className="h-4 w-4" />
      </button>
    </nav>
  );
}
