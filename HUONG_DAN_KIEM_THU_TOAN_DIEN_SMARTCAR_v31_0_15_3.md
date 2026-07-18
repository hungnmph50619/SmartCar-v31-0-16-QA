# HƯỚNG DẪN KIỂM THỬ TOÀN DIỆN SMARTCAR v31.0.15.3

## 1. Mục tiêu và nguyên tắc

Tài liệu này dùng để kiểm tra bản SmartCar trước khi demo, bảo vệ đồ án hoặc đóng bản phát hành. Không chỉ kiểm tra giao diện; phải kiểm tra đồng thời:

- Database và dữ liệu seed.
- Build, Unit Test, Integration Test.
- Đăng nhập và phân quyền bốn vai trò.
- Quy trình khách hàng xác minh hồ sơ.
- Quy trình đối tác đăng xe và nhân viên duyệt xe.
- Tìm xe, báo giá, đặt xe, thanh toán, giao nhận và trả xe.
- Hủy đơn, hoàn tiền, phụ phí, sự cố, tranh chấp và đối soát.
- Danh mục hành chính, địa chỉ, bản đồ và file riêng tư.
- Khóa tài khoản, audit log, dữ liệu thống kê và xử lý lỗi.

Quy tắc đánh giá:

- **Pass:** kết quả thực tế đúng hoàn toàn kết quả mong đợi.
- **Fail:** sai kết quả, lỗi 500, treo trang, mất dữ liệu hoặc sai phân quyền.
- **Blocked:** chưa kiểm tra được vì thiếu môi trường, email OTP, dữ liệu hoặc bước trước chưa hoàn thành.
- Mỗi lỗi phải có ảnh chụp, URL, thời điểm, tài khoản, bước tái hiện và nội dung lỗi.

---

## 2. Các file cần dùng

| File | Công dụng |
|---|---|
| `SmartCar_FULL_ONE_CLICK_RESET_INSTALL_v31_0_15_3.sql` | Xóa và cài mới toàn bộ database. Chỉ dùng cho môi trường test/demo. |
| `SmartCar_PATCH_FIX_3_LOI_v31_0_15_3.sql` | Vá database đang có, không xóa dữ liệu. |
| `RESET_HO_SO_KHACHHANG01_DE_DEMO.sql` | Xóa riêng hồ sơ xác minh để demo lại quy trình Customer → Staff. |
| `KIEM_TRA_BUILD_SMARTCAR.bat` | Restore, build và chạy test. |
| `CHAY_KIEM_THU_TU_DONG.bat` | Chạy bộ kiểm thử tự động. |
| `CHAY_DEMO_2_HE_THONG.bat` | Khởi động Web API và WebUI. |
| `KIEM_TRA_DEMO_15_PHUT.bat` | Kiểm tra nhanh API, database, tài khoản và phân quyền trước demo. |

### Chọn cách cập nhật database

**Cách A — cài sạch, được phép mất dữ liệu test:**

1. Mở SQL Server Management Studio.
2. Mở `SmartCar_FULL_ONE_CLICK_RESET_INSTALL_v31_0_15_3.sql`.
3. Chạy toàn bộ file một lần.
4. Không chạy lại script cũ v31.0.15.1.

**Cách B — giữ nguyên database đang dùng:**

1. Sao lưu database.
2. Mở `SmartCar_PATCH_FIX_3_LOI_v31_0_15_3.sql`.
3. Chạy một lần.
4. Không cần chạy full reset.

---

## 3. Tài khoản kiểm thử chuẩn

Mật khẩu chung: `a12345678`

| Vai trò | Username chính | Username test dự phòng |
|---|---|---|
| Admin | `quantri` | `quantri_test` |
| Nhân viên | `nhanvien` | `nhanvien_test` |
| Đối tác | `doitac` | `doitac_test` |
| Khách hàng | `khachhang` | `khachhang_test` |

Bốn tài khoản chính có dữ liệu seed phù hợp hơn để kiểm tra toàn bộ nghiệp vụ. Các tài khoản `_test` chủ yếu dùng kiểm tra đăng nhập và phân quyền độc lập.

Mở bốn profile trình duyệt riêng, không mở bốn vai trò trong cùng một profile vì cookie có thể ghi đè nhau:

1. Chrome thường: Customer.
2. Chrome Guest hoặc profile 2: Partner.
3. Edge hoặc profile 3: Staff.
4. Firefox hoặc profile 4: Admin.

---

## 4. Kiểm tra database sau cài đặt

Chạy truy vấn sau trong SSMS:

```sql
USE SmartCarMarketplaceDb;

SELECT u.Username, r.AppRoleName, u.EmailConfirmed, u.IsActive, u.IsDeleted
FROM dbo.AppUsers u
JOIN dbo.AppRoles r ON r.AppRoleId=u.AppRoleId
WHERE u.Username IN (N'quantri',N'nhanvien',N'doitac',N'khachhang')
ORDER BY u.AppUserId;

SELECT ProvinceType, COUNT(*) AS Total
FROM dbo.AdministrativeProvinces
WHERE IsActive=1
GROUP BY ProvinceType;

SELECT ProvinceCode, ProvinceType, ProvinceName
FROM dbo.AdministrativeProvinces
WHERE ProvinceCode='75';

SELECT COUNT(*) AS ActiveProvinceCount
FROM dbo.AdministrativeProvinces WHERE IsActive=1;

SELECT COUNT(*) AS ActiveWardCount
FROM dbo.AdministrativeWards WHERE IsActive=1;

SELECT COUNT(*) AS OrphanWardCount
FROM dbo.AdministrativeWards w
LEFT JOIN dbo.AdministrativeProvinces p ON p.ProvinceCode=w.ProvinceCode
WHERE w.IsActive=1 AND p.ProvinceCode IS NULL;
```

Kết quả bắt buộc:

- Bốn tài khoản chính đều `EmailConfirmed = 1`, `IsActive = 1`, `IsDeleted = 0`.
- Có 28 bản ghi loại `Tỉnh`.
- Có 6 bản ghi loại `Thành phố`.
- Mã `75` hiển thị `Tỉnh – Đồng Nai`.
- Tổng đơn vị cấp tỉnh đang hoạt động: 34.
- Tổng đơn vị cấp xã đang hoạt động: 3.321.
- Số xã/phường mồ côi: 0.

Không tiếp tục kiểm thử nếu một trong các kết quả trên sai.

---

## 5. Build và kiểm thử tự động

### 5.1. Điều kiện máy

- Visual Studio 2022 với workload **ASP.NET and web development**, hoặc .NET 8 SDK.
- SQL Server và SQL Server Management Studio.
- Cổng `7060` và `7154` không bị ứng dụng khác chiếm.

Mở Command Prompt tại thư mục chứa `CarBook.sln`, chạy:

```bat
KIEM_TRA_BUILD_SMARTCAR.bat
```

Kết quả bắt buộc:

- `dotnet restore` thành công.
- `dotnet build -c Release` không có lỗi.
- Unit Test và Integration Test không có test Fail.
- Có file `TestResults\smartcar-tests.trx`.

Sau đó chạy thêm:

```bat
CHAY_KIEM_THU_TU_DONG.bat
```

Nếu có lỗi package vulnerability, ghi lại tên package, phiên bản và mức độ. Không bỏ qua lỗi High/Critical khi đóng bản cuối.

### 5.2. Kiểm thử Integration với SQL Server thật

Bộ test có thể dùng InMemory nếu chưa cấu hình SQL Server. Để kiểm tra schema thật, tạo database test riêng và cấu hình biến môi trường theo hướng dẫn trong project Integration Test. Không dùng database demo chính để chạy test phá dữ liệu.

---

## 6. Khởi động và kiểm tra nhanh trước demo

Chạy:

```bat
CHAY_DEMO_2_HE_THONG.bat
```

Chờ đến khi hai cửa sổ console báo ứng dụng đã lắng nghe. Mở:

- WebUI: `https://localhost:7154`
- API live: `https://localhost:7060/health/live`

Chấp nhận chứng chỉ HTTPS phát triển nếu trình duyệt cảnh báo.

Tiếp theo chạy:

```bat
KIEM_TRA_DEMO_15_PHUT.bat
```

Bắt buộc đạt:

- WebUI trả về 200/301/302.
- API live trả về 200.
- Đăng nhập thành công đủ bốn tài khoản chính.
- `/health/ready` đạt và database đúng schema.
- Customer đọc được readiness.
- Staff đọc được hàng đợi.
- Partner đọc được dashboard.
- Admin đọc được dashboard, tài khoản và audit log.
- Customer bị chặn khỏi API Admin với HTTP 403.
- Partner bị chặn khỏi hàng đợi Staff với HTTP 403.

---

# PHẦN A — KIỂM THỬ CHỨC NĂNG

## 7. Trang công khai và điều hướng

### PUB-01 — Trang chủ

1. Chưa đăng nhập, mở `https://localhost:7154`.
2. Cuộn từ đầu đến cuối trang.
3. Bấm từng menu chính.

Mong đợi:

- Trang tải trong thời gian hợp lý, không trắng trang hoặc lỗi 500.
- Banner, xe nổi bật, dịch vụ, bài viết, đánh giá và footer có dữ liệu.
- Không có ký tự lỗi font, chuỗi kỹ thuật hoặc placeholder chưa thay.
- Link logo quay về trang chủ.
- Menu không hiển thị chức năng nội bộ khi chưa đăng nhập.

### PUB-02 — Danh sách và chi tiết xe

1. Mở danh sách xe.
2. Lọc theo địa điểm, thời gian, giá, số chỗ, hộp số, nhiên liệu và hình thức thuê.
3. Mở chi tiết một xe.

Mong đợi:

- Kết quả lọc đúng điều kiện.
- Không hiển thị xe bị khóa, ngừng hoạt động hoặc chưa duyệt.
- Chi tiết có ảnh, giá, cọc, phụ phí, địa điểm, chính sách, lịch trống và thông tin chủ xe phù hợp.
- Thay đổi thời gian thuê làm báo giá cập nhật đúng.

### PUB-03 — Responsive và trình duyệt

Kiểm tra Chrome, Edge và kích thước mobile 375×812. Không được vỡ menu, tràn bảng, che nút hoặc mất nội dung quan trọng.

---

## 8. Đăng ký, OTP, đăng nhập và tài khoản

### AUTH-01 — Đăng nhập đủ vai trò

Đăng nhập lần lượt bằng bốn profile riêng:

- `quantri / a12345678`
- `nhanvien / a12345678`
- `doitac / a12345678`
- `khachhang / a12345678`

Mong đợi: đúng dashboard, đúng menu, không ghi đè cookie giữa các profile.

### AUTH-02 — Sai mật khẩu và khóa tạm

1. Dùng một tài khoản test phụ.
2. Nhập sai mật khẩu liên tiếp đến ngưỡng khóa.
3. Nhập đúng mật khẩu ngay sau đó.

Mong đợi:

- Thông báo rõ nhưng không tiết lộ tài khoản có tồn tại hay không quá mức cần thiết.
- `FailedLoginCount` tăng.
- Tài khoản bị khóa đúng ngưỡng và `LockoutEnd` có giá trị.
- Không dùng tài khoản demo chính cho test này nếu chưa có script mở khóa.

### AUTH-03 — Đăng ký Customer mới

1. Dùng email chưa tồn tại.
2. Nhập thiếu từng trường để kiểm tra validation.
3. Nhập dữ liệu hợp lệ.
4. Gửi OTP, nhập OTP đúng.

Mong đợi:

- Email chưa xác minh không bị coi là tài khoản hoàn chỉnh.
- OTP có thời hạn, chỉ dùng một lần.
- Gửi lại OTP có cooldown.
- OTP sai có giới hạn số lần.
- Dữ liệu hợp lệ được giữ lại khi một trường khác sai.

### AUTH-04 — Đăng ký Partner

Kiểm tra tương tự Customer nhưng phải tạo đúng `AccountType=Partner`, `IsVehiclePartner=1` và có hồ sơ đối tác ở trạng thái ban đầu.

### AUTH-05 — Đăng xuất và thu hồi phiên

1. Đăng nhập một tài khoản trên hai trình duyệt.
2. Đổi mật khẩu hoặc chọn “Đăng xuất khỏi tất cả thiết bị”.
3. Thử dùng phiên cũ.

Mong đợi: token/cookie cũ bị vô hiệu hóa nhờ `TokenVersion`.

### AUTH-06 — Phân quyền URL trực tiếp

Không chỉ ẩn menu. Sao chép URL Admin/Staff rồi mở bằng Customer/Partner.

Mong đợi: 401 khi chưa đăng nhập; 403 khi đăng nhập nhưng sai vai trò; tuyệt đối không lộ dữ liệu.

---

## 9. Danh mục hành chính, địa chỉ và bản đồ

### ADDR-01 — Số lượng và Đồng Nai

1. Mở form xác minh.
2. Mở dropdown tỉnh/thành.
3. Tìm Đồng Nai.

Mong đợi:

- Có đủ 34 lựa chọn đang hoạt động.
- Hiển thị `Tỉnh Đồng Nai`, không phải `Thành phố Đồng Nai`.

### ADDR-02 — Dropdown xã/phường động

1. Chọn Hà Nội, quan sát danh sách cấp xã.
2. Chuyển sang Đồng Nai.

Mong đợi:

- Danh sách cấp xã tải lại theo tỉnh.
- Không giữ xã/phường của tỉnh trước.
- Không có bản ghi thuộc tỉnh khác.

### ADDR-03 — Chống sửa mã bằng DevTools

1. Chọn một tỉnh.
2. Dùng DevTools thay WardCode thành xã thuộc tỉnh khác.
3. Gửi form.

Mong đợi: server từ chối với thông báo “Xã/phường ... không thuộc tỉnh/thành phố đã chọn”, không chỉ dựa vào JavaScript phía trình duyệt.

### ADDR-04 — Địa chỉ hiện tại giống thường trú

Bật lựa chọn “Giống địa chỉ thường trú”. Mong đợi dữ liệu hiện tại tự đồng bộ, khóa/ẩn trường phù hợp và lưu đúng cả tên lẫn mã.

### ADDR-05 — Nhập tay và gợi ý địa chỉ

Nhập từng phần địa chỉ, kiểm tra danh sách gợi ý, chọn một gợi ý và kiểm tra tọa độ. Không chọn gợi ý, nhập tay hoàn toàn và lưu. Cả hai trường hợp phải hoạt động.

### ADDR-06 — Định vị bản đồ

1. Cho phép quyền vị trí.
2. Bấm định vị hiện tại.
3. Kéo marker hoặc chọn vị trí khác.

Mong đợi:

- Có marker/mũi tên thể hiện vị trí hiện tại.
- Bản đồ zoom đến vị trí hợp lý.
- Tọa độ và địa chỉ cập nhật đồng bộ.
- Từ chối quyền vị trí không làm treo trang; người dùng vẫn nhập tay được.

---

## 10. Quy trình xác minh khách hàng

### Chuẩn bị reset

Để demo lại từ đầu, mở SSMS và chạy:

```sql
RESET_HO_SO_KHACHHANG01_DE_DEMO.sql
```

Script sẽ ưu tiên `khachhang01`; nếu không có thì tự dùng `khachhang`.

### VER-01 — Trạng thái ban đầu

1. Đăng nhập `khachhang`.
2. Mở Hồ sơ/Xác minh tài khoản.

Mong đợi: form cho phép nhập mới, trạng thái “Chưa xác minh” hoặc tương đương; không còn thông báo duyệt cũ.

### VER-02 — Validation dữ liệu

Kiểm tra lần lượt:

- Họ tên trống.
- Ngày sinh chưa đủ tuổi.
- CCCD không đủ 12 số.
- Ngày cấp sau ngày hết hạn.
- CCCD đã hết hạn.
- GPLX trống/sai định dạng.
- GPLX cấp chưa đủ điều kiện nghiệp vụ.
- GPLX hết hạn trước ngày kết thúc chuyến.
- Thiếu từng ảnh bắt buộc.
- File sai định dạng, file quá lớn, file đổi đuôi giả.

Mong đợi: lỗi đúng trường, nội dung dễ hiểu, dữ liệu đã nhập không bị mất.

### VER-03 — Gửi hồ sơ hợp lệ

Nhập đầy đủ và tải:

- CCCD mặt trước.
- CCCD mặt sau.
- GPLX.
- Ảnh chân dung.

Mong đợi:

- Trạng thái chuyển `Chờ duyệt`.
- Form không cho gửi lặp vô hạn.
- Hồ sơ xuất hiện trong hàng đợi Staff.
- Audit log có hành động “Gửi hồ sơ xác minh”.
- File được lưu dạng riêng tư, không truy cập được bằng URL công khai tùy ý.

### VER-04 — Staff nhận xử lý độc quyền

1. Staff A nhận hồ sơ.
2. Staff B hoặc cửa sổ khác thử nhận cùng hồ sơ.

Mong đợi: không hai người cùng claim một hồ sơ; trạng thái và hạn xử lý hiển thị đúng.

### VER-05 — Yêu cầu bổ sung

Staff chọn “Yêu cầu bổ sung”, nhập lý do cụ thể.

Mong đợi:

- Customer nhận thông báo.
- Form mở lại và giữ dữ liệu cũ.
- Customer sửa/tải lại file rồi gửi.
- Claim cũ được nhả/đóng; hàng đợi không tạo bản ghi trùng.

### VER-06 — Phê duyệt

Staff xem đủ bốn ảnh, đối chiếu dữ liệu và chọn `Đã xác minh`.

Mong đợi:

- Customer nhận thông báo.
- Readiness trả `CanBook = true` khi đủ dữ liệu.
- `ReviewedByAppUserID`, `ReviewedDate` được lưu đúng.
- Claim chuyển hoàn tất.
- Customer có thể đặt xe tự lái.

### VER-07 — Từ chối

Reset rồi gửi lại hồ sơ khác, chọn `Bị từ chối` và nhập lý do. Customer phải thấy lý do; hệ thống không cho đặt xe tự lái.

### VER-08 — CCCD trùng

Dùng tài khoản Customer khác gửi cùng CCCD. Mong đợi HTTP 409/thông báo CCCD đã được dùng; không tạo hồ sơ trùng.

### VER-09 — Admin thu hồi kết quả duyệt

Admin thu hồi hồ sơ đang `Đã xác minh`. Mong đợi trạng thái chuyển theo thiết kế, Customer không còn đủ điều kiện đặt tự lái, có audit log và lý do.

---

## 11. Hồ sơ đối tác và đăng xe

### PART-01 — Hồ sơ đối tác

1. Đăng nhập `doitac`.
2. Kiểm tra hồ sơ cá nhân/doanh nghiệp, ngân hàng, địa chỉ, CCCD và điều khoản.
3. Sửa một trường không hợp lệ và gửi.

Mong đợi: validation đúng, thông tin nhạy cảm không lộ cho Customer.

### PART-02 — Tạo xe bản nháp

Nhập biển số, hãng/dòng xe, năm sản xuất, số chỗ, hộp số, nhiên liệu, mô tả, hình thức thuê và địa điểm.

Mong đợi: lưu bản nháp được; chưa xuất hiện công khai.

### PART-03 — Giá và chính sách

Kiểm tra:

- Giá giờ/ngày.
- Số giờ/ngày tối thiểu.
- Cuối tuần/ngày lễ.
- Cọc giữ chỗ và cọc bảo đảm.
- Giới hạn km và phí vượt km.
- Trả muộn.
- Giao xe và bán kính.
- Nhiên liệu, vệ sinh, hủy đơn.

Mong đợi: không nhận số âm, mức giá bất hợp lý hoặc dữ liệu mâu thuẫn.

### PART-04 — Tài liệu và ảnh xe

Tải giấy đăng ký, đăng kiểm, bảo hiểm và ảnh xe. Kiểm tra file thiếu, hết hạn, sai loại và ảnh quá lớn.

### PART-05 — Gửi duyệt

Mong đợi trạng thái phê duyệt `Chờ duyệt`; trạng thái vận hành chưa phải `Đang hoạt động`; Staff thấy đầy đủ hồ sơ và tất cả ảnh.

### PART-06 — Staff yêu cầu sửa/không đạt

Staff chọn `Chưa đạt`, nhập lý do. Partner sửa và gửi lại. Không tạo xe công khai trước khi đạt.

### PART-07 — Duyệt xe

Staff duyệt. Mong đợi trạng thái phê duyệt `Đã duyệt`, trạng thái vận hành `Đang hoạt động`, xe xuất hiện trong tìm kiếm nếu còn lịch.

### PART-08 — Sửa trường quan trọng sau duyệt

Thử sửa biển số, hãng/dòng, năm, giấy tờ hoặc dữ liệu quan trọng. Mong đợi xe quay về quy trình duyệt lại theo đặc tả. Sửa trường không quan trọng phải theo đúng quy tắc đã thống nhất.

### PART-09 — Ngừng hoạt động và khóa xe

Partner ngừng xe; Admin khóa xe. Xe không được xuất hiện công khai và không nhận đơn mới, nhưng lịch sử đơn cũ vẫn còn.

---

## 12. Tìm xe, báo giá và giữ lịch

### SEARCH-01 — Tìm theo thời gian

- Ngày bắt đầu phải trước ngày kết thúc.
- Không cho thời gian trong quá khứ.
- Kiểm tra số ngày đặt trước tối đa.
- Kiểm tra thời lượng tối thiểu của tự lái và có tài xế.

### SEARCH-02 — Xe trùng lịch

Tạo một đơn đang giữ chỗ/đã xác nhận, sau đó tìm cùng xe trong khoảng thời gian chồng lấn. Mong đợi xe không khả dụng, có tính cả buffer.

### SEARCH-03 — Báo giá

Đối chiếu thủ công:

- Đơn giá × thời lượng.
- Phụ phí giao xe.
- Cọc giữ chỗ.
- Cọc bảo đảm.
- Phí cuối tuần/ngày lễ nếu có.
- Tổng tiền và làm tròn.

Đổi thời gian, hình thức thuê hoặc giao xe; giá phải cập nhật nhất quán ở trang chi tiết, trang đặt và hóa đơn.

---

## 13. Quy trình đặt xe hoàn chỉnh

### BOOK-01 — Customer chưa xác minh đặt tự lái

Sau khi reset hồ sơ, thử đặt tự lái. Mong đợi bị chặn với thông báo cụ thể và nút dẫn đến xác minh, không im lặng.

### BOOK-02 — Customer đủ điều kiện gửi yêu cầu

1. Customer đã xác minh.
2. Chọn xe và thời gian còn trống.
3. Nhập địa điểm giao/nhận và ghi chú.
4. Bấm gửi yêu cầu.

Mong đợi:

- Tạo đúng một Reservation.
- Trạng thái `Chờ chủ xe xác nhận`.
- Lịch được giữ theo thời gian cấu hình.
- Partner nhận thông báo.
- Bấm hai lần nhanh không tạo hai đơn.

### BOOK-03 — Partner chấp nhận

Mong đợi trạng thái chuyển `Chờ thanh toán`; Customer nhận thông báo; thời hạn thanh toán được tạo.

### BOOK-04 — Partner từ chối hoặc hết hạn

Mong đợi đơn chuyển trạng thái phù hợp, lịch xe được giải phóng, Customer thấy lý do.

### BOOK-05 — Hai Customer tranh cùng một lịch

Gửi hai yêu cầu gần đồng thời. Chỉ một yêu cầu được giữ lịch; yêu cầu còn lại nhận 409/thông báo xe vừa được giữ, không overbooking.

---

## 14. Thanh toán và đối chiếu

### PAY-01 — Gửi mã giao dịch

Customer nhập mã giao dịch thanh toán mô phỏng. Kiểm tra mã trống, trùng, sai định dạng và số tiền sai.

### PAY-02 — Staff đối chiếu thành công

Staff kiểm tra và xác nhận. Mong đợi Payment chuyển trạng thái thành công, Reservation tiến đến `Đã xác nhận` hoặc trạng thái tiếp theo đúng nghiệp vụ.

### PAY-03 — Từ chối đối chiếu

Staff nhập lý do. Customer nhận thông báo và có thể gửi lại theo thiết kế; không ghi nhận tiền hai lần.

### PAY-04 — Quá hạn thanh toán

Để quá thời hạn hoặc điều chỉnh dữ liệu test. Mong đợi `Quá hạn thanh toán`, lịch xe được giải phóng và không cho xác nhận thanh toán muộn trái quy tắc.

### PAY-05 — Idempotency

Gửi lại cùng yêu cầu/mã giao dịch. Không được tạo hai Payment hoặc cộng tiền hai lần.

---

## 15. Giao xe, OTP và bắt đầu chuyến

### HAND-01 — Lập biên bản giao xe

Partner/Staff ghi số km, nhiên liệu, tình trạng, ảnh và ghi chú. Không cho thiếu trường bắt buộc.

### HAND-02 — Hai OTP độc lập

Gửi OTP cho Customer và Partner. Kiểm tra:

- Hai mã khác nhau.
- Mỗi mã chỉ xác nhận đúng bên.
- Sai mã, hết hạn, dùng lại đều bị từ chối.
- Một bên xác nhận chưa làm biên bản hoàn tất.
- Đủ hai bên mới khóa biên bản.

Mong đợi Reservation chuyển `Đang thuê` sau khi biên bản giao xe hợp lệ và đủ xác nhận.

### HAND-03 — File biên bản riêng tư

Customer chỉ xem file thuộc đơn của mình; Partner chỉ xem file thuộc xe/đơn của mình; Staff/Admin xem theo quyền. URL đoán mò hoặc thay ID phải bị chặn.

---

## 16. Trả xe, phụ phí và hoàn thành

### RETURN-01 — Biên bản trả xe

Ghi km cuối, nhiên liệu, hư hỏng, ảnh và ghi chú. Đủ hai bên xác nhận theo cơ chế OTP/ngoại lệ.

### RETURN-02 — Không có phụ phí

Mong đợi chuyển đến `Chờ đối soát`, sau đó hoàn thành khi đối soát xong.

### RETURN-03 — Có phụ phí

Partner đề xuất vượt km, nhiên liệu, vệ sinh, trả muộn hoặc hư hỏng. Mong đợi:

- Có bằng chứng và cách tính.
- Customer được phản hồi trong thời hạn.
- Customer đồng ý → tạo khoản thu đúng một lần.
- Customer từ chối → chuyển Staff/Admin xử lý.
- Không cho Partner tự sửa khoản đã chốt.

### RETURN-04 — Trả cọc/hoàn tiền

Đối chiếu số tiền đã thu, phụ phí, cọc hoàn lại và lịch sử giao dịch. Không cho số hoàn vượt số đã thu.

---

## 17. Hủy đơn, no-show, sự cố và tranh chấp

### CANCEL-01 — Customer hủy trước Partner chấp nhận

Đơn hủy, lịch giải phóng, phí hủy theo đúng chính sách.

### CANCEL-02 — Hủy sau thanh toán

Tính phí và hoàn tiền đúng mốc thời gian; tạo RefundTransaction, audit log và thông báo.

### CANCEL-03 — Partner hủy

Customer được thông báo; xử lý hoàn tiền và chế tài Partner theo thiết kế.

### NOSHOW-01 — Khách không đến nhận

Chỉ cho đánh dấu sau ngưỡng thời gian; cần bằng chứng; chuyển `Khách không đến nhận` và xử lý tiền đúng quy tắc.

### NOSHOW-02 — Chủ xe không giao

Chuyển `Chủ xe không đến giao`; bảo vệ quyền lợi Customer và hoàn tiền.

### INCIDENT-01 — Báo sự cố

Customer/Partner tạo báo cáo, đính kèm ảnh, thời gian và mô tả. Chuyển `Đang xử lý sự cố`, không mất lịch sử trước đó.

### DISPUTE-01 — Mở tranh chấp

Chỉ bên liên quan mở được, trong thời hạn cho phép. Staff xử lý; Admin xử lý ngoại lệ/kháng nghị. Kết quả phải có lý do và audit log.

---

## 18. Đối soát và hoa hồng

### SETTLE-01 — Tính tiền

Đối chiếu:

- Tổng tiền thực thu.
- Hoàn tiền.
- Phụ phí đã thu.
- Tỷ lệ hoa hồng.
- Hoa hồng nền tảng.
- Số tiền ròng của Partner.

### SETTLE-02 — Partner xem và phản hồi

Partner chỉ xem đối soát của mình. Đồng ý hoặc khiếu nại theo đúng trạng thái.

### SETTLE-03 — Staff/Admin duyệt chi trả

Không cho chi trả khi dữ liệu chưa khóa, còn tranh chấp hoặc số tiền không cân bằng. Sau khi Paid không được chi lần hai.

---

## 19. Đánh giá hai chiều và phạt nguội

### REVIEW-01 — Chỉ đánh giá đơn hoàn thành

Customer và Partner đánh giá đúng đối tượng, trong thời hạn. Mỗi bên chỉ một đánh giá; không tự đánh giá; nội dung và số sao hợp lệ.

### FINE-01 — Phạt nguội

Partner gửi yêu cầu trong thời hạn, có chứng cứ và ngày vi phạm nằm trong chuyến. Staff duyệt/từ chối; Customer được thông báo; không tạo khoản thu trùng.

---

## 20. Admin và Staff

### ADMIN-01 — Dashboard

Đối chiếu số tài khoản, xe, đơn, doanh thu, hồ sơ chờ duyệt với truy vấn database. Bộ lọc thời gian phải thay đổi số liệu đúng.

### ADMIN-02 — Quản lý tài khoản

Kiểm tra tìm kiếm, lọc loại tài khoản, khu vực, tuổi, giới tính, trạng thái xác minh và trạng thái khóa.

### ADMIN-03 — Khóa tài khoản

- Khóa tạm thời có ngày hết hạn.
- Khóa vĩnh viễn.
- Nhập lý do bắt buộc.
- Phiên hiện tại bị vô hiệu hóa.
- Mở khóa phục hồi đúng quyền.

### ADMIN-04 — Xem chi tiết hồ sơ

Admin/Staff xem được dữ liệu và ảnh cần thiết; Customer/Partner khác không xem được.

### ADMIN-05 — Audit log

Kiểm tra các hành động quan trọng: đăng nhập, gửi/duyệt hồ sơ, duyệt xe, thay đổi trạng thái đơn, thanh toán, hoàn tiền, khóa tài khoản, xử lý tranh chấp. Audit không được sửa/xóa qua giao diện thông thường.

### STAFF-01 — Hàng đợi

Không có item trùng, stale claim hoặc item đã hoàn tất vẫn ở “Cần xử lý”. Claim hết hạn phải được xử lý đúng.

---

# PHẦN B — KIỂM THỬ PHI CHỨC NĂNG

## 21. Bảo mật

### SEC-01 — Truy cập trái phép

Thử URL trực tiếp, thay ID trong URL/API và gọi endpoint bằng token sai vai trò. Kết quả phải là 401/403/404 phù hợp, không trả dữ liệu nhạy cảm.

### SEC-02 — CSRF

Các form thay đổi dữ liệu qua WebUI phải có anti-forgery. Gửi POST thiếu token phải bị từ chối.

### SEC-03 — XSS

Nhập `<script>alert(1)</script>` vào mô tả, ghi chú, lý do và bình luận. Khi hiển thị phải được encode, không chạy script.

### SEC-04 — SQL Injection

Nhập chuỗi như `' OR 1=1 --` vào tìm kiếm/đăng nhập. Không đăng nhập trái phép, không lỗi SQL và không lộ stack trace.

### SEC-05 — Upload file

Kiểm tra file `.exe` đổi đuôi `.jpg`, MIME giả, file quá lớn, tên file chứa `../`, tên Unicode dài. Hệ thống phải từ chối hoặc lưu bằng tên an toàn ngoài webroot.

### SEC-06 — JWT/cookie

Token hết hạn, TokenVersion cũ và token bị sửa chữ ký phải bị từ chối. Cookie cần Secure, HttpOnly và SameSite phù hợp trong môi trường HTTPS.

### SEC-07 — Rate limit

Gửi nhanh login, OTP, upload và API công khai. Hệ thống giới hạn hợp lý, trả 429 thay vì treo hoặc 500.

---

## 22. Hiệu năng và ổn định

### PERF-01 — Trang chủ và tìm xe

Dùng DevTools Network kiểm tra thời gian phản hồi, request lỗi, ảnh quá lớn và request lặp. Trang không được gọi API vô hạn.

### PERF-02 — Dữ liệu lớn

Tạo nhiều xe/đơn/hồ sơ trong database test, kiểm tra phân trang và lọc. Không tải toàn bộ hàng nghìn bản ghi vào một trang.

### PERF-03 — Đồng thời

Hai người cùng đặt một lịch, hai Staff cùng claim, hai lần đối chiếu cùng Payment. Hệ thống phải chống ghi trùng bằng transaction/concurrency/idempotency.

### STAB-01 — Khởi động lại

Dừng API trong khi WebUI đang mở, thao tác rồi khởi động lại. WebUI phải báo lỗi dễ hiểu, không treo; dữ liệu transaction dở dang không bị nửa vời.

### STAB-02 — Database mất kết nối

Tạm dừng SQL Server ở môi trường test. `/health/ready` phải Fail; API không báo Ready giả.

---

## 23. Khả năng sử dụng

- Mọi nút quan trọng có trạng thái loading và chống bấm lặp.
- Sau thành công có thông báo và điều hướng rõ.
- Sau thất bại vẫn giữ dữ liệu người dùng đã nhập.
- Thông báo lỗi nói được người dùng cần làm gì.
- Ngày giờ hiển thị theo múi giờ Việt Nam, không lệch ngày duyệt/giao nhận.
- Số tiền có phân tách hàng nghìn và đơn vị VNĐ nhất quán.
- Trạng thái nghiệp vụ dùng cùng một tên ở WebUI, API và database.
- Form dùng bàn phím được; label gắn đúng input; ảnh có alt phù hợp.

---

# PHẦN C — KIỂM TRA DỮ LIỆU SAU LUỒNG END-TO-END

## 24. Truy vấn đối chiếu nhanh

Sau khi hoàn thành một đơn, chạy các truy vấn sau, thay `@ReservationID`:

```sql
USE SmartCarMarketplaceDb;
DECLARE @ReservationID int = 1;

SELECT * FROM dbo.Reservations WHERE ReservationID=@ReservationID;
SELECT * FROM dbo.ReservationStatusHistories WHERE ReservationID=@ReservationID ORDER BY CreatedDate;
SELECT * FROM dbo.Payments WHERE ReservationID=@ReservationID ORDER BY CreatedDate;
SELECT * FROM dbo.HandoverReports WHERE ReservationID=@ReservationID ORDER BY CreatedDate;
SELECT * FROM dbo.AdditionalCharges WHERE ReservationID=@ReservationID ORDER BY CreatedDate;
SELECT * FROM dbo.Settlements WHERE ReservationID=@ReservationID ORDER BY CreatedDate;
SELECT * FROM dbo.RefundTransactions WHERE ReservationID=@ReservationID ORDER BY CreatedDate;
SELECT * FROM dbo.AuditLogs WHERE EntityID=CONVERT(nvarchar(50),@ReservationID) ORDER BY CreatedDate;
```

Kiểm tra:

- Không có Payment trùng mã giao dịch.
- Lịch sử trạng thái đi theo thứ tự hợp lệ.
- Không có hai biên bản đang hiệu lực cùng loại.
- Tổng tiền thu, hoàn, hoa hồng và chi Partner cân bằng.
- Không có khóa ngoại mồ côi.
- Audit log khớp người thao tác và thời gian.

---

## 25. Kịch bản End-to-End bắt buộc phải Pass

Đây là luồng quan trọng nhất để quyết định phần mềm có đủ điều kiện demo hay không:

1. Reset hồ sơ `khachhang`.
2. Customer đăng nhập và gửi hồ sơ xác minh.
3. Staff claim và phê duyệt.
4. Customer tìm một xe đang hoạt động.
5. Customer chọn thời gian còn trống và gửi yêu cầu.
6. Partner chấp nhận.
7. Customer gửi thông tin thanh toán.
8. Staff đối chiếu thành công.
9. Partner lập biên bản giao xe.
10. Customer và Partner nhập hai OTP riêng.
11. Đơn chuyển `Đang thuê`.
12. Lập biên bản trả xe.
13. Xử lý phụ phí hoặc xác nhận không có phụ phí.
14. Staff/Admin đối soát.
15. Đơn chuyển `Hoàn thành`.
16. Customer và Partner đánh giá nhau.
17. Admin xem dashboard và audit log.

Chỉ cần một bước P0 không hoàn thành thì chưa được kết luận phần mềm hoàn thiện.

---

## 26. Mẫu ghi lỗi

```text
Mã lỗi: BUG-001
Mức độ: P0 / P1 / P2 / P3
Module: Xác minh khách
Môi trường: Windows..., Chrome..., SQL Server...
Tài khoản: khachhang
Thời điểm: yyyy-MM-dd HH:mm:ss
Tiền điều kiện: ...

Các bước:
1. ...
2. ...
3. ...

Kết quả mong đợi: ...
Kết quả thực tế: ...
Thông báo/HTTP status: ...
URL: ...
Ảnh/video/log: ...
Dữ liệu database liên quan: ...
Tần suất: 1/1, 3/5...
```

Mức độ:

- **P0:** không cài/chạy được, không đăng nhập được, sai phân quyền, mất dữ liệu, không hoàn thành luồng thuê xe.
- **P1:** chức năng chính sai nhưng có cách né tạm thời.
- **P2:** chức năng phụ hoặc validation chưa tốt.
- **P3:** giao diện, chính tả, căn chỉnh, trải nghiệm nhỏ.

---

## 27. Tiêu chí kết luận bản phát hành

Chỉ đánh dấu **Đủ điều kiện demo** khi:

- Full SQL hoặc patch chạy không lỗi.
- 34 tỉnh/thành, 3.321 xã/phường; Đồng Nai là Tỉnh.
- Bốn tài khoản chính đăng nhập được bằng `a12345678`.
- Build Release và toàn bộ test tự động Pass.
- Script kiểm tra demo Pass.
- Toàn bộ test P0 Pass.
- Không có lỗi 500 chưa rõ nguyên nhân.
- Không có lỗi phân quyền hoặc lộ file riêng tư.
- Luồng End-to-End từ xác minh đến hoàn thành đơn Pass.
- Các lỗi P1 còn lại đã có danh sách, người phụ trách và phương án demo an toàn.

Chỉ đánh dấu **Hoàn thiện để nộp bản cuối** khi thêm các điều kiện:

- P1 đã sửa hết hoặc được chấp thuận bằng văn bản.
- Package không có lỗ hổng High/Critical.
- Integration Test chạy với SQL Server thật.
- Gói bàn giao không chứa `.vs`, `bin`, `obj`, file người dùng hoặc secret.
- Có bản sao database/script, tài liệu đặc tả, tài liệu kiểm thử và hướng dẫn chạy.
