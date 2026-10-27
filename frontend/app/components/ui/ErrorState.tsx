import { RotateCcw } from "lucide-react";
import { Button } from "./Button";

interface ErrorStateProps {
  title?: string;
  description: string;
  onRetry?: () => void;
}

export function ErrorState({
  title = "Đã xảy ra lỗi",
  description,
  onRetry,
}: ErrorStateProps) {
  return (
    <section
      role="alert"
      className="mx-auto my-12 max-w-xl rounded-2xl border border-[#B85C5C]/30 bg-white p-8 text-center dark:bg-[#24211E]"
    >
      <h1 className="font-serif text-xl font-bold text-[#463F3A] dark:text-[#F5F3EF]">
        {title}
      </h1>
      <p className="mt-2 text-sm text-[#8A817C] dark:text-[#A8A29E]">
        {description}
      </p>
      {onRetry && (
        <Button
          type="button"
          onClick={onRetry}
          className="mt-5"
          leftIcon={<RotateCcw className="h-4 w-4" />}
        >
          Thử lại
        </Button>
      )}
    </section>
  );
}
