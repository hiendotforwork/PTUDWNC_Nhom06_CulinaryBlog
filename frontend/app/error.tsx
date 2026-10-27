"use client";

import { useEffect } from "react";
import { ErrorState } from "./components/ui/ErrorState";

interface ErrorPageProps {
  error: Error & { digest?: string };
  reset: () => void;
}

export default function ErrorPage({ error, reset }: ErrorPageProps) {
  useEffect(() => {
    console.error(error);
  }, [error]);

  return (
    <div className="mx-auto flex min-h-[60dvh] max-w-7xl items-center justify-center px-4 py-12 sm:px-6 lg:px-8">
      <ErrorState
        title="Không thể tải trang"
        description="Đã có lỗi ngoài dự kiến. Vui lòng thử lại."
        onRetry={reset}
      />
    </div>
  );
}
