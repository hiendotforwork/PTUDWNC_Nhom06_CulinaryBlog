# 🔐 Module FR-AUTH: Authentication & User Management

Thư mục này chứa toàn bộ các đặc tả kỹ thuật và tài liệu thiết kế dành riêng cho phân hệ **Xác thực và Quản lý Người dùng (FR-AUTH)** của Culinary Blog.

---

## 📑 Thứ tự Đọc Chuẩn (Reading Workflow)

Vui lòng tiếp cận tài liệu theo thứ tự sau để nắm bắt đầy đủ từ tổng quan nghiệp vụ đến chi tiết triển khai:

| Thứ tự | Tài liệu | Mô tả | Đối tượng quan tâm chính |
|:---:|---|---|---|
| **1** | [FR-AUTH_BaoCao.md](./FR-AUTH_BaoCao.md) | Báo cáo phân tích tổng thể 8 chức năng FR-AUTH & quyết định kiến trúc | Tech Lead, Backend, Frontend |
| **2** | [FR-AUTH_Database_Design.md](./FR-AUTH_Database_Design.md) | Thiết kế bảng dữ liệu, khóa chính/ngoại, index & quan hệ EF Core | Backend, Database Admin |
| **3** | [FR-AUTH_SecuritySpec.md](./FR-AUTH_SecuritySpec.md) | Chuẩn bảo mật: Password hashing, JWT claims/key, Refresh token rotation, Brute-force | Security, Backend |
| **4** | [FR-AUTH_LoggingSpec.md](./FR-AUTH_LoggingSpec.md) | Cấu trúc Serilog, các sự kiện audit đăng ký/đăng nhập/thu hồi token | DevOps, Backend |
| **5** | [FR-AUTH_APIContract.md](./FR-AUTH_APIContract.md) | Chi tiết REST endpoints, HTTP status, request/response body | Frontend, Backend, QA |
| **6** | [FR-AUTH_ErrorCodes.md](./FR-AUTH_ErrorCodes.md) | Hệ thống mã lỗi chuẩn (`AUTH_EMAIL_EXISTS`, `VALIDATION_ERROR`,...) | Frontend, Backend, QA |
| **7** | [FR-AUTH_TestPlan.md](./FR-AUTH_TestPlan.md) | Kế hoạch & ma trận test cases kiểm thử đơn vị, tích hợp và bảo mật | QA, Backend |
| **8** | [FR-AUTH-001_Design.md](./FR-AUTH-001_Design.md) | Thiết kế chi tiết riêng cho tính năng User Registration (FR-AUTH-001) | Backend, Frontend |
| **9** | [FR-AUTH-001_Plan.md](./FR-AUTH-001_Plan.md) | Kế hoạch kiến trúc phân lớp Clean Architecture cho FR-AUTH-001 | Backend Developer |

---

## 🔗 Liên kết Liên quan
- [Documentation Index tổng thể](../../README.md)
- [Đặc tả yêu cầu phần mềm hệ thống (SRS)](../SRS_Culinary_Blog_v1.0.0.md)
- [Đặc tả thiết kế giao diện người dùng (UI/UX)](../UI_DESIGN_SPECIFICATION.md)
- [Kế hoạch triển khai TDD chi tiết](../../superpowers/plans/2026-09-23-fr-auth-001-implementation.md)
