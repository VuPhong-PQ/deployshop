# 📱 Camera Barcode Scanner cho POS

## 🚀 Tính năng mới: Quét mã vạch bằng camera điện thoại

Hệ thống POS đã được tích hợp tính năng quét mã vạch bằng camera điện thoại/webcam, không cần thiết bị quét chuyên dụng.

## ✨ Tính năng

### Camera Scanner
- **Multi-format support**: Code 128, EAN-13, EAN-8, UPC-A, UPC-E, QR Code, Data Matrix
- **Auto-detection**: Tự động nhận diện và quét mã vạch khi đưa vào khung hình
- **Mobile optimized**: Tối ưu cho điện thoại với camera sau
- **Flash support**: Hỗ trợ đèn flash cho môi trường thiếu sáng
- **Multiple cameras**: Chuyển đổi giữa camera trước/sau
- **Real-time scanning**: Quét liên tục không cần nhấn nút

### UI Features
- **Responsive design**: Hoạt động mượt trên mobile và desktop
- **Visual feedback**: Khung hình quét với animation
- **Error handling**: Xử lý lỗi camera và permissions
- **Auto-focus**: Tự động focus vào input barcode sau khi quét

## 🎯 Cách sử dụng

### Trên trang Bán hàng (Sales)

1. **Mở camera scanner**:
   - Nhấn icon 📷 bên cạnh ô nhập mã vạch
   - Hoặc sử dụng input barcode thủ công như trước

2. **Cấp quyền camera**:
   - Trình duyệt sẽ yêu cầu quyền truy cập camera
   - Chọn "Allow" để sử dụng camera

3. **Quét mã vạch**:
   - Đưa mã vạch vào khung hình trắng
   - Hệ thống tự động nhận diện và thêm sản phẩm vào giỏ hàng
   - Âm thanh thông báo khi quét thành công

4. **Điều khiển camera**:
   - 🔦 Bật/tắt đèn flash (nếu có)
   - 📷 Chuyển đổi camera trước/sau
   - ❌ Đóng camera scanner

## 📱 Hỗ trợ thiết bị

### Mobile Browsers
- ✅ **Chrome Mobile** (Android/iOS)
- ✅ **Safari Mobile** (iOS)
- ✅ **Firefox Mobile** (Android)
- ✅ **Samsung Internet** (Android)

### Desktop Browsers
- ✅ **Chrome Desktop** (Windows/Mac/Linux)
- ✅ **Edge Desktop** (Windows/Mac)
- ✅ **Firefox Desktop** (Windows/Mac/Linux)
- ✅ **Safari Desktop** (Mac)

### Thiết bị khuyến nghị
- **Smartphone** với camera sau độ phân giải cao
- **Tablet** với camera tốt
- **Laptop/Desktop** với webcam (HD trở lên)

## 🔧 Troubleshooting

### Lỗi không truy cập được camera
**Nguyên nhân**: Trình duyệt chưa được cấp quyền camera
**Giải pháp**:
1. Kiểm tra biểu tượng camera trong address bar
2. Nhấn vào biểu tượng và chọn "Allow"
3. Refresh trang và thử lại

### Camera không hiển thị
**Nguyên nhân**: Camera đang được sử dụng bởi app khác
**Giải pháp**:
1. Đóng các app camera khác
2. Restart browser
3. Thử chuyển đổi camera

### Không quét được mã vạch
**Nguyên nhân**: Ánh sáng kém hoặc mã vạch không rõ
**Giải pháp**:
1. Bật đèn flash
2. Di chuyển gần hơn với mã vạch
3. Đảm bảo mã vạch không bị mờ/hỏng
4. Thử góc quét khác

### Quét chậm hoặc không ổn định
**Nguyên nhân**: Thiết bị cấu hình thấp hoặc mạng chậm
**Giải pháp**:
1. Đóng các tab/app khác
2. Sử dụng camera chất lượng tốt hơn
3. Kiểm tra kết nối mạng

## 🚀 Performance Tips

### Cho Mobile
- Sử dụng camera sau (environment) để quét tốt hơn
- Bật đèn flash trong môi trường thiếu sáng
- Giữ điện thoại ổn định khi quét
- Khoảng cách tối ưu: 10-20cm từ mã vạch

### Cho Desktop
- Sử dụng webcam chất lượng cao
- Đảm bảo ánh sáng đủ
- Tránh phản chiếu từ màn hình

## 🔒 Privacy & Security

- Camera chỉ được sử dụng trong phiên quét
- Không lưu trữ hình ảnh hoặc video
- Tự động đóng camera khi đóng scanner
- Hoạt động hoàn toàn offline (không upload dữ liệu)

## 📋 Supported Barcode Formats

### 1D Barcodes
- **EAN-13**: Mã vạch phổ biến trên sản phẩm tiêu dùng
- **EAN-8**: Phiên bản ngắn của EAN-13
- **UPC-A**: Mã vạch Mỹ chuẩn
- **UPC-E**: Phiên bản ngắn của UPC-A
- **Code 128**: Mã vạch công nghiệp
- **Code 39**: Mã vạch cho logistics
- **ITF**: Interleaved 2 of 5

### 2D Barcodes
- **QR Code**: Mã QR phổ biến
- **Data Matrix**: Mã vuông nhỏ gọn
- **PDF417**: Mã 2D cho documents

## 🎮 Demo & Testing

Để test tính năng:

1. Mở trang **Sales** (`/sales`)
2. Nhấn icon camera 📷 bên cạnh ô barcode
3. Thử quét mã vạch từ:
   - Sản phẩm có sẵn
   - QR code generator online
   - Barcode test images

## 🔄 Integration với hệ thống

Camera scanner tích hợp hoàn toàn với logic barcode hiện có:
- Sử dụng cùng hàm `handleBarcodeSubmit()`
- Hỗ trợ tìm kiếm fuzzy matching
- Auto-refresh products nếu không tìm thấy
- Thông báo lỗi và gợi ý tạo sản phẩm mới
- Play notification sound khi quét thành công

## 📞 Support

Nếu gặp vấn đề với camera scanner:
1. Kiểm tra console browser (F12) để xem log
2. Test với thiết bị/browser khác
3. Đảm bảo https://localhost cho camera permissions
4. Fallback về input barcode thủ công nếu cần