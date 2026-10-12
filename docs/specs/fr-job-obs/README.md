# FR-JOB / FR-OBS: triển khai và kiểm thử

## Chạy môi trường Docker riêng

Từ thư mục gốc:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/test-jobs-observability.ps1
```

Script publish API, build/chạy stack `culinary-jobs-tests`, chạy tests, kiểm tra mất kết nối/phục hồi và thu bằng chứng. Stack này dùng database `culinary_jobs_tests`, không dùng database `culinary_blog` hiện có.

| Dịch vụ | Địa chỉ local |
| --- | --- |
| API | http://localhost:5059 |
| PostgreSQL | localhost:55433 |
| MinIO API / console | http://localhost:19000 / http://localhost:19001 |
| Mailpit (email local) | http://localhost:18025 |
| Seq | http://localhost:15341 |
| Redis | localhost:16379 |

Thông tin đăng nhập chỉ dành cho stack kiểm thử được khai báo trong `compose.jobs-tests.yml`. Môi trường thật phải cấu hình SMTP, Redis, MinIO, Site và endpoint OTLP theo hệ thống triển khai.

## FR-JOB

- **001:** Đăng ký lưu yêu cầu gửi email trong outbox PostgreSQL cùng transaction với user/refresh token. Dispatcher chuyển yêu cầu đã commit sang Hangfire. Email HTML có tên được encode và link ứng dụng; SMTP local gửi đến Mailpit. Retry 3 lần, cách 60/300/1800 giây, sau đó Failed.
- **002:** Upload ảnh lưu metadata và yêu cầu resize trong cùng transaction. Worker đọc ảnh gốc từ storage sở hữu, kiểm tra kích thước, xử lý orientation, loại metadata và tạo JPEG thumbnail 300×300 / medium 800×600. Hỗ trợ nguồn JPEG/PNG/WebP/AVIF. Metadata URL chỉ commit sau khi tạo đủ variants. Lỗi giữ ảnh gốc và lên lịch cleanup variants dở dang; retry 3 lần. Ảnh đã xóa được bỏ qua.
- **003:** Sitemap chạy cron `0 2 * * *`, timezone UTC; retry 2 lần. Bao gồm công thức Published chưa xóa, danh mục chưa xóa và trang tĩnh. Ghi XML hợp lệ, thay file atomically trong wwwroot và log số URL. Admin có thể gọi `POST /api/v1/admin/jobs/sitemap` để chạy ngay.
- Hangfire dùng PostgreSQL, dashboard `/hangfire` chỉ chấp nhận user đã xác thực có role Admin. API dùng JWT; khi kiểm thử dashboard cần gửi Authorization Bearer.
- Job xóa file hiện có là ví dụ delayed job. Outbox và kiểm tra CompletedAt giúp chịu restart và dispatch trùng. SMTP có semantics at-least-once: sự cố giữa gửi mail và commit DB có thể tạo email trùng.

**Điểm cập nhật SRS:** Google đã loại bỏ sitemap ping endpoint; yêu cầu ping được thay bằng khai báo sitemap trong `/robots.txt` và nộp qua Search Console khi triển khai domain thật. Không ping domain local. Nguồn: https://developers.google.com/search/blog/2023/06/sitemaps-lastmod-ping

## FR-OBS

- **001:** `GET /health` kiểm tra database, Redis và storage; `/health/live` chỉ kiểm tra process; `/health/ready` kiểm tra database và Redis. Dependency unhealthy trả HTTP 503. Response không lộ exception hoặc connection string. Local storage được hiển thị rõ khi provider Local.
- **002:** Serilog JSON console, rolling file mỗi ngày (giữ 14 file), Seq khi cấu hình trong Development. Middleware log method/path/status/elapsed/UserId, echo correlation header hợp lệ hoặc tạo ID mới. Log có TraceId. MediatR behavior log loại command/query và duration, cảnh báo khi >500ms. Không serialize password/token/request payload.
- **003:** OpenTelemetry traces cho ASP.NET Core, HttpClient, Npgsql (bao gồm lệnh EF Core), business/job/MediatR activities; export OTLP HTTP. Metrics gồm request count, duration histogram, HTTP 5xx count, recipe created/published, runtime và HTTP instrumentation. Collector test lưu traces/metrics để kiểm chứng; production có thể chuyển collector đến Jaeger/Tempo.

## Case kiểm thử

| Nhóm | Case |
| --- | --- |
| Outbox / database | Rollback cùng transaction; deduplicate; lưu CompletedAt; chạy lại không gửi mail/resize lại |
| Email | Job thành công; SMTP lỗi giữ task chưa hoàn tất; đăng ký → Hangfire → email HTML trong Mailpit; encode tên |
| Ảnh | Dimensions chính xác; URL lưu DB; lỗi upload variant thứ hai và cleanup; ảnh lỗi giữ original; bỏ qua ảnh đã xóa; upload JPEG/PNG/WebP/AVIF → worker → MinIO public read |
| Retry / Hangfire | Chính sách 3/3/2; backoff welcome 60/300/1800; worker lỗi → Scheduled; advance retry budget → Failed; state lưu PostgreSQL |
| Sitemap | XML hợp lệ; Published/categories/static; loại Draft/Archived; không để file tạm; cron/timezone persist; kích hoạt Admin và tải XML qua HTTP |
| Health | Healthy; DB không kết nối; DB/Redis/MinIO down; live/ready/aggregate đúng; start lại → healthy |
| Authorization | Dashboard anonymous/Author bị chặn; Admin được phép |
| Logging / telemetry | Correlation echo/sanitize; structured properties; HTTP/database traces; OTLP metrics và business counters |
| Regression | Toàn bộ test Application và Integration hiện có |

Bằng chứng: `artifacts/test-results/jobs-observability/` chứa TRX, dependency-failures.json, api.log và telemetry.json. Artifacts không commit vào Git.
