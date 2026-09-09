# PTUDWNC_Nhom06_CulinaryBlog
## Thông tin thành viên nhóm 6

| MSSV       | Họ và Tên            | Email cá nhân              | Tên tài khoản GitHub |
|------------|----------------------|---------------------------|----------------------|
| 2312609   | Nguyễn Ngọc Thanh Hiền         | 2312609@dlu.edu.vn   | hìedotforwork           |
| 2312756   | Nguyễn Hưng Thịnh           | 2312756@dlu.edu.vn       | elgthinhnguyen              |
| 2312588   | Ngô Văn Chương       | 2312588@dlu.edu.vn         | chuong-gif                |
| 2312565   | Nguyễn Văn An           | 2312565@dlu.edu.vn       | NguyenAn124              |

## Bảng phân công công việc

| MSSV       | Mã Module công việc | Tên công việc         | Ghi chú công việc |
|------------|---------------------|-----------------------|-------------------|
| 2312609   | FR-AUTH               | Module Xác thực và Quản lý Người dùng    | quản lý toàn bộ vòng đời xác thực người dùng         |
| 2312756   | FR-SRCH               | Module Tìm kiếm và Phân trang        | Tìm kiến, lọc công thức nấu ăn           |
| 2312588   | FR-RCP              | Module Quản lý công thức nấu ăn| Tạo, cập nhật, quản lý công thức nấu ăn         |
| 2312565   | FR-CAT              | KModule Quản lý Danh mục     | Quản lý danh mục phân loại công thức nấu ăn           |

## Mô tả tổng quan đề tài

## Giới thiệu đề tài

**Culinary Blog** là hệ thống blog ẩm thực cho phép người dùng khám phá, tìm kiếm và xem các công thức nấu ăn, đồng thời hỗ trợ tác giả xây dựng và quản lý nội dung công thức. Hệ thống cung cấp các chức năng quản lý công thức, nguyên liệu, các bước thực hiện, hình ảnh và danh mục món ăn.

Hệ thống được xây dựng theo kiến trúc Web API kết hợp ứng dụng Web, hướng tới khả năng mở rộng, bảo mật và tối ưu hiệu năng.

### Mục tiêu chính

* Cung cấp nền tảng chia sẻ và khám phá công thức nấu ăn.
* Cho phép tác giả tạo, chỉnh sửa, xuất bản và quản lý công thức.
* Hỗ trợ người dùng tìm kiếm công thức theo từ khóa và các tiêu chí lọc.
* Quản lý nguyên liệu, các bước nấu ăn và hình ảnh của từng công thức.
* Quản lý danh mục món ăn.
* Cung cấp cơ chế xác thực và phân quyền người dùng.
* Đảm bảo khả năng mở rộng và hiệu năng của hệ thống.

---

## Đối tượng sử dụng

Hệ thống hiện có các nhóm người dùng chính:

| Vai trò          | Chức năng chính                                               |
| ---------------- | ------------------------------------------------------------- |
| **Guest / User** | Xem, tìm kiếm và khám phá các công thức đã được xuất bản      |
| **Author**       | Tạo, chỉnh sửa, quản lý và xuất bản công thức                 |
| **Admin**        | Quản lý danh mục và thực hiện các chức năng quản trị hệ thống |

---

## Chức năng chính

### 1. Xác thực và quản lý người dùng

Hệ thống cung cấp các chức năng:

* Đăng ký tài khoản.
* Đăng nhập.
* Đăng nhập thông qua Google.
* Làm mới Access Token bằng Refresh Token.
* Đăng xuất.
* Xem thông tin tài khoản hiện tại.
* Cập nhật thông tin cá nhân.
* Quản lý trạng thái hoạt động của tài khoản.

Hệ thống sử dụng **JWT Bearer Authentication** kết hợp với Refresh Token để duy trì phiên đăng nhập.

---

### 2. Quản lý danh mục món ăn

Admin có thể quản lý các danh mục công thức:

* Xem danh sách danh mục.
* Xem chi tiết danh mục.
* Tạo danh mục.
* Cập nhật danh mục.
* Xóa danh mục.

Mỗi danh mục có các thông tin như:

* Tên danh mục.
* Slug.
* Mô tả.
* Hình ảnh.
* Thứ tự hiển thị.

---

### 3. Quản lý công thức nấu ăn

Author có thể xây dựng và quản lý công thức nấu ăn.

Các chức năng chính:

* Tạo công thức.
* Xem chi tiết công thức.
* Chỉnh sửa công thức.
* Xóa công thức.
* Xuất bản công thức.
* Hủy xuất bản công thức.
* Lưu trữ công thức.
* Quản lý trạng thái công thức.

Một công thức bao gồm các thông tin chính:

* Tiêu đề.
* Slug.
* Mô tả.
* Thời gian chuẩn bị.
* Thời gian nấu.
* Số khẩu phần.
* Độ khó.
* Danh mục.
* Tác giả.
* Ngày xuất bản.
* Danh sách nguyên liệu.
* Các bước thực hiện.
* Hình ảnh.

---

### 4. Quản lý nguyên liệu

Mỗi công thức có thể chứa nhiều nguyên liệu.

Hệ thống hỗ trợ:

* Thêm nguyên liệu.
* Cập nhật nguyên liệu.
* Xóa nguyên liệu.
* Sắp xếp thứ tự nguyên liệu.

Thông tin nguyên liệu bao gồm:

* Tên nguyên liệu.
* Số lượng.
* Đơn vị.
* Ghi chú.
* Thứ tự hiển thị.

---

### 5. Quản lý các bước nấu ăn

Công thức được chia thành nhiều bước thực hiện.

Mỗi bước có thể bao gồm:

* Số thứ tự bước.
* Tiêu đề.
* Mô tả.
* Thời gian hẹn giờ.
* Hình ảnh minh họa.

Hệ thống hỗ trợ:

* Thêm bước.
* Chỉnh sửa bước.
* Xóa bước.
* Quản lý thứ tự các bước.
* Thiết lập thời gian Timer cho từng bước.

---

### 6. Quản lý hình ảnh công thức

Hệ thống hỗ trợ upload và quản lý hình ảnh cho công thức.

Các chức năng chính:

* Upload hình ảnh.
* Xem danh sách hình ảnh của công thức.
* Xóa hình ảnh.
* Thiết lập hình ảnh chính.
* Quản lý thứ tự hiển thị.
* Lưu các phiên bản hình ảnh theo kích thước.

Hệ thống hỗ trợ các định dạng:

* JPEG
* PNG
* WebP
* AVIF

Giới hạn kích thước file được quy định trong SRS là **5 MB**.

Hình ảnh được lưu trữ thông qua **MinIO Object Storage**.

---

### 7. Tìm kiếm và lọc công thức

Người dùng có thể tìm kiếm công thức dựa trên từ khóa.

Hệ thống hỗ trợ các tiêu chí lọc như:

* Danh mục.
* Độ khó.
* Thời gian chuẩn bị/nấu.
* Số khẩu phần.

Hệ thống sử dụng khả năng tìm kiếm toàn văn của **PostgreSQL**, kết hợp `tsvector/tsquery` và `unaccent` để hỗ trợ tìm kiếm nội dung công thức.

Kết quả tìm kiếm được hỗ trợ:

* Phân trang.
* Sắp xếp.
* Lọc theo nhiều tiêu chí.

---

### 8. Trạng thái công thức

Công thức được quản lý theo các trạng thái nghiệp vụ như:

* **Draft** – bản nháp.
* **Published** – đã xuất bản.
* **Archived** – đã lưu trữ.

Việc thay đổi trạng thái cho phép kiểm soát vòng đời của công thức từ quá trình tạo nội dung đến khi xuất bản hoặc lưu trữ.

---

## 4. Tech Stack

### Frontend

* **Next.js** – xây dựng ứng dụng Web và giao diện người dùng.
* **JavaScript / TypeScript** – phát triển logic phía frontend.

### Backend

* **ASP.NET Core Web API** – xây dựng RESTful API.
* **.NET 10** – nền tảng runtime/framework.
* **Entity Framework Core 10** – ORM và Code First.
* **MediatR** – hỗ trợ triển khai mô hình CQRS/Mediator.
* **ASP.NET Core Identity** – quản lý authentication và user identity.
* **JWT Bearer Authentication** – xác thực API.

### Database

* **PostgreSQL 16** – hệ quản trị cơ sở dữ liệu chính.
* **Entity Framework Core Code First** – quản lý schema và migration.
* **PostgreSQL Full-Text Search** – hỗ trợ tìm kiếm công thức.

### Storage

* **MinIO** – lưu trữ object/image của hệ thống.

### Cache

* **Redis** – sử dụng cho distributed caching và các cơ chế cache dùng chung giữa nhiều instance.

### Authentication

* **JWT Access Token**
* **Refresh Token**
* **Google OAuth / Google Authentication**

### DevOps & Development Tools

* **Git** – quản lý phiên bản mã nguồn.
* **GitHub** – lưu trữ source code và hỗ trợ cộng tác.
* **Docker** – đóng gói và triển khai các thành phần của hệ thống.

---

## 9. Tóm tắt

**Culinary Blog** là một hệ thống Web quản lý và chia sẻ công thức nấu ăn, tập trung vào ba nhóm nghiệp vụ chính:

1. **Khám phá nội dung** – người dùng có thể xem, tìm kiếm và lọc công thức.
2. **Quản lý nội dung** – Author có thể tạo, chỉnh sửa, quản lý và xuất bản công thức cùng nguyên liệu, các bước và hình ảnh.
3. **Quản trị hệ thống** – Admin quản lý danh mục và các thành phần quản trị được quy định trong hệ thống.

Về công nghệ, hệ thống sử dụng **Next.js** ở phía Web Client, **ASP.NET Core Web API/.NET 10** ở phía Backend, **PostgreSQL 16** làm cơ sở dữ liệu, **Redis** cho caching, **MinIO** cho lưu trữ hình ảnh và **JWT/Google Authentication** cho xác thực. Hệ thống được thiết kế theo hướng RESTful, có versioning API, pagination, caching, full-text search, soft delete và optimistic concurrency.
