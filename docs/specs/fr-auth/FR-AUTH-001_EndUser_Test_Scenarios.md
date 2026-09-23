# Kịch bản Kiểm thử Người dùng Cuối (End-User Test Scenarios) — FR-AUTH-001: Đăng ký Tài khoản

> **Tài liệu tham chiếu:** [`FR-AUTH-001_Plan.md`](./FR-AUTH-001_Plan.md), [`FR-AUTH_APIContract.md`](./FR-AUTH_APIContract.md), [`UI_DESIGN_SPECIFICATION.md`](../UI_DESIGN_SPECIFICATION.md)  
> **Phân hệ:** Quản lý Xác thực (FR-AUTH)  
> **Chức năng:** FR-AUTH-001 Đăng ký tài khoản (User Registration)  
> **Đối tượng áp dụng:** QA Tester, End-User, Stakeholder, Product Owner  
> **Phiên bản:** 1.0.0  
> **Ngày lập:** 23/09/2026  

---

## 1. Hướng dẫn Môi trường & Chuẩn bị Kiểm thử

### 1.1. Yêu cầu môi trường chạy thử nghiệm

1. **Backend Service:**
   - URL: `http://localhost:5058`
   - Lệnh khởi chạy: `dotnet run --project src/CulinaryBlog.API`
   - Trạng thái yêu cầu: Kết nối thành công tới Database PostgreSQL.
2. **Frontend Application:**
   - URL: `http://localhost:3000`
   - Trang kiểm thử: `http://localhost:3000/register`
   - Lệnh khởi chạy: `cd frontend && pnpm dev`
3. **Trình duyệt khuyến nghị:**
   - Google Chrome / Microsoft Edge / Mozilla Firefox / Safari phiên bản mới nhất.
   - Sử dụng công cụ **DevTools (phím F12)** để theo dõi mục *Application -> Local Storage* và tab *Network*.

### 1.2. Dữ liệu chuẩn bị (Test Data Preconditions)

- **Tài khoản đã tồn tại sẵn trong hệ thống (dùng để test lỗi trùng lặp 409 Conflict):**
  - Email mẫu: `existing.user@culinaryblog.vn`
  - Username mẫu: `existing_chef`
- **Tài khoản mới hoàn toàn (dùng cho Happy Path):**
  - Email chưa từng đăng ký: `nguyenvan.a@culinaryblog.vn`
  - Username chưa từng dùng: `bep_truong_an`

---

## 2. Bảng Ma trận Kịch bản Kiểm thử (Test Scenario Matrix)

| Mã TC | Phân nhóm kiểm thử | Tên kịch bản | Mức độ ưu tiên |
| :--- | :--- | :--- | :---: |
| **TC-01** | Happy Path | Đăng ký tài khoản thành công với thông tin hợp lệ | **Cao** |
| **TC-02** | Happy Path | Tự động đăng nhập và lưu trữ Token trong Local Storage | **Cao** |
| **TC-03** | Happy Path | Duy trì phiên đăng nhập khi tải lại trang (F5 Reload) | **Cao** |
| **TC-04** | Happy Path | Đăng xuất và dọn dẹp sạch phiên làm việc | **Cao** |
| **TC-05** | Validation | Bỏ trống các trường bắt buộc | **Trung bình** |
| **TC-06** | Validation | Tên hiển thị (DisplayName) sai độ dài (< 2 hoặc > 100 ký tự) | **Trung bình** |
| **TC-07** | Validation | Tên đăng nhập (Username) sai độ dài (< 3 hoặc > 30 ký tự) | **Trung bình** |
| **TC-08** | Validation | Tên đăng nhập chứa ký tự đặc biệt không hợp lệ hoặc dấu cách | **Cao** |
| **TC-09** | Validation | Định dạng Email không hợp lệ | **Cao** |
| **TC-10** | Validation | Mật khẩu không đáp ứng quy tắc BR-AUTH-002 (thiếu ký tự hoa, số, đặc biệt...) | **Cao** |
| **TC-11** | Validation | Mật khẩu xác nhận không trùng khớp | **Cao** |
| **TC-12** | UI/UX | Tính năng Ẩn / Hiện mật khẩu trên form | **Thấp** |
| **TC-13** | Business Rule | Đăng ký với Email đã tồn tại (409 Conflict) | **Cao** |
| **TC-14** | Business Rule | Đăng ký với Tên đăng nhập đã tồn tại (409 Conflict) | **Cao** |
| **TC-15** | Business Rule | Kiểm tra tính không phân biệt hoa thường (Case-insensitive) của Email & Username | **Trung bình** |
| **TC-16** | Edge Case | Đăng ký khi mất kết nối mạng / Backend ngừng hoạt động | **Cao** |
| **TC-17** | Edge Case | Giới hạn tần suất gọi API (Rate Limiting 10 req/phút) | **Trung bình** |
| **TC-18** | Edge Case | Chống nhấn đúp liên tục nút Đăng ký (Double-submit & Loading State) | **Trung bình** |
| **TC-19** | Integration | Tương tác nút Đăng ký bằng Google (Chưa phát triển) | **Thấp** |
| **TC-20** | UI/UX | Điều hướng sang trang Đăng nhập qua liên kết phụ | **Thấp** |
| **TC-21** | UI/UX | Hiển thị giao diện trên Mobile và chế độ Dark Mode | **Thấp** |

---

## 3. Chi tiết Từng Kịch bản Kiểm thử

### Nhóm 1: Luồng Thành công (Happy Path & Onboarding)

#### 📝 TC-01: Đăng ký tài khoản thành công với thông tin hợp lệ
- **Mục tiêu:** Kiểm tra toàn bộ quy trình đăng ký tài khoản mới khi người dùng nhập dữ liệu chuẩn xác.
- **Các bước thực hiện:**
  1. Mở trình duyệt, truy cập: `http://localhost:3000/register`.
  2. Nhập các trường thông tin:
     - **Tên hiển thị:** `Hoàng Minh An`
     - **Địa chỉ Email:** `minhan.chef@culinaryblog.vn`
     - **Tên đăng nhập:** `minhan_chef`
     - **Mật khẩu:** `AnChef@2026!`
     - **Xác nhận mật khẩu:** `AnChef@2026!`
  3. Nhấn nút **"Đăng ký ngay"**.
- **Kết quả mong đợi:**
  - Nút chuyển sang trạng thái đang tải (Loading spinner).
  - Xuất hiện Toast thông báo màu xanh lá (Success): *"Đăng ký thành công! Chào mừng Hoàng Minh An"*.
  - Trình duyệt tự động chuyển hướng về trang chủ `http://localhost:3000/`.
  - Trên thanh Navbar hiển thị tên `Hoàng Minh An` và avatar người dùng đăng nhập.

---

#### 📝 TC-02: Kiểm tra lưu trữ Token trong Local Storage
- **Mục tiêu:** Xác minh JWT Token và dữ liệu người dùng được lưu trữ an toàn, đúng khóa quy định.
- **Các bước thực hiện:**
  1. Sau khi hoàn thành **TC-01**, nhấn phím **F12** để mở DevTools.
  2. Chọn tab **Application** $\rightarrow$ chọn **Local Storage** $\rightarrow$ chọn `http://localhost:3000`.
- **Kết quả mong đợi:**
  - Tồn tại khóa `auth_access_token`: chuỗi mã hóa JWT hợp lệ (có 3 phần phân tách bởi dấu chấm `.`).
  - Tồn tại khóa `auth_refresh_token`: chuỗi ký tự ngẫu nhiên Base64Url.
  - Tồn tại khóa `auth_user`: chứa JSON có cấu trúc:
    ```json
    {
      "id": "...",
      "displayName": "Hoàng Minh An",
      "userName": "minhan_chef",
      "email": "minhan.chef@culinaryblog.vn",
      "roles": ["Author"],
      "createdAt": "..."
    }
    ```

---

#### 📝 TC-03: Duy trì phiên làm việc khi tải lại trang (Persistence on Refresh)
- **Mục tiêu:** Đảm bảo phiên người dùng không bị mất và không bị lỗi chớp nháy (hydration mismatch) khi tải lại trang.
- **Các bước thực hiện:**
  1. Đang ở trang chủ trong trạng thái đã đăng nhập.
  2. Nhấn phím **F5** (hoặc Ctrl+R) để tải lại trang.
- **Kết quả mong đợi:**
  - Trang tải lại mượt mà, không xuất hiện thông báo lỗi đỏ trong Console DevTools.
  - Trạng thái đăng nhập trên Navbar vẫn giữ nguyên thông tin `Hoàng Minh An`.

---

#### 📝 TC-04: Đăng xuất tài khoản (Logout Flow)
- **Mục tiêu:** Đảm bảo người dùng có thể thoát phiên và toàn bộ token được dọn sạch.
- **Các bước thực hiện:**
  1. Nhấp chuột vào biểu tượng tài khoản trên thanh Navbar.
  2. Chọn mục **"Đăng xuất"**.
- **Kết quả mong đợi:**
  - Xuất hiện Toast thông báo màu xanh dương (Info): *"Đã đăng xuất"*.
  - Thanh Navbar quay lại trạng thái chưa đăng nhập (hiển thị nút "Đăng nhập" và "Đăng ký").
  - Mở DevTools $\rightarrow$ Local Storage: các khóa `auth_access_token`, `auth_refresh_token`, `auth_user` đã bị xóa hoàn toàn.

---

### Nhóm 2: Kiểm thử Hợp lệ Dữ liệu (Client-side Validation & UX)

#### 📝 TC-05: Bỏ trống các trường bắt buộc
- **Các bước thực hiện:**
  1. Truy cập `http://localhost:3000/register`.
  2. Không nhập bất kỳ trường nào, nhấn nút **"Đăng ký ngay"**.
- **Kết quả mong đợi:**
  - Trình duyệt kích hoạt validation HTML5 (hoặc form hiển thị cảnh báo yêu cầu nhập trường bắt buộc).
  - Không có bất kỳ request API nào được gửi lên Backend.

---

#### 📝 TC-06: Tên hiển thị (DisplayName) không hợp lệ
- **Các bước thực hiện:**
  1. Nhập Tên hiển thị là `A` (1 ký tự), các trường còn lại nhập hợp lệ. Nhấn **Đăng ký ngay**.
  2. Đổi Tên hiển thị thành chuỗi dài hơn 100 ký tự. Nhấn **Đăng ký ngay**.
- **Kết quả mong đợi:**
  - Bước 1: Hiển thị lỗi màu đỏ ngay dưới ô: *"Tên hiển thị phải có ít nhất 2 ký tự."*
  - Bước 2: Hiển thị lỗi: *"Tên hiển thị không quá 100 ký tự."*

---

#### 📝 TC-07 & TC-08: Tên đăng nhập (Username) không hợp lệ
- **Các bước thực hiện:**
  1. Nhập Username là `ab` (< 3 ký tự) $\rightarrow$ Nhấn Đăng ký.
  2. Nhập Username là chuỗi > 30 ký tự $\rightarrow$ Nhấn Đăng ký.
  3. Nhập Username chứa dấu cách: `minh an` $\rightarrow$ Nhấn Đăng ký.
  4. Nhập Username chứa ký tự tiếng Việt có dấu: `bếp_trưởng` $\rightarrow$ Nhấn Đăng ký.
  5. Nhập Username chứa ký tự đặc biệt: `an@chef!` $\rightarrow$ Nhấn Đăng ký.
- **Kết quả mong đợi:**
  - Bước 1: Hiển thị lỗi: *"Tên đăng nhập phải từ 3 ký tự trở lên."*
  - Bước 2: Hiển thị lỗi: *"Tên đăng nhập không quá 30 ký tự."*
  - Bước 3, 4, 5: Hiển thị lỗi: *"Chỉ chấp nhận chữ cái, số và dấu gạch dưới."*

---

#### 📝 TC-09: Định dạng Email không hợp lệ
- **Các bước thực hiện:**
  1. Thử lần lượt các giá trị Email:
     - `minhan` (thiếu `@` và domain)
     - `minhan@` (thiếu domain)
     - `minhan@gmail` (thiếu phần mở rộng `.com`)
     - `minh an@culinaryblog.vn` (chứa dấu cách)
  2. Nhấn nút **"Đăng ký ngay"**.
- **Kết quả mong đợi:**
  - Hiển thị lỗi màu đỏ dưới ô Email: *"Email không hợp lệ."*
  - Không gửi request đến server.

---

#### 📝 TC-10: Kiểm thử quy tắc Mật khẩu theo chuẩn BR-AUTH-002
- **Mục tiêu:** Mật khẩu phải đạt tối thiểu 8 ký tự, gồm ít nhất 1 chữ hoa, 1 chữ thường, 1 chữ số và 1 ký tự đặc biệt.
- **Các kịch bản kiểm thử:**

| Dữ liệu nhập thử | Lỗi thiếu sót | Kết quả mong đợi dưới ô Mật khẩu |
| :--- | :--- | :--- |
| `Pass1!` | Độ dài < 8 ký tự | *"Mật khẩu phải từ 8 ký tự trở lên."* |
| `password123!` | Không có chữ hoa | *"Mật khẩu phải có ít nhất 1 chữ hoa."* |
| `PASSWORD123!` | Không có chữ thường | *"Mật khẩu phải có ít nhất 1 chữ thường."* |
| `PasswordABC!` | Không có chữ số | *"Mật khẩu phải có ít nhất 1 chữ số."* |
| `Password1234` | Không có ký tự đặc biệt | *"Mật khẩu phải có ít nhất 1 ký tự đặc biệt."* |

---

#### 📝 TC-11: Mật khẩu xác nhận không khớp
- **Các bước thực hiện:**
  1. Nhập Mật khẩu: `AnChef@2026!`
  2. Nhập Xác nhận mật khẩu: `AnChef@2026?` (khác ký tự cuối)
  3. Nhấn **"Đăng ký ngay"**.
- **Kết quả mong đợi:**
  - Xuất hiện lỗi màu đỏ dưới ô Xác nhận mật khẩu: *"Mật khẩu xác nhận không khớp."*

---

#### 📝 TC-12: Tính năng Ẩn / Hiện mật khẩu
- **Các bước thực hiện:**
  1. Nhập mật khẩu vào ô Mật khẩu. Ban đầu văn bản ở dạng dấu chấm `••••••`.
  2. Nhấp vào biểu tượng Con mắt (Eye icon) bên phải ô nhập liệu.
  3. Nhấp lại lần thứ hai.
- **Kết quả mong đợi:**
  - Khi nhấp lần 1: Mật khẩu hiển thị rõ chữ (Plaintext), icon chuyển sang hình mắt gạch chéo (`EyeOff`).
  - Khi nhấp lần 2: Mật khẩu ẩn trở lại thành dạng `••••••`, icon trở lại hình con mắt (`Eye`).

---

### Nhóm 3: Quy tắc Nghiệp vụ Backend & Xung đột Dữ liệu (409 Conflict)

#### 📝 TC-13: Trùng lặp Email đã đăng ký
- **Các bước thực hiện:**
  1. Sử dụng Email đã được đăng ký ở **TC-01**: `minhan.chef@culinaryblog.vn`.
  2. Nhập Tên hiển thị khác: `An Khác`, Username khác: `ankhac_99`, Mật khẩu hợp lệ: `AnChef@2026!`.
  3. Nhấn **"Đăng ký ngay"**.
- **Kết quả mong đợi:**
  - Network tab: API `/api/v1/auth/register` trả về HTTP status **409 Conflict** với mã `AUTH_EMAIL_EXISTS`.
  - Trên form xuất hiện dòng lỗi màu đỏ ngay dưới ô **Địa chỉ Email**: *"Email đã được sử dụng"*.
  - Form không bị reset dữ liệu đã nhập, người dùng không bị chuyển trang.

---

#### 📝 TC-14: Trùng lặp Tên đăng nhập (Username)
- **Các bước thực hiện:**
  1. Nhập một Email mới: `new_user123@culinaryblog.vn`.
  2. Nhập Username đã tồn tại từ **TC-01**: `minhan_chef`.
  3. Nhập mật khẩu hợp lệ và nhấn **"Đăng ký ngay"**.
- **Kết quả mong đợi:**
  - Network tab: API trả về HTTP status **409 Conflict** với mã `AUTH_USERNAME_EXISTS`.
  - Xuất hiện thông báo lỗi màu đỏ ngay dưới ô **Tên đăng nhập**: *"Tên đăng nhập đã được sử dụng"*.

---

#### 📝 TC-15: Không phân biệt hoa/thường (Case-insensitivity)
- **Các bước thực hiện:**
  1. Đăng ký với Email viết hoa: `MinhAn.Chef@CulinaryBlog.VN` (đã có `minhan.chef@culinaryblog.vn`).
  2. Đăng ký với Username viết hoa: `MinhAn_Chef` (đã có `minhan_chef`).
- **Kết quả mong đợi:**
  - Hệ thống tự động nhận diện trùng lặp và chặn lại với lỗi 409 Conflict tương ứng.

---

### Nhóm 4: Khả năng Chịu lỗi & Xử lý Ngoại lệ (Edge Cases)

#### 📝 TC-16: Xử lý khi mất kết nối mạng / Backend ngừng hoạt động
- **Các bước thực hiện:**
  1. Điền đầy đủ form đăng ký hợp lệ.
  2. Tắt dịch vụ Backend ASP.NET Core (hoặc ngắt kết nối WiFi / mạng).
  3. Nhấn **"Đăng ký ngay"**.
- **Kết quả mong đợi:**
  - Nút đăng ký kết thúc trạng thái loading.
  - Hiển thị Toast thông báo lỗi màu đỏ: *"Không thể kết nối server"*.
  - Trang web không bị crash hay trắng màn hình (No unhandled exception).

---

#### 📝 TC-17: Giới hạn tần suất gọi API (Rate Limiting)
- **Các bước thực hiện:**
  1. Sử dụng script hoặc liên tục gửi yêu cầu đăng ký hơn 10 lần trong vòng 1 phút từ cùng 1 địa chỉ IP.
- **Kết quả mong đợi:**
  - Từ lần thứ 11, API trả về mã lỗi **429 Too Many Requests**.
  - Giao diện người dùng hiển thị thông báo lỗi phù hợp, không làm treo ứng dụng.

---

#### 📝 TC-18: Chống nhấn đúp liên tục (Double Submit)
- **Các bước thực hiện:**
  1. Điền thông tin hợp lệ.
  2. Nhấp chuột liên tiếp 3-4 lần thật nhanh vào nút **"Đăng ký ngay"**.
- **Kết quả mong đợi:**
  - Ngay sau cú nhấp đầu tiên, nút chuyển sang trạng thái disabled (`isLoading = true`).
  - Trình duyệt chỉ gửi duy nhất 1 HTTP Request lên server.

---

#### 📝 TC-19: Tương tác nút "Đăng ký bằng Google"
- **Các bước thực hiện:**
  1. Tại trang đăng ký, nhấn vào nút **"Đăng ký bằng Google"**.
- **Kết quả mong đợi:**
  - Hiển thị Toast thông báo thông tin (Info): *"Đăng ký Google đang được phát triển."*.
  - Người dùng vẫn ở lại trang đăng ký, không xảy ra chuyển hướng lỗi.

---

#### 📝 TC-20: Điều hướng liên kết sang trang Đăng nhập
- **Các bước thực hiện:**
  1. Nhấn vào dòng chữ liên kết **"Đăng nhập ngay"** ở cuối form.
- **Kết quả mong đợi:**
  - Chuyển hướng mượt mà sang trang `http://localhost:3000/login`.

---

## 4. Biên bản Đánh giá & Nghiệm thu Kiểm thử (Sign-off Template)

| Thông tin nghiệm thu | Chi tiết |
| :--- | :--- |
| **Người thực hiện kiểm thử:** | ........................................................... |
| **Ngày kiểm thử:** | ...... / ...... / 2026 |
| **Phiên bản Frontend:** | `v1.0.0` (Commit: `5b95fc2`) |
| **Phiên bản Backend:** | `net10.0` (FR-AUTH-001) |
| **Tổng số kịch bản:** | 21 Test Cases |
| **Số kịch bản Đạt (Passed):** | ...... / 21 |
| **Số kịch bản Lỗi (Failed):** | ...... / 21 |
| **Kết luận của Tester:** | $\square$ **ĐẠT (Ready to Release)** &nbsp;&nbsp;&nbsp;&nbsp; $\square$ **CẦN SỬA LỖI (Blocked)** |
| **Chữ ký Tester:** | ........................................................... |
