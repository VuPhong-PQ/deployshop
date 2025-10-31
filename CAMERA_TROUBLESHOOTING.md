# 🔧 Hướng dẫn khắc phục lỗi Camera khi quét mã vạch

## 🚨 Vấn đề: "Không thể truy cập camera"

Đây là lỗi phổ biến khi sử dụng tính năng quét mã vạch bằng camera trên điện thoại/máy tính. Dưới đây là các cách khắc phục:

## ✅ Giải pháp theo từng trình duyệt

### 📱 Chrome Mobile (Android/iOS)
1. **Kiểm tra URL**: Đảm bảo website sử dụng `https://` (không phải `http://`)
2. **Cấp quyền camera**:
   - Nhấn vào biểu tượng khóa 🔒 bên trái thanh địa chỉ
   - Chọn "Camera" → "Allow" (Cho phép)
   - Làm mới trang (swipe down để refresh)

### 🍎 Safari Mobile (iOS)
1. **Settings → Safari → Camera**: Chọn "Allow"
2. **Trong app Safari**:
   - Nhấn vào biểu tượng "AA" bên trái thanh địa chỉ
   - Chọn "Website Settings"
   - Bật "Camera"

### 🦊 Firefox Mobile
1. Nhấn vào biểu tượng shield 🛡️ bên trái thanh địa chỉ
2. Chọn "Camera" → "Allow"
3. Refresh trang

### 💻 Desktop Browsers
- **Chrome/Edge**: Nhấn vào icon camera 📷 trong address bar → Allow
- **Firefox**: Nhấn vào shield icon → Camera permissions → Allow
- **Safari**: Safari → Preferences → Websites → Camera → Allow

## 🔍 Kiểm tra nhanh

### ✅ Yêu cầu bắt buộc
- [ ] Website phải dùng HTTPS (hoặc localhost)
- [ ] Trình duyệt hỗ trợ Camera API
- [ ] Camera không bị ứng dụng khác sử dụng
- [ ] Quyền camera đã được cấp

### 🔧 Công cụ debug
Nhấn **F12** → Console để xem thông báo lỗi chi tiết:
- `NotAllowedError`: Chưa cấp quyền camera
- `NotFoundError`: Không tìm thấy camera
- `NotReadableError`: Camera đang được sử dụng
- `OverConstrainedError`: Cấu hình camera không hỗ trợ

## 🛠️ Troubleshooting

### 🔴 Vẫn không hoạt động?
1. **Đóng các app camera khác** (Zoom, Skype, Teams, v.v.)
2. **Khởi động lại trình duyệt**
3. **Thử trình duyệt khác**
4. **Kiểm tra Antivirus** có chặn camera không
5. **Test camera** với app khác trước

### 📱 Dành cho Mobile
- Sử dụng **camera sau** (environment) để quét tốt hơn
- Bật **đèn flash** nếu thiếu sáng
- Giữ điện thoại **ổn định** khi quét
- Khoảng cách tối ưu: **10-20cm** từ mã vạch

### 💡 Giải pháp thay thế
Nếu camera vẫn không hoạt động:

1. **Nhập thủ công**: Nhấn nút "⌨️ Nhập thủ công" 
2. **Máy quét USB**: Sử dụng máy quét mã vạch chuyên dụng
3. **Keyboard barcode scanner**: Máy quét tự động nhập như bàn phím

## 🎯 Các tính năng đã cải thiện

### ✨ Error Handling tốt hơn
- Thông báo lỗi chi tiết theo từng nguyên nhân
- Hướng dẫn khắc phục cụ thể cho từng trình duyệt
- Auto-retry với cấu hình camera khác nhau

### 🔄 Retry Logic
- Tự động thử lại với cấu hình camera đơn giản hơn
- Fallback từ HD → Standard → Basic quality
- Chuyển từ back camera → front camera → any camera

### 📱 Mobile-Friendly
- Responsive design cho điện thoại
- Touch-friendly controls
- Optimized scanning area cho màn hình nhỏ

### 🆘 Fallback Options
- **Manual Input Dialog**: UI đẹp thay vì prompt()
- **Sample barcodes**: Để test nhanh
- **Permission Guide**: Hướng dẫn chi tiết từng browser

## 🎮 Test & Demo

### Để test tính năng camera:
1. Vào trang **Bán hàng** (`/sales`) hoặc **Kho hàng** (`/inventory`)
2. Nhấn icon camera 📷 bên cạnh ô tìm kiếm
3. Cấp quyền camera khi trình duyệt hỏi
4. Quét mã vạch từ sản phẩm hoặc QR code online

### Mã vạch test:
- `8901030805091` (Coca Cola)
- `8901030750045` (Pepsi) 
- `1234567890123` (Example)

## 📞 Hỗ trợ

Nếu vẫn gặp vấn đề:
1. Kiểm tra console (F12) để xem log chi tiết
2. Chụp màn hình thông báo lỗi
3. Thử với thiết bị/browser khác
4. Sử dụng manual input làm giải pháp tạm thời

---

**💡 Lưu ý**: Camera scanner hoạt động tốt nhất trên:
- Chrome Mobile (Android/iOS)
- Safari Mobile (iOS) 
- Chrome Desktop với webcam HD
- Môi trường HTTPS với ánh sáng đủ