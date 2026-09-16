# Thiết kế database Culinary Blog — feature/chuong v1.1

Ngày: 2026-09-16. Cơ sở: SRS v1.0.0, chương 7 (trang 54–60), FR-RCP-001 đến FR-RCP-010 (trang 27–36). Các quyết định dưới đây thay thế những mô tả mâu thuẫn tương ứng trong SRS theo quyền tùy biến của nhóm. Đây là thiết kế để triển khai EF Core Code First, chưa phải migration đã chạy.

## 1. Phạm vi và quy ước

- PostgreSQL 16; EF Core Code First là nguồn quản lý schema. Không duy trì song song một bộ CREATE TABLE thủ công và migrations.
- Giữ tên bảng/cột PascalCase của SRS. Khi viết SQL trực tiếp phải dùng dấu nháy kép.
- Khóa nghiệp vụ là UUID v4. Riêng người dùng và vai trò giữ khóa string của ASP.NET Core Identity; AuthorId/UserId phải cùng kiểu với AspNetUsers.Id.
- Thời gian dùng timestamptz, trao đổi bằng UTC. Giá trị số lượng và dinh dưỡng dùng numeric, không dùng float.
- Ảnh lưu ở MinIO; database lưu URL ổn định, không lưu URL ký có thời hạn hoặc dữ liệu ảnh nhị phân.
- Redis là cache, không phải nguồn dữ liệu chính. Các bảng Identity và Hangfire do thư viện tương ứng quản lý; không tự thiết kế lại.

## 2. Quyết định đã chốt

| Vấn đề | Quyết định | Lý do và ảnh hưởng |
|---|---|---|
| Hard delete hay soft delete Recipe | Xóa mềm bằng IsDeleted; không có thao tác khôi phục trong phạm vi hiện tại | Theo mô hình chung chương 7; tránh mất dữ liệu do thao tác nhầm. Archive vẫn là trạng thái nghiệp vụ, không phải xóa. |
| Xóa các bản ghi con | Ingredients, Steps, Images cũng xóa mềm | Các unique index chỉ xét bản ghi còn hiệu lực. Xóa công thức đánh dấu toàn bộ con trong cùng transaction. |
| Xóa file khi xóa công thức/ảnh | Xóa mềm recipe giữ toàn bộ file ảnh; xóa riêng ảnh mới lên lịch dọn các biến thể qua Hangfire | Bảo toàn khả năng khôi phục recipe. Chưa có chính sách purge; không tự xóa file chỉ vì recipe/ảnh có IsDeleted=true. |
| Quantity và Unit | Cùng NULL hoặc cùng có giá trị; nếu có, Quantity > 0 và Unit không được trắng | Hỗ trợ “muối vừa đủ” mà vẫn chặn trường hợp có số lượng nhưng thiếu đơn vị. UI hiển thị “vừa đủ” khi cả hai NULL. |
| Độ dài tên nguyên liệu | 1–200 ký tự sau trim | Theo chương 7, áp dụng đồng nhất UI/API/DB. |
| Tên thời lượng bước | TimerMinutes ở cả entity/API/DB | Tránh hai tên cho cùng dữ liệu. NULL hoặc >= 0. |
| Tiêu đề bước | Title bắt buộc, 1–200 ký tự; server mặc định “Bước 1”, “Bước 2” khi tạo nếu request không có tiêu đề | Giữ mô hình chương 7; người dùng có thể sửa tên. Renumber không ghi đè tên do người dùng nhập. |
| CookTime | >= 0; PrepTime > 0; Servings > 0 | Hỗ trợ món không cần nấu. |
| Instructions | Giữ text NOT NULL, mặc định chuỗi rỗng, nội dung tổng quan tùy chọn | Chi tiết thực hiện nằm trong RecipeSteps; không duy trì hai bản sao của danh sách bước. |
| Slug | Unique toàn bảng, kể cả bản ghi đã xóa; tự sinh khi tạo, không đổi khi sửa title | Không tái sử dụng URL cũ. Trùng slug tự thêm -2, -3…; bảo vệ bằng unique index và retry khi tạo đồng thời. Rút ngắn phần gốc để cả hậu tố nằm trong 220 ký tự. |
| PublishedAt | Thời điểm xuất bản đầu tiên; không xóa khi unpublish/archive và không đổi khi publish lại | Có lịch sử đã xuất bản, ý nghĩa ổn định. Không dùng để xác định công thức hiện công khai. |
| Archive/Unarchive | Draft hoặc Published → Archived; Unarchive → Draft | Khôi phục lưu trữ không tự công khai lại. Thêm hành vi/API unarchive khi triển khai. |
| Quyền xem | Guest: Published; Author: Published hoặc công thức của mình ở mọi trạng thái; Admin: tất cả bản ghi chưa xóa | Áp dụng cho danh sách, chi tiết và endpoint con; tìm kiếm công khai chỉ trả Published. |
| RowVersion | bytea 16 byte, ứng dụng sinh token ngẫu nhiên mới khi ghi; cấu hình concurrency token | Không phụ thuộc giả định SQL Server timestamp. Client gửi rowVersion dạng Base64 trong body; upload dùng trường multipart. Không dùng If-Match trong hợp đồng FR-RCP. |
| Category bị xóa | Chỉ xóa mềm khi không còn công thức chưa xóa thuộc danh mục, kể cả Archived | Phù hợp nghiệp vụ; giữ FK tới dữ liệu lịch sử. |

Các quyết định này là lựa chọn của thiết kế dự án, không phải khẳng định rằng SRS ban đầu đã thống nhất như vậy.

## 3. Quan hệ

- AspNetUsers 1 → 0..N Recipes: mỗi công thức có đúng một tác giả.
- Categories 1 → 0..N Recipes: mỗi công thức có đúng một danh mục.
- Recipes 1 → 0..N RecipeIngredients / RecipeSteps / RecipeImages.
- AspNetUsers 1 → 0..N RefreshTokens.
- RecipeNutrition là owned value object với các cột Nutrition_* nằm trong Recipes, không có bảng riêng.

Không thêm bảng Ingredient dùng chung, bảng nối RecipeCategory, bookmark, bình luận hoặc đánh giá vì ngoài phạm vi SRS hiện tại.

## 4. Từ điển dữ liệu

### Cột chung của Recipes, Categories và ba bảng con

| Cột | Kiểu PostgreSQL | Quy tắc |
|---|---|---|
| Id | uuid | PK, UUID v4 sinh tại ứng dụng |
| CreatedAt | timestamptz | NOT NULL, thời điểm tạo UTC |
| UpdatedAt | timestamptz | NULL lúc mới tạo, cập nhật khi thay đổi |
| IsDeleted | boolean | NOT NULL DEFAULT false |
| RowVersion | bytea | NOT NULL, 16 byte, sinh khi tạo và đổi mỗi lần ghi |

Không áp dụng máy móc BaseEntity cho Identity hoặc RefreshTokens: chúng có mô hình riêng dưới đây.

### Recipes

| Cột | Kiểu | Quy tắc |
|---|---|---|
| Title | varchar(200) | NOT NULL; trim; 5–200 ký tự |
| Slug | varchar(220) | NOT NULL; unique toàn bảng; không rỗng |
| Description | text | NOT NULL; trim; 1–2000 ký tự |
| Instructions | text | NOT NULL DEFAULT ''; tổng quan tùy chọn |
| CategoryId | uuid | NOT NULL; FK Categories.Id, DELETE RESTRICT |
| AuthorId | varchar(450) | NOT NULL; FK AspNetUsers.Id, DELETE RESTRICT |
| PrepTime | integer | NOT NULL; > 0, phút |
| CookTime | integer | NOT NULL; >= 0, phút |
| Servings | integer | NOT NULL; > 0 |
| Difficulty | smallint | NOT NULL DEFAULT 1; 1 Easy, 2 Medium, 3 Hard, 4 Expert |
| Status | smallint | NOT NULL DEFAULT 0; 0 Draft, 1 Published, 2 Archived |
| PublishedAt | timestamptz | NULL trước lần xuất bản đầu; Status=1 phải có giá trị |
| SearchVector | tsvector | Trigger tính từ Title và Description |
| Nutrition_Calories | numeric(8,2) | NULL hoặc >= 0; kcal/khẩu phần |
| Nutrition_Protein | numeric(8,2) | NULL hoặc >= 0; gram/khẩu phần |
| Nutrition_Carbohydrates | numeric(8,2) | NULL hoặc >= 0; gram/khẩu phần |
| Nutrition_Fat | numeric(8,2) | NULL hoặc >= 0; gram/khẩu phần |
| Nutrition_Fiber | numeric(8,2) | NULL hoặc >= 0; gram/khẩu phần |
| Nutrition_Sodium | numeric(8,2) | NULL hoặc >= 0; mg/khẩu phần |

Draft được thiếu bước, ảnh, nguyên liệu, dinh dưỡng; thông tin cơ bản ở trên vẫn phải hợp lệ khi lưu. Xuất bản yêu cầu ít nhất một nguyên liệu và một bước còn hiệu lực; không bắt buộc ảnh hoặc dinh dưỡng.

### RecipeIngredients

| Cột | Kiểu | Quy tắc |
|---|---|---|
| RecipeId | uuid | NOT NULL; FK Recipes.Id, DELETE CASCADE khi hard purge |
| Name | varchar(200) | NOT NULL, không trắng |
| Quantity | numeric(10,3) | NULL hoặc > 0 |
| Unit | varchar(50) | NULL hoặc không trắng; cặp NULL với Quantity |
| Notes | varchar(500) | NULL |
| OrderIndex | integer | NOT NULL DEFAULT 0; >= 0 |

Không được xóa nguyên liệu cuối của công thức Published; trả 422, yêu cầu unpublish trước. Cho phép trùng Name trong một công thức vì có thể dùng nguyên liệu cho nhiều phần. Sắp xếp OrderIndex rồi Id; không bắt buộc OrderIndex unique.

### RecipeSteps

| Cột | Kiểu | Quy tắc |
|---|---|---|
| RecipeId | uuid | NOT NULL; FK Recipes.Id, DELETE CASCADE khi hard purge |
| StepNumber | integer | NOT NULL; > 0; unique theo RecipeId đối với bước chưa xóa |
| Title | varchar(200) | NOT NULL; không trắng |
| Description | text | NOT NULL; không trắng; tối đa 2000 ký tự |
| TimerMinutes | integer | NULL hoặc >= 0 |
| ImageUrl | varchar(500) | NULL; URL ảnh thuộc kho của ứng dụng |

Server tự cấp StepNumber khi thêm. Request tạo được thiếu Title, server điền “Bước N”. Sau mỗi thao tác, StepNumber của các bước còn hiệu lực phải liên tục từ 1. Không cho xóa bước cuối của công thức Published; trả 422 và yêu cầu unpublish trước.

### RecipeImages

| Cột | Kiểu | Quy tắc |
|---|---|---|
| RecipeId | uuid | NOT NULL; FK Recipes.Id, DELETE CASCADE khi hard purge |
| OriginalUrl | varchar(500) | NOT NULL; không trắng |
| MediumUrl | varchar(500) | NULL trong lúc chưa xử lý ảnh |
| ThumbnailUrl | varchar(500) | NULL trong lúc chưa xử lý ảnh |
| AltText | varchar(200) | NULL |
| IsPrimary | boolean | NOT NULL DEFAULT false |
| OrderIndex | integer | NOT NULL DEFAULT 0; >= 0 |

Một công thức không có ảnh thì không có primary. Khi có ảnh còn hiệu lực, nghiệp vụ đảm bảo đúng một primary; unique index chỉ đảm bảo tối đa một. Ảnh thay thế được chọn theo OrderIndex, CreatedAt, Id để kết quả ổn định.

### Categories

Ngoài cột chung: Name varchar(100) NOT NULL, Slug varchar(120) NOT NULL, Description text NULL, ImageUrl varchar(500) NULL, OrderIndex integer NOT NULL DEFAULT 0 CHECK >= 0. Name/Slug không trắng; Slug unique toàn bảng. Name unique không phân biệt hoa/thường theo lower(Name); trim tại ứng dụng. Không bỏ dấu khi xét unique tên. Giữ tên và slug của bản ghi đã xóa để tránh tái sử dụng nhầm.

### Identity và RefreshTokens

AspNetUsers dùng IdentityUser<string>; bổ sung DisplayName varchar(100) NOT NULL, AvatarUrl varchar(500) NULL, Bio text NULL, IsActive boolean NOT NULL DEFAULT true, CreatedAt timestamptz NOT NULL. Giữ các bảng roles, user roles, claims, logins, tokens do Identity sinh. Guest là người chưa đăng nhập, không phải bản ghi role. Seed Author và Admin.

RefreshTokens: Id uuid PK; UserId varchar(450) NOT NULL FK AspNetUsers.Id ON DELETE CASCADE; TokenHash varchar(64) NOT NULL UNIQUE; ExpiresAt timestamptz NOT NULL; RevokedAt timestamptz NULL; ReplacedByTokenHash varchar(64) NULL; CreatedAt timestamptz NOT NULL; CreatedByIp varchar(45) NULL. ExpiresAt > CreatedAt. TokenHash là SHA-256 của token ngẫu nhiên đủ mạnh; không lưu raw token. Bảng này không dùng soft delete. Thay token phải revoke token cũ và tạo token mới trong cùng transaction. ReplacedByTokenHash không đặt FK để việc dọn token hết hạn không vướng chuỗi lịch sử.

## 5. Index và tìm kiếm

Index bắt buộc ngoài PK:

| Bảng | Index |
|---|---|
| Recipes | UNIQUE Slug; CategoryId; AuthorId |
| Recipes | (CreatedAt DESC, Id DESC) WHERE NOT IsDeleted AND Status=1 |
| Recipes | (AuthorId, Status, CreatedAt DESC, Id DESC) WHERE NOT IsDeleted |
| Recipes | GIN SearchVector |
| RecipeSteps | UNIQUE (RecipeId, StepNumber) WHERE NOT IsDeleted |
| RecipeSteps | RecipeId đầy đủ để hỗ trợ FK và purge cả lịch sử |
| RecipeIngredients | (RecipeId, OrderIndex, Id) |
| RecipeImages | (RecipeId, OrderIndex, Id); UNIQUE RecipeId WHERE IsPrimary AND NOT IsDeleted |
| Categories | UNIQUE Slug; UNIQUE lower(Name) |
| RefreshTokens | UNIQUE TokenHash; UserId; ExpiresAt |

Tất cả phân trang thêm Id làm tiêu chí cuối để thứ tự ổn định khi giá trị sort trùng. Không tạo index cho mọi trường một cách máy móc; đo query thực tế trước khi thêm index kết hợp Category/Difficulty/CookTime.

Tìm kiếm: dùng cấu hình simple với unaccent, Title trọng số cao hơn Description; trigger BEFORE INSERT/UPDATE OF Title, Description cập nhật SearchVector. Chuẩn hóa truy vấn tìm kiếm tương tự dữ liệu. Cài unaccent và pg_trgm theo SRS; chỉ thêm trigram index khi triển khai truy vấn gần đúng cần nó. Truy vấn public luôn lọc Status=1 AND NOT IsDeleted; SearchVector không phải cơ chế phân quyền.

## 6. Transaction, concurrency và toàn vẹn dữ liệu

1. Mọi lệnh sửa công thức hoặc con của nó phải xác thực quyền owner/admin ở backend, lấy recipe chưa xóa và kiểm tra phiên bản client.
2. UPDATE Recipes với điều kiện Id và RowVersion cũ, đồng thời đổi RowVersion và UpdatedAt. Không khớp hàng nào → 409. Thực hiện cập nhật này trước các thay đổi con trong cùng transaction, kể cả khi chỉ sửa ingredient/step/image.
3. Sau khi giữ được quyền ghi hàng cha, đọc/kiểm tra trạng thái và dữ liệu con cần thiết, rồi ghi tất cả. Khi thất bại rollback toàn bộ. Cách này tuần tự hóa các mutation cùng công thức và bảo vệ quy tắc publish/bước cuối/primary.
4. Đổi primary: tắt primary cũ và lưu trước; bật primary mới và lưu sau; cả hai trong một transaction. Không để EF chọn thứ tự UPDATE gây đụng unique index.
5. Renumber: sau khi xóa mềm bước, chuyển các bước còn lại sang dải số dương tạm thời không đụng số hiện tại, lưu; rồi gán 1..N và lưu. Thực hiện trong cùng transaction, kiểm tra không vượt integer. Partial unique index kiểm tra ngay nên không hoán đổi số trực tiếp.
6. Xóa recipe: đổi IsDeleted của cha và tất cả con trong một transaction. ON DELETE CASCADE không tự chạy khi chỉ cập nhật IsDeleted.
7. Không truy cập endpoint con bằng childId đơn lẻ: luôn kiểm tra RecipeId, trạng thái cha, trạng thái con và quyền người dùng. Global query filter không thay thế phân quyền.
8. Xóa mềm category và tạo/chuyển recipe vào category phải cùng khóa hàng category, kiểm tra lại IsDeleted trong transaction. Đếm recipe rồi xóa mà không khóa có thể đua với thao tác tạo recipe.
9. Job sinh ảnh phải kiểm tra lại ảnh/recipe còn hiệu lực trước khi ghi kết quả; cập nhật metadata bằng concurrency token hiện tại với retry có giới hạn. Không hồi sinh ảnh đã xóa.
10. Khi xóa riêng ảnh, DB commit và enqueue Hangfire không nguyên tử mặc định; cần cơ chế chuyển giao bền vững khi triển khai, không tuyên bố transaction DB bảo đảm enqueue. Không dùng job quét mọi IsDeleted để dọn ảnh vì ảnh của recipe xóa mềm phải được giữ. Xóa file phải idempotent và chỉ áp dụng cho yêu cầu xóa ảnh riêng.

CHECK ở DB bảo vệ enum, độ dài, dấu của số, cặp Quantity/Unit, độ dài token và PublishedAt khi Published. Quy tắc liên bảng như phải có nguyên liệu và bước khi publish được bảo vệ trong luồng transaction nghiệp vụ, không viết CHECK truy vấn bảng khác.

## 7. Đối chiếu FR-RCP

| Yêu cầu | Dữ liệu/quy tắc phục vụ |
|---|---|
| 001 Danh sách | Recipes + Category + Author + primary image; bộ lọc quyền; index và sort ổn định |
| 002 Chi tiết | Recipe cùng các collection còn hiệu lực; nutrition trong hàng cha |
| 003 Tạo | Draft; validate FK; unique slug; transaction tạo cha/con |
| 004 Sửa | RowVersion, kiểm tra owner/admin, giữ slug |
| 005 Publish/unpublish | Status; PublishedAt lần đầu; ít nhất một nguyên liệu và một bước |
| 006 Archive | Status=2; unarchive về Draft |
| 007 Xóa | Soft delete cha/con; giữ file ảnh để có khả năng khôi phục |
| 008 Ảnh | Metadata URL; partial unique primary; chọn primary thay thế |
| 009 Nguyên liệu | Quantity/Unit cùng NULL hoặc cùng hợp lệ; OrderIndex |
| 010 Bước | Unique số bước còn hiệu lực; renumber nguyên tử; bảo vệ bước cuối khi Published |

## 8. Thứ tự triển khai và kiểm chứng

1. Nhóm thống nhất kiểu khóa Identity, DbContext chung và một người điều phối migration nền.
2. Implement entities/value objects/enums; Fluent API mapping và audit/concurrency interceptor; không tạo bảng BaseEntity hay RecipeNutrition.
3. Sinh migration, review FK/CHECK/index/trigger; chạy trên PostgreSQL 16 trống rồi chạy lại qua database update để kiểm tra không phát sinh migration ngoài ý muốn.
4. Seed hai role, category mẫu và dữ liệu thử Draft/Published/Archived; không đưa mật khẩu hoặc token thật vào repository.
5. Integration test trên PostgreSQL thật: trùng slug tự thêm hậu tố, kể cả request đồng thời; FK không tồn tại; cặp Quantity/Unit; hai primary; renumber; publish thiếu nguyên liệu/bước; xóa nguyên liệu/bước cuối; hai request cùng RowVersion chỉ một thành công; xóa category đồng thời tạo recipe; soft delete không lộ con; job ảnh gặp recipe đã xóa.
6. Kiểm tra truy vấn danh sách/tìm kiếm bằng EXPLAIN ANALYZE với dữ liệu mẫu đủ lớn trước khi điều chỉnh index.

Tài liệu này chưa khẳng định các kiểm thử đã chạy. Hiện chưa tạo migration, chưa thay đổi database và chưa commit/push.

## Tham khảo triển khai

- PostgreSQL 16 constraints: https://www.postgresql.org/docs/16/ddl-constraints.html
- EF Core optimistic concurrency, application-managed tokens: https://learn.microsoft.com/en-us/ef/core/saving/concurrency
- Npgsql concurrency mapping: https://www.npgsql.org/efcore/modeling/concurrency.html


## 9. Hợp đồng API đã thống nhất

Nguồn quyết định: `docs/readme-branch-chuong/README.md`.

- JSON: orderIndex, timerMinutes, prepTime, cookTime; thời gian là phút.
- Phân trang: page=1, pageSize=12 (1–50); sortBy=createdAt, sortOrder=desc. Response: items, totalCount, page, pageSize, totalPages, hasNextPage, hasPreviousPage.
- Server cấp số bước; Title thiếu khi tạo được mặc định “Bước N”.
- PATCH /recipes/{id}/images/{imageId}/primary chọn ảnh chính; PATCH metadata chỉ sửa altText/orderIndex. Upload tự đặt ảnh đầu làm primary.
- PATCH /recipes/{id}/unarchive trả về Draft.
- 400: sai định dạng request/file không hợp lệ; 422: validation/nghiệp vụ; 409: concurrency hoặc dữ liệu trùng không được giải quyết tự động. Slug trùng được tự thêm hậu tố, không phải lỗi yêu cầu người dùng đổi tên.
- RowVersion gửi trong body cho cả mutation con và DELETE; multipart khi upload. Thiếu/sai token đầu vào trả 422; token cũ trả 409.
- Cache dùng chung: chỉ nội dung Published; chi tiết 5 phút, tìm kiếm 1 phút, danh sách công khai 15 phút. Invalidate sau mutation; không cache chung dữ liệu riêng của Author/Admin.
