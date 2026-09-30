# Lab 2 của Ngô Văn Chương

MSSV 2312588, lớp CTK47A, nhóm 06, nhánh feature/chuong. Hạn nộp theo ảnh giao bài: 22/09/2026 lúc 23:59. Bài yêu cầu nền backend, entity/configuration/DbContext, migration và dữ liệu ngẫu nhiên; chưa yêu cầu hoàn thiện tất cả API/UI FR-RCP.

## Kết quả đã kiểm chứng ngày 21/09/2026

- Kế thừa cấu trúc bốn project backend và thư viện có sẵn của nhóm; bổ sung tham chiếu Application → Domain, Identity EF Core, EF Design và dotnet-ef local phiên bản 10.0.12.
- Bổ sung Recipe/Category/Ingredient/Step/Image/Nutrition; ApplicationUser và RefreshToken; cấu hình FK, CHECK, unique/partial index, soft-delete query filters, audit và RowVersion.
- Hai migration: InitialLab2RecipeSchema và AddRecipeSearchSupport. Có trigger tìm kiếm bỏ dấu tiếng Việt, GIN và unique tên category không phân biệt hoa thường.
- PostgreSQL 16 chạy trong Docker riêng: project culinary-chuong-lab2, database culinary_lab2, cổng localhost 55432, schema culinary.
- Seed: 20 categories, 100 recipes, 1.000 ingredients, 500 steps. Min mỗi recipe: 10 ingredients và 5 steps. Chạy seed lần hai không tăng số lượng.
- Build: 0 lỗi, 0 cảnh báo. Kiểm chứng thực tế: RowVersion cũ bị từ chối; soft-delete cha ẩn con nhưng giữ bản ghi; chặn thời gian âm, quantity thiếu unit, slug trùng, số bước trùng, hai primary; tìm kiếm “pho bo” khớp “Phở bò” và trigger đã tạo SearchVector cho dữ liệu mẫu.

## Chạy hệ thống hiện tại

Mở Docker Desktop và chờ engine chạy. Từ thư mục gốc dự án:

```powershell
docker compose up -d --build
cd frontend
pnpm install
pnpm dev
```

API chạy tại `http://localhost:5058`, frontend tại `http://localhost:3000`. PostgreSQL, tài khoản, công thức và ảnh tải lên đều nằm trong Docker Volume trên từng máy.

Xem bảng bằng Docker Desktop: chọn container `culinary-blog-postgres-1` → Exec, chạy `psql -U culinary -d culinary_blog`, rồi `\dt public.*` và `\dt culinary.*`. Dừng mà giữ dữ liệu bằng `docker compose down`. Không dùng `docker compose down -v` nếu muốn giữ database và ảnh.

## Quyết định thay Supabase bằng Docker

Nhóm dùng PostgreSQL 16 trong Docker làm database duy nhất. `ApplicationDbContext` quản lý cả ASP.NET Identity và module công thức qua một migration thống nhất. API tự chạy migration và seed khi container khởi động. Ảnh được lưu tại `/app/wwwroot/uploads` trong Docker Volume `recipe-images` và được API phục vụ qua `/uploads/...`.

Dữ liệu không đồng bộ giữa máy thành viên. Nhóm đồng bộ cấu trúc bằng migration trong Git; khi trình bày chọn máy có bộ dữ liệu demo hoàn chỉnh. Cách này không cần tài khoản, mật khẩu hoặc dịch vụ Supabase.

## Giới hạn hiện tại và báo cáo trung thực

- Module FR-RCP đã có API vòng đời công thức, nguyên liệu, bước làm và ảnh; vẫn cần tiếp tục kiểm thử giao diện theo từng luồng người dùng.
- Tài khoản seed là dữ liệu giả, bị vô hiệu hóa và không có mật khẩu. Không dùng nó để chứng minh chức năng đăng nhập. Seed không ghi đè/xóa dữ liệu có sẵn; nếu dữ liệu mẫu bị sửa/xóa, verify sẽ báo thiếu thay vì âm thầm khôi phục.
- Frontend gọi .NET API; URL ảnh tương đối được ghép với địa chỉ API Docker.
- Mã Lab 2 đã được commit/push: d030f6818660b322aa584d1741b2fe1f4c532796 trên feature/chuong (đã xác nhận bằng git ls-remote ngày 21/09/2026). Bản báo cáo và cập nhật hướng dẫn sau commit này cần commit riêng nếu muốn lưu trong Git. Chưa mở PR; không gộp main trực tiếp.

## Các commit có ý nghĩa đề xuất

1. feat(domain): add recipe persistence entities
2. feat(database): add EF mappings and PostgreSQL migrations
3. feat(lab2): seed and verify required sample data
4. docs(lab2): document evidence and frontend integration gaps

Chia theo thay đổi thực tế, không sửa ngày commit hoặc tạo commit rỗng. Chỉ commit sau khi xem diff và kiểm tra build ở mỗi mốc.
