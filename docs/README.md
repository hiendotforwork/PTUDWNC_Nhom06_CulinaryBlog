# 📚 Culinary Blog — Documentation Index & Reading Guide

Chào mừng bạn đến với hệ thống tài liệu kỹ thuật của dự án **Culinary Blog**. 

Tài liệu được phân loại và sắp xếp theo **chu trình phát triển phần mềm (SDLC)** từ mức độ vĩ mô (Yêu cầu tổng thể hệ thống) đến chi tiết kỹ thuật (Kiến trúc module, Hợp đồng API, và Kế hoạch thực thi).

---

## 🗺️ Sơ đồ Phân cấp Tài liệu (Documentation Hierarchy)

```
docs/
├── specs/
│   ├── SRS_Culinary_Blog_v1.0.0.md           # [TẦNG 1] Đặc tả yêu cầu phần mềm tổng thể
│   ├── UI_DESIGN_SPECIFICATION.md             # [TẦNG 1] Thiết kế hệ thống giao diện (UI/UX)
│   └── fr-auth/                               # [TẦNG 2 & 3] Module Xác thực (FR-AUTH)
│       ├── FR-AUTH_BaoCao.md                  # 1. Báo cáo phân tích kiến trúc tổng thể
│       ├── FR-AUTH_Database_Design.md         # 2. Thiết kế Cơ sở dữ liệu
│       ├── FR-AUTH_SecuritySpec.md            # 3. Đặc tả Bảo mật & Quản lý Token
│       ├── FR-AUTH_LoggingSpec.md             # 4. Đặc tả Ghi log & Giám sát (Audit)
│       ├── FR-AUTH_APIContract.md             # 5. Hợp đồng giao diện REST API
│       ├── FR-AUTH_ErrorCodes.md              # 6. Danh mục Mã lỗi & Chuẩn phản hồi
│       ├── FR-AUTH_TestPlan.md                # 7. Kế hoạch & Ma trận kiểm thử
│       ├── FR-AUTH-001_Design.md              # 8. Thiết kế chi tiết tính năng: Đăng ký
│       ├── FR-AUTH-001_Plan.md                # 9. Kế hoạch kiến trúc triển khai FR-AUTH-001
│       └── FR-AUTH-001_EndUser_Test_Scenarios.md # 10. Kịch bản kiểm thử người dùng cuối (UAT)
└── superpowers/
    └── plans/
        └── 2026-09-23-fr-auth-001-implementation.md # [TẦNG 4] Kế hoạch thực thi TDD chi tiết
```

---

## 📖 Thứ tự Đọc Chuẩn theo Quy trình Kỹ thuật (Standard Reading Order)

### 📌 TẦNG 1: Nền tảng & Yêu cầu Hệ thống (System-Level)
*Dành cho toàn bộ team để hiểu bài toán kinh doanh, phạm vi dự án và ngôn ngữ thiết kế giao diện.*

1. **[SRS Specification v1.0.0](specs/SRS_Culinary_Blog_v1.0.0.md)**  
   *Mục đích:* Tài liệu đặc tả yêu cầu toàn diện (Software Requirements Specification) của hệ thống: Danh sách Actor, các phân hệ chức năng (FR-AUTH, FR-RECIPE, FR-INTERACT, FR-ADMIN) và yêu cầu phi chức năng (NFR).
2. **[UI/UX Design Specification](specs/UI_DESIGN_SPECIFICATION.md)**  
   *Mục đích:* Thiết kế nhận diện hình ảnh, bảng màu, typography, responsive wireframes, design tokens và trạng thái tương tác component cho Next.js Frontend.

---

### 📌 TẦNG 2: Thiết kế Module Xác thực (FR-AUTH Module-Level)
*Dành cho Backend & Frontend Developers, Tech Lead, DevOps và Security Engineers.*

Thứ tự đọc logic khuyến nghị cho module Xác thực:

1. **[FR-AUTH_BaoCao.md](specs/fr-auth/FR-AUTH_BaoCao.md) — Tổng quan & Phân tích Nghiệp vụ**  
   *Nội dung:* Báo cáo phân tích chuyên sâu toàn bộ module FR-AUTH (từ FR-AUTH-001 đến FR-AUTH-008), các quyết định kiến trúc cốt lõi (ADR) và lựa chọn công nghệ.
2. **[FR-AUTH_Database_Design.md](specs/fr-auth/FR-AUTH_Database_Design.md) — Thiết kế Cơ sở dữ liệu**  
   *Nội dung:* Lược đồ PostgreSQL, bảng ASP.NET Core Identity (`AspNetUsers`, `AspNetRoles`), bảng `RefreshTokens`, khóa ngoại, chỉ mục (Index) tối ưu hóa tìm kiếm và các ràng buộc dữ liệu.
3. **[FR-AUTH_SecuritySpec.md](specs/fr-auth/FR-AUTH_SecuritySpec.md) — Tiêu chuẩn Bảo mật**  
   *Nội dung:* Thuật toán băm mật khẩu (PBKDF2), cấu trúc JWT (HS256/RS256), cơ chế bảo vệ Refresh Token (SHA-256 hash), kiểm soát phiên và phòng chống brute-force/DoS.
4. **[FR-AUTH_LoggingSpec.md](specs/fr-auth/FR-AUTH_LoggingSpec.md) — Chuẩn Logging & Giám sát**  
   *Nội dung:* Cấu trúc Structured Log bằng Serilog, các mốc sự kiện bảo mật quan trọng (Audit trail) và quy định che giấu thông tin nhạy cảm (PII/Masking).
5. **[FR-AUTH_APIContract.md](specs/fr-auth/FR-AUTH_APIContract.md) — Hợp đồng REST API**  
   *Nội dung:* Định nghĩa chi tiết các endpoints (`/api/v1/auth/*`), HTTP Methods, Headers, Request Body và Response Schema để liên kết Backend với Frontend.
6. **[FR-AUTH_ErrorCodes.md](specs/fr-auth/FR-AUTH_ErrorCodes.md) — Danh mục Mã lỗi**  
   *Nội dung:* Chuẩn hóa phản hồi lỗi theo RFC 7807 Problem Details, bảng tra cứu mã lỗi (`AUTH_EMAIL_EXISTS`, `VALIDATION_ERROR`,...) và HTTP Status tương ứng.
7. **[FR-AUTH_TestPlan.md](specs/fr-auth/FR-AUTH_TestPlan.md) — Kế hoạch Kiểm thử**  
   *Nội dung:* Ma trận test cases, tiêu chí nghiệm thu (Acceptance Criteria), kịch bản kiểm thử bảo mật và quy trình kiểm thử tự động.

---

### 📌 TẦNG 3: Thiết kế Chi tiết Tính năng (Feature-Level Specs)
*Dành cho lập trình viên trực tiếp phát triển tính năng cụ thể.*

1. **[FR-AUTH-001_Design.md](specs/fr-auth/FR-AUTH-001_Design.md) — Thiết kế Chức năng Đăng ký**  
   *Nội dung:* Phân tích chuyên biệt cho tính năng Đăng ký tài khoản (FR-AUTH-001): Luồng hoạt động (Activity Diagram), quy tắc kiểm tra mật khẩu (Password Policy) và chuyển đổi trạng thái (State Machine).
2. **[FR-AUTH-001_Plan.md](specs/fr-auth/FR-AUTH-001_Plan.md) — Kế hoạch Kiến trúc Triển khai**  
   *Nội dung:* Phân bổ công việc vào mô hình 4 tầng Clean Architecture (Domain, Application, Infrastructure, API Layer), danh sách 27 file cần tạo và các gói NuGet cần thiết.
3. **[FR-AUTH-001_EndUser_Test_Scenarios.md](specs/fr-auth/FR-AUTH-001_EndUser_Test_Scenarios.md) — Kịch bản Kiểm thử Người dùng Cuối (UAT/Manual Test)**  
   *Nội dung:* Bộ 21 kịch bản kiểm thử chi tiết từng bước cho Tester/Người dùng cuối: Happy path, client validation (BR-AUTH-002), 409 conflict, edge cases và biên bản nghiệm thu.

---

### 📌 TẦNG 4: Kế hoạch Thực thi Kỹ thuật & TDD (Execution-Level)
*Dành cho kỹ sư lập trình và AI Agent thực hiện code theo TDD.*

1. **[2026-09-23-fr-auth-001-implementation.md](superpowers/plans/2026-09-23-fr-auth-001-implementation.md) — Backend Implementation Plan**  
   *Nội dung:* Kế hoạch thực thi 6 task chi tiết từng bước Red-Green-Refactor, quản lý giao dịch nguyên tử (Atomic Transaction), xử lý xung đột đồng thời (Race Condition) và trọn bộ 46 Unit/Integration tests.
2. **[2026-09-26-fr-auth-frontend-integration.md](superpowers/plans/2026-09-26-fr-auth-frontend-integration.md) — Frontend Integration Plan**  
   *Nội dung:* Kế hoạch tích hợp giao diện Next.js thay thế mock data bằng gọi API backend `POST /api/v1/auth/register`, lưu trữ token và bắt mã lỗi.

---

## 🎯 Bảng Tra Cứu Nhanh theo Vai Trò (Quick Reference by Role)

| Vai trò | Các tài liệu cần đọc ưu tiên |
|---------|------------------------------|
| **Tech Lead / Solution Architect** | `SRS_Culinary_Blog_v1.0.0.md`, `FR-AUTH_BaoCao.md`, `FR-AUTH_Database_Design.md`, `FR-AUTH_SecuritySpec.md` |
| **Backend Developer (.NET)** | `FR-AUTH_Database_Design.md`, `FR-AUTH_APIContract.md`, `FR-AUTH-001_Plan.md`, `2026-09-23-fr-auth-001-implementation.md` |
| **Frontend Developer (Next.js)** | `UI_DESIGN_SPECIFICATION.md`, `FR-AUTH_APIContract.md`, `FR-AUTH_ErrorCodes.md` |
| **QA / Test Engineer** | `FR-AUTH-001_EndUser_Test_Scenarios.md`, `FR-AUTH_TestPlan.md`, `FR-AUTH_APIContract.md`, `FR-AUTH_ErrorCodes.md` |
| **DevOps / Security Specialist** | `FR-AUTH_SecuritySpec.md`, `FR-AUTH_LoggingSpec.md`, `FR-AUTH_Database_Design.md` |

---

## 📝 Quy ước Đóng góp & Bổ sung Tài liệu (Documentation Guidelines)

1. **Định dạng:** Sử dụng Markdown tiêu chuẩn (GitHub Flavored Markdown), hỗ trợ Mermaid diagrams cho sơ đồ.
2. **Vị trí lưu trữ:**
   - Các đặc tả yêu cầu toàn hệ thống: lưu tại `docs/specs/`.
   - Các tài liệu theo từng phân hệ chức năng: tạo thư mục con `docs/specs/fr-<module-name>/`.
   - Kế hoạch thực thi mã nguồn / TDD plans: lưu tại `docs/superpowers/plans/`.
3. **Cập nhật mục lục:** Khi thêm tài liệu mới, vui lòng cập nhật liên kết và tóm tắt vào file này (`docs/README.md`).
