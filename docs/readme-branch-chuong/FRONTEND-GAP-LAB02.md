# Đối chiếu frontend của nhóm với FR-RCP

Kiểm tra mã nguồn ngày 21/09/2026 trên feature/chuong, đã merge commit frontend 149a1f0. Đây là rà soát code, chưa phải kiểm thử giao diện bằng trình duyệt.

## Kết luận

Có thể tái sử dụng giao diện của nhóm. Hiện đây là demo dùng React state/localStorage và mockData, chưa phải chức năng full-stack. Không cần làm lại giao diện trước khi hoàn thành Lab 2.

| FR | Đã có trong code | Cần bổ sung khi tích hợp |
|---|---|---|
| 001 Danh sách | Trang chủ/tìm kiếm, lọc category/difficulty/time, sort | API phân trang và totalCount, quy tắc quyền xem, sort cookTime; chưa có route /recipes riêng |
| 002 Chi tiết | /recipes/[slug], RecipeDetailView | Gọi API, quyền Draft/Archived, dữ liệu đầy đủ và trạng thái lỗi |
| 003 Tạo | /recipes/create và RecipeForm | Tạo Draft qua API; bỏ bắt buộc ảnh; Draft được thiếu ingredients/steps; slug do server sinh |
| 004 Sửa | /recipes/[slug]/edit | Owner/admin ở backend, RowVersion và thông báo 409 |
| 005 Xuất bản | Mock publishRecipe và nút publish | Kiểm tra nguyên liệu/bước; unpublish; chuyển trạng thái qua API |
| 006 Lưu trữ | Mock archiveRecipe; lọc trạng thái ở profile | Unarchive về Draft và quyền truy cập backend |
| 007 Xóa | Xóa phần tử khỏi mảng localStorage | Soft delete DB, giữ ảnh khi xóa recipe; xác nhận và token |
| 008 Ảnh | FileReader tạo data URL, chọn primary/xóa ảnh | Upload MinIO qua API, metadata/thumbnail, xử lý lỗi và đồng thời |
| 009 Nguyên liệu | Thêm/sửa/xóa hàng form | amount → quantity numeric nullable, notes/orderIndex; “vừa đủ”; CRUD backend |
| 010 Bước | Thêm/xóa và đánh số phía client | title/timerMinutes/imageUrl trong form; server cấp số; CRUD backend |

## Khác biệt dữ liệu cần xử lý

- `frontend/app/lib/types.ts`: role User/Admin thay vì Author/Admin; thiếu Expert; ingredient amount là string; step thiếu Title/TimerMinutes; image url/caption thay OriginalUrl/AltText; nutrition carbs thay Carbohydrates, thiếu Fiber/Sodium; recipe thiếu RowVersion.
- `frontend/app/context/AppContext.tsx`: mặc định đăng nhập mock, login không xác thực mật khẩu, dữ liệu lưu localStorage. Không coi đây là phân quyền/backend thực.
- `frontend/app/components/recipe/RecipeForm.tsx`: bắt có ảnh ngay cả lưu Draft; không cho xóa hàng nguyên liệu/bước cuối ngay cả Draft; giá trị cookTime=0 lúc sửa bị fallback thành 30 do dùng `||`.
- `ImageUploadZone.tsx`: chỉ kiểm MIME/size phía client và tạo data URL, chưa tải lên kho file.
- Favorites/likes có trong demo nhưng bookmark/rating ngoài phạm vi SRS v1.0. Không đưa chúng thành tiêu chí hoàn thành FR-RCP.
- Routes khác wireframe: /login, /register, /recipes/create, /recipes/[slug]/edit, /profile thay /auth/* và /dashboard/recipes/*. Nhóm có thể giữ route demo rồi thống nhất routing; khác tên route không tự chứng minh thiếu tính năng.

Thứ tự sau Lab 2: thống nhất DTO với nhóm → API đọc danh sách/chi tiết → thay mock ở hai trang đó → tạo/sửa Draft → ingredients/steps/images → publish/archive/delete. Giữ giao diện nhóm làm nền, sửa từng phần có kiểm chứng.
