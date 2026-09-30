# FR-FILE: MinIO và kiểm tra backend

Backend dùng MinIO mặc định. Không lưu access key/secret trong Git.
SDK: https://github.com/minio/minio-dotnet
Hangfire/PostgreSQL: https://github.com/hangfire-postgres/Hangfire.PostgreSql

## Khởi động MinIO local

Cài Docker Desktop và bật Linux containers. Trong PowerShell tại thư mục repo,
đặt MINIO_ROOT_USER và MINIO_ROOT_PASSWORD bằng giá trị local tự chọn (password tối thiểu 8 ký tự).
Không dùng các khóa local này trên production.

    $env:MINIO_ROOT_USER = "<local-access-key>"
    $env:MINIO_ROOT_PASSWORD = "<local-secret>"
    docker compose -f compose.storage.yml up -d
    docker compose -f compose.storage.yml logs minio-init

minio-init phải kết thúc với exit code 0. API S3 ở http://localhost:9000,
console ở http://localhost:9001. Bucket culinary-blog được tạo idempotent với
quyền đọc công khai. Upload/delete vẫn cần credentials. Dữ liệu nằm trên volume minio-data.
Không dùng down -v nếu muốn giữ ảnh.

## Chạy API

Giữ ConnectionStrings__DefaultConnection trỏ vào PostgreSQL đã có schema của dự án.
Đặt ConnectionStrings__HangfireConnection nếu muốn lưu jobs trong database riêng;
mặc định dùng DefaultConnection, schema hangfire. Tài khoản database phải có quyền tạo schema/bảng Hangfire.

    $env:MinIO__Endpoint = "localhost:9000"
    $env:MinIO__AccessKey = $env:MINIO_ROOT_USER
    $env:MinIO__SecretKey = $env:MINIO_ROOT_PASSWORD
    $env:MinIO__UseSsl = "false"
    $env:MinIO__BucketName = "culinary-blog"
    $env:MinIO__PublicBaseUrl = "http://localhost:9000"
    dotnet run --project src/CulinaryBlog.API

Nếu API chạy trong Docker, Endpoint là minio:9000; PublicBaseUrl vẫn phải là URL
mà trình duyệt truy cập được. Local filesystem chỉ được chọn rõ ràng với
FileStorage__Provider=Local trong Development. Các URL local cũ không tự chuyển
sang MinIO: cần chuyển dữ liệu/URL trước khi đổi provider trên database có dữ liệu.

## API contract

- POST /api/v1/recipes/{recipeId}/images: multipart file và altText tùy chọn.
  Cần Author/Admin và quyền sở hữu recipe (Admin được phép). Trả 201 với metadata ảnh.
- DELETE /api/v1/recipes/{recipeId}/images/{imageId}: JSON rowVersion, cùng quyền như upload.
- POST /api/v1/users/me/avatar: multipart file; mọi tài khoản đăng nhập được phép.
  Trả 201 với avatarUrl. Thay ảnh sẽ lên lịch xóa ảnh cũ.
- DELETE /api/v1/users/me/avatar: xóa avatar của chính tài khoản hiện tại; trả 204,
  kể cả khi chưa có avatar.
- Xóa recipe sẽ lên lịch xóa original/medium/thumbnail và ảnh bước làm.

Ảnh tối đa 5 MiB, kiểm tra dung lượng thực của stream, MIME, phần mở rộng và magic bytes
JPEG/PNG/WebP/AVIF. Đây là kiểm tra định dạng, không phải giải mã toàn bộ ảnh hoặc quét malware.
Sai ảnh/tham số: 422. Request quá giới hạn multipart: 413. MinIO lỗi khi upload: 503.
Tên object là recipes/{id}/{guid}.{ext} hoặc avatars/{id}/{guid}.{ext}.
Dịch vụ delete chỉ nhận URL thuộc đúng public URL và bucket cấu hình.

Xóa nền dùng Hangfire, trì hoãn 1 phút rồi retry tối đa 3 lần. Job được lưu trước khi
commit thay đổi metadata; nếu enqueue thất bại, thay đổi metadata không được lưu.
Job kiểm tra các tham chiếu còn sống trước khi xóa. Nếu commit thất bại hoặc còn
tham chiếu sau các lần retry, job giữ trạng thái Failed để kiểm tra/thử lại; không xóa
tệp đang được dùng. Không mở Hangfire dashboard công khai.
Upload thành công nhưng lưu metadata thất bại sẽ lên lịch dọn object; nếu cả
database/job storage đều ngừng, URL cần dọn được ghi vào log.

## Search & Pagination

GET /api/v1/recipes: page=1, pageSize=12 (tối đa 50), categoryId, difficulty,
minPrepTime, maxCookTime (chỉ CookTime), minServings, sort=createdAt|-createdAt|title|-title|cookTime|-cookTime.
Giữ tương thích frontend với sortBy/sortOrder và mine=true.
Danh sách công khai chỉ Published; mine=true cần đăng nhập và giới hạn owner hoặc Admin.
GET /api/v1/recipes/search?q=pho&page=1&pageSize=12: PostgreSQL FTS, prefix,
không dấu, rank giảm dần, chỉ Published. Query 2–256 ký tự, ít nhất 2 chữ/số.
Các sort đều có Id làm khóa phụ để phân trang ổn định.
Input sai định dạng, offset tràn int, sort lạ trả 422.

API chỉ có một route danh sách; search và quản lý recipe dùng ApplicationDbContext.
Script SQL bổ sung chỉ dùng với schema culinary đã tồn tại; không tạo database mới.
Không bật cache chung cho danh sách cá nhân để tránh trộn dữ liệu theo quyền.

## Kiểm tra

    dotnet restore CulinaryBlog.slnx
    dotnet build CulinaryBlog.slnx --no-restore
    dotnet test CulinaryBlog.slnx --no-build

Test thường dùng InMemory và storage giả. Test MinIO thật mặc định được skip;
sau khi Compose sẵn sàng:

    $env:RUN_MINIO_TESTS = "1"
    dotnet test tests/CulinaryBlog.Integration.Tests --filter FullyQualifiedName~MinioLiveTests

Test này cố định ở localhost:9000, tạo object riêng, đọc ảnh công khai rồi xóa hai lần.
Không đụng dữ liệu có sẵn. Cần chạy thêm kiểm tra PostgreSQL thật cho FTS/trigger/GIN
khi môi trường database local sẵn sàng. InMemory không xác nhận hành vi FTS.

Nếu ổ C thiếu dung lượng, đặt TEMP/TMP, DOTNET_CLI_HOME, NUGET_HTTP_CACHE_PATH
và NUGET_PACKAGES vào thư mục tmp/ trên ổ D trước khi build.
