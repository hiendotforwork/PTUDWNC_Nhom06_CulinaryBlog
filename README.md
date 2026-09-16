# Culinary Blog - Blog Ẩm thực và Nấu ăn

Hệ thống blog ẩm thực cho phép người dùng khám phá, tìm kiếm và xem công thức nấu ăn. Hỗ trợ tác giả xây dựng, quản lý và xuất bản công thức.

## Thông tin thành viên

| MSSV | Họ và Tên | GitHub |
|------|-----------|--------|
| 2312609 | Nguyễn Ngọc Thanh Hiền | hìedotforwork |
| 2312756 | Nguyễn Hưng Thịnh | elgthinhnguyen |
| 2312588 | Ngô Văn Chương | chuong-gif |
| 2312565 | Nguyễn Văn An | NguyenAn124 |

## Phân công Module

| Module | Chức năng |
|--------|-----------|
| FR-AUTH | Xác thực & Quản lý người dùng |
| FR-SRCH | Tìm kiếm & Phân trang |
| FR-RCP | Quản lý công thức nấu ăn |
| FR-CAT | Quản lý danh mục |

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
- **Entity Framework Core 10** + PostgreSQL
- **MediatR** (CQRS pattern)
- **Mapster** (object mapping)
- **JWT Authentication**

## Cài đặt và Chạy dự án

### Yêu cầu
- .NET 10 SDK
- Node.js 18+
- pnpm
- PostgreSQL 16
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

### Cấu hình

Backend cần cấu hình `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=culinaryblog;Username=postgres;Password=yourpassword"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "MinIO": {
    "Endpoint": "localhost:9000",
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin"
  },
  "Jwt": {
    "AccessTokenSecret": "your-secret-key-min-32-characters",
    "RefreshTokenSecret": "your-refresh-secret-key-min-32-characters"
  }
}
```

## Vai trò người dùng

| Vai trò | Chức năng |
|---------|-----------|
| Guest | Xem, tìm kiếm công thức đã xuất bản |
| Author | Tạo, chỉnh sửa, xuất bản công thức |
| Admin | Quản lý danh mục, quản trị hệ thống |

## Tài liệu tham khảo

- [SRS Specification](./docs/specs/fr-auth/SRS_Culinary_Blog_v1.0.0.md)
- [API Contract FR-AUTH](./docs/specs/fr-auth/FR-AUTH_APIContract.md)
- [UI Design Spec](./docs/specs/UI_DESIGN_SPECIFICATION.md)
- [Database Design FR-AUTH](./docs/specs/fr-auth/FR-AUTH_Database_Design.md)
