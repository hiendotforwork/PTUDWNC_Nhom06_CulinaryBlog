GIÁO TRÌNH PHÁT TRIỂN ỨNG DỤNG WEB NÂNG CAO
Phiên bản V4 · .NET 10 + Next.js App Router
TÀI LIỆU ĐẶC TẢ YÊU CẦU PHẦN MỀM
Software Requirements Specification (SRS)
Tiêu chuẩn IEEE 830 / ISO/IEC/IEEE 29148:2018
Dự án: Blog Ẩm thực và Nấu ăn
Culinary Blog
Phiên bản tài liệu 1.0.0
Ngày phát hành 04/06/2026
Trạng thái Đã duyệt (Approved)
Công nghệ Backend .NET 10 Minimal APIs, C#
Công nghệ Frontend Next.js App Router, TypeScript
Cơ sở dữ liệu PostgreSQL 16
Object Storage MinIO (S3-Compatible)
Cache Redis 7
Tài liệu này được biên soạn theo tiêu chuẩn IEEE 830 / ISO/IEC/IEEE 29148:2018.

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
LỊCH SỬ THAY ĐỔI TÀI LIỆU

| Phiên |                   |                   | Trạng |
| ----- | ----------------- | ----------------- | ----- |
| Ngày  | Tác giả / Vai trò | Nội dung thay đổi |       |
| bản   |                   |                   | thái  |

Phát hành lần đầu – Bản hoàn
| 1.0.0 04/06/2026 | Senior BA / Architect | | Approved |
| ------------------ | ---------------------- | --- | --------- |
chỉnh theo IEEE 830 / ISO 29148.
| | | Bổ sung Chương 7 (Data Model), | Under |
| ------------------ | ---------- | -------------------------------- | ------- |
| 0.9.0 20/05/2026 | Senior BA | | |
| | | Chương 8 (API Spec) và Phụ lục. | Review |
Hoàn thiện Chương 3 (FR), bổ
| 0.8.0 05/05/2026 | Senior BA | | Draft |
| ------------------ | ---------- | --- | ------ |
sung FR-FILE, FR-JOB, FR-OBS.
Phác thảo ban đầu: Chương 1–4
| 0.5.0 15/04/2026 | Senior BA | | Draft |
| ------------------ | ---------- | --- | ------ |
(skeleton).

Phê duyệt tài liệu: Tài liệu phiên bản 1.0.0 đã được xem xét và phê duyệt bởi Trưởng nhóm
Kiến trúc Hệ thống (Lead Systems Architect). Mọi thay đổi từ phiên bản 1.0.0 trở đi đều phải
thông qua quy trình Change Request (CR) và được cập nhật vào bảng này.
| | | | |
| --- | --- | --- | --- |
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao • Trang 2 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
MỤC LỤC
LỊCH SỬ THAY ĐỔI TÀI LIỆU .................................................................................................. 2
MỤC LỤC ................................................................................................................................... 3
CHƯƠNG 1. GIỚI THIỆU ......................................................................................................... 6
1.1. Mục đích Tài liệu ............................................................................................................. 6
1.2. Phạm vi Sản phẩm.......................................................................................................... 6
1.2.1. Tên và Định danh ..................................................................................................... 6
1.2.2. Mô tả Sản phẩm ....................................................................................................... 6
1.2.3. Những gì KHÔNG thuộc phạm vi............................................................................. 7
1.3. Định nghĩa, Từ viết tắt và Ký hiệu .................................................................................. 7
1.4. Tài liệu Tham chiếu......................................................................................................... 8
1.5. Tổng quan Tài liệu ........................................................................................................ 10
CHƯƠNG 2. MÔ TẢ TỔNG QUAN HỆ THỐNG .................................................................... 11
2.1. Bối cảnh Sản phẩm....................................................................................................... 11
2.1.1. Vị trí trong Hệ sinh thái ........................................................................................... 11
2.1.2. Quan hệ với Hệ thống Ngoài.................................................................................. 11
2.2. Chức năng Sản phẩm Tổng quát ................................................................................. 12
2.3. Các Lớp Người dùng và Đặc điểm............................................................................... 12
2.4. Môi trường Vận hành .................................................................................................... 13
2.4.1. Môi trường Server (Production) ............................................................................. 13
2.4.2. Môi trường Phát triển (Development) .................................................................... 13
2.4.3. Yêu cầu Trình duyệt Client..................................................................................... 14
2.5. Ràng buộc Thiết kế và Hiện thực ................................................................................. 14
2.6. Giả định và Phụ thuộc................................................................................................... 15
2.6.1. Giả định................................................................................................................... 15
2.6.2. Phụ thuộc Bên ngoài .............................................................................................. 15
CHƯƠNG 3. YÊU CẦU CHỨC NĂNG CHI TIẾT ................................................................... 17
3.1. Module Xác thực và Quản lý Người dùng (FR-AUTH) ................................................ 17
FR-AUTH-001: Đăng ký Tài khoản (User Registration)................................................... 17
FR-AUTH-002: Đăng nhập bằng Email/Mật khẩu (Local Login) ..................................... 18
FR-AUTH-003: Đăng nhập bằng Google OAuth 2.0 ....................................................... 19
FR-AUTH-004: Làm mới Access Token (Token Refresh) ............................................... 20
FR-AUTH-005: Đăng xuất (Logout / Token Revocation) ................................................. 21
FR-AUTH-006: Xem Hồ sơ Cá nhân (View Profile)......................................................... 22
FR-AUTH-007: Cập nhật Hồ sơ Cá nhân (Update Profile) ............................................. 23
3.2. Module Quản lý Danh mục (FR-CAT) .......................................................................... 23
FR-CAT-001: Xem Danh sách Danh mục........................................................................ 23
FR-CAT-002: Xem Chi tiết Danh mục và Công thức ....................................................... 24
FR-CAT-003: Tạo Danh mục Mới [Admin]....................................................................... 25
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao • Trang 3 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
FR-CAT-004: Cập nhật Danh mục [Admin] ..................................................................... 26
FR-CAT-005: Xóa Danh mục [Admin] .............................................................................. 26
3.3. Module Quản lý Công thức Nấu ăn (FR-RCP)............................................................. 27
FR-RCP-001: Xem Danh sách Công thức (Paginated + Filtered + Sorted) ................... 27
FR-RCP-002: Xem Chi tiết Công thức ............................................................................. 28
FR-RCP-003: Tạo Công thức Nấu ăn Mới [Author/Admin] ............................................. 29
FR-RCP-004: Cập nhật Công thức [Author-Owner/Admin] ............................................. 30
FR-RCP-005: Xuất bản / Hủy Xuất bản Công thức ......................................................... 31
FR-RCP-006: Lưu trữ Công thức (Archive) ..................................................................... 32
FR-RCP-007: Xóa Công thức [Author-Owner/Admin] ..................................................... 32
FR-RCP-008: Quản lý Ảnh Công thức (Upload / Set Primary / Delete) .......................... 33
FR-RCP-009: Quản lý Nguyên liệu (CRUD RecipeIngredient) ....................................... 34
FR-RCP-010: Quản lý Các bước Thực hiện (CRUD RecipeStep).................................. 35
3.4. Module Tìm kiếm và Phân trang (FR-SRCH) ............................................................... 36
FR-SRCH-001: Tìm kiếm Toàn văn bản (Full-Text Search)............................................ 36
FR-SRCH-002/003/004: Lọc, Sắp xếp và Phân trang (Tóm tắt) ..................................... 37
3.5. Module Quản lý Tệp tin (FR-FILE) ............................................................................... 37
3.6. Module Background Jobs (FR-JOB)............................................................................. 38
3.7. Module Quan sát Hệ thống (FR-OBS).......................................................................... 39 4. Yêu cầu Phi Chức năng (NFR)............................................................................................ 40
4.1. Hiệu năng (NFR-PERF) ................................................................................................ 40
4.2. Bảo mật (NFR-SEC) ..................................................................................................... 41
4.3. Khả năng Sử dụng (NFR-USE) .................................................................................... 42
4.4. Độ tin cậy (NFR-REL) ................................................................................................... 42
4.5. Khả năng Bảo trì (NFR-MAINT).................................................................................... 43
4.6. Khả năng Mở rộng (NFR-SCALE) ................................................................................ 44
4.7. Tối ưu SEO (NFR-SEO) ............................................................................................... 44 5. Yêu cầu Giao diện Ngoài ..................................................................................................... 46
5.1. Giao diện Người dùng (UI) ........................................................................................... 46
5.2. Giao diện Phần mềm – REST API ............................................................................... 47
5.3. Giao diện Dịch vụ Bên thứ ba....................................................................................... 47
5.4. Giao diện Phần cứng .................................................................................................... 48 6. Kiến trúc Hệ thống ............................................................................................................... 50
6.1. Tổng quan Kiến trúc...................................................................................................... 50
6.2. Kiến trúc Backend – Clean Architecture....................................................................... 50
6.3. CQRS + MediatR Pipeline ............................................................................................ 51
6.4. Mô hình Quan hệ Thực thể (ERD tóm tắt) ................................................................... 52
6.5. Triển khai – Docker Compose ...................................................................................... 52 7. Mô hình Dữ liệu ................................................................................................................... 54
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao • Trang 4 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
7.1. BaseEntity (Abstract) .................................................................................................... 54
7.2. Recipe ........................................................................................................................... 54
7.2.1. RecipeNutrition (Owned Entity — cột trong bảng Recipes) .................................. 56
7.3. RecipeStep .................................................................................................................... 57
7.4. RecipeIngredient ........................................................................................................... 57
7.5. RecipeImage ................................................................................................................. 58
7.6. Category ........................................................................................................................ 58
7.7. ApplicationUser (extends IdentityUser) ........................................................................ 58
7.8. RefreshToken................................................................................................................ 59 8. Đặc tả REST API ................................................................................................................. 61
8.1. Authentication Module (/auth) ....................................................................................... 61
8.2. Categories Module (/categories)................................................................................... 62
8.3. Recipes Module (/recipes) ............................................................................................ 63
8.4. Recipe Images (/recipes/{id}/images) ........................................................................... 64
8.5. Recipe Steps (/recipes/{id}/steps) ................................................................................ 64
8.6. Recipe Ingredients (/recipes/{id}/ingredients)............................................................... 65
8.7. Health Check Endpoints ............................................................................................... 65
Phụ lục A – HTTP Status Codes ............................................................................................. 67
Phụ lục B – Application Error Codes ....................................................................................... 67
Phụ lục C – Từ điển Thuật ngữ ............................................................................................... 69
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao • Trang 5 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
CHƯƠNG 1. GIỚI THIỆU
1.1. Mục đích Tài liệu
Tài liệu Đặc tả Yêu cầu Phần mềm (Software Requirements Specification – SRS) này được
biên soạn theo tiêu chuẩn IEEE 830-1998 và ISO/IEC/IEEE 29148:2018 nhằm mô tả đầy đủ,
chính xác và nhất quán toàn bộ yêu cầu chức năng (Functional Requirements) và yêu cầu phi
chức năng (Non-Functional Requirements) của dự án ứng dụng web Blog Ẩm thực và Nấu
ăn (Culinary Blog).
Tài liệu này phục vụ các đối tượng sau:
• Nhóm phát triển Backend (.NET 10/C#): Căn cứ thiết kế API, domain model, và
business rules.
• Nhóm phát triển Frontend (Next.js/TypeScript): Căn cứ thiết kế giao diện, luồng
người dùng và tích hợp API.
• Kỹ sư Kiểm thử (QA/QC): Cơ sở xây dựng test cases, kiểm thử chấp nhận
(acceptance testing).
• Kiến trúc sư Hệ thống: Tham chiếu khi đưa ra quyết định kiến trúc (architecture
decisions).
• Giảng viên và Sinh viên: Tài liệu học thuật mẫu cho dự án thực hành xuyên suốt
giáo trình.
• Stakeholder / Product Owner: Phê duyệt phạm vi và ưu tiên tính năng.
Phạm vi hiệu lực: Tài liệu này có hiệu lực từ phiên bản 1.0.0 và là tài liệu nền tảng (baseline)
cho toàn bộ vòng đời phát triển dự án. Mọi thay đổi yêu cầu sau khi tài liệu được phê duyệt
phải tuân theo quy trình quản lý thay đổi (Change Management Process).
1.2. Phạm vi Sản phẩm
1.2.1. Tên và Định danh
Thuộc tính Giá trị
Tên sản phẩm Culinary Blog – Blog Ẩm thực và Nấu ăn
Định danh dự án CULINARY-BLOG-V1
Loại hệ thống Ứng dụng Web Full-Stack (API-Driven Architecture)
Phiên bản sản phẩm 1.0.0
Môi trường đích Cloud/On-premise (Docker Compose + Nginx)
1.2.2. Mô tả Sản phẩm
Culinary Blog là một nền tảng web cho phép người dùng chia sẻ, khám phá và lưu trữ các
công thức nấu ăn từ nhiều nền ẩm thực khác nhau. Ứng dụng cung cấp hệ sinh thái hoàn
chỉnh bao gồm:
• Nền tảng chia sẻ công thức: Tác giả (Author) đăng tải công thức với hình ảnh,
danh sách nguyên liệu chi tiết, hướng dẫn từng bước thực hiện và thông tin dinh
dưỡng.
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 6 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
• Tổ chức nội dung: Phân loại công thức theo danh mục (Category), độ khó
(Difficulty Level), thời gian chuẩn bị và nấu.
• Tìm kiếm thông minh: Full-Text Search tiếng Việt sử dụng PostgreSQL
tsvector/tsquery với unaccent extension.
• Bảo mật đa lớp: Xác thực JWT stateless, phân quyền theo vai trò (RBAC) và theo
tài nguyên (Resource-Based Authorization), đăng nhập Google OAuth 2.0.
• Tối ưu hiệu năng và SEO: Redis distributed cache, Next.js ISR, Open Graph
Protocol, JSON-LD Schema.org Recipe markup.
• Quan sát hệ thống: Structured logging (Serilog), distributed tracing
(OpenTelemetry), health check endpoints.
1.2.3. Những gì KHÔNG thuộc phạm vi
Các tính năng sau đây nằm ngoài phạm vi phiên bản 1.0.0:
• Hệ thống bình luận (Comment System) và đánh giá sao (Rating System).
• Tính năng lưu/đánh dấu công thức yêu thích (Bookmark/Favorite).
• Thông báo real-time (SignalR/WebSocket).
• Ứng dụng di động native (iOS/Android).
• Thanh toán / Tính năng thương mại điện tử.
• Hệ thống nhắn tin trực tiếp giữa người dùng.
• GraphQL API (định hướng sau khóa học).
1.3. Định nghĩa, Từ viết tắt và Ký hiệu
Thuật ngữ / Viết tắt Định nghĩa đầy đủ
Software Requirements Specification – Đặc tả Yêu cầu Phần
SRS
mềm.
FR Functional Requirement – Yêu cầu chức năng.
NFR Non-Functional Requirement – Yêu cầu phi chức năng.
API Application Programming Interface – Giao diện lập trình ứng dụng.
Representational State Transfer – Kiểu kiến trúc API phổ biến
REST
nhất.
JWT JSON Web Token – Chuẩn token xác thực stateless (RFC 7519).
RBAC Role-Based Access Control – Kiểm soát truy cập dựa trên vai trò.
Command Query Responsibility Segregation – Pattern tách biệt
CQRS
lệnh và truy vấn.
Domain-Driven Design – Phương pháp thiết kế phần mềm lấy
DDD
domain làm trung tâm.
Object-Relational Mapper – Công cụ ánh xạ object-database (EF
ORM
Core).
FTS Full-Text Search – Tìm kiếm toàn văn bản.
Incremental Static Regeneration – Kỹ thuật tái tạo trang tĩnh của
ISR
Next.js.
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 7 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
Thuật ngữ / Viết tắt Định nghĩa đầy đủ
Largest Contentful Paint – Core Web Vital đo tốc độ tải nội dung
LCP
lớn nhất.
Cumulative Layout Shift – Core Web Vital đo độ ổn định bố cục
CLS
trang.
Interaction to Next Paint – Core Web Vital đo thời gian phản hồi
INP
tương tác.
Continuous Integration / Continuous Delivery – Tích hợp và triển
CI/CD
khai liên tục.
Device-independent pixel unit used in OOXML (1 inch = 1440
DXA
DXA).
TTL Time-To-Live – Thời gian sống của dữ liệu trong cache.
SSR Server-Side Rendering – Render HTML trên server.
SSG Static Site Generation – Tạo trang tĩnh lúc build time.
Must Have / Should Have / Could Have / Won't Have – Mô hình
MoSCoW
phân loại ưu tiên.
Request For Comments – Tài liệu tiêu chuẩn kỹ thuật (e.g., RFC
RFC
7807).
ERD Entity Relationship Diagram – Sơ đồ quan hệ thực thể.
Password-Based Key Derivation Function 2 – Thuật toán hash mật
PBKDF2
khẩu an toàn.
CDN Content Delivery Network – Mạng phân phối nội dung.
Multipurpose Internet Mail Extensions – Chuẩn định dạng tệp trên
MIME
Internet.
JavaScript Object Notation for Linked Data – Định dạng dữ liệu có
JSON-LD
cấu trúc cho SEO.
1.4. Tài liệu Tham chiếu
Tài liệu / Tiêu
STT Nguồn / URL
chuẩn
IEEE Std 830-
1998 –
Recommended
1 Practice for https://ieeexplore.ieee.org/document/720574
Software
Requirements
Specifications
ISO/IEC/IEEE
29148:2018 –
2 https://www.iso.org/standard/72089.html
Requirements
Engineering
OWASP Top
3 https://owasp.org/www-project-top-ten/
10:2021 – Top 10
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 8 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
Tài liệu / Tiêu
STT Nguồn / URL
chuẩn
Web Application
Security Risks
RFC 7807 –
4 Problem Details https://datatracker.ietf.org/doc/html/rfc7807
for HTTP APIs
RFC 7519 –
5 JSON Web Token https://datatracker.ietf.org/doc/html/rfc7519
(JWT)
RFC 6749 – The
OAuth 2.0
6 https://datatracker.ietf.org/doc/html/rfc6749
Authorization
Framework
.NET 10 Minimal
7 APIs – Microsoft https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis
Learn
ASP.NET Core
8 Identity – https://learn.microsoft.com/aspnet/core/security/authentication/identity
Microsoft Learn
Entity Framework
9 Core 10 https://learn.microsoft.com/ef/core/
Documentation
Next.js 15 App
10 Router https://nextjs.org/docs
Documentation
PostgreSQL 16
11 Documentation – https://www.postgresql.org/docs/16/textsearch.html
Full-Text Search
Redis 7
12 https://redis.io/docs/
Documentation
MinIO S3-
13 Compatible https://min.io/docs/
Object Storage
Google Web
14 Vitals – Core Web https://web.dev/explore/learn-core-web-vitals
Vitals
Schema.org
15 Recipe – https://schema.org/Recipe
Structured Data
OpenTelemetry
16 .NET https://opentelemetry.io/docs/languages/dotnet/
Documentation
Serilog
17 https://serilog.net/
Documentation
Hangfire
18 https://docs.hangfire.io/
Documentation
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 9 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
Tài liệu / Tiêu
STT Nguồn / URL
chuẩn
FluentValidation
19 https://docs.fluentvalidation.net/
Documentation
Giáo trình Phát
triển Ứng dụng
20 N/A (tài liệu nội bộ)
Web Nâng cao V4
– Nội bộ
1.5. Tổng quan Tài liệu
Tài liệu SRS này được tổ chức thành 8 chương chính và 3 phụ lục, theo cấu trúc từ tổng quan
đến chi tiết:
• Chương 2 – Mô tả Tổng quan: Bối cảnh sản phẩm, chức năng tóm tắt, các lớp
người dùng, môi trường vận hành và ràng buộc thiết kế.
• Chương 3 – Yêu cầu Chức năng: 27 FR được đặc tả chi tiết theo format chuẩn,
nhóm thành 7 module chức năng.
• Chương 4 – Yêu cầu Phi chức năng: Hiệu năng, bảo mật, khả năng sử dụng, độ
tin cậy, khả năng bảo trì/mở rộng và SEO.
• Chương 5 – Giao diện Ngoài: Tích hợp với các hệ thống và dịch vụ ngoài (Google
OAuth, MinIO, Redis, SendGrid).
• Chương 6 – Kiến trúc Hệ thống: Clean Architecture Backend, Next.js App Router
Frontend, chiến lược caching và deployment.
• Chương 7 – Mô hình Dữ liệu: ERD mô tả văn bản và bảng định nghĩa chi tiết từng
entity/table.
• Chương 8 – Đặc tả API REST: Quy ước, chuẩn lỗi RFC 7807, và bảng tổng hợp tất
cả ~30 endpoint.
• Phụ lục A-C: HTTP Status Codes, Application Error Codes, và Từ điển thuật ngữ.
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 10 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
CHƯƠNG 2. MÔ TẢ TỔNG QUAN HỆ THỐNG

2.1. Bối cảnh Sản phẩm
2.1.1. Vị trí trong Hệ sinh thái
Culinary Blog vận hành theo mô hình API-Driven Architecture, trong đó Backend (.NET 10)
và Frontend (Next.js) là hai hệ thống độc lập giao tiếp hoàn toàn qua HTTP/JSON RESTful
API. Không có server-side rendering truyền thống (MVC Razor/Blazor) hay shared view
engine giữa hai tầng.

Sơ đồ bối cảnh hệ thống (Context Diagram):

┌─────────────────────────────────────────────────────────────────┐
│ CULINARY BLOG SYSTEM │
│ │
│ ┌──────────────────┐ ┌───────────────────────────────┐ │
│ │ NEXT.JS FRONTEND│◄──────►│ .NET 10 BACKEND API │ │
│ │ (App Router) │ REST │ (Minimal APIs + Clean Arch) │ │
│ │ Port: 3000 │ JSON │ Port: 5000 │ │
│ └──────────────────┘ └──────────────┬────────────────┘ │
│ │ │
│ ┌──────┐ ┌────────┐ ┌────────┐ ┌────────┐ ┌───────────┐ │
│ │ Pgsql│ │ Redis │ │ MinIO │ │Hangfire│ │Google Auth│ │
│ │:5432 │ │:6379 │ │:9000 │ │ Jobs │ │ OAuth2.0 │ │
│ └──────┘ └────────┘ └────────┘ └────────┘ └───────────┘ │
└─────────────────────────────────────────────────────────────────┘

Hình 2.1. Sơ đồ bối cảnh hệ thống Culinary Blog

2.1.2. Quan hệ với Hệ thống Ngoài
| Hệ thống Ngoài | Vai trò | Giao thức / Chuẩn | Hướng tích hợp |
| --------------- | ----------------------- | -------------------- | --------------------- |
| | Hệ quản trị CSDL quan | TCP + Npgsql Driver | |
| Supabase | hệ chính (PostgreSQL 16 | | Backend → Supabase |
| Database | managed database) | (EF Core) | PostgreSQL |
| | Distributed Cache & | TCP + | |
| Redis 7 | | | Backend → Redis |
| | Session Store | StackExchange.Redis | |
| | Object Storage cho ảnh | HTTP/S3 API + | |
| MinIO (S3) | | | Backend → MinIO |
| | công thức | MinIO .NET SDK | |
Google OAuth Đăng nhập bên thứ ba HTTPS + OpenID Client ↔ Google ↔
| 2.0 | (Identity Provider) | Connect | Backend |
| ---- | -------------------- | -------- | -------- |
Hangfire Background Job In-process (.NET) Backend (internal)
Processing (embedded)
Structured Log
| Serilog / Seq | Aggregation | HTTP Sink → Seq | Backend → Seq |
| -------------- | ------------ | ---------------- | -------------- |
(development)
| OpenTelemetry | Distributed Tracing & | | |
| -------------- | ---------------------- | ------------ | -------------------- |
| | | OTLP / gRPC | Backend → Collector |
| Collector | Metrics (production) | | |
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 11 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
| Hệ thống Ngoài | Vai trò | Giao thức / Chuẩn | | Hướng tích hợp | |
| --------------- | ---------------------- | ------------------ | --- | ----------------- | --- |
| Nginx (Reverse | SSL termination, load | | | Client → Nginx → | |
HTTP/HTTPS
| Proxy) | balancing, static serving | | | Services | |
| ------- | -------------------------- | --- | --- | --------- | --- |

2.2. Chức năng Sản phẩm Tổng quát
Culinary Blog cung cấp 7 nhóm chức năng chính, được hiện thực hóa qua 27 Functional
Requirements chi tiết tại Chương 3:

| Nhóm chức năng | Mã nhóm | Số Mô tả tóm tắt |     |     |     |
| -------------- | ------- | ---------------- | --- | --- | --- |

FR
Xác thực & Quản lý Đăng ký, đăng nhập (email + Google), JWT
FR-AUTH 7
| Người dùng | | refresh token, logout, quản lý profile. | | | |
| ----------- | --- | ---------------------------------------- | --- | --- | --- |
CRUD danh mục công thức (Category) – phân
| Quản lý Danh mục | FR-CAT | 5 | | | |
| ----------------- | ------- | --- | --- | --- | --- |
quyền Admin.
Quản lý Công thức nấu CRUD recipe, publish/archive, quản lý
FR-RCP 10
| ăn | | ảnh/bước/nguyên liệu. | | | |
| --- | --- | ---------------------- | --- | --- | --- |
Tìm kiếm & Phân trang FR-SRCH 4 Full-Text Search (PostgreSQL), filter, sort,
offset pagination.
Quản lý Tệp tin FR-FILE 2 Upload/Delete ảnh trên MinIO S3-compatible.
Email chào mừng, thumbnail generation,
| Background Jobs | FR-JOB | 3 | | | |
| ---------------- | ------- | --- | --- | --- | --- |
sitemap XML (Hangfire).
Health checks, structured logging, distributed
| Quan sát Hệ thống | FR-OBS | 3 | | | |
| ------------------ | ------- | --- | --- | --- | --- |
tracing.

2.3. Các Lớp Người dùng và Đặc điểm
Hệ thống định nghĩa 3 loại tác nhân (Actor) với quyền hạn khác nhau:

Ưu tiên
| Vai trò | Mô tả | Điều kiện | Quyền hạn chính | | phục |
| -------- | ------ | ---------- | ---------------- | --- | ----- |
vụ
Cao
Người dùng chưa
| | | | Xem danh sách & chi tiết | | (đây là |
| --- | --- | --- | ------------------------- | --- | -------- |
xác thực, truy cập
Khách (Guest Không cần tài recipe (Published), xem đại đa
ứng dụng mà
| / Anonymous) | | khoản | danh mục, tìm kiếm. | | số |
| ------------- | --- | ------ | -------------------- | --- | --- |
không có tài
| | | | KHÔNG được tạo/sửa/xóa. | | người |
| --- | --- | --- | ------------------------ | --- | ------ |
khoản.
dùng)

- Tất cả quyền của Guest.
  | | Người dùng đã | | | | Cao |
  | --- | -------------- | --- | --- | --- | ---- |
- Tạo/sửa/xóa recipe CỦA
  | | đăng ký và xác | | | | (nhà |
  | --------- | ----------------- | --------------- | ------------------------- | --- | --------- |
  | Tác giả | | Có tài khoản & | MÌNH. + Upload ảnh, quản | | |
  | | thực thành công. | | | | sản |
  | (Author) | | JWT hợp lệ | lý steps/ingredients. + | | |
  | | Được tự động gán | | | | xuất nội |
  Publish/Archive recipe của
  | | khi đăng ký. | | | | dung) |
  | --- | ------------- | --- | --- | --- | ------ |
  mình.
  | | Người quản lý hệ | | + Tất cả quyền của Author. | | |
  | -------------- | ----------------- | --------------- | --------------------------- | --- | --------- |
  | Quản trị viên | | Có tài khoản & | | | Trung |
  | | thống với quyền | | + Quản lý (CRUD) danh | | |
  | (Admin) | | role Admin | | | bình (số |
  | | cao nhất. Được | | mục. + Sửa/xóa bất kỳ | | |
  CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 12 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
Ưu tiên
| Vai trò | Mô tả | Điều kiện | Quyền hạn chính | phục |
| -------- | ------ | ---------- | ---------------- | ----- |
vụ
| | gán thủ công qua | | recipe của bất kỳ Author. + | lượng |
| --- | ------------------ | --- | ---------------------------- | ------ |
| | database seeding. | | Truy cập Hangfire | ít) |
Dashboard. + Xem
structured logs.

Ghi chú về phân quyền: Hệ thống triển khai 3 tầng phân quyền. (1) Role-Based
Authorization: phân biệt quyền dựa trên role (Guest/Author/Admin). (2) Resource-Based
Authorization: Author chỉ sửa/xóa được recipe của chính mình (AuthorId == currentUserId).
(3) Policy-Based Authorization: Policy "VerifiedAuthor" yêu cầu email đã xác nhận. Admin có
quyền bypass resource ownership check.

2.4. Môi trường Vận hành
2.4.1. Môi trường Server (Production)
| Thành phần | Yêu cầu tối thiểu | Khuyến nghị | Ghi chú | |
| ------------- | ------------------ | ---------------- | -------------------------- | --- |
| | Linux Ubuntu | Ubuntu 22.04 | | |
| Hệ điều hành | | | Docker phải được cài đặt | |
| | 22.04 LTS | LTS / Debian 12 | | |
| | .NET 10.0 Runtime | .NET 10.0.x | Cung cấp qua Docker image | |
.NET Runtime
(aspnet) latest patch mcr.microsoft.com/dotnet/aspnet:10.0
| | Node.js 20 LTS | | Chỉ cần lúc build Next.js; production | |
| ----------- | ---------------- | --------------- | -------------------------------------- | --- |
| Node.js | | Node.js 22 LTS | | |
| | (build only) | | dùng standalone output | |
| | | PostgreSQL | Extensions: unaccent, pg_trgm bắt | |
| PostgreSQL | PostgreSQL 16.x | | | |
| | | 16.x | buộc | |
| Redis | Redis 7.x | Redis 7.2.x | Persistent mode với AOF | |
| | MinIO | MinIO latest | Bucket policy: public-read cho recipe | |
MinIO
| | RELEASE.2024+ | stable | images | |
| --- | -------------- | ------- | ------- | --- |
Docker Engine
| | Docker Engine | | Docker Compose cho local dev và | |
| ------- | -------------- | --------------- | -------------------------------- | --- |
| Docker | | 27.x + Compose | | |
| | 24.x | | staging | |
v2
Nginx 1.26+
| Nginx | Nginx 1.24+ | | Reverse proxy, SSL termination | |
| ------ | ------------ | --- | ------------------------------- | --- |
(stable)
| RAM | 4 GB minimum | 8 GB+ | RAM cần tăng nếu Redis cache lớn | |
| ---- | ------------- | ------ | --------------------------------- | --- |
CPU-intensive: FTS indexing, image
| CPU | 2 vCPU minimum | 4 vCPU+ | | |
| ---- | --------------- | -------- | --- | --- |
processing
Disk 20 GB SSD 50 GB+ SSD MinIO object storage tốn nhiều disk
minimum

2.4.2. Môi trường Phát triển (Development)
| Thành phần | Yêu cầu | | | |
| ------------ | ------------------------------------------- | --- | --- | --- |
| .NET 10 SDK | dotnet SDK 10.0.x (bao gồm CLI và runtime) | | | |
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 13 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
Thành phần Yêu cầu
Node.js Node.js 20+ LTS với npm 10+
Docker Desktop 4.x+ (Windows/macOS) hoặc Docker Engine (Linux) –
Docker Desktop
để chạy PostgreSQL, Redis, MinIO local
Visual Studio 2022 v17.12+ / Rider 2024+ / VS Code với C# Dev Kit
IDE / Editor
extension
Git Git 2.40+ với Git LFS (nếu lưu asset lớn)
Postman / Scalar Postman hoặc Scalar UI (tích hợp sẵn, chạy tại /scalar) để test API
2.4.3. Yêu cầu Trình duyệt Client
Trình duyệt Phiên bản tối thiểu Ghi chú
Khuyến nghị chính – tốt nhất cho
Google Chrome 90+
Developer Tools
Mozilla Firefox 88+ Hỗ trợ đầy đủ
Microsoft Edge 90+ (Chromium) Hỗ trợ đầy đủ (Chromium-based)
Hỗ trợ đầy đủ; Safari 13 trở xuống
Safari 14+ (macOS 11+)
KHÔNG đảm bảo
Mobile Chrome (Android) 90+ Responsive design, touch-friendly
Mobile Safari (iOS) iOS 14+ Hỗ trợ đầy đủ
Internet Explorer Mọi phiên bản KHÔNG hỗ trợ (EOL)
2.5. Ràng buộc Thiết kế và Hiện thực
Các ràng buộc sau đây là bắt buộc và không thể thương lượng trong suốt quá trình phát triển:
Mã ràng
Loại Mô tả ràng buộc
buộc
Backend PHẢI tuân thủ Clean Architecture với 4 tầng riêng biệt:
CONS-001 Kiến trúc Domain, Application, Infrastructure, Presentation. Tầng Domain
không được phụ thuộc bất kỳ thư viện ngoài nào.
CQRS với MediatR là pattern bắt buộc cho tầng Application. Mỗi
CONS-002 Pattern use case được hiện thực dưới dạng Command hoặc Query
Handler riêng biệt.
Ngôn ngữ / Backend: .NET 10 Minimal APIs (không dùng MVC Controllers).
CONS-003
Framework Frontend: Next.js App Router (không dùng Pages Router).
Xác thực PHẢI sử dụng JWT stateless (access token 15 phút,
CONS-004 Bảo mật refresh token 7 ngày). Mật khẩu PHẢI được hash với PBKDF2 qua
ASP.NET Core Identity.
API PHẢI tuân thủ RESTful design. Phản hồi lỗi PHẢI theo RFC
CONS-005 API Design 7807 (application/problem+json). API versioning qua URL path
(/api/v1/).
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 14 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
Mã ràng
| | Loại Mô tả ràng buộc | | |
| --- | ---------------------- | --- | --- |
buộc
Supabase Database (PostgreSQL 16) là DBMS duy nhất. Migrations qua EF Core Code First.
CONS-006 Database First. Không viết raw SQL trực tiếp (dùng LINQ hoặc Raw SQL có
parameterization qua EF Core).
Kích thước tệp tải lên tối đa 5 MB. Định dạng chỉ chấp nhận:
| CONS-007 | File Upload | | |
| --------- | ------------ | --- | --- |
image/jpeg, image/png, image/webp, image/avif. Kiểm tra MIME
type (không chỉ extension).
Input validation PHẢI qua FluentValidation kết hợp MediatR
| CONS-008 | Validation | | |
| --------- | ----------- | --- | --- |
Pipeline Behavior. Không validation trong Endpoint handler.
Ứng dụng PHẢI được đóng gói Docker. Dockerfile multi-stage
CONS-009 Container build (SDK → aspnet runtime). Docker Compose cho local
development.
CONS-010 Logging Structured logging với Serilog là bắt buộc. Mọi log entry PHẢI có
CorrelationId, RequestPath, UserId (khi đã xác thực).

2.6. Giả định và Phụ thuộc
2.6.1. Giả định
• Môi trường development có kết nối Internet để pull Docker images và package
NuGet/npm.
• Supabase Database được sử dụng làm managed PostgreSQL database cho môi
trường development và production. Redis và MinIO được cung cấp qua Docker
Compose trong development hoặc dưới dạng managed service trong production.
• Người dùng cuối có trình duyệt hiện đại và kết nối Internet đủ ổn định để load ảnh từ
MinIO.
• Dữ liệu test (seed) được tạo bằng thư viện Bogus với 50 recipe mẫu và 5 tác giả
mẫu.
• Email service (SendGrid hoặc SMTP) được cấu hình sẵn khi triển khai production để
gửi email chào mừng.
• Giới hạn dữ liệu kỳ vọng (initial scale): ≤ 10,000 công thức, ≤ 5,000 người dùng,
≤ 50 danh mục – phù hợp với single-server deployment.

2.6.2. Phụ thuộc Bên ngoài
Mức độ ảnh hưởng
| Phụ thuộc | Phiên bản | | Kế hoạch dự phòng |
| ---------- | ---------- | --- | ------------------ |
nếu không khả dụng
Vẫn có đăng nhập
Google OAuth v2 (OpenID Cao – Mất chức năng email/password. Hiển thị thông
| 2.0 API | Connect) | đăng nhập Google | |
| -------- | --------- | ----------------- | --- |
báo "Google login tạm thời
không khả dụng".
Fallback về local FileSystem
| | MinIO | Cao – Không | |
| ----------- | -------------- | -------------------- | ---------------------------- |
| MinIO / S3 | | | storage (development only). |
| | RELEASE.2024+ | upload/xem được ảnh | |
Production cần MinIO.
Hệ thống tiếp tục hoạt động
| | | Trung bình – Mất | nhưng mọi request đều query |
| ------ | ---- | ---------------------- | ------------------------------ |
| Redis | 7.x | | |
| | | cache, hiệu năng giảm | database. Cache miss graceful |
degradation.
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 15 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
Mức độ ảnh hưởng
Phụ thuộc Phiên bản Kế hoạch dự phòng
nếu không khả dụng
Backup định kỳ (pg_dump).
Rất cao – Toàn bộ hệ
PostgreSQL 16.x Readiness probe sẽ fail, Nginx
thống ngừng
trả 503.
Fire-and-forget jobs sẽ bị mất;
Hangfire (in- Thấp – Background Recurring jobs bỏ qua chu kỳ.
v1.8+
process) jobs không chạy Không ảnh hưởng core
functionality.
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 16 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
CHƯƠNG 3. YÊU CẦU CHỨC NĂNG CHI TIẾT
Chương này đặc tả chi tiết 27 Functional Requirements (FR) được nhóm thành 7 module
chức năng. Mỗi FR được mô tả theo template chuẩn bao gồm: Mã yêu cầu, Tên, Nhóm chức
năng, Tác nhân, Mức ưu tiên (MoSCoW), Mô tả, Điều kiện tiên quyết, Luồng chính, Luồng
thay thế/Ngoại lệ, HTTP Endpoint, Kết quả mong đợi và HTTP Status Code.
Quy ước mức ưu tiên MoSCoW: M (Must Have – Bắt buộc), S (Should Have – Nên có), C
(Could Have – Có thể có), W (Won't Have – Không trong scope hiện tại).
3.1. Module Xác thực và Quản lý Người dùng (FR-AUTH)
Module này quản lý toàn bộ vòng đời xác thực người dùng: từ đăng ký, đăng nhập đa phương
thức, duy trì phiên làm việc với cơ chế token rotation, đến quản lý hồ sơ cá nhân. Backend
sử dụng ASP.NET Core Identity kết hợp JWT và OAuth 2.0.
FR-AUTH-001: Đăng ký Tài khoản (User Registration)
Mã yêu cầu FR-AUTH-001
Tên yêu cầu Đăng ký Tài khoản Mới
Nhóm chức năng Module Xác thực và Quản lý Người dùng (FR-AUTH)
Tác nhân Khách (Guest / Anonymous User)
Mức ưu tiên M – Must Have (Bắt buộc)
(MoSCoW)
Hệ thống cho phép người dùng chưa có tài khoản tạo một tài khoản
mới bằng cách cung cấp thông tin cơ bản. Sau khi đăng ký thành công,
Mô tả người dùng tự động được gán role "Author" và nhận bộ token để truy
cập ngay lập tức (auto-login sau đăng ký). Hệ thống kích hoạt job gửi
email chào mừng bất đồng bộ qua Hangfire.

1. Người dùng chưa đăng nhập vào hệ thống. 2. Endpoint POST
   Điều kiện tiên quyết /api/v1/auth/register đang hoạt động. 3. PostgreSQL database đang kết
   nối thành công.
1. Người dùng (client) gửi HTTP POST đến /api/v1/auth/register với
   JSON body: { "fullName": "...", "email": "...", "userName": "...",
   "password": "..." }.
1. RegisterCommand được tạo và dispatch đến MediatR.
1. ValidationBehavior chạy RegisterCommandValidator: kiểm tra
   fullName không rỗng, email đúng format, userName không chứa ký tự
   Luồng chính (Happy đặc biệt, password tối thiểu 8 ký tự (1 chữ hoa, 1 chữ số, 1 ký tự đặc
   Path) biệt).
1. RegisterCommandHandler kiểm tra email chưa tồn tại trong
   database (UserManager.FindByEmailAsync).
1. Tạo ApplicationUser mới qua factory method
   ApplicationUser.Create(fullName, email, userName).
1. UserManager.CreateAsync(user, password) – ASP.NET Core
   Identity tự hash password với PBKDF2.
   CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 17 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0 7. UserManager.AddToRoleAsync(user, "Author") – gán role mặc định. 8. JwtService.GenerateAccessToken() – tạo JWT access token
(HS256, 15 phút). 9. JwtService.GenerateRefreshToken() – tạo refresh token ngẫu nhiên
(512-bit, 7 ngày). 10. Lưu RefreshToken vào bảng refresh_tokens trong database. 11. BackgroundJob.Enqueue<WelcomeEmailJob>() – đẩy job gửi
email chào mừng vào Hangfire queue (fire-and-forget). 12. Trả về HTTP 201 Created với AuthResponseDto: { accessToken,
refreshToken, expiresAt, user: { id, fullName, email, userName,
avatarUrl, roles } }.
A1 – Email đã tồn tại: Tại bước 4, nếu email đã được đăng ký → Throw
ConflictException → GlobalExceptionMiddleware trả về HTTP 409
Conflict với RFC 7807 body.
A2 – Password không đủ mạnh: Tại bước 3 hoặc 6,
UserManager.CreateAsync trả về IdentityError → Throw
Luồng thay thế / ValidationException → HTTP 422 Unprocessable Entity với danh sách
Ngoại lệ lỗi chi tiết.
A3 – Dữ liệu đầu vào không hợp lệ: Tại bước 3, FluentValidation fail →
HTTP 422 với từng field lỗi (theo RFC 7807 ValidationProblemDetails).
A4 – Database không kết nối: EF Core ném DbUpdateException →
HTTP 500 Internal Server Error (GlobalExceptionMiddleware log lỗi,
không expose stack trace).
HTTP Method & POST /api/v1/auth/register
Endpoint
Tài khoản mới được tạo trong database, role "Author" được gán,
Kết quả mong đợi refresh token được persist, email chào mừng được đẩy vào Hangfire
queue. Client nhận được access token và refresh token.
201 Created – Đăng ký thành công. 409 Conflict – Email đã tồn tại. 422
HTTP Status Code trả
Unprocessable Entity – Dữ liệu không hợp lệ. 500 Internal Server Error
về
– Lỗi hệ thống.
FR-AUTH-002: Đăng nhập bằng Email/Mật khẩu (Local Login)
Mã yêu cầu FR-AUTH-002
Tên yêu cầu Đăng nhập bằng Email và Mật khẩu
Nhóm chức năng Module Xác thực và Quản lý Người dùng (FR-AUTH)
Tác nhân Tác giả đã đăng ký (Author) hoặc Quản trị viên (Admin)
Mức ưu tiên M – Must Have (Bắt buộc)
(MoSCoW)
Hệ thống cho phép người dùng đã có tài khoản đăng nhập bằng email
và mật khẩu. Mỗi lần đăng nhập thành công tạo ra một cặp access
Mô tả token mới (JWT, 15 phút) và refresh token mới (7 ngày). Cơ chế Token
Rotation: refresh token cũ KHÔNG bị xóa ngay mà được đánh dấu đã
sử dụng (để phát hiện token reuse attack).

1. Người dùng đã có tài khoản hợp lệ trong hệ thống. 2. Tài khoản
   Điều kiện tiên quyết chưa bị khóa (LockoutEnabled = false hoặc chưa đến lockout
   deadline).
   CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 18 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0

1. Client gửi POST /api/v1/auth/login với body: { "email": "...",
   "password": "..." }.
2. LoginCommand được dispatch qua MediatR.
3. ValidationBehavior kiểm tra email format và password không rỗng.
4. LoginCommandHandler tìm user:
   UserManager.FindByEmailAsync(email).
5. Xác minh mật khẩu: UserManager.CheckPasswordAsync(user,
   password) – so sánh với PBKDF2 hash.
   Luồng chính (Happy
6. Kiểm tra tài khoản không bị lockout:
   Path)
   UserManager.IsLockedOutAsync(user).
7. Tạo access token mới: JwtService.GenerateAccessToken(user,
   roles).
8. Tạo refresh token mới: JwtService.GenerateRefreshToken(userId).
9. Lưu refresh token mới vào database.
10. Ghi nhận đăng nhập thành công:
    UserManager.ResetAccessFailedCountAsync(user).
11. Trả về HTTP 200 OK với AuthResponseDto.
    A1 – Tài khoản không tồn tại hoặc mật khẩu sai: HTTP 401
    Unauthorized với message generic "Email hoặc mật khẩu không đúng"
    (KHÔNG tiết lộ tài khoản có tồn tại hay không – tránh User
    Enumeration Attack).
    Luồng thay thế /
    Ngoại lệ A2 – Tài khoản bị lockout: HTTP 423 Locked với thông báo thời gian
    unlock còn lại.
    A3 – Vượt quá số lần thử sai (5 lần): AccessFailedCount tăng lên, sau
    5 lần → tài khoản bị lockout 15 phút (cấu hình qua LockoutOptions).
    HTTP Method & POST /api/v1/auth/login
    Endpoint
    Access token và refresh token mới được tạo và trả về. Refresh token
    Kết quả mong đợi
    được lưu vào database.
    200 OK – Đăng nhập thành công. 401 Unauthorized – Sai email/mật
    HTTP Status Code trả
    khẩu. 422 Unprocessable Entity – Dữ liệu không hợp lệ. 423 Locked –
    về
    Tài khoản bị khóa.
    FR-AUTH-003: Đăng nhập bằng Google OAuth 2.0
    Mã yêu cầu FR-AUTH-003
    Tên yêu cầu Đăng nhập / Đăng ký bằng Google OAuth 2.0
    Nhóm chức năng Module Xác thực và Quản lý Người dùng (FR-AUTH)
    Tác nhân Khách (Guest) – lần đầu / Người dùng đã đăng ký trước đó qua Google
    Mức ưu tiên S – Should Have
    (MoSCoW)
    Hệ thống hỗ trợ đăng nhập qua tài khoản Google sử dụng OAuth 2.0
    Authorization Code Flow với PKCE. Nếu đây là lần đăng nhập Google
    đầu tiên, hệ thống tự động tạo tài khoản mới từ thông tin Google profile
    Mô tả
    (email, display name, avatar URL) và gán role "Author". Nếu email đã
    tồn tại từ đăng ký thủ công trước đó, hệ thống liên kết Google login với
    tài khoản hiện có.
    CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 19 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0

1. Google OAuth 2.0 Credentials (ClientId, ClientSecret) đã được cấu
   Điều kiện tiên quyết hình trong appsettings. 2. Redirect URI đã được đăng ký trong Google
   Cloud Console. 3. Người dùng có tài khoản Google hợp lệ.
1. Frontend (Next.js) redirect người dùng đến Google Authorization
   Endpoint với scopes: openid, email, profile.
1. Người dùng xác nhận cấp quyền trên Google Consent Screen.
1. Google redirect về callback URL (Next.js) với Authorization Code.
1. Auth.js v5 (Next.js) xử lý callback, lấy access token từ Google và lấy
   profile.
1. Frontend gửi POST /api/v1/auth/google với Google
   ExternalLoginInfo.
   Luồng chính (Happy
1. GoogleLoginCommandHandler tìm user bằng
   Path)
   UserManager.FindByLoginAsync("Google", providerKey).
1. Nếu chưa có tài khoản: kiểm tra email → nếu email chưa tồn tại thì
   tạo ApplicationUser mới từ Google profile, gán role "Author" →
   AddLoginAsync.
1. Nếu email đã tồn tại (đã đăng ký thủ công): liên kết Google login →
   AddLoginAsync với tài khoản hiện có.
1. Tạo access token và refresh token, lưu vào database.
1. Trả về HTTP 200 OK với AuthResponseDto.
   A1 – Google token không hợp lệ hoặc hết hạn: HTTP 401
   Unauthorized.
   Luồng thay thế /
   A2 – Email Google bị revoke quyền: HTTP 400 Bad Request.
   Ngoại lệ
   A3 – Google API không khả dụng: HTTP 502 Bad Gateway với
   message thích hợp.
   HTTP Method & POST /api/v1/auth/google
   Endpoint
   Người dùng được đăng nhập (hoặc tự động đăng ký), nhận
   Kết quả mong đợi
   AuthResponseDto. Tài khoản mới (nếu có) được tạo với role "Author".
   200 OK – Đăng nhập/đăng ký thành công. 401 Unauthorized – Token
   HTTP Status Code trả
   Google không hợp lệ. 400 Bad Request – Thiếu thông tin Google
   về
   profile.
   FR-AUTH-004: Làm mới Access Token (Token Refresh)
   Mã yêu cầu FR-AUTH-004
   Tên yêu cầu Làm mới Access Token bằng Refresh Token
   Nhóm chức năng Module Xác thực và Quản lý Người dùng (FR-AUTH)
   Tác nhân Tác giả (Author) / Quản trị viên (Admin) – có refresh token hợp lệ
   Mức ưu tiên M – Must Have (Bắt buộc)
   (MoSCoW)
   Khi access token hết hạn (sau 15 phút), client sử dụng refresh token
   còn hiệu lực để lấy cặp token mới mà không cần người dùng đăng
   nhập lại. Cơ chế Token Rotation bắt buộc: mỗi lần refresh, refresh
   Mô tả
   token cũ bị vô hiệu hóa (IsRevoked = true, RevokedAt =
   DateTime.UtcNow) và một refresh token MỚI được tạo ra. Đây là biện
   pháp chống Refresh Token Reuse Attack.
   CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 20 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0

1. Client có refresh token hợp lệ (chưa hết hạn, chưa bị revoke, chưa
   Điều kiện tiên quyết bị thay thế). 2. Người dùng tương ứng vẫn còn tồn tại trong database
   và chưa bị khóa.
1. Client gửi POST /api/v1/auth/refresh với body: { "refreshToken": "..."
   }.
1. RefreshTokenCommand dispatch qua MediatR.
1. Handler tìm refresh token trong database: bao gồm User navigation
   property.
   Luồng chính (Happy 4. Kiểm tra: token tồn tại, IsRevoked == false, ExpiresAt >
   Path) DateTime.UtcNow, user vẫn active.
1. Đánh dấu token cũ: IsRevoked = true, ReplacedByToken =
   newToken, RevokedAt = DateTime.UtcNow.
1. Tạo access token mới cho user.
1. Tạo refresh token mới, lưu vào database.
1. Trả về HTTP 200 OK với AuthResponseDto chứa cặp token mới.
   A1 – Refresh token không tìm thấy trong database: HTTP 401
   Unauthorized.
   A2 – Refresh token đã hết hạn: HTTP 401 Unauthorized, client phải
   đăng nhập lại.
   Luồng thay thế /
   A3 – Refresh token đã bị revoke (Reuse Attack detected): HTTP 401
   Ngoại lệ
   Unauthorized. LOG SECURITY ALERT với mức WARNING. Có thể
   kích hoạt revoke toàn bộ refresh tokens của user đó (paranoid mode).
   A4 – User bị xóa hoặc bị khóa sau khi token được cấp: HTTP 401
   Unauthorized.
   HTTP Method & POST /api/v1/auth/refresh
   Endpoint
   Refresh token cũ bị invalidate. Access token mới (15 phút) và refresh
   Kết quả mong đợi
   token mới (7 ngày) được tạo và trả về.
   HTTP Status Code trả 200 OK – Refresh thành công. 401 Unauthorized – Token không hợp
   về lệ, hết hạn hoặc đã bị revoke.
   FR-AUTH-005: Đăng xuất (Logout / Token Revocation)
   Mã yêu cầu FR-AUTH-005
   Tên yêu cầu Đăng xuất và Thu hồi Refresh Token
   Nhóm chức năng Module Xác thực và Quản lý Người dùng (FR-AUTH)
   Tác nhân Tác giả (Author) / Quản trị viên (Admin) đang đăng nhập
   Mức ưu tiên M – Must Have
   (MoSCoW)
   Người dùng đăng xuất khỏi hệ thống. Vì JWT access token là stateless
   (không thể revoke trực tiếp trước khi hết hạn), hành động logout chủ
   Mô tả
   yếu là revoke refresh token tương ứng trong database. Client có trách
   nhiệm xóa access token khỏi bộ nhớ (localStorage/cookie) phía client.
1. Người dùng đang đăng nhập với access token hợp lệ trong
   Điều kiện tiên quyết
   Authorization header. 2. Client gửi refresh token muốn revoke.
   Luồng chính (Happy 1. Client gửi POST /api/v1/auth/logout với Authorization: Bearer
   Path) {accessToken} header và body: { "refreshToken": "..." }.
   CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 21 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0 2. Middleware xác thực JWT (UseAuthentication) xác minh access
token. 3. LogoutCommandHandler tìm refresh token trong database. 4. Nếu tìm thấy và thuộc về user hiện tại: đánh dấu IsRevoked = true,
RevokedAt = DateTime.UtcNow. 5. Lưu thay đổi vào database. 6. Trả về HTTP 204 No Content.
A1 – Refresh token không tìm thấy: Vẫn trả về HTTP 204 (idempotent
Luồng thay thế / – không tiết lộ trạng thái).
Ngoại lệ A2 – Access token đã hết hạn: Vẫn cho phép logout nếu refresh token
hợp lệ; hoặc HTTP 401 nếu không cung cấp refresh token.
HTTP Method & POST /api/v1/auth/logout
Endpoint
Refresh token bị đánh dấu IsRevoked = true trong database. Các lần
Kết quả mong đợi
refresh tiếp theo với token này sẽ thất bại.
HTTP Status Code trả 204 No Content – Đăng xuất thành công (hoặc token không tồn tại –
về idempotent). 401 Unauthorized – Access token không hợp lệ.
FR-AUTH-006: Xem Hồ sơ Cá nhân (View Profile)
Mã yêu cầu FR-AUTH-006
Tên yêu cầu Xem Hồ sơ Cá nhân
Nhóm chức năng Module Xác thực và Quản lý Người dùng (FR-AUTH)
Tác nhân Tác giả (Author) / Quản trị viên (Admin) đang đăng nhập
Mức ưu tiên S – Should Have
(MoSCoW)
Trả về thông tin hồ sơ của người dùng hiện đang đăng nhập, dựa trên
Mô tả UserId được trích xuất từ JWT claims. Không bao giờ trả về
PasswordHash hoặc SecurityStamp.
Điều kiện tiên quyết 1. Người dùng đang đăng nhập với access token hợp lệ.

1. Client gửi GET /api/v1/auth/me với Authorization: Bearer
   {accessToken}.
2. Middleware xác thực JWT, trích xuất UserId từ claim NameIdentifier.
   Luồng chính (Happy 3. GetCurrentUserQuery dispatch qua MediatR.
   Path) 4. Handler tìm user: UserManager.FindByIdAsync(userId).
3. Map sang UserProfileDto: { id, fullName, email, userName,
   avatarUrl, roles, emailConfirmed, createdAt }.
4. Trả về HTTP 200 OK với UserProfileDto.
   Luồng thay thế / A1 – User đã bị xóa khỏi database sau khi token được cấp: HTTP 404
   Ngoại lệ Not Found.
   HTTP Method & GET /api/v1/auth/me
   Endpoint
   Trả về thông tin hồ sơ đầy đủ của người dùng (không có thông tin nhạy
   Kết quả mong đợi
   cảm như password hash).
   CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 22 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
HTTP Status Code trả 200 OK – Thành công. 401 Unauthorized – Chưa đăng nhập. 404 Not
về Found – User không tồn tại.
FR-AUTH-007: Cập nhật Hồ sơ Cá nhân (Update Profile)
Mã yêu cầu FR-AUTH-007
Tên yêu cầu Cập nhật Hồ sơ Cá nhân
Nhóm chức năng Module Xác thực và Quản lý Người dùng (FR-AUTH)
Tác nhân Tác giả (Author) / Quản trị viên (Admin) đang đăng nhập
Mức ưu tiên S – Should Have
(MoSCoW)
Người dùng có thể cập nhật FullName và AvatarUrl của mình. Email và
UserName không thể thay đổi qua endpoint này (đây là quy trình riêng
Mô tả
có xác nhận OTP). Sử dụng PATCH (partial update) để chỉ cập nhật
các field được cung cấp.

1. Người dùng đang đăng nhập. 2. Dữ liệu mới phải hợp lệ (FullName
   Điều kiện tiên quyết
   không rỗng, AvatarUrl là URL hợp lệ nếu cung cấp).
1. Client gửi PATCH /api/v1/auth/me với body: { "fullName": "...",
   "avatarUrl": "..." }.
1. UpdateProfileCommand dispatch qua MediatR, UserId lấy từ JWT
   claims.
   Luồng chính (Happy
1. ValidationBehavior kiểm tra: fullName 2–100 ký tự, avatarUrl là URL
   Path)
   hợp lệ (nếu cung cấp).
1. Handler tìm user, cập nhật FullName và/hoặc AvatarUrl.
1. UserManager.UpdateAsync(user).
1. Trả về HTTP 200 OK với UserProfileDto đã cập nhật.
   Luồng thay thế / A1 – Dữ liệu không hợp lệ: HTTP 422 Unprocessable Entity.
   Ngoại lệ
   HTTP Method & PATCH /api/v1/auth/me
   Endpoint
   Kết quả mong đợi Hồ sơ người dùng được cập nhật trong database. Trả về hồ sơ mới.
   HTTP Status Code trả 200 OK – Cập nhật thành công. 401 Unauthorized – Chưa đăng nhập.
   về 422 Unprocessable Entity – Dữ liệu không hợp lệ.
   3.2. Module Quản lý Danh mục (FR-CAT)
   Module quản lý danh mục (Category) phân loại công thức nấu ăn. Danh mục được tạo và duy
   trì bởi Admin; Author và Guest chỉ có quyền đọc. Mỗi danh mục có Slug duy nhất phục vụ
   URL thân thiện SEO. Danh mục được cache với IMemoryCache (TTL 1 giờ) vì thay đổi ít
   thường xuyên.
   FR-CAT-001: Xem Danh sách Danh mục
   Mã yêu cầu FR-CAT-001
   Tên yêu cầu Xem Danh sách Tất cả Danh mục
   CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 23 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
Nhóm chức năng Module Quản lý Danh mục (FR-CAT)
Tác nhân Tất cả (Guest / Author / Admin)
Mức ưu tiên M – Must Have
(MoSCoW)
Trả về danh sách tất cả danh mục công thức hiện có trong hệ thống,
kèm số lượng công thức đã xuất bản (Published) trong mỗi danh mục.
Mô tả
Kết quả được cache với IMemoryCache (TTL 60 phút) và sắp xếp theo
Name tăng dần.

1. Ít nhất một danh mục tồn tại trong database (hoặc trả về mảng rỗng).
   Điều kiện tiên quyết
2. Không yêu cầu xác thực.
3. Client gửi GET /api/v1/categories.
4. GetCategoriesQuery dispatch qua MediatR.
5. Handler kiểm tra IMemoryCache với key "categories:all".
6. Cache hit: trả về dữ liệu từ cache.
   Luồng chính (Happy
   Path) 5. Cache miss: query database
   (IUnitOfWork.Categories.GetAllWithRecipeCount()), map sang
   CategoryDto[].
7. Lưu vào IMemoryCache với TTL 60 phút (sliding expiration).
8. Trả về HTTP 200 OK với CategoryDto[].
   Luồng thay thế / A1 – Không có danh mục nào: HTTP 200 OK với mảng rỗng [].
   Ngoại lệ
   HTTP Method & GET /api/v1/categories
   Endpoint
   Mảng CategoryDto[] với các field: { id, name, slug, description,
   Kết quả mong đợi
   recipeCount }. Kết quả được serve từ cache khi có.
   HTTP Status Code trả 200 OK – Thành công (kể cả khi trống).
   về
   FR-CAT-002: Xem Chi tiết Danh mục và Công thức
   Mã yêu cầu FR-CAT-002
   Tên yêu cầu Xem Chi tiết Danh mục và Danh sách Công thức thuộc Danh mục
   Nhóm chức năng Module Quản lý Danh mục (FR-CAT)
   Tác nhân Tất cả (Guest / Author / Admin)
   Mức ưu tiên M – Must Have
   (MoSCoW)
   Trả về thông tin chi tiết của một danh mục cụ thể (theo Slug) kèm danh
   sách phân trang các công thức đã xuất bản (Published) thuộc danh
   Mô tả
   mục đó. Guest chỉ thấy Published recipes; Author thấy thêm Draft
   recipes của chính mình trong danh mục.
9. Danh mục với slug tương ứng phải tồn tại. 2. Không yêu cầu xác
   Điều kiện tiên quyết
   thực.
   Luồng chính (Happy 1. Client gửi GET /api/v1/categories/{slug}?page=1&pageSize=12.
   Path) 2. GetCategoryBySlugQuery dispatch qua MediatR.
   CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 24 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0 3. Handler tìm category theo slug:
\_unitOfWork.Categories.GetBySlugAsync(slug). 4. Query recipes thuộc category với Status == Published (+ Draft của
currentUser nếu đã đăng nhập). 5. Apply pagination (OFFSET-based: SKIP (page-1)\*pageSize TAKE
pageSize). 6. Map sang CategoryDetailDto kèm
PagedResult<RecipeSummaryDto>. 7. Trả về HTTP 200 OK.
Luồng thay thế / A1 – Slug không tồn tại: HTTP 404 Not Found với RFC 7807 body.
Ngoại lệ
HTTP Method & GET /api/v1/categories/{slug}?page={n}&pageSize={n}
Endpoint
{ category: CategoryDto, recipes: { items: RecipeSummaryDto[],
Kết quả mong đợi
totalCount, page, pageSize, totalPages } }
HTTP Status Code trả 200 OK – Thành công. 404 Not Found – Slug không tồn tại.
về
FR-CAT-003: Tạo Danh mục Mới [Admin]
Mã yêu cầu FR-CAT-003
Tên yêu cầu Tạo Danh mục Công thức Mới
Nhóm chức năng Module Quản lý Danh mục (FR-CAT)
Tác nhân Quản trị viên (Admin)
Mức ưu tiên M – Must Have
(MoSCoW)
Admin tạo danh mục công thức mới. Slug được tự động sinh từ Name
(slugify: chuyển sang chữ thường, bỏ dấu, thay khoảng trắng bằng "-").
Mô tả Nếu Slug đã tồn tại, hệ thống thêm suffix số (e.g., "mon-chinh-2"). Sau
khi tạo, cache danh mục (IMemoryCache key "categories:all") bị
invalidate.

1. Người dùng đang đăng nhập với role Admin. 2. Name chưa tồn tại
   Điều kiện tiên quyết
   trong database.
1. Admin gửi POST /api/v1/categories với Authorization: Bearer
   {adminJwt} và body: { "name": "...", "description": "..." }.
1. RequireAuthorization("Admin") middleware kiểm tra role.
1. CreateCategoryCommand dispatch qua MediatR.
1. ValidationBehavior: name 2–50 ký tự, không chứa HTML.
1. SlugHelper.Generate(name) tạo slug.
   Luồng chính (Happy
1. Kiểm tra slug chưa tồn tại. Nếu trùng, thêm "-2", "-3",... cho đến khi
   Path)
   unique.
1. Category.Create(name, slug, description) tạo entity.
1. \_unitOfWork.Categories.AddAsync(entity).
1. \_unitOfWork.SaveChangesAsync().
1. MemoryCache.Remove("categories:all") – invalidate cache.
1. Trả về HTTP 201 Created với CategoryDto và Location header.
   CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 25 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
Luồng thay thế / A1 – Thiếu role Admin: HTTP 403 Forbidden.
Ngoại lệ A2 – Dữ liệu không hợp lệ: HTTP 422.
HTTP Method & POST /api/v1/categories
Endpoint
Danh mục mới được tạo trong database. Cache danh mục bị xóa.
Kết quả mong đợi
Location header trỏ đến /api/v1/categories/{newSlug}.
201 Created – Tạo thành công. 403 Forbidden – Không có quyền
HTTP Status Code trả
Admin. 409 Conflict – Name đã tồn tại. 422 Unprocessable Entity – Dữ
về
liệu không hợp lệ.
FR-CAT-004: Cập nhật Danh mục [Admin]
Mã yêu cầu FR-CAT-004
Tên yêu cầu Cập nhật Thông tin Danh mục
Nhóm chức năng Module Quản lý Danh mục (FR-CAT)
Tác nhân Quản trị viên (Admin)
Mức ưu tiên M – Must Have
(MoSCoW)
Admin cập nhật Name và/hoặc Description của danh mục. Slug
Mô tả KHÔNG thay đổi khi đổi tên (để tránh broken links). Sau khi cập nhật,
cache bị invalidate.
Điều kiện tiên quyết 1. Admin đang đăng nhập. 2. Danh mục với ID tương ứng tồn tại.

1. Admin gửi PUT /api/v1/categories/{id} với body: { "name": "...",
   "description": "..." }.
2. Kiểm tra role Admin.
   Luồng chính (Happy
3. UpdateCategoryCommand dispatch qua MediatR.
   Path)
4. Tìm category theo ID, cập nhật Name và Description.
5. Lưu thay đổi, invalidate cache.
6. Trả về HTTP 200 OK với CategoryDto đã cập nhật.
   Luồng thay thế / A1 – ID không tồn tại: HTTP 404.
   Ngoại lệ A2 – Thiếu role Admin: HTTP 403.
   HTTP Method & PUT /api/v1/categories/{id:guid}
   Endpoint
   Kết quả mong đợi Thông tin danh mục được cập nhật. Cache invalidated.
   HTTP Status Code trả 200 OK – Cập nhật thành công. 403 Forbidden. 404 Not Found. 422
   về Unprocessable Entity.
   FR-CAT-005: Xóa Danh mục [Admin]
   Mã yêu cầu FR-CAT-005
   Tên yêu cầu Xóa Danh mục
   Nhóm chức năng Module Quản lý Danh mục (FR-CAT)
   Tác nhân Quản trị viên (Admin)
   CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 26 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
Mức ưu tiên S – Should Have
(MoSCoW)
Admin xóa một danh mục. Quy tắc nghiệp vụ: KHÔNG được xóa danh
mục còn chứa công thức (dù là Published hay Draft). Admin phải
Mô tả
chuyển tất cả công thức sang danh mục khác trước khi xóa. Đây là soft
constraint để bảo vệ toàn vẹn dữ liệu.

1. Admin đang đăng nhập. 2. Danh mục tồn tại và không còn công thức
   Điều kiện tiên quyết
   nào.
1. Admin gửi DELETE /api/v1/categories/{id}.
1. Kiểm tra role Admin.
1. DeleteCategoryCommand dispatch.
   Luồng chính (Happy
   Path)
1. Đếm số recipe trong category: nếu > 0 → Throw
   ConflictException("Danh mục còn chứa {count} công thức.").
1. Xóa entity, lưu thay đổi, invalidate cache.
1. Trả về HTTP 204 No Content.
   A1 – Danh mục có recipe: HTTP 409 Conflict với thông báo số lượng
   Luồng thay thế /
   recipe.
   Ngoại lệ
   A2 – ID không tồn tại: HTTP 404.
   HTTP Method & DELETE /api/v1/categories/{id:guid}
   Endpoint
   Kết quả mong đợi Danh mục bị xóa khỏi database. HTTP 204 được trả về.
   HTTP Status Code trả 204 No Content – Xóa thành công. 403 Forbidden. 404 Not Found. 409
   về Conflict – Danh mục còn recipe.
   3.3. Module Quản lý Công thức Nấu ăn (FR-RCP)
   Module cốt lõi của hệ thống. Recipe là aggregate root chứa các child entity: RecipeStep,
   RecipeIngredient, RecipeImage và Owned Entity RecipeNutrition. Tất cả mutation
   (Create/Update/Delete) đi qua UnitOfWork để đảm bảo tính nhất quán transaction.
   Concurrency được xử lý qua RowVersion (Timestamp) để phát hiện lost update khi hai Author
   cùng sửa một recipe.
   FR-RCP-001: Xem Danh sách Công thức (Paginated + Filtered + Sorted)
   Mã yêu FR-RCP-001
   cầu
   Tên yêu Xem Danh sách Công thức Nấu ăn với Phân trang, Lọc và Sắp xếp
   cầu
   Nhóm Module Quản lý Công thức Nấu ăn (FR-RCP)
   chức
   năng
   Tác nhân Tất cả (Guest / Author / Admin)
   Mức ưu M – Must Have
   tiên
   (MoSCoW)
   CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 27 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
Trả về danh sách phân trang các công thức. Guest và Author khác chỉ thấy Status == Published. Author thấy
thêm Draft/Archived của chính mình. Admin thấy tất cả trạng thái. Hỗ trợ lọc theo CategoryId, DifficultyLevel,
Mô tả
thời gian nấu; sắp xếp theo createdAt, title, cookTime. Kết quả được cache với Output Cache (.NET 10) theo
policy "RecipeList" (TTL 15 phút, vary by query string).
Điều kiện 1. Không yêu cầu xác thực (endpoint public cho Published recipes). 2. Tham số page >= 1, pageSize trong [1,
tiên quyết 50].

1. Client gửi GET
   /api/v1/recipes?page=1&pageSize=12&categoryId={guid}&difficulty=Easy&maxCookTime=30&sort=-createdAt.
2. GetRecipesQuery dispatch qua MediatR.
3. Handler xây dựng IQueryable với filters từ query params.
   Luồng 4. Áp dụng Authorization filter: nếu Guest → chỉ Published; nếu Author → Published OR (Draft AND AuthorId
   chính == userId); nếu Admin → tất cả.
   (Happy
4. Apply sorting: sort="-createdAt" → ORDER BY CreatedAt DESC; sort="title" → ORDER BY Title ASC.
   Path)
5. COUNT total trước khi pagination.
6. Apply OFFSET-LIMIT pagination.
7. Map sang PagedResult<RecipeSummaryDto>.
8. Trả về HTTP 200 OK. Output Cache lưu response theo key = {path}?{queryString}.
   Luồng A1 – page hoặc pageSize không hợp lệ: HTTP 422. A2 – categoryId không tồn tại: HTTP 200 với items rỗng
   thay thế / (không throw 404).
   Ngoại lệ
   HTTP GET
   Method & /api/v1/recipes?page={n}&pageSize={n}&categoryId={guid}&difficulty={level}&maxCookTime={min}&sort={field}
   Endpoint
   Kết quả PagedResult<RecipeSummaryDto>: { items[], totalCount, page, pageSize, totalPages, hasNextPage,
   mong đợi hasPreviousPage }.
   HTTP 200 OK – Thành công (kể cả items rỗng). 422 Unprocessable Entity – Tham số không hợp lệ.
   Status
   Code trả
   về
   FR-RCP-002: Xem Chi tiết Công thức
   Mã yêu FR-RCP-002
   cầu
   Tên yêu Xem Chi tiết Công thức Nấu ăn
   cầu
   Nhóm Module Quản lý Công thức Nấu ăn (FR-RCP)
   chức
   năng
   Tác nhân Tất cả (Guest / Author / Admin)
   Mức ưu M – Must Have
   tiên
   (MoSCoW)
   Trả về toàn bộ thông tin chi tiết của một công thức cụ thể, bao gồm: thông tin cơ bản, danh sách nguyên liệu
   (RecipeIngredient[]) sắp xếp theo SortOrder, các bước thực hiện (RecipeStep[]) sắp xếp theo StepNumber, ảnh
   Mô tả minh họa (RecipeImage[]), thông tin dinh dưỡng (RecipeNutrition), thông tin danh mục và tác giả. Recipe Draft
   chỉ được xem bởi tác giả sở hữu hoặc Admin. Endpoint được cache với Output Cache policy "RecipeDetail"
   (TTL 60 phút) và tagged với "recipes" để hỗ trợ tag-based invalidation.
   CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 28 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
Điều kiện 1. Recipe với slug tương ứng tồn tại. 2. Nếu Recipe ở trạng thái Draft/Archived: người yêu cầu phải là tác giả
tiên quyết hoặc Admin.

1. Client gửi GET /api/v1/recipes/{slug}.
2. GetRecipeBySlugQuery dispatch qua MediatR.
   Luồng 3. Handler query Recipe với Eager Loading:
   chính Include(Steps).Include(Ingredients).Include(Images).Include(Category).Include(Author).IncludeOwned(Nutrition).
   (Happy 4. Kiểm tra null → NotFoundException nếu không tìm thấy.
   Path)
3. Kiểm tra Status: nếu Draft/Archived → chỉ tác giả hoặc Admin mới được xem (Authorization check).
4. Map sang RecipeDetailDto (bao gồm tất cả nested collections).
5. Trả về HTTP 200 OK. Tag output cache entry với ["recipes", $"recipe:{slug}"].
   Luồng A1 – Slug không tồn tại: HTTP 404 Not Found.
   thay thế / A2 – Recipe Draft/Archived, người dùng không có quyền: HTTP 403 Forbidden.
   Ngoại lệ
   HTTP GET /api/v1/recipes/{slug}
   Method &
   Endpoint
   Kết quả RecipeDetailDto đầy đủ gồm tất cả nested data (steps, ingredients, images, nutrition, category, author).
   mong đợi
   HTTP 200 OK – Thành công. 403 Forbidden – Không có quyền xem Draft. 404 Not Found – Slug không tồn tại.
   Status
   Code trả
   về
   FR-RCP-003: Tạo Công thức Nấu ăn Mới [Author/Admin]
   Mã yêu cầu FR-RCP-003
   Tên yêu cầu Tạo Công thức Nấu ăn Mới
   Nhóm chức năng Module Quản lý Công thức Nấu ăn (FR-RCP)
   Tác nhân Tác giả (Author) / Quản trị viên (Admin)
   Mức ưu tiên M – Must Have
   (MoSCoW)
   Author hoặc Admin tạo mới một công thức nấu ăn. Trạng thái ban đầu
   luôn là Draft (chưa công khai). Slug được tự động sinh từ Title. Steps
   Mô tả
   và Ingredients có thể được tạo cùng lúc (trong cùng request) hoặc
   thêm riêng lẻ sau qua FR-RCP-009/010.
6. Người dùng đang đăng nhập với role Author hoặc Admin. 2.
   Điều kiện tiên quyết
   CategoryId tham chiếu đến danh mục đã tồn tại.
7. Author gửi POST /api/v1/recipes với body: { title, description,
   categoryId, prepTimeMinutes, cookTimeMinutes, servings, difficulty,
   instructions?, nutrition?: {...}, steps?: [...], ingredients?: [...] }.
8. Kiểm tra xác thực (RequireAuthorization).
   Luồng chính (Happy 3. CreateRecipeCommand dispatch.
   Path) 4. ValidationBehavior: title 5–200 ký tự, prepTime/cookTime/servings >
   0, categoryId valid Guid.
9. SlugHelper.Generate(title), kiểm tra slug unique.
10. Recipe.Create(title, description, categoryId, authorId, prepTime,
    cookTime, servings, difficulty).
    CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 29 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0 7. Nếu có steps: thêm từng RecipeStep.Create() vào recipe.Steps. 8. Nếu có ingredients: thêm từng RecipeIngredient.Create() vào
recipe.Ingredients. 9. Nếu có nutrition: recipe.SetNutrition(calories, protein, carbs, fat). 10. \_unitOfWork.Recipes.AddAsync(recipe), SaveChangesAsync(). 11. Invalidate Output Cache tag "recipes". 12. Trả về HTTP 201 Created với RecipeDto.
A1 – Không có quyền Author/Admin: HTTP 401/403.
Luồng thay thế / A2 – CategoryId không tồn tại: HTTP 422 với lỗi "Category không hợp
Ngoại lệ lệ.".
A3 – Slug đã tồn tại (title trùng): HTTP 409 Conflict.
HTTP Method & POST /api/v1/recipes
Endpoint
Recipe mới được tạo với Status = Draft, Slug được sinh tự động.
Kết quả mong đợi
Cache "recipes" bị invalidate.
201 Created – Tạo thành công. 401/403 – Chưa đăng nhập / Không có
HTTP Status Code trả
quyền. 409 Conflict – Slug đã tồn tại. 422 Unprocessable Entity – Dữ
về
liệu không hợp lệ.
FR-RCP-004: Cập nhật Công thức [Author-Owner/Admin]
Mã yêu cầu FR-RCP-004
Tên yêu cầu Cập nhật Thông tin Công thức Nấu ăn
Nhóm chức năng Module Quản lý Công thức Nấu ăn (FR-RCP)
Tác nhân Tác giả sở hữu (Author – Owner) / Quản trị viên (Admin)
Mức ưu tiên M – Must Have
(MoSCoW)
Cập nhật thông tin của một công thức. Resource-Based Authorization
được áp dụng: chỉ Author sở hữu recipe (AuthorId == currentUserId)
Mô tả hoặc Admin được phép. Concurrency control qua RowVersion (ETag
pattern): client phải gửi RowVersion hiện tại trong If-Match header; nếu
mismatch → conflict.

1. Author/Admin đang đăng nhập. 2. Recipe với ID tương ứng tồn tại.
   Điều kiện tiên quyết 3. Client cung cấp RowVersion hợp lệ trong If-Match header (hoặc
   trong request body).
1. Author gửi PUT /api/v1/recipes/{id} với body: { title, description,
   categoryId, prepTime, cookTime, servings, difficulty, instructions,
   nutrition? }.
1. Kiểm tra xác thực.
1. UpdateRecipeCommand dispatch.
   Luồng chính (Happy 4. Lấy recipe từ database theo ID.
   Path)
1. IAuthorizationService.AuthorizeAsync(user, recipe,
   Operations.Update) – kiểm tra resource-based auth.
1. Kiểm tra RowVersion: DbContext sẽ ném
   DbUpdateConcurrencyException nếu RowVersion mismatch.
1. Update các field của recipe entity qua domain method
   recipe.Update(...).
   CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 30 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0 8. Cập nhật Nutrition nếu có. 9. SaveChangesAsync() – nếu RowVersion mismatch tại đây → ném
ConcurrencyException → HTTP 409. 10. Invalidate cache: EvictByTagAsync("recipes"),
EvictByTagAsync($"recipe:{slug}"). 11. Trả về HTTP 200 OK với RecipeDto đã cập nhật.
A1 – Không phải owner (Author khác): HTTP 403 Forbidden.
Luồng thay thế / A2 – Concurrency conflict (RowVersion mismatch): HTTP 409 Conflict
Ngoại lệ – "Dữ liệu đã bị thay đổi bởi người dùng khác."
A3 – ID không tồn tại: HTTP 404.
HTTP Method & PUT /api/v1/recipes/{id:guid}
Endpoint
Kết quả mong đợi Recipe được cập nhật, cache bị invalidate, trả về RecipeDto mới nhất.
HTTP Status Code trả 200 OK. 403 Forbidden – Không phải owner. 404 Not Found. 409
về Conflict – Concurrency hoặc slug trùng. 422 Unprocessable Entity.
FR-RCP-005: Xuất bản / Hủy Xuất bản Công thức
Mã yêu cầu FR-RCP-005
Tên yêu cầu Xuất bản (Publish) / Hủy Xuất bản (Unpublish) Công thức
Nhóm chức năng Module Quản lý Công thức Nấu ăn (FR-RCP)
Tác nhân Tác giả sở hữu (Author – Owner) / Quản trị viên (Admin)
Mức ưu tiên M – Must Have
(MoSCoW)
Thay đổi trạng thái công thức: Draft → Published (xuất bản) hoặc
Published → Draft (hủy xuất bản). Business rule: KHÔNG thể publish
Mô tả
nếu recipe không có ít nhất 1 bước thực hiện (Steps.Count > 0). Khi
publish, Recipe trở nên công khai và được đưa vào index tìm kiếm.

1. Recipe tồn tại, người dùng là owner hoặc Admin. 2. Để publish:
   Điều kiện tiên quyết
   recipe phải có ít nhất 1 RecipeStep.
1. Author gửi PATCH /api/v1/recipes/{id}/publish (để xuất bản) hoặc
   PATCH /api/v1/recipes/{id}/unpublish.
1. PublishRecipeCommand dispatch với isPublish = true/false.
1. Kiểm tra resource-based authorization.
   Luồng chính (Happy 4. Gọi domain method: recipe.Publish() hoặc recipe.Unpublish().
   Path) 5. recipe.Publish() kiểm tra: Steps.Count == 0 → Throw
   DomainException("Recipe phải có ít nhất 1 bước thực hiện.").
1. Set Status = Published/Draft, UpdatedAt = DateTime.UtcNow.
1. SaveChangesAsync(), invalidate cache.
1. Trả về HTTP 200 OK với RecipeDto.
   A1 – Recipe không có bước thực hiện: HTTP 422 với
   Luồng thay thế / DomainException message.
   Ngoại lệ A2 – Recipe đã ở trạng thái mong muốn: Idempotent, trả về HTTP 200
   OK.
   CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 31 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
HTTP Method & PATCH /api/v1/recipes/{id:guid}/publish | PATCH
Endpoint /api/v1/recipes/{id:guid}/unpublish
Status recipe được thay đổi thành Published hoặc Draft. Cache bị
Kết quả mong đợi
invalidate.
HTTP Status Code trả 200 OK – Thành công. 403 Forbidden. 404 Not Found. 422
về Unprocessable Entity – Thiếu steps.
FR-RCP-006: Lưu trữ Công thức (Archive)
Mã yêu cầu FR-RCP-006
Tên yêu cầu Lưu trữ Công thức (Archive / Unarchive)
Nhóm chức năng Module Quản lý Công thức Nấu ăn (FR-RCP)
Tác nhân Tác giả sở hữu / Quản trị viên (Admin)
Mức ưu tiên S – Should Have
(MoSCoW)
Chuyển Recipe sang trạng thái Archived. Recipe Archived không hiển
thị trong danh sách công khai nhưng không bị xóa khỏi database (soft
Mô tả
hide). Hữu ích để ẩn recipe cũ không còn phù hợp mà không mất dữ
liệu.
Điều kiện tiên quyết 1. Recipe tồn tại, người dùng có quyền.

1. Author gửi PATCH /api/v1/recipes/{id}/archive.
2. Kiểm tra authorization.
   Luồng chính (Happy
3. recipe.Archive() → Status = Archived.
   Path)
4. SaveChangesAsync(), invalidate cache.
5. HTTP 200 OK.
   Luồng thay thế / A1 – ID không tồn tại: HTTP 404. A2 – Không có quyền: HTTP 403.
   Ngoại lệ
   HTTP Method & PATCH /api/v1/recipes/{id:guid}/archive
   Endpoint
   Kết quả mong đợi Status = Archived. Recipe không còn xuất hiện trong public listing.
   HTTP Status Code trả 200 OK. 403 Forbidden. 404 Not Found.
   về
   FR-RCP-007: Xóa Công thức [Author-Owner/Admin]
   Mã yêu cầu FR-RCP-007
   Tên yêu cầu Xóa Vĩnh viễn Công thức Nấu ăn
   Nhóm chức năng Module Quản lý Công thức Nấu ăn (FR-RCP)
   Tác nhân Tác giả sở hữu (Author – Owner) / Quản trị viên (Admin)
   Mức ưu tiên M – Must Have
   (MoSCoW)
   Xóa vĩnh viễn một công thức và tất cả dữ liệu liên quan (cascade
   Mô tả
   delete: Steps, Ingredients, Images). Các file ảnh trên MinIO được xóa
   CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 32 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
bất đồng bộ qua Hangfire fire-and-forget job để tránh blocking HTTP
response. Đây là hard delete (không dùng soft delete pattern cho
recipe).
Điều kiện tiên quyết 1. Recipe tồn tại. 2. Người dùng là owner hoặc Admin.

1. Author/Admin gửi DELETE /api/v1/recipes/{id}.
2. Kiểm tra xác thực và resource-based authorization.
3. Lấy danh sách URL ảnh từ recipe.Images.
4. \_unitOfWork.Recipes.Remove(recipe), SaveChangesAsync() –
   cascade delete Steps, Ingredients, Images trong database.
   Luồng chính (Happy
   Path) 5. Với mỗi imageUrl:
   BackgroundJob.Enqueue<IFileStorageService>(svc =>
   svc.DeleteAsync(url)) – xóa ảnh trên MinIO bất đồng bộ.
5. EvictByTagAsync("recipes"),
   EvictByTagAsync($"recipe:{recipe.Slug}") – invalidate cache.
6. Trả về HTTP 204 No Content.
   A1 – ID không tồn tại: HTTP 404.
   Luồng thay thế / A2 – Không phải owner: HTTP 403.
   Ngoại lệ A3 – Xóa MinIO file thất bại (job retry): Hangfire tự động retry 3 lần.
   Nếu vẫn fail, log error nhưng không ảnh hưởng response đã trả về.
   HTTP Method & DELETE /api/v1/recipes/{id:guid}
   Endpoint
   Recipe và tất cả child entities bị xóa khỏi database. Ảnh trên MinIO
   Kết quả mong đợi
   được lên lịch xóa qua Hangfire.
   HTTP Status Code trả 204 No Content – Xóa thành công. 403 Forbidden. 404 Not Found.
   về
   FR-RCP-008: Quản lý Ảnh Công thức (Upload / Set Primary / Delete)
   Mã yêu cầu FR-RCP-008
   Tên yêu cầu Upload Ảnh, Đặt Ảnh Chính, Xóa Ảnh Công thức
   Nhóm chức năng Module Quản lý Công thức Nấu ăn (FR-RCP)
   Tác nhân Tác giả sở hữu / Quản trị viên (Admin)
   Mức ưu tiên M – Must Have
   (MoSCoW)
   Author quản lý ảnh minh họa cho công thức của mình. Upload dùng
   multipart/form-data. Ảnh được lưu trên MinIO với path:
   recipes/{recipeId}/{uuid}.{ext}. Ảnh đầu tiên tự động được đặt làm ảnh
   Mô tả chính (IsPrimary = true). Hỗ trợ 3 thao tác: Upload (POST), đặt ảnh
   chính (PATCH primary), Xóa (DELETE). Validation bắt buộc: MIME
   type (image/jpeg, image/png, image/webp, image/avif) và kích thước tối
   đa 5MB.
7. Author/Admin đang đăng nhập. 2. Recipe tồn tại và người dùng có
   Điều kiện tiên quyết
   quyền.
   --- UPLOAD ---
   Luồng chính (Happy
   Path)
8. POST /api/v1/recipes/{id}/images với multipart/form-data chứa field
   "file".
   CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 33 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0 2. Validate MIME type: chỉ chấp nhận image/jpeg, image/png,
image/webp, image/avif. 3. Validate kích thước: file.Length <= 5*1024*1024 bytes (5MB). 4. Validate magic bytes: đọc 4 bytes đầu để xác nhận định dạng thực
sự (JPEG: FF D8 FF; PNG: 89 50 4E 47). 5. IFileStorageService.UploadAsync(file, "recipes/{id}") → trả về URL
công khai. 6. RecipeImage.Create(url, altText, isPrimary: !recipe.Images.Any()) →
thêm vào recipe. 7. SaveChangesAsync(), invalidate cache. 8. HTTP 201 Created với { url, isPrimary }.
--- SET PRIMARY IMAGE --- 9. PATCH /api/v1/recipes/{id}/images/{imageId}/primary. 10. Tìm image theo imageId, đặt IsPrimary = true, đặt tất cả ảnh khác
IsPrimary = false. 11. HTTP 200 OK.
--- DELETE IMAGE --- 12. DELETE /api/v1/recipes/{id}/images/{imageId}. 13. Xóa entity khỏi database. 14. BackgroundJob.Enqueue xóa file trên MinIO. 15. Nếu ảnh bị xóa là IsPrimary và còn ảnh khác: tự động đặt ảnh đầu
tiên còn lại làm primary. 16. HTTP 204 No Content.
A1 – MIME type không hợp lệ: HTTP 400 Bad Request.
A2 – File vượt quá 5MB: HTTP 400 với message "Kích thước file vượt
Luồng thay thế / quá giới hạn 5MB.".
Ngoại lệ A3 – Magic bytes không khớp MIME type: HTTP 400 "File không hợp
lệ.".
A4 – MinIO không khả dụng: HTTP 503 Service Unavailable.
POST /api/v1/recipes/{id}/images | PATCH
HTTP Method &
/api/v1/recipes/{id}/images/{imgId}/primary | DELETE
Endpoint
/api/v1/recipes/{id}/images/{imgId}
Ảnh được upload lên MinIO, URL lưu vào database. IsPrimary được
Kết quả mong đợi
quản lý chính xác.
Upload: 201 Created. Set Primary: 200 OK. Delete: 204 No Content.
HTTP Status Code trả
400 Bad Request – File không hợp lệ. 403/404 – Lỗi quyền/không tìm
về
thấy.
FR-RCP-009: Quản lý Nguyên liệu (CRUD RecipeIngredient)
Mã yêu cầu FR-RCP-009
Tên yêu cầu Thêm / Cập nhật / Xóa Nguyên liệu Công thức
Nhóm chức năng Module Quản lý Công thức Nấu ăn (FR-RCP)
Tác nhân Tác giả sở hữu / Quản trị viên (Admin)
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 34 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
Mức ưu tiên M – Must Have
(MoSCoW)
Author quản lý danh sách nguyên liệu (RecipeIngredient) của công
thức. Mỗi nguyên liệu có: Name (tên), Quantity (số lượng), Unit (đơn vị:
Mô tả gram/ml/muỗng/cái/củ...), Notes (ghi chú tuỳ chọn), SortOrder (thứ tự
hiển thị). Endpoint hỗ trợ thêm mới (POST), cập nhật (PUT), xóa
(DELETE) từng nguyên liệu riêng lẻ.

1. Recipe tồn tại và người dùng có quyền. 2. Quantity > 0, Unit không
   Điều kiện tiên quyết
   rỗng, Name 1–100 ký tự.
   --- THÊM NGUYÊN LIỆU ---
1. POST /api/v1/recipes/{id}/ingredients với body: { name, quantity, unit,
   notes?, sortOrder? }.
1. Validate, tạo RecipeIngredient.Create(recipeId, name, qty, unit,
   notes, sortOrder).
1. \_unitOfWork.Recipes (qua navigation) thêm ingredient,
   SaveChangesAsync().
1. HTTP 201 Created.
   Luồng chính (Happy
   Path)
   --- CẬP NHẬT NGUYÊN LIỆU ---
1. PUT /api/v1/recipes/{id}/ingredients/{ingId} với body fields cần cập
   nhật.
1. Tìm ingredient, cập nhật, SaveChangesAsync(). HTTP 200 OK.
   --- XÓA NGUYÊN LIỆU ---
1. DELETE /api/v1/recipes/{id}/ingredients/{ingId}.
1. Xóa entity, SaveChangesAsync(). HTTP 204 No Content.
   Luồng thay thế / A1 – Recipe/Ingredient không tồn tại: HTTP 404. A2 – Không có quyền:
   Ngoại lệ HTTP 403. A3 – Dữ liệu không hợp lệ: HTTP 422.
   HTTP Method & POST/PUT/DELETE /api/v1/recipes/{id}/ingredients/{ingId?}
   Endpoint
   Kết quả mong đợi Danh sách nguyên liệu được cập nhật chính xác. Cache bị invalidate.
   HTTP Status Code trả 201/200/204 – Thành công. 403/404/422 – Lỗi tương ứng.
   về
   FR-RCP-010: Quản lý Các bước Thực hiện (CRUD RecipeStep)
   Mã yêu cầu FR-RCP-010
   Tên yêu cầu Thêm / Cập nhật / Xóa Bước Thực hiện Công thức
   Nhóm chức năng Module Quản lý Công thức Nấu ăn (FR-RCP)
   Tác nhân Tác giả sở hữu / Quản trị viên (Admin)
   Mức ưu tiên M – Must Have
   (MoSCoW)
   Author quản lý các bước thực hiện (RecipeStep) của công thức. Mỗi
   bước có: StepNumber (thứ tự, tự động tăng), Description (mô tả bước),
   Mô tả
   DurationMinutes (thời gian ước tính cho bước, tùy chọn), ImageUrl
   (ảnh minh họa cho bước riêng, tùy chọn). Khi xóa một bước, hệ thống
   CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 35 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
tự động renumber các bước còn lại để đảm bảo StepNumber liên tục
(1, 2, 3...).

1. Recipe tồn tại, người dùng có quyền. 2. Description không rỗng, tối
   Điều kiện tiên quyết
   đa 2000 ký tự.
1. POST /api/v1/recipes/{id}/steps với body: { description,
   durationMinutes?, imageUrl? }.
1. StepNumber = recipe.Steps.Max(s => s.StepNumber) + 1 (hoặc 1
   nếu chưa có bước nào).
1. RecipeStep.Create(recipeId, stepNumber, description,
   durationMinutes).
   Luồng chính (Happy 4. SaveChangesAsync(). HTTP 201 Created.
   Path)
   --- XÓA BƯỚC ---
1. DELETE /api/v1/recipes/{id}/steps/{stepId}.
1. Xóa step, sau đó renumber: cập nhật StepNumber của tất cả steps
   còn lại theo thứ tự.
1. SaveChangesAsync(). HTTP 204 No Content.
   Luồng thay thế / A1 – Recipe không tồn tại: HTTP 404. A2 – Không có quyền: HTTP
   Ngoại lệ 403.
   HTTP Method & POST/PUT/DELETE /api/v1/recipes/{id}/steps/{stepId?}
   Endpoint
   Danh sách steps được cập nhật với StepNumber liên tục. Cache bị
   Kết quả mong đợi
   invalidate.
   HTTP Status Code trả 201/200/204 – Thành công. 403/404/422 – Lỗi.
   về
   3.4. Module Tìm kiếm và Phân trang (FR-SRCH)
   FR-SRCH-001: Tìm kiếm Toàn văn bản (Full-Text Search)
   Mã yêu cầu FR-SRCH-001
   Tên yêu cầu Tìm kiếm Toàn văn bản Công thức (Full-Text Search)
   Nhóm chức năng Module Tìm kiếm và Phân trang (FR-SRCH)
   Tác nhân Tất cả (Guest / Author / Admin)
   Mức ưu tiên M – Must Have
   (MoSCoW)
   Hệ thống cung cấp tính năng tìm kiếm toàn văn bản (FTS) cho công
   thức sử dụng PostgreSQL tsvector/tsquery với cấu hình tiếng Việt.
   Trường SearchVector (computed column) được tự động cập nhật bởi
   Mô tả
   PostgreSQL trigger khi Title hoặc Description thay đổi. Kết quả được
   xếp hạng bởi ts_rank(). Hỗ trợ tìm kiếm gần đúng với unaccent
   extension (bỏ dấu tiếng Việt: "pho" tìm được "phở").
1. PostgreSQL extensions unaccent và pg_trgm đã được install. 2. GIN
   Điều kiện tiên quyết index trên cột SearchVector đã được tạo. 3. Tham số q không rỗng, tối
   thiểu 2 ký tự.
   CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 36 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0

1. Client gửi GET
   /api/v1/recipes/search?q=pho+bo&page=1&pageSize=10.
2. SearchRecipesQuery dispatch với SearchTerm = "pho bo", Page =
   1, PageSize = 10.
3. Handler xây dựng tsquery từ search terms: "pho:_ & bo:_" (prefix
   matching).
4. LINQ query với EF Core: .Where(r =>
   Luồng chính (Happy r.SearchVector.Matches(EF.Functions.ToTsQuery("vietnamese",
   Path) query))).
5. Apply ORDER BY ts_rank(SearchVector, query) DESC để kết quả
   liên quan nhất lên đầu.
6. Chỉ trả về Status == Published recipes.
7. Apply pagination, trả về PagedResult<RecipeSummaryDto> với field
   relevanceScore.
8. Kết quả KHÔNG cache (vì query string đa dạng) hoặc cache ngắn (5
   phút) với vary by query.
   A1 – Query rỗng hoặc < 2 ký tự: HTTP 422.
   A2 – Không tìm thấy kết quả: HTTP 200 với items = [] và message gợi
   Luồng thay thế /
   ý.
   Ngoại lệ
   A3 – Ký tự đặc biệt trong query (SQL injection attempt): EF Core
   parameterize tự động; tsquery sanitization loại bỏ ký tự nguy hiểm.
   HTTP Method & GET /api/v1/recipes/search?q={searchTerm}&page={n}&pageSize={n}
   Endpoint
   PagedResult<RecipeSummaryDto> được xếp hạng theo độ liên quan
   Kết quả mong đợi
   (ts_rank). Hỗ trợ tìm kiếm không dấu tiếng Việt.
   HTTP Status Code trả 200 OK – Thành công (kể cả kết quả rỗng). 422 – Query không hợp lệ.
   về
   FR-SRCH-002/003/004: Lọc, Sắp xếp và Phân trang (Tóm tắt)
   Ba FR còn lại của module Search được tích hợp sẵn vào FR-RCP-001 và FR-SRCH-001.
   Bảng tóm tắt:
   Mã FR Tên Tham số Query Mô tả
   categoryId={guid}
   FR- Lọc kết quả theo một hoặc
   difficulty={Easy|Medium|Hard}
   SRCH- Lọc công thức nhiều tiêu chí. Các filter kết
   maxCookTime={minutes}
   002 hợp bằng AND logic.
   minServings={n}
   sort={field} VD: sort=createdAt
   FR- Tiền tố "-" = descending.
   (ASC) sort=-createdAt
   SRCH- Sắp xếp kết quả Mặc định: sort=-createdAt
   (DESC) sort=title, sort=-
   003 (mới nhất trước).
   cookTime
   Offset-based pagination
   FR- page={n} (default: 1) (SKIP/TAKE). Response
   Phân trang
   SRCH- pageSize={n} (default: 12, max: bao gồm totalCount,
   (Offset-based)
   004 50) totalPages, hasNextPage,
   hasPreviousPage.
   3.5. Module Quản lý Tệp tin (FR-FILE)
   CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 37 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
Module xử lý tất cả thao tác với file binary trên hệ thống lưu trữ đối tượng (Object Storage)
MinIO S3-compatible. Abstraction layer IFileStorageService cho phép swap implementation
(MinIO ↔ AWS S3 ↔ local filesystem) mà không cần thay đổi Application Layer.

| Mã FR Tên | Mô tả                                      |     |     | Ràng buộc kỹ thuật   |     |
| --------- | ------------------------------------------ | --- | --- | -------------------- | --- |
|           | IFileStorageService.UploadAsync(IFormFile, |     |     | Max size: 5MB. MIME: |     |

FR- folder, ct) → string (public URL). Tạo unique JPEG/PNG/WebP/AVIF.
Upload File
FILE- filename = {folder}/{Guid.NewGuid()}{ext} để Magic bytes validation.
lên MinIO
001 ngăn path traversal. Preserve MIME type Bucket: "culinary-blog".
| | gốc. | | | Policy: public-read. | |
| --- | ----- | --- | --- | --------------------- | --- |
Nếu object không tồn tại
IFileStorageService.DeleteAsync(fileUrl, ct).
trên MinIO → không
| FR- | Trích xuất object name từ URL, gọi | | | | |
| --- | ----------------------------------- | --- | --- | --- | --- |
Xóa File throw exception
| FILE- khỏi MinIO | RemoveObjectAsync(). Thường được gọi từ | | | | |
| ----------------- | ---------------------------------------- | --- | --- | --- | --- |
(idempotent). Lỗi kết nối
| 002 | Hangfire background job (fire-and-forget) | | | | |
| ---- | ------------------------------------------ | --- | --- | --- | --- |
MinIO → Hangfire retry
sau khi xóa recipe.
tối đa 3 lần.

3.6. Module Background Jobs (FR-JOB)
Module xử lý các tác vụ nền không đồng bộ sử dụng Hangfire. Hangfire chạy in-process trong
.NET API và sử dụng PostgreSQL làm persistent storage cho job queue. Dashboard quản lý
jobs tại /hangfire (chỉ Admin). Hỗ trợ 3 loại job: Fire-and-forget (chạy ngay), Delayed (chạy
sau N giây/phút) và Recurring (lịch cron).

| Mã FR Tên Job | Loại | Trigger |     | Mô tả | Retry Policy |
| ------------- | ---- | ------- | --- | ----- | ------------ |

Tự động retry
Gửi email HTML
3 lần với
chào mừng đến
exponential
địa chỉ email vừa
backoff (1
| FR- | | Sau FR-AUTH-001 | thành | đăng ký. Email | |
| -------- | --------- | ---------------- | ------ | ------------------ | -------------- |
| Welcome | Fire-and- | | | | phút, 5 phút, |
| JOB- | | công | | template bao gồm: | |
Email Job forget
30 phút). Sau 3
| 001 | | (BackgroundJob.Enqueue) | | tên người dùng, | |
| ---- | --- | ------------------------ | --- | ---------------- | --- |
lần fail →
link kích hoạt email
chuyển sang
(nếu cần), link đến
Failed state,
ứng dụng.
log error.
Tạo thumbnail
(300x300px) và
| | | | | medium image | Retry 3 lần. |
| ----------------- | --------- | --------------- | ------- | ------------------ | --------------- |
| FR- Image Resize | | | | (800x600px) từ | Nếu fail: ảnh |
| | Fire-and- | Sau FR-RCP-008 | upload | | |
| JOB- / Thumbnail | | | | ảnh gốc. Lưu cả 3 | gốc vẫn hiển |
| | forget | ảnh thành công | | | |
| 002 Job | | | | phiên bản lên | thị, chỉ thiếu |
thumbnail.
MinIO. Cập nhật
URLs vào
database.
Tạo file
sitemap.xml chứa
Retry 2 lần nếu
URL tất cả
| FR- Sitemap | | | | | fail. Log kết |
| ---------------- | ---------- | ------------------------ | --- | ------------------- | --------------- |
| | | Hàng ngày lúc 02:00 AM | | Published recipes, | |
| JOB- Generation | Recurring | | | | quả (số URL |
| | | UTC (cron: "0 2 \* \* \*") | | categories và | |
| 003 Job | | | | | trong sitemap) |
pages tĩnh. Upload
qua Serilog.
sitemap.xml lên
MinIO hoặc lưu
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 38 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
| Mã FR Tên Job | Loại | Trigger | Mô tả | Retry Policy |
| --------------- | ----- | -------- | ------ | ------------- |
vào wwwroot. Gửi
thông báo đến
Google Search
Console (ping).

3.7. Module Quan sát Hệ thống (FR-OBS)
Module cung cấp khả năng quan sát (Observability) toàn diện theo ba trụ cột: Logging
(Serilog), Metrics (OpenTelemetry), và Distributed Tracing (OpenTelemetry). Đây là yêu cầu
bắt buộc cho production deployment.

| Mã FR Tên | Mô tả |     | Kỹ thuật / Công cụ |     |
| --------- | ----- | --- | ------------------ | --- |

Hệ thống cung cấp 3 endpoint
| | health check với mục đích khác | | IHealthCheck, | |
| --- | ------------------------------- | --- | -------------------------------- | --- |
| | nhau: • GET /health – tổng hợp | | AspNetCore.HealthChecks.NpgSql, | |
| | tất cả components (database, | | AspNetCore.HealthChecks.Redis, | |
FR-
Health Check Redis, MinIO). • GET AspNetCore.HealthChecks.Minio.
OBS-
Endpoints /health/live – Liveness probe Liveness chỉ trả healthy. Readiness
001
| | (chỉ kiểm tra process còn | | fail khi DB/Redis down → | |
| --- | ------------------------------ | --- | ----------------------------- | --- |
| | sống). • GET /health/ready – | | Kubernetes/Nginx ngừng route | |
| | Readiness probe (kiểm tra kết | | traffic. | |
nối database và Redis).
Mọi HTTP request được log
với: CorrelationId (X-
Serilog + CorrelationIdMiddleware.
Correlation-ID header), HTTP
Sinks: Console (structured JSON),
method/path/status, elapsed
| FR- | | | File (rolling daily), Seq | |
| --- | --- | --- | -------------------------- | --- |
Structured time (ms), UserId (khi đã xác
| OBS- | | | (development). Log levels: Debug | |
| ---- | --- | --- | --------------------------------- | --- |
Logging thực). MediatR Pipeline
| 002 | | | (development), Information | |
| ---- | --- | --- | --------------------------- | --- |
Behavior (LoggingBehavior) log
(production), Warning/Error (luôn
tất cả Commands/Queries vào.
luôn).
Performance alert khi request >
500ms.
OpenTelemetry instrumentation
cho: HTTP request traces
| | (ActivitySource), EF Core | | OpenTelemetry .NET SDK, OTLP | |
| --- | --------------------------- | --- | -------------------------------- | --- |
| | database operation traces, | | exporter. Activity.TraceId được | |
FR- Distributed
| | custom business metrics | | include trong structured log (log | |
| --- | ------------------------ | --- | ---------------------------------- | --- |
OBS- Tracing &
| | (recipe created/published | | correlation với trace). Metrics: | |
| --- | -------------------------- | --- | --------------------------------- | --- |
003 Metrics
| | count). Traces được export đến | | request count, duration histogram, | |
| --- | ------------------------------- | --- | ----------------------------------- | --- |
| | Seq (development) hoặc | | error rate. | |
Jaeger/Grafana Tempo
(production).

|     |     |     |     |     |
| --- | --- | --- | --- | --- |

CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 39 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0 4. Yêu cầu Phi Chức năng (NFR)

Phần này mô tả các thuộc tính chất lượng hệ thống theo mô hình ISO/IEC 25010 (FURPS+).
Mỗi yêu cầu phi chức năng được gán mã định danh, mức ưu tiên và tiêu chí đo lường định
lượng cụ thể. Các NFR này ràng buộc thiết kế kiến trúc và lựa chọn công nghệ toàn bộ hệ
thống.

| Mã NFR | Danh mục | Số yêu | Ưu tiên |
| ------ | -------- | ------ | ------- |

cầu
| NFR-PERF | Hiệu năng (Performance) | 5 | Cao |
| --------- | ------------------------- | --- | -------- |
| NFR-SEC | Bảo mật (Security) | 6 | Rất cao |
| NFR-USE | Khả năng sử dụng | 4 | Trung |
| | (Usability) | | bình |
| NFR-REL | Độ tin cậy (Reliability) | 3 | Cao |
| NFR- | Khả năng bảo trì | 4 | Trung |
| MAINT | (Maintainability) | | bình |
| NFR- | Khả năng mở rộng | 3 | Cao |
| SCALE | (Scalability) | | |
| NFR-SEO | Tối ưu SEO (SEO) | 4 | Cao |

4.1. Hiệu năng (NFR-PERF)
Toàn bộ các chỉ số hiệu năng được đo trong môi trường production với tải thực tế. Các
ngưỡng dưới đây áp dụng cho trường hợp cache warm (Redis hit rate ≥ 80%).

NFR-PERF-001 Thời gian phản hồi API: • p50 ≤ 150ms — cho tất cả GET
Response Time API endpoints với dữ liệu cache. • p95 ≤ 500ms — cho tất cả API
endpoints (kể cả write operations). • p99 ≤ 1000ms — không
vượt quá 1 giây trong mọi trường hợp. Đo bằng:
OpenTelemetry + Grafana / k6 load test.
NFR-PERF-002 Hệ thống xử lý đồng thời ≥ 100 concurrent users mà không
Throughput degradation: • Trên phần cứng: 2 vCPU, 4GB RAM (single
instance). • Horizontal scaling: thêm instance tăng tuyến tính.
Đo bằng: k6 smoke test → load test → stress test.
NFR-PERF-003 Cache Redis Cache hit rate ≥ 80% trong điều kiện steady-state. Các
Effectiveness đối tượng cache: • Category list: TTL = 30 phút (ít thay đổi). •
Recipe detail: TTL = 5 phút (cache-aside pattern). • Search
results: TTL = 1 phút. Cache invalidation: Event-driven — xóa
cache khi Create/Update/Delete.
NFR-PERF-004 Tất cả queries đến PostgreSQL: • Không có N+1 query
Database Query problem — bắt buộc dùng .Include()/.ThenInclude() và
projection. • Index: đảm bảo mọi WHERE/ORDER BY column
đều có B-tree index tương ứng. • Slow query log: cảnh báo khi
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 40 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
query > 100ms (Serilog performance behavior). • EXPLAIN
ANALYZE: phải pass review trước khi merge.
NFR-PERF-005 Next.js frontend đạt chuẩn Google Core Web Vitals (đo bằng
Frontend Lighthouse CI): • LCP (Largest Contentful Paint) ≤ 2.5s. • CLS
Performance (Core (Cumulative Layout Shift) ≤ 0.1. • INP (Interaction to Next
Web Vitals) Paint) ≤ 200ms. • First Load JS Bundle ≤ 200KB (gzipped). Kỹ
thuật: ISR (Incremental Static Regeneration), Image
Optimization (next/image), Code Splitting.
4.2. Bảo mật (NFR-SEC)
Toàn bộ yêu cầu bảo mật tuân thủ OWASP Top 10 (2021) và được kiểm thử qua security
review trước khi release production.
NFR-SEC-001 Mật khẩu phải được hash bằng ASP.NET Core Identity mặc
Password & Hashing định (PBKDF2-HMACSHA512, iteration count ≥ 100.000).
Không bao giờ lưu plaintext password. Yêu cầu độ phức tạp: ≥
8 ký tự, chứa ít nhất 1 chữ hoa + 1 chữ thường + 1 số + 1 ký
tự đặc biệt (cấu hình qua IdentityOptions.Password).
NFR-SEC-002 JWT Access Token: JWT signed bằng HS256, TTL = 15 phút, claim:
Token Security userId, email, roles, jti. Refresh Token: 128-bit
cryptographically secure random bytes, hash SHA-256 trước
khi lưu DB, TTL = 7 ngày. Rotation: Refresh token bị revoke
ngay sau khi dùng, cấp token mới (Refresh Token Rotation).
Detection: Nếu refresh token đã bị revoke được dùng lại →
revoke toàn bộ family (Reuse Detection).
NFR-SEC-003 Rate Giới hạn yêu cầu theo IP để ngăn brute force và DDoS: • Auth
Limiting endpoints (/auth/_): 10 request/phút/IP. • API chung: 100
request/phút/IP. • Upload endpoints: 5 request/phút/IP.
Implementation: ASP.NET Core Rate Limiting middleware
(Fixed Window, sliding window cho auth). HTTP 429 khi vượt
giới hạn với Retry-After header.
NFR-SEC-004 Input Toàn bộ input được validate tại Application Layer
Validation & File (FluentValidation) TRƯỚC khi xử lý: • SQL Injection: EF Core
Upload Security parameterized queries (không raw SQL với user input). • XSS:
Input sanitization + Content-Security-Policy header. • MIME
Validation: Đọc magic bytes (không tin vào Content-Type
header) khi upload. • File size: Kiểm tra trước khi read stream
(không buffer toàn bộ vào memory trước). • Path Traversal:
GUID-based filename generation (không dùng tên file của
user).
NFR-SEC-005 HTTPS Toàn bộ traffic phải qua HTTPS (TLS 1.2+): • Nginx: redirect
& CORS HTTP → HTTPS, HSTS header (max-age=31536000). •
CORS Policy: Chỉ cho phép origin được cấu hình qua
appsettings (không wildcard _). • Allowed Origins:
http://localhost:3000 (dev), https://domain.com (prod). •
Cookie: SameSite=Strict, Secure=true (nếu dùng cookie cho
refresh token).
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 41 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
NFR-SEC-006 Kiểm tra phân quyền tại Application Layer (không chỉ ở
Authorization & Presentation Layer): • Authorization Handler:
Resource Ownership RecipeAuthorizationHandler xác minh ResourceOwnership
(Author chỉ xóa recipe của mình). • Role-based policies:
"AuthorPolicy", "AdminPolicy" (không hardcode role string). •
Sensitive endpoints (DELETE, PATCH publish): double-check
user ID trước khi commit. • Audit trail: Log mọi write operation
với userId + timestamp (Serilog).
NFR-SEC-007 Secrets Không bao giờ commit secrets vào Git: • Development:
Management ASP.NET Core User Secrets (dotnet user-secrets). •
Production: Environment variables (Docker Compose env_file /
Kubernetes Secrets). • Rotation: Khuyến nghị rotate JWT
signing key mỗi 90 ngày. • Scanning: Pre-commit hook kiểm
tra với truffleHog/gitleaks.
4.3. Khả năng Sử dụng (NFR-USE)
NFR-USE-001 Giao diện hiển thị chính xác trên tất cả breakpoints: • Mobile:
Responsive Design 320px – 767px (single column, touch-friendly). • Tablet: 768px
– 1199px (2-column grid). • Desktop: ≥ 1200px (full layout).
Framework: Tailwind CSS utility-first. Không sử dụng CSS
framework override. Kiểm thử: Chrome DevTools responsive
mode + BrowserStack (iOS, Android).
NFR-USE-002 Tuân thủ WCAG 2.1 Level AA: • Semantic HTML5: <article>,
Accessibility (a11y) <nav>, <main>, <aside>. • ARIA attributes: aria-label, aria-
expanded, role trên interactive elements. • Keyboard
navigation: tất cả chức năng dùng được bằng bàn phím (Tab,
Enter, Escape). • Color contrast ratio ≥ 4.5:1 (text) và ≥ 3:1 (UI
components). • Screen reader: test với NVDA (Windows) và
VoiceOver (macOS/iOS).
NFR-USE-003 Error Thông báo lỗi phải rõ ràng và actionable: • API: trả về RFC
Messages 7807 Problem Details (type, title, status, detail, errors{}). •
Frontend: hiển thị ngay bên cạnh field lỗi (React Hook Form
inline validation). • Server errors (5xx): hiển thị thông báo thân
thiện, không lộ stack trace. • I18n-ready: error messages sử
dụng error code (không hardcode tiếng Việt/Anh).
NFR-USE-004 Mọi async operation phải có visual feedback: • Loading
Loading States skeleton: hiển thị trong khi fetch data (không blank screen). •
Optimistic update: UI cập nhật ngay, rollback nếu API fail. •
Toast notification: xác nhận thành công/thất bại sau write
operation. • Progress indicator: upload ảnh hiển thị progress
bar (%) realtime.
4.4. Độ tin cậy (NFR-REL)
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 42 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
NFR-REL-001 Uptime Hệ thống có uptime ≥ 99.5% (≈ 3.65 giờ downtime/năm). •
SLA Maintenance window: công bố trước 48 giờ qua banner thông
báo. • Health check: /health/ready probe mỗi 10 giây
(Kubernetes readiness probe). • Monitoring: Uptime Robot /
Better Uptime gửi alert khi down > 1 phút.
NFR-REL-002 Error Hệ thống xử lý lỗi gracefully, không crash toàn bộ: • Global
Handling & Resilience Exception Handler Middleware: bắt tất cả unhandled
exceptions → trả 500 Problem Details + log. • Database
connection pool: tự reconnect, timeout 30s. • Redis failover:
nếu Redis down → fallback database (không cache), không
throw exception. • Hangfire retry: mỗi job tối đa 3 retry với
exponential backoff. • Circuit Breaker: (tùy chọn nâng cao)
Polly cho external HTTP calls.
NFR-REL-003 Data Dữ liệu không bị mất trong trường hợp restart hoặc crash: •
Durability PostgreSQL WAL (Write-Ahead Logging): đảm bảo ACID. •
Backup: pg_dump tự động hàng ngày lúc 03:00 AM, lưu 30
ngày. • MinIO: dữ liệu file trên volume persistent (không
ephemeral container storage). • Refresh tokens: lưu DB
(không Redis) để survive restart. • Soft delete: Recipe được
đánh dấu IsDeleted thay vì xóa vật lý (có thể khôi phục).
4.5. Khả năng Bảo trì (NFR-MAINT)
NFR-MAINT-001 Code Toàn bộ code phải pass static analysis trước khi merge: •
Quality .NET: SonarAnalyzer, StyleCop, EditorConfig (indent, naming
conventions). • TypeScript/React: ESLint (Airbnb ruleset),
Prettier. • Không có compiler warnings trong build CI. • Code
review: ít nhất 1 reviewer phê duyệt Pull Request.
NFR-MAINT-002 Test Độ phủ test tối thiểu: • Unit tests: ≥ 80% line coverage
Coverage (Application layer commands, queries, validators). • Integration
tests: tất cả API endpoints có ít nhất 1 happy path + 1 error
case. • E2E tests: 5 critical user flows (register, login, create
recipe, publish, search). Tool: xUnit (backend), Jest + Testing
Library (frontend), Playwright (E2E).
NFR-MAINT-003 Tài liệu kỹ thuật bắt buộc: • README.md: hướng dẫn setup
Documentation dev environment (Docker Compose) trong < 5 phút. • API
documentation: tự động sinh từ XML comments +
Scalar/Swagger UI tại /scalar. • Architecture Decision Records
(ADR): ghi lại mọi quyết định kiến trúc quan trọng. •
CHANGELOG.md: cập nhật mỗi release (theo Keep a
Changelog + SemVer).
NFR-MAINT-004 Tuân thủ nghiêm ngặt dependency rules của Clean
Clean Architecture Architecture: • Domain layer: KHÔNG dependency vào bất kỳ
Compliance layer nào khác. Không có nuget packages ngoài
FluentValidation. • Application layer: chỉ depend vào Domain.
KHÔNG reference Infrastructure. • Infrastructure layer: depend
vào Application (implements interfaces). • Vi phạm: được phát
hiện qua ArchUnit.NET tests hoặc custom Architecture test
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 43 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
project. • CQRS: Commands thay đổi state, Queries đọc data
— không trộn lẫn.
4.6. Khả năng Mở rộng (NFR-SCALE)
NFR-SCALE-001 API được thiết kế stateless để hỗ trợ horizontal scaling: • JWT
Stateless Backend authentication (không session server-side). • Distributed cache
(Redis, không in-memory IMemoryCache) cho mọi shared
state. • Distributed lock (RedLock) cho các tác vụ singleton
(sitemap generation). • Hangfire: chạy với multiple workers
(IBackgroundJobServer), PostgreSQL làm shared queue.
NFR-SCALE-002 Chiến lược database scaling: • Connection pooling: Npgsql
Database Scaling built-in pool (max 100 connections/instance). • Read replica
(tùy chọn): EF Core split queries + IQueryable routing qua
IDbContextFactory. • Index strategy: B-tree cho equality/range,
GIN cho full-text search (tsvector). • Table partitioning: (nâng
cao) partition Recipe by CreatedAt khi > 1 triệu rows.
NFR-SCALE-003 Hạ tầng có thể scale theo chiều ngang: • Docker: mỗi service
Infrastructure Scaling là container riêng biệt (API, Postgres, Redis, MinIO, Nginx). •
Nginx: load balancer upstream pool cho nhiều API instances. •
MinIO: Distributed Mode (4+ nodes) cho production storage
scaling. • CDN: static assets (Next.js \_next/static) được serve
qua CDN (Cloudflare).
4.7. Tối ưu SEO (NFR-SEO)
NFR-SEO-001 Mỗi trang công thức nấu ăn phải có JSON-LD Schema.org
Structured Data Recipe markup: • @type: "Recipe" • Thuộc tính: name,
description, image, author, datePublished, prepTime,
cookTime, totalTime, recipeYield, recipeIngredient[],
recipeInstructions[], nutrition. • Validate: Google Rich Results
Test — phải pass 100%. • Kết quả: Rich Snippets trên Google
Search (star rating, time, ingredients).
NFR-SEO-002 Meta Mỗi trang phải có đầy đủ: • <title>: "{Recipe Name} | Culinary
Tags & Open Graph Blog" (≤ 60 ký tự). • <meta name="description">: mô tả 150–
160 ký tự. • Open Graph: og:title, og:description, og:image
(1200×630px), og:url, og:type. • Twitter Card:
summary_large_image. • Canonical URL: tránh duplicate
content (slug-based URL). • Robots: index, follow (published) |
noindex (draft/archived).
NFR-SEO-003 Sitemap XML tự động: • Sinh bởi FR-JOB-003 (Hangfire
Sitemap & Robots Recurring Job, hàng ngày 02:00 AM UTC). • Bao gồm: tất cả
Published recipes + category pages + trang tĩnh. • Format:
sitemap.xml chuẩn, có <loc>, <lastmod>, <changefreq>,
<priority>. • Robots.txt: cho phép tất cả crawlers, khai báo
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 44 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
Sitemap URL. • Ping Google Search Console sau khi update
sitemap.
NFR-SEO-004 URL URL phải thân thiện SEO: • Recipes: /recipes/{slug} — slug là
Structure chữ thường, gạch nối, không dấu. • Categories:
/categories/{slug}. • Slug generation: tự động từ title, unique,
không thay đổi sau khi publish. • Redirect: Nếu slug thay đổi
(draft) → 301 redirect từ slug cũ sang slug mới. • Không dùng
query params cho nội dung chính (chỉ dùng cho
filter/sort/pagination).
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 45 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0 5. Yêu cầu Giao diện Ngoài

Chương này mô tả tất cả giao diện giữa hệ thống Culinary Blog với các thực thể bên ngoài:
người dùng cuối, phần cứng, phần mềm bên thứ ba và giao tiếp mạng. Mọi giao tiếp đều qua
HTTPS (TLS 1.2+) trong môi trường production.

5.1. Giao diện Người dùng (UI)
Hệ thống cung cấp giao diện web duy nhất trên nền Next.js App Router, hoạt động như Single
Page Application (SPA) với Server-Side Rendering (SSR) và Incremental Static Regeneration
(ISR).

| Màn hình / Route | Mô tả               | Loại Rendering    | Yêu cầu Auth |
| ---------------- | ------------------- | ----------------- | ------------ |
| /                | Trang chủ: danh     | ISR               | Không        |
|                  | sách recipe nổi bật | (revalidate=3600) |              |

- categories
  | /recipes | Danh sách tất cả | SSR (dynamic) | Không |
  | --------- | ----------------- | -------------- | ------ |
  recipes với
  filter/sort/search
  | /recipes/[slug] | Chi tiết recipe: | ISR | Không |
  | ---------------- | -------------------- | ----------------- | ------ |
  | | ingredients, steps, | (revalidate=300) | |
  nutrition, JSON-LD
  | /categories | Danh sách category | ISR | Không |
  | ------------ | ------------------- | ---- | ------ |
  (revalidate=3600)
  | /categories/[slug] | Danh sách recipe | ISR | Không |
  | ------------------- | ------------------ | ----------------- | ---------------- |
  | | theo category | (revalidate=600) | |
  | /auth/login | Form đăng nhập | CSR | Không (redirect |
  | | (email/password + | | nếu đã login) |
  Google OAuth
  button)
  | /auth/register | Form đăng ký tài | CSR | Không |
  | --------------- | ----------------- | ---- | ------ |
  khoản mới
  | /dashboard | Trang tổng quan | CSR | Bắt buộc |
  | ------------------- | ------------------ | ---- | --------------- |
  | | của Author/Admin | | (Author/Admin) |
  | /dashboard/recipes | Quản lý danh sách | CSR | Bắt buộc |
  recipe của user
  | /dashboard/recipes/new | Form tạo recipe mới | CSR | Bắt buộc |
  | ----------------------------- | -------------------- | ---- | --------------- |
  | | (multi-step wizard) | | (Author/Admin) |
  | /dashboard/recipes/[id]/edit | Form chỉnh sửa | CSR | Bắt buộc |
  | | recipe | | (Owner/Admin) |
  | /dashboard/categories | Quản lý categories | CSR | Bắt buộc |
  | | (chỉ Admin) | | (Admin) |
  CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 46 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
Màn hình / Route Mô tả Loại Rendering Yêu cầu Auth
/profile Xem và chỉnh sửa CSR Bắt buộc
thông tin cá nhân
/search Trang kết quả full- SSR Không
text search
5.2. Giao diện Phần mềm – REST API
Backend cung cấp RESTful API theo chuẩn JSON. Toàn bộ endpoints được tiền tố /api/v1.
Xem chi tiết tại Chương 8.
Giao thức HTTP/1.1 và HTTP/2 qua HTTPS (TLS 1.2+). Nginx
termination SSL.
Base URL (dev) http://localhost:5000/api/v1
Base URL (prod) https://api.culinaryblog.com/api/v1
Content-Type application/json; charset=utf-8 (request và response).
Multipart/form-data cho file upload endpoints.
Authentication Bearer Token trong Authorization header: Authorization:
Bearer <access_token>. Refresh token: trong request body
(không dùng cookie để tránh CSRF).
Response Format Success: { "data": {...}, "meta": { "page":1, "pageSize":10,
"total":100 } } Error: RFC 7807 Problem Details { "type", "title",
"status", "detail", "errors":{} }
Versioning URL Path versioning: /api/v1/. Khi có breaking changes →
/api/v2/ (v1 được duy trì tối thiểu 6 tháng).
CORS Headers Access-Control-Allow-Origin: <configured-origins> Access-
Control-Allow-Methods: GET, POST, PUT, PATCH, DELETE,
OPTIONS Access-Control-Allow-Headers: Content-Type,
Authorization, X-Correlation-ID
Rate Limit Headers X-RateLimit-Limit: 100 X-RateLimit-Remaining: 87 X-
RateLimit-Reset: 1700000000 (Unix timestamp) Retry-After: 30
(seconds, khi 429)
Correlation ID X-Correlation-ID header: sinh tự động nếu không có trong
request, trả về trong response. Gán vào tất cả log entries
(Serilog MDC).
5.3. Giao diện Dịch vụ Bên thứ ba
Dịch vụ Mục đích Giao thức / SDK Cấu hình / Secrets
Google OAuth Đăng nhập OAuth 2.0 Authorization Code + PKCE. GoogleClientId, GoogleClientSecret
2.0 / đăng ký Redirect URI: /api/v1/auth/google/callback. (User Secrets / env var). Google Cloud
bằng tài Scopes: openid, email, profile. Console → OAuth 2.0 Client ID.
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 47 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
| Dịch vụ | Mục đích | Giao thức / SDK | | Cấu hình / Secrets |
| -------- | --------- | ---------------- | --- | ------------------- |
khoản
Google
MinIO (S3- Lưu trữ file AWS SDK for .NET (AWSSDK.S3). MinIO**Endpoint, MinIO**AccessKey,
compatible) ảnh công Endpoint override cho MinIO. Presigned MinIO**SecretKey,
thức URL cho direct browser upload (optional). MinIO**BucketName. Docker service:
minio:9000.
Hangfire Background Nuget: Hangfire.Core, Dùng chung ConnectionString với
| | job | Hangfire.AspNetCore, | | PostgreSQL. HANGFIRE_SCHEMA = |
| --- | ---- | --------------------- | --- | ------------------------------ |
processing Hangfire.PostgreSql. In-process server. hangfire.
Dashboard: /hangfire (Admin only, policy-
protected).
Serilog + Seq Structured Serilog.Sinks.Console (JSON), Seq**ServerUrl = http://seq:5341
logging & Serilog.Sinks.File, Serilog.Sinks.Seq. (Docker). Production: Elastic / Azure
| | log | HTTP ingest API. | | Monitor. |
| --- | ---- | ----------------- | --- | --------- |
aggregation
OpenTelemetry Distributed OpenTelemetry .NET SDK. OTLP exporter. OTEL_EXPORTER_OTLP_ENDPOINT.
tracing & Tracing: HttpClient, EF Core, AspNetCore. Development: Seq OTLP. Production:
| | metrics | | | Grafana Tempo / Jaeger. |
| --- | -------- | --- | --- | ------------------------ |
SMTP / Email Gửi MailKit (IEmailSender). Kết nối qua SMTP Smtp**Host, Smtp**Port,
| | welcome | với TLS. | | Smtp**Username, Smtp\_\_Password. |
| --- | ---------- | --------- | --- | -------------------------------- |
| | email (FR- | | | Development: Mailhog (Docker). |
JOB-001)
Google Search Ping HTTP GET: Không cần API key. Gọi trong FR-JOB-
Console sitemap https://www.google.com/ping?sitemap={url} 003.
update

5.4. Giao diện Phần cứng
Hệ thống là web application, không giao tiếp trực tiếp với phần cứng chuyên biệt. Yêu cầu
phần cứng tối thiểu cho server:

| Thành phần | Development (local)      |     | Production (minimum)        |     |
| ---------- | ------------------------ | --- | --------------------------- | --- |
| CPU        | 2 cores (Intel/AMD/ARM64 |     | 2 vCPU (VPS/Cloud instance, |     |
|            | — Apple M-series được hỗ |     | x86_64)                     |     |

trợ qua Docker)
| RAM | 8 GB (chạy Docker | | 4 GB (API + dependencies riêng lẻ) | |
| ---- | ------------------ | --- | ----------------------------------- | --- |
Compose đầy đủ: API + PG

- Redis + MinIO + Seq)
  | Storage | 20 GB SSD (cho Docker | | 50 GB SSD (production data | |
  | -------- | ------------------------- | --- | --------------------------- | --- |
  | | images + database data + | | growth) | |
  MinIO volumes)
  Network Kết nối internet (npm/nuget Bandwidth ≥ 1 Gbps, IP tĩnh
  packages, Google OAuth)
  CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 48 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
| Thành phần | Development (local) | Production (minimum) |
| ----------- | -------------------- | --------------------- |
Browser Client Chrome 112+, Firefox Tương tự — không hỗ trợ IE11
113+, Safari 16+, Edge
112+ (ES2020+)

|     |     |     |
| --- | --- | --- |

CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 49 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0 6. Kiến trúc Hệ thống

Chương này mô tả tổng quan kiến trúc phần mềm của hệ thống Culinary Blog. Hệ thống được
thiết kế theo mô hình Client-Server với hai tầng riêng biệt: Frontend (Next.js) và Backend
(.NET 10 Minimal API), giao tiếp qua REST API. Backend tuân thủ nguyên tắc Clean
Architecture kết hợp CQRS pattern.

6.1. Tổng quan Kiến trúc

| Tầng             | Technology              | Vai trò                 | Giao tiếp với |
| ---------------- | ----------------------- | ----------------------- | ------------- |
| Client           | Browser                 | Người dùng tương tác    | Next.js App   |
| (Browser/Mobile) | (Chrome/Firefox/Safari) | qua giao diện web       |               |
| Frontend         | Next.js 14+ App         | Rendering UI, route     | Backend       |
|                  | Router, TypeScript,     | management, client-side | REST API      |
|                  | Tailwind CSS, Auth.js   | state. SSR/ISR cho SEO. |               |

v5, TanStack Query,
React Hook Form +
Zod
Nginx Reverse Nginx Alpine (Docker) SSL termination, load Frontend
| Proxy | | balancing, static file | :3000, |
| ------ | --- | ----------------------- | ------------ |
| | | caching, rate limiting | Backend API |
| | | basic. | :5000 |
Backend API ASP.NET Core .NET Business logic, PostgreSQL,
| | 10 Minimal API | authentication, data | Redis, MinIO, |
| ------------ | --------------- | ------------------------- | -------------- |
| | | access, background jobs. | Email |
| Cache Layer | Redis 7 | Distributed cache cho | Backend API |
recipe/category/search
results. Rate limiting
counters.
Object Storage MinIO (S3-compatible) Lưu file ảnh: original, Backend API
| | | medium (800×600), | (via |
| --- | --- | --------------------- | ----------- |
| | | thumbnail (300×300). | AWSSDK.S3) |
Database PostgreSQL 16 Persistent relational data Backend API
| | | storage. Full-text search | (via EF Core) |
| --- | --- | -------------------------- | -------------- |
via tsvector.
Observability Serilog + Seq, Logging, metrics, Backend API
| | OpenTelemetry + | distributed tracing. | |
| --- | ---------------- | --------------------- | --- |
Grafana/Jaeger

6.2. Kiến trúc Backend – Clean Architecture
Backend tuân thủ Clean Architecture (Robert C. Martin) với nguyên tắc Dependency Rule:
dependency chỉ đi vào trong (hướng Domain). Không bao giờ có reference từ
Domain/Application ra Infrastructure.
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 50 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
Domain Layer Nhân lõi hệ thống. Chứa: • Entities: Recipe, Category,
(CulinaryBlog.Domain) ApplicationUser, RecipeStep, RecipeIngredient,
RecipeImage. • Value Objects: Slug, EmailAddress. •
Owned Entities: RecipeNutrition. • Domain Events
(optional): RecipePublishedEvent. • Enums:
RecipeDifficulty, RecipeStatus. • Interfaces:
IRepository<T>, IRecipeRepository,
ICategoryRepository. • Không có NuGet dependencies
(chỉ .NET BCL).
Application Layer Orchestration Layer. Chứa: • Commands (CQRS write):
(CulinaryBlog.Application) CreateRecipeCommand, PublishRecipeCommand,
LoginCommand... • Queries (CQRS read):
GetRecipesQuery, GetRecipeBySlugQuery... •
Handlers (MediatR IRequestHandler): xử lý logic
business cho mỗi command/query. • DTOs / Response
models: RecipeDto, UserDto, PagedResult<T>. •
Validators (FluentValidation): validation rules cho mỗi
command. • Pipeline Behaviors: ValidationBehavior,
LoggingBehavior, CachingBehavior,
PerformanceBehavior. • Service interfaces:
IEmailService, IJwtService, IFileStorageService,
ICurrentUser.
Infrastructure Layer Implements application interfaces. Chứa: • EF Core:
(CulinaryBlog.Infrastructure) CulinaryBlogDbContext, configurations, migrations,
repositories. • Repository implementations:
RecipeRepository (LINQ + EF Core + FTS),
CategoryRepository. • JWT Service: JwtService
(System.IdentityModel.Tokens.Jwt). • File Storage:
MinioFileStorageService (AWSSDK.S3). • Email:
MailKitEmailService. • Cache: RedisCacheService
(StackExchange.Redis). • Hangfire job registrations. •
EF Core Interceptors: AuditInterceptor (auto set
CreatedAt/UpdatedAt).
Presentation Layer HTTP interface. Chứa: • Minimal API Endpoint Groups:
(CulinaryBlog.API) AuthEndpoints, RecipesEndpoints,
CategoriesEndpoints. • Middleware:
GlobalExceptionMiddleware, CorrelationIdMiddleware,
RateLimitingMiddleware. • DI Configuration: Program.cs

- Extension methods (AddApplication,
  AddInfrastructure, AddPresentation). • OpenAPI: Scalar
  UI tại /scalar, XML documentation comments. •
  Authentication: JWT Bearer + Google OAuth via Auth.js
  v5 (frontend) hoặc ASP.NET Google provider.
  6.3. CQRS + MediatR Pipeline
  CQRS (Command Query Responsibility Segregation) tách biệt read và write models. Mỗi
  request đi qua MediatR Pipeline Behaviors theo thứ tự:
  CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 51 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
| Thứ Pipeline Behavior | | Trách nhiệm | Áp dụng cho |
| ----------------------- | --- | ------------ | ------------ |
tự
| 1 LoggingBehavior | | Log request type, | Tất cả Commands và |
| ------------------- | --- | -------------------- | ------------------- |
| | | parameters, elapsed | Queries |
time. Cảnh báo nếu >
500ms.
2 ValidationBehavior Chạy FluentValidation Tất cả Commands và
| | | validators đã đăng ký. | Queries có Validator |
| --- | --- | ----------------------- | --------------------- |
Throw
ValidationException nếu
có lỗi.
3 CachingBehavior Kiểm tra Redis cache Queries implements
| | | trước khi xử lý. | ICacheable (GET |
| --- | --- | ---------------------- | ---------------- |
| | | Implements ICacheable | endpoints) |
interface trên Query.
| 4 Handler | | Thực thi business logic: | Tất cả (bắt buộc) |
| ------------------ | --- | ------------------------- | ------------------ |
| (IRequestHandler) | | gọi repositories, raise | |
domain events, tạo
response DTO.
5 CacheInvalidationBehavior Xóa cache liên quan sau Commands thay đổi
| | | khi Command thành | data |
| --- | --- | ------------------ | ----------------------- |
| | | công. Implements | (Create/Update/Delete) |
ICacheInvalidator.

6.4. Mô hình Quan hệ Thực thể (ERD tóm tắt)
Hệ thống sử dụng PostgreSQL 16 với EF Core Code First. Tất cả entities kế thừa BaseEntity
(Id, CreatedAt, UpdatedAt, IsDeleted, RowVersion).

| Thực thể | Quan hệ |     | Bảng PostgreSQL |
| -------- | ------- | --- | --------------- |

Recipe Nhiều RecipeStep (1:N) Nhiều "Recipes" "RecipeSteps"
| | RecipeIngredient (1:N) Nhiều | | "RecipeIngredients" |
| --- | -------------------------------- | --- | --------------------------- |
| | RecipeImage (1:N) Một | | "RecipeImages" (owned — |
| | RecipeNutrition (1:1 Owned) Một | | cột trong Recipes) |
| | Category (N:1) Một | | "Categories" "AspNetUsers" |
Author/ApplicationUser (N:1)
ApplicationUser Nhiều Recipe (Author, 1:N) Nhiều "AspNetUsers" (Identity)
| | RefreshToken (1:N) | | "RefreshTokens" |
| ------------- | -------------------------- | --- | ---------------- |
| Category | Nhiều Recipe (1:N) | | "Categories" |
| RefreshToken | Một ApplicationUser (N:1) | | "RefreshTokens" |

6.5. Triển khai – Docker Compose
Toàn bộ hệ thống được containerized với Docker Compose. Development dùng docker-
compose.yml, Production dùng docker-compose.prod.yml với optimized build + secrets
management.
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 52 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0

| Service | Image | Port | Volume / Dependency |
| ------- | ----- | ---- | ------------------- |

(host:container)
| nginx | nginx:alpine | 80:80, 443:443 | Depends: api, frontend |
| ------ | ------------- | --------------- | ----------------------- |
Volume: ./nginx/nginx.conf,
./ssl/
| api | culinaryblog-api | 5000:8080 | Depends: postgres, redis, |
| --------- | ----------------- | ---------- | -------------------------------- |
| | (Dockerfile) | | minio Env file: .env.production |
| frontend | culinaryblog-web | 3000:3000 | Depends: api |
(Dockerfile)
| postgres | postgres:16-alpine | 5432:5432 | Volume: |
| --------- | ------------------- | ---------- | -------- |
pgdata:/var/lib/postgresql/data
Env: POSTGRES_DB, USER,
PASSWORD
| redis | redis:7-alpine | 6379:6379 | Volume: redisdata:/data |
| ------ | --------------- | ---------- | ------------------------ |
Command: redis-server --
appendonly yes
minio minio/minio:latest 9000:9000, Volume: miniodata:/data
| | | 9001:9001 | Command: server /data -- |
| --- | --- | ---------- | ------------------------ |
| | | (Console) | console-address :9001 |
seq datalust/seq:latest 5341:80 Volume: seqdata:/data Dev
only — không deploy
production
mailhog mailhog/mailhog 8025:8025 (UI), Dev only — test email
1025:1025
(SMTP)

CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 53 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0 7. Mô hình Dữ liệu

Chương này đặc tả cấu trúc dữ liệu đầy đủ của hệ thống Culinary Blog. Tất cả entities kế thừa
BaseEntity và sử dụng Soft Delete pattern (IsDeleted flag). Database: PostgreSQL 16 với EF
Core 10 Code First.

7.1. BaseEntity (Abstract)
Tất cả thực thể kế thừa từ BaseEntity. Không tạo bảng riêng (Table-Per-Hierarchy không
được dùng ở đây — mỗi entity có bảng riêng với các cột kế thừa).

| Column | Kiểu dữ | Ràng buộc | Mô tả |     |
| ------ | ------- | --------- | ----- | --- |

liệu
| Id | uuid (Guid) | PRIMARY KEY, | Khóa chính UUID v4 — tránh | |
| --- | ------------ | ------------- | --------------------------- | --- |
| | | DEFAULT | sequential ID guessing. | |
gen_random_uuid()
CreatedAt timestamptz NOT NULL, Thời điểm tạo bản ghi. Set bởi
| | | DEFAULT NOW() | AuditInterceptor (EF Core). | |
| ---------- | ------------ | -------------- | ----------------------------- | --- |
| UpdatedAt | timestamptz | NULL | Thời điểm cập nhật cuối. Set | |
bởi AuditInterceptor khi
SaveChanges.
IsDeleted boolean NOT NULL, Soft delete flag. Global Query
| | | DEFAULT false | Filter: .Where(x => | |
| --- | --- | -------------- | -------------------- | --- |
!x.IsDeleted).
RowVersion bytea NOT NULL, Optimistic concurrency control.
| | (timestamp) | Concurrency Token | EF Core [Timestamp] | |
| --- | ------------ | ------------------ | -------------------- | --- |
annotation.

7.2. Recipe
Thực thể trung tâm của hệ thống. Một Recipe thuộc một Category và một Author. Chứa
Owned Entity RecipeNutrition và các Collection Navigation Properties.

| Column | Kiểu dữ | Ràng buộc | Index | Mô tả |
| ------ | ------- | --------- | ----- | ----- |

liệu
| Id | uuid | PK (kế thừa) | PK | (BaseEntity) |
| --- | ----- | ------------- | --- | ------------- |
Title varchar(200) NOT NULL IDX_Recipe_Title (GIN Tiêu đề công
| | | | trigram — optional) | thức. Unique |
| --- | --- | --- | -------------------- | ------------- |
không bắt buộc
(có thể trùng
title khác nhau
slug).
Slug varchar(220) NOT NULL, IDX_Recipe_Slug URL-friendly
| | | UNIQUE | (UNIQUE B-tree) | identifier. Sinh |
| --- | --- | ------- | ---------------- | ----------------- |
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 54 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
| Column | Kiểu dữ | Ràng buộc | Index | Mô tả |
| ------- | -------- | ---------- | ------ | ------ |
liệu
từ Title + chuẩn
hóa (lowercase,
replace space
→ -). Không
thay đổi sau
Publish.
| Description | text | NOT NULL | — | Mô tả ngắn (≤ |
| ------------ | ----- | --------- | --- | -------------- |
2000 ký tự).
Hiển thị trong
card preview và
SEO meta
description.
| Instructions | text | NOT NULL | — | Hướng dẫn |
| ------------- | ----- | --------- | --- | ---------- |
tổng quan dạng
markdown
(legacy field).
Chi tiết dùng
RecipeSteps.
| PrepTime | integer | NOT NULL, | — | Thời gian |
| --------- | -------- | ----------- | --- | ----------------- |
| | | CHECK > 0 | | chuẩn bị (phút). |
| CookTime | integer | NOT NULL, | — | Thời gian nấu |
| | | CHECK >= 0 | | (phút). 0 cho |
"No cook"
recipes.
| Servings | integer | NOT NULL, | — | Số khẩu phần |
| --------- | -------- | ---------- | --- | ------------- |
| | | CHECK > 0 | | (portions). |
Difficulty smallint NOT NULL, IDX_Recipe_Difficulty RecipeDifficulty:
| | (enum) | DEFAULT 1 | | 1=Easy, |
| --- | ------- | ---------- | --- | -------- |
2=Medium,
3=Hard,
4=Expert.
Status smallint NOT NULL, IDX_Recipe_Status RecipeStatus:
| | (enum) | DEFAULT 0 | | 0=Draft, |
| --- | ------- | ---------- | --- | --------- |
1=Published,
2=Archived.
CategoryId uuid NOT NULL, FK IDX_Recipe_CategoryId Khóa ngoại đến
| | | → | (B-tree) | Category. ON |
| --- | --- | -------------- | --------- | ------------- |
| | | Categories.Id | | DELETE |
RESTRICT
(không xóa
category có
recipe).
AuthorId varchar(450) NOT NULL, FK IDX_Recipe_AuthorId Khóa ngoại đến
| | | → | (B-tree) | ApplicationUser |
| --- | --- | --------------- | --------- | ---------------- |
| | | AspNetUsers.Id | | (Author). |
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 55 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
Column Kiểu dữ Ràng buộc Index Mô tả
liệu
SearchVector tsvector NULL IDX*Recipe_Search Full-text search
(GIN) vector. Được
cập nhật bởi
PostgreSQL
TRIGGER khi
Title/Description
thay đổi. Dùng
unaccent
extension cho
tiếng Việt.
PublishedAt timestamptz NULL IDX_Recipe_PublishedAt Thời điểm
publish. Set khi
Status chuyển
sang Published.
NULL nếu chưa
publish.
CreatedAt timestamptz NOT NULL — (BaseEntity)
UpdatedAt timestamptz NULL — (BaseEntity)
IsDeleted boolean NOT NULL IDX_Recipe_IsDeleted (BaseEntity) —
(partial) Global Query
Filter.
RowVersion bytea NOT NULL — (BaseEntity) —
Optimistic
concurrency.
7.2.1. RecipeNutrition (Owned Entity — cột trong bảng Recipes)
Owned Entity — không có bảng riêng. Các cột được nhúng trực tiếp vào bảng Recipes với
tiền tố "Nutrition*".
Column trong DB Property C# Kiểu Mô tả
Nutrition_Calories Calories decimal(8,2)? Năng lượng (kcal /
serving). Nullable.
Nutrition_Protein Protein decimal(8,2)? Đạm (gram / serving).
Nullable.
Nutrition_Carbohydrates Carbohydrates decimal(8,2)? Tinh bột (gram / serving).
Nullable.
Nutrition_Fat Fat decimal(8,2)? Chất béo (gram / serving).
Nullable.
Nutrition_Fiber Fiber decimal(8,2)? Chất xơ (gram / serving).
Nullable.
Nutrition_Sodium Sodium decimal(8,2)? Natri (mg / serving).
Nullable.
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 56 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0

7.3. RecipeStep
Các bước thực hiện chi tiết của một Recipe, được sắp xếp theo StepNumber.

| Column   | Kiểu | Ràng buộc       | Mô tả                       |
| -------- | ---- | --------------- | --------------------------- |
| Id       | uuid | PK (BaseEntity) | UUID khóa chính.            |
| RecipeId | uuid | NOT NULL, FK →  | Khóa ngoại. Cascade delete: |
|          |      | Recipes.Id, ON  | xóa Recipe → xóa tất cả     |
|          |      | DELETE CASCADE  | Steps.                      |

StepNumber integer NOT NULL, CHECK > Thứ tự bước (1, 2, 3...).
| | | 0 | UNIQUE cùng RecipeId |
| --- | --- | --- | --------------------- |
(composite unique).
Title varchar(200) NOT NULL Tên bước ngắn gọn (ví dụ: "Sơ
chế nguyên liệu").
| Description | text | NOT NULL | Mô tả chi tiết bước thực hiện. |
| ------------ | ----- | --------- | ------------------------------- |
TimerMinutes integer NULL, CHECK >= 0 Thời gian cần cho bước này
(phút). NULL nếu không áp
dụng.
| ImageUrl | varchar(500) | NULL | URL ảnh minh họa bước (trên |
| --------- | ------------- | ----- | ---------------------------- |
MinIO). Nullable.

7.4. RecipeIngredient

| Column   | Kiểu | Ràng buộc       | Mô tả                  |
| -------- | ---- | --------------- | ---------------------- |
| Id       | uuid | PK (BaseEntity) | UUID khóa chính.       |
| RecipeId | uuid | NOT NULL, FK →  | Khóa ngoại với cascade |
|          |      | Recipes.Id, ON  | delete.                |

DELETE CASCADE
| Name | varchar(200) | NOT NULL | Tên nguyên liệu (ví dụ: "Thịt |
| ----- | ------------- | --------- | ------------------------------ |
bò thăn").
Quantity decimal(10,3) NULL Số lượng (ví dụ: 500). Nullable
cho "nguyên liệu vừa đủ".
| Unit | varchar(50) | NULL | Đơn vị đo lường (gram, ml, |
| ----- | ------------ | ----- | --------------------------- |
thìa canh, quả...). Nullable.
| Notes | varchar(500) | NULL | Ghi chú tùy chọn (ví dụ: "thái |
| ------ | ------------- | ----- | ------------------------------- |
lát mỏng"). Nullable.
| OrderIndex | integer | NOT NULL, | Thứ tự hiển thị trong danh |
| ----------- | -------- | ---------- | --------------------------- |
| | | DEFAULT 0 | sách nguyên liệu. |

CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 57 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
7.5. RecipeImage

| Column | Kiểu | Ràng buộc       | Mô tả            |
| ------ | ---- | --------------- | ---------------- |
| Id     | uuid | PK (BaseEntity) | UUID khóa chính. |

RecipeId uuid NOT NULL, FK → Khóa ngoại với cascade delete.
Recipes.Id, ON
DELETE CASCADE
OriginalUrl varchar(500) NOT NULL URL ảnh gốc trên MinIO (ví dụ:
.../recipes/{recipeId}/{guid}.jpg).
| MediumUrl | varchar(500) | NULL | URL ảnh medium 800×600 |
| ---------- | ------------- | ----- | ----------------------- |
(sinh bởi FR-JOB-002).
Nullable khi job chưa chạy.
| ThumbnailUrl | varchar(500) | NULL | URL ảnh thumbnail 300×300 |
| ------------- | ------------- | ----- | -------------------------- |
(sinh bởi FR-JOB-002).
Nullable.
| AltText | varchar(200) | NULL | Alt text cho accessibility. |
| -------- | ------------- | ----- | ---------------------------- |
Nullable.
IsPrimary boolean NOT NULL, Ảnh chính (hiển thị đầu tiên).
| | | DEFAULT false | Chỉ có 1 ảnh IsPrimary=true / |
| --- | --- | -------------- | ------------------------------ |
Recipe.
| OrderIndex | integer | NOT NULL, | Thứ tự hiển thị gallery. |
| ----------- | -------- | ---------- | ------------------------- |
DEFAULT 0

7.6. Category

| Column | Kiểu | Ràng buộc       | Mô tả            |
| ------ | ---- | --------------- | ---------------- |
| Id     | uuid | PK (BaseEntity) | UUID khóa chính. |

Name varchar(100) NOT NULL, UNIQUE Tên danh mục (ví dụ: "Món
khai vị").
Slug varchar(120) NOT NULL, UNIQUE, URL-friendly name. Sinh từ
| | | IDX_Category_Slug | Name. |
| ------------ | ------------- | ------------------ | --------------------------- |
| Description | text | NULL | Mô tả danh mục. Nullable. |
| ImageUrl | varchar(500) | NULL | URL ảnh đại diện category. |
Nullable.
OrderIndex integer NOT NULL, Thứ tự hiển thị trên navigation.
DEFAULT 0

7.7. ApplicationUser (extends IdentityUser)
Kế thừa từ ASP.NET Core Identity IdentityUser<string>. Bảng: "AspNetUsers". Thêm các
custom columns:
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 58 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0

| Column | Kiểu | Ràng buộc | Mô tả |
| ------ | ---- | --------- | ----- |

(custom)
| DisplayName | varchar(100) | NOT NULL | Tên hiển thị công khai |
| ------------ | ------------- | --------- | ----------------------- |
(không phải username).
| AvatarUrl | varchar(500) | NULL | URL ảnh avatar. Nullable. |
| ---------- | ------------- | ----- | -------------------------- |
Sinh từ Google Avatar khi
đăng ký OAuth.
| Bio | text | NULL | Tiểu sử ngắn của tác giả. |
| ---- | ----- | ----- | -------------------------- |
Nullable. Hiển thị trên author
profile.
| IsActive | boolean | NOT NULL, | Trạng thái tài khoản. Admin |
| ---------- | ------------ | ------------- | ------------------------------ |
| | | DEFAULT true | có thể deactivate user (ban). |
| CreatedAt | timestamptz | NOT NULL, | Ngày tạo tài khoản. |
DEFAULT NOW()

Identity columns (kế thừa): Id (varchar 450), UserName, NormalizedUserName, Email,
NormalizedEmail, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber,
TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount.

7.8. RefreshToken

| Column | Kiểu         | Ràng buộc      | Mô tả             |
| ------ | ------------ | -------------- | ----------------- |
| Id     | uuid         | PK             | UUID khóa chính.  |
| UserId | varchar(450) | NOT NULL, FK → | Chủ sở hữu token. |

AspNetUsers.Id, ON
DELETE CASCADE
| TokenHash | varchar(64) | NOT NULL, UNIQUE, | SHA-256 hash |
| ---------- | ------------ | ------------------ | ------------- |
IDX_RefreshToken_Hash của raw token.
Không lưu raw
token.
| ExpiresAt | timestamptz | NOT NULL | Thời hạn token (7 |
| ---------- | ------------ | --------- | ------------------ |
ngày kể từ
CreatedAt).
| RevokedAt | timestamptz | NULL | Thời điểm revoke. |
| ---------- | ------------ | ----- | ------------------ |
NULL = còn hiệu
lực.
| ReplacedByTokenHash | varchar(64) | NULL | Hash của token |
| -------------------- | ------------ | ----- | --------------- |
mới (khi rotation).
Để trace token
family.
| CreatedAt | timestamptz | NOT NULL, DEFAULT | Thời điểm tạo. |
| ---------- | ------------ | ------------------ | --------------- |
NOW()
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 59 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
| Column | Kiểu | Ràng buộc | Mô tả |
| ------------ | ------------ | ---------- | --------------- |
| CreatedByIp | varchar(45) | NULL | IP address tạo |
token. Lưu để
audit.

|     |     |     |     |
| --- | --- | --- | --- |

CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 60 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0 8. Đặc tả REST API

Chương này liệt kê tất cả API endpoints của hệ thống Culinary Blog. Base URL: /api/v1. Tài
liệu chi tiết (request/response schemas) được sinh tự động qua Scalar UI tại /scalar.

Convention HTTP Method + Path (prefixed /api/v1) auth required = Bearer
JWT Access Token bắt buộc role = Role tối thiểu cần thiết
(Author ⊂ Admin)
| Pagination | | Query params: | | | |
| ----------- | --- | -------------- | --- | --- | --- |
?page=1&pageSize=10&sortBy=createdAt&sortOrder=desc
Response wrapper: { "data":[], "meta":{ "page", "pageSize",
"total", "totalPages" } }
Error Format RFC 7807 Problem Details: { "type":"about:blank", "title":"...",
"status":400, "detail":"...", "errors":{"field":["msg"]} }

8.1. Authentication Module (/auth)

| Method | Endpoint | Mô tả | Auth | Request | Response |
| ------ | -------- | ----- | ---- | ------- | -------- |

Body /
Params
POST /auth/register Đăng ký tài Không { email, 201: { userId,
| | | khoản mới | | password, | email, |
| --- | --- | ---------- | --- | -------------- | ------------ |
| | | | | displayName } | displayName |
} 400:
validation
errors 409:
email đã tồn
tại
| POST | /auth/login | Đăng nhập | Không | { email, | 200: { |
| ----- | ------------ | --------------- | ------ | ----------- | ------------- |
| | | email/password | | password } | accessToken, |
refreshToken,
expiresIn }
401: sai
credentials
429: quá giới
hạn rate limit
| POST | /auth/google | Đăng nhập | Không | { idToken } — | 200: { |
| ----- | ------------- | ------------- | ------ | -------------- | -------------- |
| | | Google OAuth | | ID Token từ | accessToken, |
| | | | | Google Sign- | refreshToken, |
| | | | | In JS SDK | expiresIn } |
400: invalid
token
POST /auth/refresh Làm mới Không (dùng { refreshToken 200: {
| | | Access Token | refreshToken) | } | accessToken, |
| --- | --- | ------------- | -------------- | --- | ------------- |
refreshToken,
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 61 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
| Method | Endpoint | Mô tả | Auth | Request | Response | |
| ------- | --------- | ------ | ----- | -------- | --------- | --- |
Body /
Params
expiresIn }
401: token
hết hạn / bị
revoke
POST /auth/logout Đăng xuất, Bearer JWT { refreshToken 204: No
| | | revoke Refresh | | } | Content 401: | |
| ---- | --------- | --------------- | ----------- | --- | ------------- | --- |
| | | Token | | | Unauthorized | |
| GET | /auth/me | Lấy thông tin | Bearer JWT | — | 200: { id, | |
| | | user hiện tại | | | email, | |
displayName,
avatarUrl,
bio, roles }
401:
Unauthorized
| PATCH | /auth/me | Cập nhật | Bearer JWT | { | 200: { id, | |
| ------ | --------- | -------------- | ----------- | -------------- | --------------- | --- |
| | | profile người | | displayName?, | email, | |
| | | dùng | | avatarUrl?, | displayName, | |
| | | | | bio? } | avatarUrl, bio | |
} 400:
validation
401:
Unauthorized

8.2. Categories Module (/categories)

| Method | Endpoint | Mô tả | Auth / Request |     |     | Response |
| ------ | -------- | ----- | -------------- | --- | --- | -------- |

Role
| GET | /categories | Lấy danh | Không — | | | 200: [{ id, |
| ---- | ------------ | ----------- | --------- | --- | --- | ------------- |
| | | sách tất | | | | name, slug, |
| | | cả | | | | description, |
| | | categories | | | | imageUrl, |
recipeCount
}]
GET /categories/{slug} Lấy chi Không ?page=1&pageSize=10&sortBy=... 200: {
| | | tiết | | | | category, |
| --- | --- | --------- | --- | --- | --- | ------------- |
| | | category | | | | recipes: |
| | | + danh | | | | PagedResult |
| | | sách | | | | } 404: |
| | | recipes | | | | Category not |
found
POST /categories Tạo Bearer { name, description?, imageUrl? } 201: { id,
| | | category | + | | | name, slug, |
| --- | --- | --------- | ------ | --- | --- | -------------- |
| | | mới | Admin | | | description } |
400:
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 62 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
| Method | Endpoint | Mô tả | Auth / Request | | Response |
| ------- | --------- | ------ | ---------------- | --- | --------- |
Role
validation
403:
Forbidden
409: name
đã tồn tại
PUT /categories/{id} Cập nhật Bearer { name, description?, imageUrl?, 200:
| | | category | + orderIndex? } | | category |
| --- | --- | --------- | ----------------- | --- | --------- |
| | | | Admin | | updated |
400/403/404
| DELETE | /categories/{id} | Xóa | Bearer — | | 204: No |
| ------- | ----------------- | --------- | ---------- | --- | ------------- |
| | | category | + | | Content 403: |
| | | (soft | Admin | | Forbidden |
| | | delete) | | | 404: Not |
found 409:
Có recipes
thuộc
category này

8.3. Recipes Module (/recipes)

| Method | Endpoint | Mô tả | Auth / Role | Request |     |
| ------ | -------- | ----- | ----------- | ------- | --- |

GET /recipes Danh sách Không ?page&pageSize&sortBy&sortOrder&categoryId&difficulty&minPrepTime&maxPrepTime
recipes
(Published,
paginated)
| GET | /recipes/{slug} | Chi tiết | Không (Draft: | — | |
| ---- | ---------------- | ------------ | -------------- | --- | --- |
| | | recipe theo | Author/Admin) | | |
slug (kèm
steps,
ingredients,
images,
nutrition)
GET /recipes/search Full-text Không ?q={keyword}&page&pageSize&categoryId&difficulty
search
công thức
POST /recipes Tạo recipe Bearer { title, description, categoryId, prepTime, cookTime, servings, difficulty, instructions,
| | | mới (trạng | (Author/Admin) | nutrition? } | |
| --- | --- | ----------- | --------------- | ------------- | --- |
thái Draft)
PUT /recipes/{id} Cập nhật Bearer { title?, description?, categoryId?, prepTime?, cookTime?, servings?, difficulty?,
| | | thông tin | (Owner/Admin) | instructions?, nutrition? } | |
| --- | --- | ---------- | -------------- | ---------------------------- | --- |
cơ bản
recipe
| PATCH | /recipes/{id}/publish | Publish | Bearer | — | |
| ------ | ---------------------- | -------- | -------------- | --- | --- |
| | | recipe | (Owner/Admin) | | |
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 63 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
| Method | Endpoint | Mô tả | Auth / Role | Request | |
| ------- | --------- | ------ | ------------ | -------- | --- |
(Draft →
Published)
| PATCH | /recipes/{id}/unpublish | Unpublish | Bearer | — | |
| ------ | ------------------------ | ---------- | -------------- | --- | --- |
| | | recipe | (Owner/Admin) | | |
(Published
→ Draft)
| PATCH | /recipes/{id}/archive | Archive | Bearer | — | |
| ------- | ---------------------- | ----------- | -------------- | --- | --- |
| | | recipe | (Owner/Admin) | | |
| DELETE | /recipes/{id} | Xóa recipe | Bearer | — | |
| | | (soft | (Owner/Admin) | | |
delete)

8.4. Recipe Images (/recipes/{id}/images)

| Method | Endpoint | Mô tả | Auth | Request | Response |
| ------ | -------- | ----- | ---- | ------- | -------- |

POST /recipes/{id}/images Upload ảnh Bearer multipart/form- 201: {
| | | mới cho | (Owner/Admin) | data: file | imageId, |
| --- | --- | -------- | -------------- | ----------- | ------------- |
| | | recipe | | (image), | originalUrl, |
| | | | | altText?, | altText, |
| | | | | isPrimary? | isPrimary } |
400:
MIME
invalid /
size >
5MB
403/404
PATCH /recipes/{id}/images/{imageId} Cập nhật Bearer { altText?, 200:
| | | metadata | (Owner/Admin) | isPrimary?, | image |
| --- | --- | ---------- | -------------- | -------------- | -------- |
| | | ảnh | | orderIndex? } | updated |
| | | (altText, | | | 403/404 |
isPrimary,
orderIndex)
DELETE /recipes/{id}/images/{imageId} Xóa ảnh Bearer — 204: No
| | | (MinIO file | (Owner/Admin) | | Content |
| --- | --- | ------------ | -------------- | --- | -------- |
| | | deleted | | | 403/404 |
async via
Hangfire)

8.5. Recipe Steps (/recipes/{id}/steps)

CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 64 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
| Method | Endpoint | | Mô tả Auth | | Request | Response |
| ------- | --------- | --- | ------------ | --- | -------- | --------- |
POST /recipes/{id}/steps Thêm Bearer { stepNumber, 201:
| | | | bước (Owner/Admin) | | title, | RecipeStepDto |
| ---- | ----------------------------- | --- | -------------------- | --- | --------------- | -------------- |
| | | | mới | | description, | 400/403/404 |
| | | | vào | | timerMinutes?, | |
| | | | recipe | | imageUrl? } | |
| PUT | /recipes/{id}/steps/{stepId} | | Cập Bearer | | { | 200: |
| | | | nhật (Owner/Admin) | | stepNumber?, | RecipeStepDto |
| | | | một | | title?, | 400/403/404 |
| | | | bước | | description?, | |
timerMinutes?,
imageUrl? }
DELETE /recipes/{id}/steps/{stepId} Xóa Bearer — 204: No
| | | | một (Owner/Admin) | | | Content |
| --- | --- | --- | ------------------- | --- | --- | -------- |
| | | | bước | | | 403/404 |

8.6. Recipe Ingredients (/recipes/{id}/ingredients)

| Method | Endpoint |     | Mô tả | Auth | Request | Response |
| ------ | -------- | --- | ----- | ---- | ------- | -------- |

POST /recipes/{id}/ingredients Thêm Bearer { name, 201:
| | | | nguyên | (Owner/Admin) | quantity?, | RecipeIngredientDto |
| --- | --- | --- | ------- | -------------- | ----------- | -------------------- |
| | | | liệu | | unit?, | 400/403/404 |
notes?,
orderIndex?
}
PUT /recipes/{id}/ingredients/{ingId} Cập Bearer { name?, 200:
| | | | nhật | (Owner/Admin) | quantity?, | RecipeIngredientDto |
| --- | --- | --- | ------- | -------------- | ----------- | -------------------- |
| | | | nguyên | | unit?, | 400/403/404 |
| | | | liệu | | notes?, | |
orderIndex?
}
DELETE /recipes/{id}/ingredients/{ingId} Xóa Bearer — 204: No Content
| | | | nguyên | (Owner/Admin) | | 403/404 |
| --- | --- | --- | ------- | -------------- | --- | -------- |
liệu

8.7. Health Check Endpoints

| Method                                         | Endpoint         | Mô tả   | Auth | Response             |     |     |
| ---------------------------------------------- | ---------------- | ------- | ---- | -------------------- | --- | --- |
| GET /health Tổng hợp health Không 200: Healthy | 503: Unhealthy { |
|                                                |                  | tất cả  |      | "status":"Healthy",  |     |     |
| ---                                            | ---              | ------- | ---  | -------------------- | --- | --- |

dependencies "entries":{"database":{"status":"Healthy"},...}
| | | (DB, Redis, | | } | | |
| --- | --- | ------------ | --- | --- | --- | --- |
MinIO)
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 65 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
Method Endpoint Mô tả Auth Response
GET /health/live Liveness probe Không 200: Healthy (luôn luôn, trừ khi process
— chỉ kiểm tra crashed)
process còn
sống
GET /health/ready Readiness probe Không 200: Healthy (DB + Redis up) 503:
— kiểm tra DB Unhealthy (không nhận traffic)
và Redis sẵn
sàng
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 66 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
Phụ lục A – HTTP Status Codes
Bảng dưới đây liệt kê tất cả HTTP Status Codes được sử dụng trong API Culinary Blog, cùng
ngữ cảnh sử dụng cụ thể.
Code Status Ngữ cảnh sử dụng
200 OK GET request thành công; PATCH trả về resource đã cập nhật;
POST /auth/login thành công.
201 Created POST tạo resource mới thành công (Recipe, Category, Step,
Ingredient, Image). Response body chứa resource vừa tạo.
204 No Content DELETE thành công; POST /auth/logout thành công. Không
có response body.
400 Bad Request Validation lỗi (FluentValidation), request body malformed, file
MIME không hợp lệ, business rule vi phạm (ví dụ: publish
recipe thiếu ingredients).
401 Unauthorized Access Token thiếu hoặc invalid; Refresh Token hết hạn / bị
revoke.
403 Forbidden Đã xác thực nhưng không có quyền: Author truy cập endpoint
Admin; Author cố xóa recipe của người khác.
404 Not Found Resource không tồn tại hoặc đã soft-delete (IsDeleted=true).
409 Conflict Trùng lặp unique field (email đã đăng ký, category slug đã tồn
tại); Xóa category đang có recipes.
422 Unprocessable Dữ liệu hợp lệ về cú pháp nhưng không thể xử lý về ngữ
Entity nghĩa (ví dụ: RowVersion conflict — Optimistic Concurrency).
429 Too Many Rate limit bị vượt. Response kèm header Retry-After (giây).
Requests
500 Internal Server Lỗi không xử lý được (unhandled exception). Trả RFC 7807,
Error log đầy đủ qua Serilog. Không lộ stack trace.
503 Service Health check failed (DB/Redis down); hoặc server overloaded.
Unavailable
Phụ lục B – Application Error Codes
Hệ thống sử dụng Application Error Codes (mã lỗi tùy chỉnh) trong trường RFC 7807 "type"
để frontend có thể xử lý lỗi theo programmatic way mà không phụ thuộc vào chuỗi message
(có thể thay đổi theo locale).
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 67 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
| Error Code | HTTP | Mô tả | Module |
| ----------- | ----- | ------ | ------- |
Status
| AUTH_EMAIL_EXISTS | 409 | Email đã được đăng | Auth |
| ------------------ | ---- | ------------------- | ----- |
ký bởi tài khoản khác.
| AUTH_INVALID_CREDENTIALS | 401 | Email hoặc mật khẩu | Auth |
| ------------------------- | ---- | -------------------- | ----- |
không đúng.
| AUTH_TOKEN_EXPIRED | 401 | Access Token đã hết | Auth |
| ------------------- | ---- | -------------------- | ----- |
hạn (15 phút).
| AUTH_TOKEN_INVALID | 401 | Access Token sai định | Auth |
| ------------------- | ---- | ---------------------- | ----- |
dạng hoặc chữ ký
không hợp lệ.
| AUTH_REFRESH_TOKEN_EXPIRED | 401 | Refresh Token đã hết | Auth |
| --------------------------- | ---- | --------------------- | ----- |
hạn (7 ngày).
| AUTH_REFRESH_TOKEN_REVOKED | 401 | Refresh Token đã bị | Auth |
| --------------------------- | ---- | -------------------- | ----- |
thu hồi (reuse
detection).
| AUTH_GOOGLE_TOKEN_INVALID | 400 | Google ID Token | Auth |
| -------------------------- | ---- | ---------------- | ----- |
không hợp lệ hoặc đã
hết hạn.
| AUTH_ACCOUNT_DISABLED | 403 | Tài khoản bị vô hiệu | Auth |
| ---------------------- | ---- | --------------------- | ----- |
hóa (IsActive=false)
bởi Admin.
| RECIPE_NOT_FOUND | 404 | Recipe với id/slug | Recipe |
| ----------------- | ---- | ------------------- | ------- |
không tồn tại hoặc đã
bị xóa.
| RECIPE_SLUG_EXISTS | 409 | Slug đã tồn tại — tự | Recipe |
| ------------------- | ---- | --------------------- | ------- |
động thêm suffix
(slug-1, slug-2...).
RECIPE_PUBLISH_INCOMPLETE 400 Recipe thiếu điều kiện Recipe
publish: phải có ít nhất
1 ingredient và 1 step.
| RECIPE_FORBIDDEN | 403 | User không phải | Recipe |
| ----------------- | ---- | ---------------- | ------- |
owner và không phải
Admin.
| RECIPE_CONCURRENCY_CONFLICT | 422 | RowVersion không | Recipe |
| ---------------------------- | ---- | ----------------- | ------- |
khớp — resource đã
được cập nhật bởi
request khác. Client
cần reload.
| CATEGORY_NOT_FOUND | 404 | Category không tồn | Category |
| ------------------- | ---- | ------------------- | --------- |
tại.
| CATEGORY_NAME_EXISTS | 409 | Tên category đã tồn | Category |
| --------------------- | ---- | -------------------- | --------- |
tại.
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 68 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
| Error Code | HTTP | Mô tả | Module |
| ----------- | ----- | ------ | ------- |
Status
| CATEGORY_DELETE_HAS_RECIPES | 409 | Không thể xóa | Category |
| ---------------------------- | ---- | -------------- | --------- |
category đang có
recipes thuộc về.
| FILE_SIZE_EXCEEDED | 400 | File upload vượt quá | File |
| ------------------- | ---- | --------------------- | ----- |
giới hạn 5MB.
| FILE_MIME_INVALID | 400 | Loại file không được | File |
| ------------------ | ---- | --------------------- | ----- |
phép. Chỉ chấp nhận
JPEG, PNG, WebP,
AVIF.
| VALIDATION_ERROR | 400 | Một hoặc nhiều field | Common |
| ----------------- | ---- | --------------------- | ------- |
không hợp lệ. Xem
"errors" object.
| RATE_LIMIT_EXCEEDED | 429 | Quá giới hạn request. | Common |
| -------------------- | ---- | ---------------------- | ------- |
Xem Retry-After
header.

Phụ lục C – Từ điển Thuật ngữ

| Thuật ngữ Viết Định nghĩa |     |     |     |
| ------------------------- | --- | --- | --- |

tắt
| Access Token AT JSON Web Token (JWT) dùng để xác thực API | | | |
| ------------------------------------------------------------ | --- | --- | --- |
request. TTL = 15 phút. Ký bằng HS256.
Application Error Code AEC Mã lỗi tùy chỉnh dạng SCREAMING_SNAKE_CASE
trong trường "type" của RFC 7807 Problem Details.
Archive — Trạng thái Recipe khi bị ẩn khỏi public listing nhưng
không bị xóa. RecipeStatus.Archived.
| Author — Role người dùng mặc định sau khi đăng ký. Có thể | | | |
| ------------------------------------------------------------ | --- | --- | --- |
tạo/quản lý recipe của mình.
Background Job — Tác vụ xử lý bất đồng bộ chạy ngoài HTTP request
cycle, quản lý bởi Hangfire.
Clean Architecture CA Kiến trúc phần mềm của Robert C. Martin tách biệt
concerns theo layers (Domain, Application,
Infrastructure, Presentation). Dependency chỉ đi vào
trong (hướng Domain).
Command Query CQRS Pattern tách biệt write model (Commands) và read
| Responsibility model (Queries) để tối ưu từng luồng riêng. | | | |
| ------------------------------------------------------------ | --- | --- | --- |
Segregation
Content Delivery CDN Mạng phân phối nội dung tĩnh (ảnh, JS, CSS) từ
| Network server gần người dùng nhất. | | | |
| ------------------------------------- | --- | --- | --- |
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 69 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
Thuật ngữ Viết Định nghĩa
tắt
Core Web Vitals CWV Chỉ số đo lường UX của Google: LCP (tải trang), CLS
(ổn định layout), INP (phản hồi tương tác).
CQRS - Xem Command Query Responsibility Segregation
Docker Compose — Công cụ định nghĩa và chạy multi-container Docker
application qua file YAML.
Draft — Trạng thái mặc định của Recipe khi mới tạo. Chỉ
Author/Admin thấy.
Full-Text Search FTS Tìm kiếm ngôn ngữ tự nhiên trong PostgreSQL qua
tsvector/tsquery + unaccent extension.
Hangfire — Thư viện .NET xử lý background jobs: fire-and-forget,
delayed, recurring.
HTTP Status Code — Mã phản hồi HTTP chuẩn (RFC 7231) cho biết kết
quả xử lý request (2xx: thành công, 4xx: client error,
5xx: server error).
Incremental Static ISR Tính năng Next.js tái sinh (regenerate) trang tĩnh theo
Regeneration chu kỳ (revalidate interval) thay vì build lại toàn bộ.
JSON Web Token JWT Chuẩn mở (RFC 7519) định nghĩa cách truyền thông
tin an toàn giữa các bên dưới dạng JSON object
được ký.
MediatR — Thư viện .NET triển khai Mediator pattern. Dispatch
Commands/Queries qua Handler có pipeline
behaviors.
MinIO — Object storage server mã nguồn mở tương thích
Amazon S3 API. Dùng để lưu trữ ảnh.
Non-Functional NFR Yêu cầu chất lượng hệ thống: hiệu năng, bảo mật, độ
Requirement tin cậy, khả năng bảo trì...
Nginx — Web server hiệu năng cao, dùng làm reverse proxy,
load balancer và SSL termination.
OpenTelemetry OTEL Framework quan sát hệ thống phân tán: distributed
tracing, metrics, logs.
Optimistic Concurrency — Kỹ thuật xử lý concurrent writes bằng RowVersion —
không lock DB, phát hiện conflict khi save.
Published — Trạng thái Recipe khi được công bố công khai.
RecipeStatus.Published.
Rate Limiting — Giới hạn số lượng request từ một IP trong khoảng
thời gian nhất định để ngăn brute force/DDoS.
Refresh Token RT Token dài hạn (7 ngày) dùng để lấy Access Token
mới mà không cần đăng nhập lại.
Refresh Token — Mỗi lần dùng Refresh Token để refresh → token cũ bị
Rotation revoke, cấp token mới (bảo mật cao hơn).
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 70 / 71

Culinary Blog – Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) v1.0.0
Thuật ngữ Viết Định nghĩa
tắt
Reuse Detection — Cơ chế phát hiện khi Refresh Token đã bị revoke
được dùng lại → revoke toàn bộ token family của
user.
Slug — Chuỗi URL-friendly, dạng chữ-thường-gạch-nối, duy
nhất, dùng để định danh Recipe/Category trên URL.
Soft Delete — Đánh dấu IsDeleted=true thay vì xóa vật lý khỏi
database. Dữ liệu có thể khôi phục.
Software SRS Tài liệu đặc tả yêu cầu phần mềm theo IEEE 830 /
Requirements ISO/IEC/IEEE 29148.
Specification
TanStack Query — Thư viện React quản lý server state: caching,
background refetch, optimistic updates.
tsvector / tsquery — Kiểu dữ liệu PostgreSQL cho full-text search. tsvector
là chỉ mục đã xử lý, tsquery là biểu thức tìm kiếm.
Unit of Work UoW Pattern đảm bảo nhiều operations được thực hiện
trong một transaction duy nhất.
CONFIDENTIAL • Phát triển Ứng dụng Web Nâng cao V4 • Trang 71 / 71
