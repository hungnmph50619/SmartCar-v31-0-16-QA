# Kiểm thử SmartCar v31.0.16

## 1. Chuẩn bị

1. Sao lưu database nếu cần giữ dữ liệu.
2. Chạy SQL FULL ONE CLICK v31.0.16 cho môi trường test sạch, hoặc chạy PATCH v31.0.16 trên database v31.0.15.3.
3. Kết quả SQL phải trả về `ActiveProvinceCount = 34` và `ActiveWardCount = 3321`.
4. Chạy `KIEM_TRA_BUILD_SMARTCAR.bat`, sau đó `CHAY_DEMO_2_HE_THONG.bat`.

## 2. Kiểm thử hồ sơ đối tác

1. Đăng nhập tài khoản đối tác, mở `Hồ sơ`.
2. Mở danh sách tỉnh/thành phố: phải có 34 đơn vị, không chỉ còn Hà Nội, Ninh Bình, TP.HCM, Đà Nẵng và Hải Phòng.
3. Chọn lần lượt một vài tỉnh khác nhau; danh sách xã/phường/đặc khu phải thay đổi đúng theo tỉnh.
4. Chọn `Địa chỉ hiện tại giống địa chỉ thường trú`; gửi hồ sơ và kiểm tra địa chỉ hiện tại được sao chép đúng.
5. Bỏ chọn, nhập địa chỉ hiện tại khác rồi gửi; dữ liệu phải lưu đúng hai cặp mã hành chính.
6. Với loại `Doanh nghiệp/Tổ chức`, kiểm tra danh mục tại phần địa chỉ trụ sở.
7. Dùng DevTools sửa mã xã thành mã thuộc tỉnh khác rồi gửi; API phải từ chối với thông báo xã/phường không thuộc tỉnh đã chọn.

## 3. Kiểm thử giới hạn mật khẩu

Dùng tài khoản thử nghiệm riêng để tránh gián đoạn tài khoản demo đang sử dụng.

1. Nhập sai mật khẩu 4 lần: mỗi lần báo tên đăng nhập hoặc mật khẩu không đúng.
2. Nhập sai lần thứ 5: hệ thống trả thông báo đã sai 5 lần và khóa 15 phút, kèm giờ được thử lại.
3. Nhập mật khẩu đúng trong thời gian khóa: vẫn không được đăng nhập.
4. Mở khóa từ màn hình quản trị: đăng nhập đúng phải thành công ngay và bộ đếm sai phải về 0.
5. Lặp lại đến lần thứ 5 rồi chờ đủ 15 phút: đăng nhập đúng phải thành công và trạng thái khóa tự động được xóa.

## 4. Kết quả đạt

- Danh mục hành chính: 34 tỉnh/thành phố, 3.321 xã/phường/đặc khu.
- Không chấp nhận cặp tỉnh–xã không hợp lệ.
- Khóa đúng ở lần sai thứ 5, kéo dài 15 phút, có thể mở khóa từ quản trị.
