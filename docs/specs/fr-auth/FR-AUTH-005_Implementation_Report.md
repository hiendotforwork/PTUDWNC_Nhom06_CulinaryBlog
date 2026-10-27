# Báo cáo thực hiện FR-AUTH-005 — Đăng xuất

## 1. Mô tả và mục đích

FR-AUTH-005 cho phép người dùng kết thúc phiên đăng nhập bằng cách thu hồi **một refresh token**. Access token JWT là stateless nên không thể bị thu hồi ngay; frontend xóa token cục bộ, còn backend đánh dấu refresh token tương ứng là revoked để không thể làm mới phiên tiếp theo.

Endpoint: `POST /api/v1/auth/logout` → luôn trả `204 No Content` cho mọi trạng thái hợp lệ của request token (không tồn tại, hết hạn, đã revoke, rỗng). Endpoint vẫn hoạt động khi access token hết hạn hoặc Authorization header không hợp lệ.

## 2. Kế hoạch thực hiện

1. Tạo command/handler để hash, tìm và revoke refresh token còn active.
2. Thêm endpoint logout idempotent, không yêu cầu Bearer JWT.
3. Kết nối frontend: gọi logout API, sau đó luôn xóa auth state cục bộ.
4. Bổ sung unit, integration và regression tests.

Chi tiết: [Kế hoạch triển khai](../../superpowers/plans/2026-10-12-fr-auth-005-logout.md).

## 3. File thay đổi

| Khu vực | File |
|---|---|
| Application | `src/CulinaryBlog.Application/Commands/Auth/Logout/LogoutCommand.cs` |
| Application | `src/CulinaryBlog.Application/Commands/Auth/Logout/LogoutCommandHandler.cs` |
| Application | `src/CulinaryBlog.Application/DTOs/Auth/LogoutRequest.cs` |
| API | `src/CulinaryBlog.API/Controllers/AuthController.cs` |
| Frontend | `frontend/app/lib/api.ts` |
| Frontend | `frontend/app/context/AppContext.tsx` |
| Frontend | `frontend/app/components/layout/Header.tsx` |
| Tests | `tests/CulinaryBlog.Application.Tests/Commands/LogoutCommandHandlerTests.cs` |
| Tests | `tests/CulinaryBlog.Integration.Tests/AuthControllerTests.cs` |

## 4. Kết quả so với kế hoạch

| Hạng mục | Kết quả |
|---|---|
| Revoke một token active | Hoàn thành — hash SHA-256, chỉ revoke token tìm thấy và còn active. |
| Idempotent / không lộ trạng thái token | Hoàn thành — các trường hợp token sai, rỗng, hết hạn, đã revoke hoặc không tồn tại đều trả `204`. |
| Logout khi access token hết hạn | Hoàn thành — endpoint `[AllowAnonymous]`; refresh token là bằng chứng sở hữu phiên. |
| Xóa trạng thái frontend | Hoàn thành — luôn xóa access token, refresh token, user và `currentUser`, kể cả khi request mạng thất bại. |
| Không thêm migration/dependency | Hoàn thành — dùng entity, repository và schema `RefreshTokens` có sẵn. |

## 5. Kết quả test

- `dotnet test CulinaryBlog.slnx`: **105 unit tests pass**, **45 integration tests pass**; **17 live-infrastructure tests skipped** vì Docker stack riêng chưa được bật.
- Logout integration tests: **23/23 pass**, gồm token active, unknown, expired, revoked, body rỗng/null/blank và Bearer expired/malformed.
- Frontend lint: **0 errors**; còn 3 warnings có sẵn, không liên quan logout.
- `next build --webpack`: pass. Turbopack build bị môi trường chặn bind port.

## 6. Kịch bản test theo góc nhìn người dùng

| Kịch bản | Kết quả mong đợi |
|---|---|
| Người dùng nhấn “Đăng xuất” khi đang đăng nhập | Quay về trạng thái khách; token cục bộ bị xóa; refresh token bị thu hồi. |
| Người dùng nhấn đăng xuất hai lần | Cả hai lần đều thành công, không báo lỗi và không lộ token có tồn tại hay không. |
| Access token đã hết hạn rồi người dùng đăng xuất | Vẫn đăng xuất thành công nếu refresh token còn hợp lệ. |
| Browser gửi Authorization header bị lỗi | Vẫn đăng xuất thành công và thu hồi refresh token. |
| Mạng/API lỗi khi người dùng đăng xuất | Browser vẫn xóa thông tin đăng nhập để người dùng không còn phiên cục bộ. |
| Refresh token hết hạn/đã bị thu hồi/không tồn tại | Hiển thị trạng thái đã đăng xuất; API trả `204`, không tiết lộ chi tiết token. |

## 7. Commit liên quan

- `7a1003e` — Application logout command
- `91e6526` — Logout API endpoint
- `dffd135` — Frontend logout integration
- `b8d40f1` — Edge-case regression tests
