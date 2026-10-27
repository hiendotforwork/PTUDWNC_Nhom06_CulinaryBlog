# Culinary Blog - Frontend

Giao diện blog chia sẻ công thức nấu ăn, xây dựng bằng Next.js App Router, React,
TypeScript và Tailwind CSS.

## Chạy ứng dụng

```bash
pnpm install
pnpm dev
```

Mặc định giao diện gọi backend tại `http://localhost:5058`. Có thể cấu hình URL
khác bằng biến môi trường `NEXT_PUBLIC_API_URL`.

Các lệnh kiểm tra và chạy bản production:

```bash
pnpm lint
pnpm build
pnpm start
```

## Cấu trúc giao diện

- `app/lib/api.ts`: gọi API, xử lý phản hồi/lỗi và ánh xạ dữ liệu backend.
- `app/lib/auth.ts` và `app/context/AppContext.tsx`: lưu thông tin xác thực và
  quản lý trạng thái dùng chung của người dùng, công thức, danh mục và thông báo.
- `app/components/ui/`: các thành phần UI tái sử dụng như Button, Input, Toast,
  Skeleton, ErrorState và Pagination.
- `app/components/layout/` và `app/layout.tsx`: Header, Footer và bố cục chung
  cho các trang.
- `app/loading.tsx` và `app/error.tsx`: giao diện tải trang và xử lý lỗi cấp ứng
  dụng.

Tailwind CSS được dùng để thiết kế giao diện; `lucide-react` cung cấp icon,
`clsx` và `tailwind-merge` hỗ trợ ghép class CSS.
