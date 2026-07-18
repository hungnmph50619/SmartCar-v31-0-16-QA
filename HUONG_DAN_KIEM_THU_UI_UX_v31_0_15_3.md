# Hướng dẫn kiểm thử UI/UX SmartCar v31.0.15.3

## A. Kích thước màn hình

Kiểm tra lần lượt bằng DevTools: 375×812, 430×932, 768×1024, 1024×768, 1366×768 và 1920×1080. Ở mỗi kích thước, không được có thanh cuộn ngang toàn trang, chữ hoặc nút chồng nhau, nút chính bị khuất.

## B. Thanh điều hướng

- Ở 1024×768, navbar phải thu thành menu thay vì dồn tất cả mục trên một dòng.
- Dùng Tab để chọn nút Menu, nhấn Enter/Space để mở.
- Mỗi mục menu mobile phải có vùng bấm đủ cao và không chồng lên mục khác.

## C. Đăng nhập và đăng ký

- Tất cả trường có label luôn nhìn thấy.
- Trình duyệt nhận đúng autocomplete username, email, phone và password.
- Nút Hiện/Ẩn không gửi form.
- Bật Caps Lock khi nhập mật khẩu phải có cảnh báo.
- Liên kết Điều khoản/Chính sách mở đúng trang mới.
- Gửi form sai phải giữ dữ liệu thường nhưng không được điền lại mật khẩu vào HTML.

## D. Đặt xe

- Liên kết điều khoản và hoàn tiền không được 404.
- Tiền tính bằng JavaScript hiển thị theo vi-VN.
- Bấm gửi thiếu trường phải cuộn/focus tới lỗi và có thông báo cụ thể.
- Bấm gửi liên tục không được tạo nhiều đơn.

## E. Dashboard đối tác

- Bấm Tất cả/Đang hoạt động/Tạm khóa/Bảo dưỡng/Ngừng cho thuê phải lọc đúng.
- Trạng thái không có xe hiện “Không có xe ở trạng thái đã chọn”.
- Hồ sơ xe chờ duyệt xuất hiện ở phần “Hồ sơ xe đã gửi duyệt”.
- Bấm Ngừng cho thuê phải có xác nhận; chọn Hủy không được đổi trạng thái.

## F. Nhân viên

- Bấm Từ chối hoặc Yêu cầu bổ sung khi chưa nhập lý do: không gửi form, focus vào ô lý do.
- Ảnh CCCD/GPLX/chân dung mở tab mới và có mô tả ảnh.
- Các thao tác Xóa/Khóa/Từ chối/Ngừng/Thu hồi trong khu vực Admin/Staff phải có xác nhận.

## G. Route và trang lỗi

- `/Home/Index` chuyển về `/Default/Index`.
- `/Home/Privacy` chuyển về `/Policy/Privacy`.
- `/Policy/Index` chuyển về `/Policy/OperatingRules`.
- Tạo một lỗi thử nghiệm an toàn; trang lỗi phải là tiếng Việt và không lộ stack trace.

## H. Bàn phím và zoom

- Dùng chỉ bàn phím để đăng nhập, đăng ký, mở menu, lọc xe và gửi form.
- Zoom 200%: nội dung vẫn đọc và thao tác được.
- Cookie dialog giữ focus bên trong; Escape đóng với lựa chọn cookie cần thiết.
