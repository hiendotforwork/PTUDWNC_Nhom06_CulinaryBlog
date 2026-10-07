# Culinary Blog - Blog Ẩm thực và Nấu ăn

Hệ thống blog ẩm thực cho phép người dùng khám phá, tìm kiếm và xem công thức nấu ăn. Hỗ trợ tác giả xây dựng, quản lý và xuất bản công thức.

## Thông tin thành viên

| MSSV    | Họ và Tên              | GitHub         |
| ------- | ---------------------- | -------------- |
| 2312609 | Nguyễn Ngọc Thanh Hiền | hiendotforwork |
| 2312756 | Nguyễn Hưng Thịnh      | elgthinhnguyen |
| 2312588 | Ngô Văn Chương         | chuong-gif     |
| 2312565 | Nguyễn Văn An          | NguyenAn124    |

## Phân công công việc theo Module chi tiết

**1. Nguyễn Ngọc Thanh Hiền (2312609)**
- **FR-AUTH (Xác thực & Quản lý người dùng):**
  - **FR-AUTH-001:** Đăng ký Tài khoản (User Registration).
  - **FR-AUTH-002:** Đăng nhập bằng Email/Mật khẩu (Local Login).
  - **FR-AUTH-003:** Đăng nhập bằng Google OAuth 2.0.
  - **FR-AUTH-004:** Làm mới Access Token (Token Refresh).
  - **FR-AUTH-005:** Đăng xuất (Logout / Token Revocation).
  - **FR-AUTH-006:** Xem Hồ sơ Cá nhân (View Profile).
  - **FR-AUTH-007:** Cập nhật Hồ sơ Cá nhân (Update Profile).

**2. Nguyễn Hưng Thịnh (2312756)**
- **FR-SRCH (Tìm kiếm & Phân trang):**
  - **FR-SRCH-001:** Tìm kiếm Toàn văn bản (Full-Text Search) với PostgreSQL.
  - **FR-SRCH-002/003/004:** Lọc, Sắp xếp và Phân trang (Paginated + Filtered + Sorted).
- **FR-FILE (Quản lý tệp tin):**
  - Tích hợp MinIO S3-compatible để Upload/Delete tệp tin (ảnh công thức, avatar).
- **FR-JOB:** Module Background Jobs
- **FR-OBS:** Module Quan sát Hệ thống

**3. Ngô Văn Chương (2312588)**
- **FR-RCP (Quản lý công thức nấu ăn):**
  - **FR-RCP-001:** Xem Danh sách Công thức (Paginated + Filtered + Sorted).
  - **FR-RCP-002:** Xem Chi tiết Công thức.
  - **FR-RCP-003:** Tạo Công thức Nấu ăn Mới [Author/Admin].
  - **FR-RCP-004:** Cập nhật Công thức [Author-Owner/Admin].
  - **FR-RCP-005:** Xuất bản / Hủy Xuất bản Công thức.
  - **FR-RCP-006:** Lưu trữ Công thức (Archive).
  - **FR-RCP-007:** Xóa Công thức [Author-Owner/Admin].
  - **FR-RCP-008:** Quản lý Ảnh Công thức (Upload / Set Primary / Delete).
  - **FR-RCP-009:** Quản lý Nguyên liệu (CRUD RecipeIngredient).
  - **FR-RCP-010:** Quản lý Các bước Thực hiện (CRUD RecipeStep).

**4. Nguyễn Văn An (2312565)**
- **FR-CAT (Quản lý danh mục):**
  - **FR-CAT-001:** Xem Danh sách Danh mục.
  - **FR-CAT-002:** Xem Chi tiết Danh mục và Công thức.
  - **FR-CAT-003:** Tạo Danh mục Mới [Admin].
  - **FR-CAT-004:** Cập nhật Danh mục [Admin].
  - **FR-CAT-005:** Xóa Danh mục [Admin].

## Cấu trúc dự án

```
/
├── frontend/              # Next.js 16 + TypeScript + Tailwind CSS
│   └── app/              # App Router
│
├── src/                   # ASP.NET Core Backend
│   ├── CulinaryBlog.API/
│   ├── CulinaryBlog.Application/
│   ├── CulinaryBlog.Domain/
│   └── CulinaryBlog.Infrastructure/
│
└── docs/                  # Tài liệu đặc tả
    └── specs/fr-auth/     # SRS, API contracts, test plans
```

## Tech Stack

### Frontend

- **Next.js 16** (App Router)
- **React 19**
- **TypeScript**
- **Tailwind CSS 4**
- **pnpm**

### Backend

- **ASP.NET Core Web API** (.NET 10)
- **Entity Framework Core 10** + PostgreSQL 16 trong Docker
- **MediatR** (CQRS pattern)
- **Mapster** (object mapping)
- **JWT Authentication**

## Cài đặt và Chạy dự án

### Yêu cầu

- .NET 10 SDK
- Node.js 18+
- pnpm
- Docker Desktop

### Database và Backend

```bash
# Tại thư mục gốc dự án
docker compose up -d --build
```

Lệnh này chạy PostgreSQL, tự áp dụng migration, tạo dữ liệu mẫu và chạy API tại `http://localhost:5058`. Database và ảnh được giữ trong Docker Volume khi dừng container.

### Frontend

```bash
# Di chuyển vào thư mục frontend
cd frontend

# Cài đặt dependencies
pnpm install

# Chạy development server
pnpm dev
```

Frontend sẽ chạy tại `http://localhost:3000`

### Dữ liệu Docker

- PostgreSQL trên máy: `localhost:55432`, database `culinary_blog`, user `culinary`.
- Sao chép `.env.example` thành `.env` nếu muốn đổi mật khẩu phát triển.
- `docker compose down` dừng hệ thống nhưng giữ dữ liệu.
- `docker compose down -v` xóa toàn bộ database và ảnh local để tạo lại từ đầu.
- Mỗi thành viên có dữ liệu riêng trên máy; Git chỉ đồng bộ code và migration.

## Vai trò người dùng

| Vai trò | Chức năng                           |
| ------- | ----------------------------------- |
| Guest   | Xem, tìm kiếm công thức đã xuất bản |
| Author  | Tạo, chỉnh sửa, xuất bản công thức  |
| Admin   | Quản lý danh mục, quản trị hệ thống |

## Tài liệu tham khảo

- 📖 **[Documentation Index & Reading Guide Module FR-AUTH](./docs/README.md)** (Mục lục điều hướng và thứ tự đọc tài liệu)

## FR-JOB / FR-OBS

Hướng dẫn Docker và các case kiểm thử: [triển khai](docs/specs/fr-job-obs/README.md) và [báo cáo test](docs/specs/fr-job-obs/TEST_REPORT.md).
