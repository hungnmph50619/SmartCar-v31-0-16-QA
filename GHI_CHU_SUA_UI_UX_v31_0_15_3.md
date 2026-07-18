# SmartCar v31.0.15.3 — sửa UI/UX ưu tiên cao

## Đã sửa

- Liên kết điều khoản đặt xe và chính sách hoàn tiền mở đúng trang; thêm route tương thích `/Policy/Index`.
- Navbar chuyển sang `navbar-expand-xl`, tránh tràn ở tablet và tăng vùng bấm menu mobile.
- Đăng nhập/đăng ký có label, autocomplete, nút hiện/ẩn mật khẩu, cảnh báo Caps Lock và liên kết điều khoản/bảo mật.
- Không điền lại mật khẩu vào HTML khi form đăng ký báo lỗi.
- Bộ lọc trạng thái xe trên dashboard đối tác hoạt động thật; bổ sung danh sách hồ sơ xe đã gửi duyệt.
- Thao tác ngừng cho thuê có xác nhận.
- Nhân viên không thể từ chối/yêu cầu bổ sung khi chưa nhập lý do.
- Ảnh giấy tờ có alt và liên kết mở tab mới có `rel=noopener noreferrer`.
- Trang Home mẫu chuyển hướng về trang thật; trang lỗi được Việt hóa.
- Metadata template được thay bằng SmartCar/nhóm SD-46.
- Cookie banner có role dialog, quản lý focus và hỗ trợ Escape.
- Đồng bộ phiên bản mã nguồn thành 31.0.15.3.

## Kiểm thử nhanh UI/UX

1. Mở ở 375×812, 768×1024, 1024×768 và 1366×768. Navbar không tràn; menu mobile mở/đóng được.
2. Đăng nhập: dùng Tab đi qua toàn bộ form, thử nút Hiện/Ẩn, bật Caps Lock.
3. Đăng ký: bấm liên kết Điều khoản và Chính sách; xác nhận mở đúng tab, không mất dữ liệu form.
4. Đặt xe: mở điều khoản và chính sách hoàn tiền; giá JavaScript hiển thị theo định dạng Việt Nam.
5. Dashboard đối tác: bấm từng bộ lọc; danh sách chỉ còn đúng trạng thái; chọn trạng thái không có xe phải hiện thông báo trống.
6. Bấm Ngừng cho thuê rồi chọn Hủy trong hộp xác nhận; trạng thái không được thay đổi.
7. Staff duyệt hồ sơ: bấm Yêu cầu bổ sung hoặc Từ chối khi lý do trống; form phải giữ nguyên và focus vào ô lý do.
8. Mở `/Home/Index`, `/Home/Privacy`, `/Policy/Index`; phải chuyển tới trang SmartCar hợp lệ, không còn scaffold/404.
9. Xóa localStorage `smartcar_cookie_consent`, tải lại trang; dùng Tab, Shift+Tab và Escape trong cookie dialog.
