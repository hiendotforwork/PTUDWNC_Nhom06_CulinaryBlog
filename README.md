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
- **Entity Framework Core 10** + Supabase Database (PostgreSQL)
- **MediatR** (CQRS pattern)
- **Mapster** (object mapping)
- **JWT Authentication**

## Cài đặt và Chạy dự án

### Yêu cầu

- .NET 10 SDK
- Node.js 18+
- pnpm
- Tài khoản và project Supabase
- Redis (tùy chọn, cho caching)
- MinIO (tùy chọn, cho image storage)

### Backend

```bash
# Di chuyển vào thư mục src
cd src/CulinaryBlog.Infrastructure

# Cài đặt NuGet packages
dotnet restore

# Tạo migration (nếu cần)
dotnet ef migrations add <MigrationName> --startup-project ../CulinaryBlog.API

# Chạy migration
dotnet ef database update --startup-project ../CulinaryBlog.API

# Di chuyển vào thư mục API và chạy
cd ../CulinaryBlog.API
dotnet run
```

Backend sẽ chạy tại `http://localhost:5058`

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

### Cấu hình Cơ sở dữ liệu (Supabase)

#### 1. Thông số kết nối của nhóm

- **Host:** `aws-0-ap-northeast-1.pooler.supabase.com`
- **Port:** `5432` (Session Mode - bắt buộc cho EF Core)
- **Database:** `postgres`
- **Username:** `postgres.wflwzknzqaajqzzuwaxq`
- **Password:** _(Liên hệ Hiền để nhận mật khẩu nội bộ)_

#### 2. Cài đặt bằng một dòng lệnh (Khuyên dùng - Bảo mật tuyệt đối)

Thành viên mở Terminal tại thư mục gốc dự án và chạy lệnh sau (thay `<mat_khau_db>` bằng mật khẩu được chia sẻ):

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=aws-0-ap-northeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.wflwzknzqaajqzzuwaxq;Password=<mat_khau_db>;SSL Mode=Require;Trust Server Certificate=true" --project src/CulinaryBlog.API
```

_(Lệnh này lưu mật khẩu vào máy cá nhân ngoài cây thư mục Git, 100% không sợ bị commit lộ lên GitHub)._

#### 3. Cách phụ: Dùng `appsettings.Development.json`

Nếu không dùng CLI, bạn có thể tạo/sửa file `src/CulinaryBlog.API/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=aws-0-ap-northeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.wflwzknzqaajqzzuwaxq;Password=<mat_khau_db>;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

> [!WARNING]
> Không dùng Direct Connection (`db.wflwzknzqaajqzzuwaxq.supabase.co`) vì mạng internet thông thường chưa có IPv6 sẽ bị lỗi timeout.

## Vai trò người dùng

| Vai trò | Chức năng                           |
| ------- | ----------------------------------- |
| Guest   | Xem, tìm kiếm công thức đã xuất bản |
| Author  | Tạo, chỉnh sửa, xuất bản công thức  |
| Admin   | Quản lý danh mục, quản trị hệ thống |

## Tài liệu tham khảo

- 📖 **[Documentation Index & Reading Guide Module FR-AUTH](./docs/README.md)** (Mục lục điều hướng và thứ tự đọc tài liệu)
