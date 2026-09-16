import React from "react";
import Link from "next/link";
import { UtensilsCrossed, Search, PlusCircle, RotateCcw } from "lucide-react";
import { Button } from "../ui/Button";

export interface EmptyStateProps {
  type?: "recipes" | "search" | "favorites";
  title?: string;
  description?: string;
  actionText?: string;
  actionHref?: string;
  onActionClick?: () => void;
}

export const EmptyState: React.FC<EmptyStateProps> = ({
  type = "recipes",
  title,
  description,
  actionText,
  actionHref,
  onActionClick,
}) => {
  let defaultIcon = <UtensilsCrossed className="w-12 h-12 text-[#C98F7D]" />;
  let defaultTitle = "Chưa có công thức nào";
  let defaultDesc = "Hãy tạo công thức đầu tiên của bạn và chia sẻ cùng cộng đồng yêu bếp.";
  let defaultActionText = "Tạo công thức mới";
  let defaultHref = "/recipes/create";

  if (type === "search") {
    defaultIcon = <Search className="w-12 h-12 text-[#8A817C]" />;
    defaultTitle = "Không tìm thấy kết quả phù hợp";
    defaultDesc = "Hãy thử từ khóa khác hoặc xóa bớt các bộ lọc đang áp dụng.";
    defaultActionText = "Xóa bộ lọc";
  } else if (type === "favorites") {
    defaultTitle = "Chưa có công thức yêu thích";
    defaultDesc = "Nhấp vào biểu tượng trái tim trên các công thức bạn muốn lưu lại.";
    defaultActionText = "Khám phá công thức";
    defaultHref = "/";
  }

  return (
    <div className="double-bezel max-w-md mx-auto my-12 text-center">
      <div className="double-bezel-inner p-8 sm:p-10 flex flex-col items-center gap-4">
        <div className="w-20 h-20 rounded-full bg-[#FAF9F6] dark:bg-[#2A2723] border border-[#DCD8D2] dark:border-[#3D3934] flex items-center justify-center shadow-inner">
          {defaultIcon}
        </div>
        <h3 className="font-serif text-xl font-bold text-[#463F3A] dark:text-[#F5F3EF]">
          {title || defaultTitle}
        </h3>
        <p className="text-xs sm:text-sm text-[#8A817C] dark:text-[#A8A29E] leading-relaxed max-w-xs">
          {description || defaultDesc}
        </p>

        <div className="mt-2">
          {onActionClick ? (
            <Button
              variant="primary"
              size="md"
              onClick={onActionClick}
              leftIcon={<RotateCcw className="w-4 h-4" />}
            >
              {actionText || defaultActionText}
            </Button>
          ) : defaultHref ? (
            <Link href={actionHref || defaultHref}>
              <Button
                variant="primary"
                size="md"
                leftIcon={<PlusCircle className="w-4 h-4" />}
              >
                {actionText || defaultActionText}
              </Button>
            </Link>
          ) : null}
        </div>
      </div>
    </div>
  );
};
