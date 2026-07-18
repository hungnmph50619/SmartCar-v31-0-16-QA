# SmartCar v31.0.16 — địa chỉ hành chính và bảo vệ đăng nhập

## Nội dung đã sửa

- Hồ sơ đối tác không còn danh sách cứng 5 tỉnh/thành phố.
- Giao diện tải đủ 34 tỉnh/thành phố và 3.321 xã/phường/đặc khu đang hoạt động từ database.
- Khi đổi tỉnh/thành phố, danh sách xã/phường/đặc khu được tải lại theo đúng mã tỉnh.
- API xác minh cặp mã tỉnh–xã, lấy tên chuẩn từ database và từ chối dữ liệu bị sửa sai trên trình duyệt.
- Hồ sơ đối tác lưu cả mã tỉnh và mã xã cho địa chỉ thường trú, hiện tại hoặc trụ sở.
- Sai mật khẩu 5 lần liên tiếp sẽ khóa đăng nhập 15 phút; ngay lần thứ 5 trả về thông báo thời điểm được thử lại.
- Mở khóa tài khoản trong trang quản trị đặt lại số lần đăng nhập sai.
- Có audit log khi hệ thống tự khóa tài khoản do sai mật khẩu.

## Cập nhật database

- Cài mới/test sạch: chạy `SmartCar_FULL_ONE_CLICK_RESET_INSTALL_v31_0_16.sql`.
- Giữ dữ liệu đang có từ v31.0.15.3: sao lưu rồi chạy `SmartCar_PATCH_DIA_CHI_DANG_NHAP_v31_0_16.sql`.
- Nếu patch báo danh mục không đủ 34/3.321 thì database cũ chưa đồng bộ dữ liệu hành chính; dùng file FULL ONE CLICK trên môi trường test.

## Lưu ý build

Dùng .NET SDK theo `global.json`, sau đó chạy `KIEM_TRA_BUILD_SMARTCAR.bat` trước khi demo.
