# Quyết định đã thống nhất — feature/chuong

Trạng thái: người phụ trách đã đồng ý trong cuộc trao đổi với trợ lý. Đây là tùy biến phục vụ phần FR-RCP của Chương, không phải tuyên bố giảng viên hoặc nhóm trưởng đã duyệt PR.

## Tài liệu sử dụng

- Thiết kế: `docs/database/DATABASE-BRANCH-CHUONG.md`.
- SRS làm việc: `D:\Tập file học tập\PDF\Phát triển ứng dụng web nâng cao\SRS_Culinary_Blog_v1.0.0.md`, đã sửa trực tiếp các mục liên quan theo bảng dưới.
- SRS PDF gốc giữ nguyên. Các số trang trong bản Markdown là tham chiếu tới PDF gốc, không phải phân trang của bản đã chỉnh.
- Khi triển khai FR-RCP, dùng quy tắc hiện hành dưới đây. Các khác biệt của Auth/Category ngoài phạm vi này chưa được xem là đã giải quyết.

## 12 mâu thuẫn và lựa chọn

| # | Nguồn khác nhau | Quyết định | Vì sao |
|---|---|---|---|
| 1 | RCP-007 hard delete; NFR-REL-003, chương 7, 8.3 soft delete | Xóa mềm recipe và con; giữ ảnh khi xóa mềm recipe | Có khả năng khôi phục đầy đủ; chưa bổ sung UI/API khôi phục hoặc chính sách purge |
| 2 | RCP-005 chỉ yêu cầu bước; phụ lục B yêu cầu cả nguyên liệu | Publish cần ít nhất 1 nguyên liệu và 1 bước còn hiệu lực; Draft được thiếu | Nội dung công khai đủ để làm theo |
| 3 | RCP-003 trùng slug trả 409; phụ lục B tự thêm hậu tố | Tự thêm -2, -3…; tạo thành công khi có slug duy nhất | Cho phép nhiều tác giả đặt cùng tên món |
| 4 | RCP-003 CookTime > 0; 7.2 >= 0 | CookTime >= 0; PrepTime và Servings > 0 | Hỗ trợ món không cần nấu |
| 5 | RCP-009 Quantity/Unit bắt buộc; 7.4 và 8.6 cho NULL | Cả hai NULL hoặc cả hai có giá trị; Quantity > 0, Unit không trắng | Hỗ trợ “vừa đủ” mà không để số lượng thiếu đơn vị |
| 6 | Mô tả RCP-001 có Archived; luồng chỉ có Draft | Author thấy Published và mọi trạng thái của chính mình chưa xóa | Quản lý được nội dung lưu trữ; tìm kiếm công khai vẫn chỉ Published |
| 7 | RCP-010 server đánh số; 8.5 client gửi StepNumber | Server cấp số khi thêm và đánh lại số khi xóa | Tránh trùng số/khoảng trống |
| 8 | RCP-010 không có Title; 7.3/8.5 bắt buộc | Title không bắt buộc trong form/request tạo; server mặc định “Bước N”; DB luôn có Title | Dữ liệu đầy đủ, giảm thao tác nhập |
| 9 | RCP-008 endpoint primary riêng; 8.4 dùng metadata | PATCH /recipes/{id}/images/{imageId}/primary để chọn primary; metadata chỉ altText/orderIndex | Tách hành vi chọn ảnh chính, tránh bỏ hết primary |
| 10 | RCP/SRCH dùng sort và items; chương 8 dùng sortBy/sortOrder và data/meta | sortBy + sortOrder; items/totalCount/page/pageSize/totalPages/hasNextPage/hasPreviousPage; mặc định 12, tối đa 50 | API rõ ràng, UI có đủ thông tin |
| 11 | RCP dùng 409 concurrency/422 validation; phụ lục đảo khác | 400 sai định dạng request; 422 dữ liệu/nghiệp vụ không hợp lệ; 409 xung đột phiên bản/trùng dữ liệu | UI biết khi nào sửa form, khi nào tải dữ liệu mới |
| 12 | Chi tiết 60 hoặc 5 phút; tìm kiếm không cache/5/1 phút | Chi tiết công khai 5 phút, tìm kiếm công khai 1 phút; invalidate sau thay đổi | Hạn chế dữ liệu cũ và giảm tải |

## Các điểm được làm rõ, không phải tất cả đều là mâu thuẫn

- Tên thống nhất: OrderIndex, TimerMinutes, PrepTime, CookTime; JSON dùng camelCase; thời gian tính bằng phút.
- Name nguyên liệu: 1–200 ký tự sau trim ở UI/API/DB. DB 200 trong khi validator 100 không tự nó là lỗi, nhưng nay thống nhất một giới hạn.
- Unarchive đưa Archived về Draft, không tự công khai. Bổ sung PATCH /recipes/{id}/unarchive.
- Chỉ cache dùng chung nội dung Published; không cache chung phản hồi có Draft/Archived hoặc thông tin quản trị theo người dùng.
- RowVersion: bytea 16 byte, ứng dụng đổi token khi sửa cả cha lẫn con; client gửi Base64 trong body (upload dùng trường multipart rowVersion), không dùng If-Match trong hợp đồng FR-RCP này. DELETE cũng nhận body chứa rowVersion; cần thể hiện rõ trong OpenAPI và client.
- Instructions được bỏ trống ở request, lưu chuỗi rỗng thay NULL.
- Xóa riêng ảnh theo RCP-008 vẫn được dọn file qua Hangfire; khác với xóa mềm cả recipe. Job dọn ảnh chỉ nhận yêu cầu xóa ảnh riêng, không quét mọi IsDeleted để xóa file.
- Món đang Published không được xóa nguyên liệu cuối hoặc bước cuối; unpublish trước. Đây là hệ quả giữ điều kiện xuất bản sau chỉnh sửa.
- Slug giữ nguyên khi đổi title; unique kể cả dữ liệu đã xóa. PublishedAt giữ thời điểm xuất bản đầu tiên như bản thiết kế database.

## Phối hợp và kiểm chứng

Các quy ước API dùng chung (phân trang, mã lỗi, tên trường) cần được gửi kèm PR để thành viên khác tích hợp. Không tự thay đổi nghiệp vụ Auth/Category chưa được thống nhất.

Kiểm tra khi triển khai: tự thêm slug khi trùng và khi tạo đồng thời; publish thiếu một trong hai danh sách; CookTime=0; Quantity/Unit; quyền xem Archived; đánh số bước và Title mặc định; đổi primary; lỗi 400/422/409; cache không lộ Draft; soft delete recipe giữ file; xóa ảnh riêng không ảnh hưởng ảnh khác; concurrency khi sửa con.

Chưa tạo migration hoặc triển khai API chỉ bằng việc cập nhật tài liệu này.
