# Báo cáo thay đổi: tích hợp MinIO và hoàn thiện backend

Ngày tổng hợp: 30/09/2026.
Nhánh bàn giao: update-minio, tách từ fix/backend-tests-dotnet10.
Phạm vi: các thay đổi chưa commit sau commit dc9bd62; không bao gồm lại toàn bộ lịch sử Search & Pagination trước đó.

## 1. FR-FILE-001: Upload tệp tin

- Bổ sung MinioFileStorageService triển khai IFileStorageService, sử dụng MinIO .NET SDK qua giao thức S3-compatible.
- Dùng chung storage cho ảnh công thức và avatar; giữ LocalFileStorageService cho Development khi cấu hình rõ ràng.
- Tên object do server tạo bằng GUID trong recipes/{id}/ hoặc avatars/{id}/.
- Kiểm tra dung lượng thực của stream tối đa 5 MiB; đối chiếu MIME, phần mở rộng và magic bytes JPEG/PNG/WebP/AVIF.
- Giới hạn đường dẫn và chuẩn hóa loại ảnh lưu trữ; trả URL công khai để lưu trong database.
- Cấu hình endpoint nội bộ, public URL, bucket và SSL riêng; credentials đọc từ biến môi trường/user secrets.
- Lỗi ảnh trả HTTP 422; lỗi kho tệp được ánh xạ thành HTTP 503.

## 2. FR-FILE-002: Delete tệp tin

- Xóa object qua MinIO; hỗ trợ gọi xóa lặp lại, kiểm tra URL thuộc đúng storage/bucket.
- Bổ sung IFileDeletionQueue và job Hangfire lưu trên PostgreSQL.
- Lên lịch sau 1 phút, retry tối đa 3 lần; kiểm tra tham chiếu avatar/ảnh công thức/ảnh bước làm trước khi xóa.
- Xóa ảnh công thức hoặc recipe sẽ lên lịch dọn các URL liên quan.
- Thay/xóa avatar sẽ lên lịch dọn ảnh cũ; upload thành công nhưng lưu metadata thất bại sẽ lên lịch dọn object mới.
- Job được đăng ký trước khi commit metadata. Nếu database không commit hoặc tệp còn được dùng, job không xóa tệp đang được tham chiếu; có thể giữ trạng thái Failed để xử lý tiếp.

## 3. API ảnh và avatar

| API | Chức năng | Quyền |
| --- | --- | --- |
| POST /api/v1/recipes/{recipeId}/images | Upload ảnh công thức | Author sở hữu recipe hoặc Admin |
| DELETE /api/v1/recipes/{recipeId}/images/{imageId} | Xóa ảnh, nhận rowVersion | Author sở hữu recipe hoặc Admin |
| POST /api/v1/users/me/avatar | Upload/thay avatar, multipart file | Tài khoản đăng nhập, chỉ avatar của mình |
| DELETE /api/v1/users/me/avatar | Xóa avatar, trả 204 kể cả chưa có avatar | Tài khoản đăng nhập, chỉ avatar của mình |

## 4. Sửa lỗi backend và Search & Pagination

- Loại bỏ route GET /api/v1/recipes bị khai báo trùng giữa Minimal API và controller.
- Sửa thứ tự MapControllers/Run, bỏ helper ParseInteger trùng và luồng truy cập nullable trước validation.
- Dùng ApplicationDbContext cho cả search và quản lý recipe, bỏ đăng ký runtime DbContext dư thừa.
- Hoàn thiện filter minPrepTime, maxCookTime, minServings; maxCookTime chỉ tính CookTime.
- Hỗ trợ sort theo contract và giữ sortBy/sortOrder, mine=true cho frontend hiện có.
- Giới hạn pageSize 1–50, kiểm tra offset tràn số, difficulty và sort không hợp lệ; trả 422 cho dữ liệu sai.
- Thêm Id làm khóa sắp xếp phụ để phân trang ổn định.
- Kiểm tra query search 2–256 ký tự và ít nhất hai chữ/số; chuyển tạo tsquery sang hàm PostgreSQL.
- Đồng bộ script SearchVector/trigger/GIN với schema culinary và cấu hình tìm tiếng Việt.

## 5. Cấu hình và tài liệu

- Thêm compose.storage.yml: MinIO local, console, volume lưu dữ liệu và bước tạo bucket culinary-blog với quyền đọc công khai.
- Bổ sung cấu hình MinIO trong appsettings.json, không thêm credentials thật.
- Thêm docs/FR-FILE-setup.md hướng dẫn chạy, API contract, kiểm thử và lưu ý chuyển từ local storage sang MinIO.
- Thêm dependencies Minio, Hangfire.AspNetCore và Hangfire.PostgreSql.

## 6. Kết quả kiểm tra

Kết quả đã chạy ở lượt triển khai ngay trước khi tổng hợp báo cáo:

- Build solution thành công, 0 warning và 0 error tại lần build được ghi nhận.
- Application tests: 80 đạt.
- Integration tests: 21 đạt, 1 bỏ qua.
- Tổng cộng 101 test đạt, không có test lỗi.
- Test bao phủ validation ảnh, giới hạn dung lượng, đường dẫn/URL không hợp lệ, upload/delete storage, phân quyền upload ảnh, avatar, query pagination và job bảo vệ tệp đang được dùng.
- git diff --check đạt khi kiểm tra trước commit.

## 7. Giới hạn xác minh và công việc vận hành tiếp theo

- Máy chưa có Docker; test MinIO thật đã được chuẩn bị nhưng chưa chạy.
- Chưa xác minh Hangfire/PostgreSQL và FTS/trigger/GIN với dịch vụ thật trong lượt triển khai này.
- Test InMemory không xác nhận giao dịch và các hành vi riêng của PostgreSQL.
- Cần cấu hình PostgreSQL, credentials MinIO, chạy Compose rồi bật RUN_MINIO_TESTS=1 để kiểm tra end-to-end.
- URL/tệp local cũ không tự chuyển sang MinIO; cần chuyển dữ liệu trước khi đổi provider trên database đã có ảnh.
- Phạm vi thay đổi là backend, cấu hình và test; không bổ sung giao diện avatar/frontend trong đợt này.

## 8. Danh sách file thay đổi

Danh sách dưới đây gồm file sửa và file mới của đợt bàn giao (ngoài chính báo cáo này):
- `compose.storage.yml`
- `docs/FR-FILE-setup.md`
- `src/CulinaryBlog.API/appsettings.json`
- `src/CulinaryBlog.API/Controllers/AvatarsController.cs`
- `src/CulinaryBlog.API/Controllers/RecipeImagesController.cs`
- `src/CulinaryBlog.API/Controllers/RecipesController.cs`
- `src/CulinaryBlog.API/CulinaryBlog.API.csproj`
- `src/CulinaryBlog.API/Middleware/ExceptionHandlingMiddleware.cs`
- `src/CulinaryBlog.API/Program.cs`
- `src/CulinaryBlog.API/Services/FileDeletionJob.cs`
- `src/CulinaryBlog.API/Services/LocalFileStorageService.cs`
- `src/CulinaryBlog.API/Services/StorageRegistration.cs`
- `src/CulinaryBlog.Application/Exceptions/FileStorageException.cs`
- `src/CulinaryBlog.Application/Files/ImageFile.cs`
- `src/CulinaryBlog.Application/Interfaces/IFileDeletionQueue.cs`
- `src/CulinaryBlog.Application/Recipes/Validation/GetRecipesQueryValidator.cs`
- `src/CulinaryBlog.Application/Recipes/Validation/SearchRecipesQueryValidator.cs`
- `src/CulinaryBlog.Infrastructure/CulinaryBlog.Infrastructure.csproj`
- `src/CulinaryBlog.Infrastructure/Data/ApplicationDbContext.cs`
- `src/CulinaryBlog.Infrastructure/Persistence/Sql/20260922_RecipeSearchVector.sql`
- `src/CulinaryBlog.Infrastructure/Recipes/RecipeRepository.cs`
- `src/CulinaryBlog.Infrastructure/Storage/MinioFileStorageService.cs`
- `src/CulinaryBlog.Infrastructure/Storage/MinioStorageOptions.cs`
- `tests/CulinaryBlog.Application.Tests/MinioFileStorageTests.cs`
- `tests/CulinaryBlog.Application.Tests/RecipeRepositoryTests.cs`
- `tests/CulinaryBlog.Integration.Tests/CustomWebApplicationFactory.cs`
- `tests/CulinaryBlog.Integration.Tests/FileDeletionJobTests.cs`
- `tests/CulinaryBlog.Integration.Tests/MinioLiveTests.cs`
- `tests/CulinaryBlog.Integration.Tests/SearchAndFileTests.cs`
- `tests/CulinaryBlog.Integration.Tests/TestFileStorage.cs`

## 9. Giải quyết conflict với main

- Đồng bộ main tại d8dd2bb vào update-minio.
- Ghép 5 file conflict: Program, RecipesController, RecipeImagesController,
  ExceptionHandlingMiddleware và ApplicationDbContext.
- Giữ login/rate limiting/Problem Details và repository/Unit of Work từ main;
  giữ Search & Pagination, MinIO, avatar và hàng đợi xóa tệp.
- Bổ sung migration RestoreRecipeSearchTrigger cho baseline Docker mới và test
  kiểm tra SQL resource được đóng gói.
- compose.yml giữ Local storage của nhóm; compose.minio.yml bổ sung cấu hình
  cho API khi chạy chung compose.storage.yml.
- Kiểm tra sau merge: 95 application tests + 29 integration tests đạt (124 tổng);
  1 test MinIO thật bỏ qua do môi trường chưa có Docker.
- Không chạy migration trên database dùng chung; chưa kiểm thử dịch vụ Docker thật.