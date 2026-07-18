# SmartCar v31.0.15.2 - sửa 3 lỗi trước demo

1. Đồng bộ `quantri`, `nhanvien`, `doitac`, `khachhang` về mật khẩu `a12345678` và bật `IsActive=1`.
2. Sửa Đồng Nai mã `75` từ `Thành phố` thành `Tỉnh`; thêm kiểm tra 34 đơn vị cấp tỉnh = 28 tỉnh + 6 thành phố.
3. Bổ sung `RESET_HO_SO_KHACHHANG01_DE_DEMO.sql`; ưu tiên `khachhang01`, nếu không có thì tự dùng `khachhang`.
4. Cập nhật cảnh báo trong `KIEM_TRA_DEMO_15_PHUT.ps1`.
5. Có thêm script vá database đang cài, không xóa dữ liệu: `SmartCar_PATCH_FIX_3_LOI_v31_0_15_2.sql`.
