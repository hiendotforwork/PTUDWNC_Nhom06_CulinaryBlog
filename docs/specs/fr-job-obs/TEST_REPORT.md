# Báo cáo kiểm thử FR-JOB / FR-OBS

Ngày kiểm thử: **07/10/2026**. Stack: `culinary-jobs-tests`; PostgreSQL 16, Redis 7, MinIO build từ official source tag RELEASE.2025-04-22T22-12-26Z, Mailpit, Seq và OpenTelemetry Collector.

## Kết quả

**150/150 tests đạt; 0 thất bại; 0 bị bỏ qua.**

| Bộ test | Đạt | Thất bại | Bỏ qua |
| --- | ---: | ---: | ---: |
| Application | 100 | 0 | 0 |
| Integration (33 hiện có + 16 FR-JOB/FR-OBS + 1 MinIO live) | 50 | 0 | 0 |

Các case database mới được chạy với PostgreSQL thật. Email HTML được nhận trong Mailpit; JPEG/PNG/WebP/AVIF được upload qua API, xử lý bởi worker Hangfire và các variant trên MinIO được tải lại để kiểm tra kích thước. Test retry xác nhận Scheduled rồi Failed sau khi advance ngân sách retry lưu trong PostgreSQL; không chờ backoff nhiều phút trong test.

## Kiểm tra Docker bổ sung

| Sự cố | /health/live | /health/ready | /health | Phục hồi |
| --- | ---: | ---: | ---: | --- |
| Redis dừng | 200 | 503 | 503 | Healthy sau start |
| MinIO dừng | 200 | 200 | 503 | Healthy sau start |
| PostgreSQL dừng | 200 | 503 | 503 | Healthy sau start |
| API restart | — | 200 sau startup | 200 | Không đổi dữ liệu/migration |

Snapshot trước/sau API restart đều **102 recipes, 11 users, 35 outbox tasks, 3 migrations**. Database kiểm thử độc lập với dữ liệu mẫu trong `culinary_blog`.

Đã kiểm chứng structured fields CorrelationId, TraceId, UserId, method/path/status/elapsed và RequestType. Có warning cho request >500ms. Seq trả được events đã nhận. Collector nhận HTTP/Npgsql spans, request count/duration/error metrics và recipe created/published counters.

## Bằng chứng và chạy lại

- TRX và bằng chứng ở `artifacts/test-results/jobs-observability/`: hai file TRX, `dependency-failures.json`, `restart.json`, `observability.json`, `api.log`, `telemetry.json`.
- Chạy: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/test-jobs-observability.ps1`.
- API kiểm thử: http://localhost:5059; Mailpit: http://localhost:18025; MinIO console: http://localhost:19001; Seq: http://localhost:15341.
- API Admin kích hoạt sitemap: `POST /api/v1/admin/jobs/sitemap`; dashboard `/hangfire` yêu cầu JWT có role Admin.
- Xem [hướng dẫn và ma trận case](README.md) để biết chi tiết.

Trong quá trình test, đã sửa logic startup cũ làm lệch schema Identity và phân tách lịch sử migration giữa public/culinary. Startup hiện dùng history public, hợp nhất history cũ và giữ Identity trong schema khớp DbContext; case restart đã qua.

## Điều chỉnh theo hệ thống hiện hành

Google đã ngừng sitemap ping endpoint. Đã khai báo sitemap qua `/robots.txt`; nộp sitemap trong Search Console khi có domain và tài khoản triển khai thật. Tham khảo [thông báo chính thức của Google](https://developers.google.com/search/blog/2023/06/sitemaps-lastmod-ping).

SMTP trong kiểm thử chỉ gửi đến Mailpit local. Production cần cấu hình SMTP, MinIO/Redis và OTLP endpoint thật. Mail delivery có semantics at-least-once trong cửa sổ sự cố giữa SMTP và commit completion.

## Kiểm tra trước khi push branch

Đã ghép thay đổi trên main tại commit 25cbd62, giữ cập nhật category và frontend. Chạy lại toàn bộ test trên checkout mới: 100 Application + 50 Integration đều đạt; diff không có lỗi whitespace.
