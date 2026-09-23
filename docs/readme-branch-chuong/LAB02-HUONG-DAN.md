# Lab 2 của Ngô Văn Chương

MSSV 2312588, lớp CTK47A, nhóm 06, nhánh feature/chuong. Hạn nộp theo ảnh giao bài: 22/09/2026 lúc 23:59. Bài yêu cầu nền backend, entity/configuration/DbContext, migration và dữ liệu ngẫu nhiên; chưa yêu cầu hoàn thiện tất cả API/UI FR-RCP.

## Kết quả đã kiểm chứng ngày 21/09/2026

- Kế thừa cấu trúc bốn project backend và thư viện có sẵn của nhóm; bổ sung tham chiếu Application → Domain, Identity EF Core, EF Design và dotnet-ef local phiên bản 10.0.12.
- Bổ sung Recipe/Category/Ingredient/Step/Image/Nutrition; ApplicationUser và RefreshToken; cấu hình FK, CHECK, unique/partial index, soft-delete query filters, audit và RowVersion.
- Hai migration: InitialLab2RecipeSchema và AddRecipeSearchSupport. Có trigger tìm kiếm bỏ dấu tiếng Việt, GIN và unique tên category không phân biệt hoa thường.
- PostgreSQL 16 chạy trong Docker riêng: project culinary-chuong-lab2, database culinary_lab2, cổng localhost 55432, schema culinary.
- Seed: 20 categories, 100 recipes, 1.000 ingredients, 500 steps. Min mỗi recipe: 10 ingredients và 5 steps. Chạy seed lần hai không tăng số lượng.
- Build: 0 lỗi, 0 cảnh báo. Kiểm chứng thực tế: RowVersion cũ bị từ chối; soft-delete cha ẩn con nhưng giữ bản ghi; chặn thời gian âm, quantity thiếu unit, slug trùng, số bước trùng, hai primary; tìm kiếm “pho bo” khớp “Phở bò” và trigger đã tạo SearchVector cho dữ liệu mẫu.

## Chạy lại trên máy hiện tại

Mở Docker Desktop và chờ engine chạy. Từ thư mục gốc dự án:

```powershell
docker compose --env-file .env.local -p culinary-chuong-lab2 -f compose.lab2.yml up -d --wait
dotnet tool restore
dotnet restore
dotnet build
$entry = Get-Content .env.local | Where-Object { $_ -like 'LAB2_POSTGRES_PASSWORD=*' }
$labPassword = $entry.Substring('LAB2_POSTGRES_PASSWORD='.Length)
$env:ConnectionStrings__DefaultConnection = "Host=127.0.0.1;Port=55432;Database=culinary_lab2;Username=culinary_lab2;Password=$labPassword"
dotnet run --no-launch-profile --project src/CulinaryBlog.API -- --lab2-migrate
dotnet run --no-launch-profile --project src/CulinaryBlog.API -- --lab2-seed
dotnet run --no-launch-profile --project src/CulinaryBlog.API -- --lab2-verify
dotnet run --project tests/CulinaryBlog.DatabaseChecks
```

Trên máy mới, tự tạo `.env.local` chứa `LAB2_POSTGRES_PASSWORD=<mật khẩu local của bạn>` trước khi chạy. File này đã được Git ignore. Không đổi mật khẩu trong file nếu volume PostgreSQL cũ đã được khởi tạo, trừ khi đã đổi mật khẩu trong DB tương ứng. Các phép kiểm thử có thay đổi dữ liệu đều rollback và bị giới hạn ở database local culinary_lab2.

Xem bảng bằng Docker Desktop: chọn container PostgreSQL → Exec, chạy `psql -U culinary_lab2 -d culinary_lab2`, rồi `\dt culinary.*`. Dừng mà giữ dữ liệu: `docker compose --env-file .env.local -p culinary-chuong-lab2 -f compose.lab2.yml stop`. Không dùng `down -v` nếu muốn giữ dữ liệu.

## Supabase của nhóm

Chưa kết nối hoặc thay đổi Supabase; người dùng chọn kiểm thử cục bộ trước. Ảnh dashboard “No migrations” không chứng minh tất cả schema trống; phải kiểm tra trước khi áp dụng.

Supabase dùng PostgreSQL, không thay thế backend .NET/EF Core. Dùng direct connection nếu có IPv6 hoặc Session pooler cổng 5432 nếu cần IPv4. Không dùng URL REST/API key làm connection string database. Lấy thông tin thực tế từ nút Connect; lưu connection string trong biến môi trường/User Secrets, không commit.

Migration này tạo schema `culinary` để tách khỏi các schema Supabase `auth`, `storage`, `public`; frontend sẽ gọi .NET API, không mở schema này cho truy cập public bằng Supabase Data API. Trước triển khai chung, nhóm trưởng cần review schema và quyền truy cập, đối chiếu bảng hiện có, thống nhất migrations. Không chạy script SQL và EF migration theo hai luồng độc lập; `lab2-migration.sql` là bản xuất từ EF để review.

Tài liệu kết nối chính thức: https://supabase.com/docs/guides/database/connecting-to-postgres

## Giới hạn hiện tại và báo cáo trung thực

- Đây là nền persistence Lab 2, chưa hoàn thành các command/handler/validator/API nghiệp vụ FR-RCP. Các public setters phục vụ mapping/seed sẽ được bao bọc bằng domain methods khi triển khai nghiệp vụ.
- Audit hiện đổi token entity được ghi. Quy tắc cập nhật token cha khi sửa con, kiểm tra quyền, publish đủ nguyên liệu/bước, renumber và chuyển ảnh chính trong transaction phải được triển khai trong các mutation handlers; chưa coi là đã có API hoàn chỉnh.
- Tài khoản seed là dữ liệu giả, bị vô hiệu hóa và không có mật khẩu. Không dùng nó để chứng minh chức năng đăng nhập. Seed không ghi đè/xóa dữ liệu có sẵn; nếu dữ liệu mẫu bị sửa/xóa, verify sẽ báo thiếu thay vì âm thầm khôi phục.
- Chưa tích hợp frontend với backend/Supabase. Frontend của thành viên khác không ghi là thành quả cá nhân của Chương.
- Mã Lab 2 đã được commit/push: d030f6818660b322aa584d1741b2fe1f4c532796 trên feature/chuong (đã xác nhận bằng git ls-remote ngày 21/09/2026). Bản báo cáo và cập nhật hướng dẫn sau commit này cần commit riêng nếu muốn lưu trong Git. Chưa mở PR; không gộp main trực tiếp.

## Các commit có ý nghĩa đề xuất

1. feat(domain): add recipe persistence entities
2. feat(database): add EF mappings and PostgreSQL migrations
3. feat(lab2): seed and verify required sample data
4. docs(lab2): document evidence and frontend integration gaps

Chia theo thay đổi thực tế, không sửa ngày commit hoặc tạo commit rỗng. Chỉ commit sau khi xem diff và kiểm tra build ở mỗi mốc.
