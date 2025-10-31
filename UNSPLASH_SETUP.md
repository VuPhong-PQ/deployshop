# Hướng dẫn cài đặt Unsplash API để sử dụng tính năng AI tìm hình ảnh

## 🚀 Tính năng AI Image Search

Hệ thống đã được tích hợp tính năng AI tự động tìm và tải hình ảnh cho sản phẩm dựa trên tên sản phẩm. Khi bạn thêm hoặc sửa sản phẩm, hệ thống sẽ tự động tìm hình ảnh phù hợp từ Unsplash.

## 📝 Cách lấy Unsplash Access Key

### Bước 1: Đăng ký tài khoản Unsplash Developer
1. Truy cập: https://unsplash.com/developers
2. Đăng ký hoặc đăng nhập tài khoản Unsplash
3. Chấp nhận điều khoản sử dụng API

### Bước 2: Tạo ứng dụng mới
1. Nhấp vào "New Application"
2. Điền thông tin ứng dụng:
   - **Application name**: RetailPoint POS System
   - **Description**: AI image search for retail products
   - **Website URL**: http://localhost:5173 (hoặc domain của bạn)
3. Chấp nhận điều khoản và tạo ứng dụng

### Bước 3: Lấy Access Key
1. Sau khi tạo ứng dụng thành công, bạn sẽ thấy:
   - **Access Key** (Public key)
   - **Secret Key** (Private key)
2. **Sao chép Access Key** (không phải Secret Key)

## ⚙️ Cấu hình hệ thống

### Cách 1: Cập nhật appsettings.json (Khuyến nghị)
Mở file `Backend/RetailPointBackend/appsettings.json` và thay thế:

```json
{
  "UnsplashAccessKey": "YOUR_UNSPLASH_ACCESS_KEY_HERE"
}
```

Thành:

```json
{
  "UnsplashAccessKey": "access_key_bạn_vừa_sao_chép"
}
```

### Cách 2: Sử dụng Environment Variables (Bảo mật cao hơn)
Thêm vào biến môi trường:
```
UNSPLASH_ACCESS_KEY=access_key_của_bạn
```

## 🔄 Khởi động lại hệ thống

Sau khi cấu hình Access Key:

1. **Dừng backend** (Ctrl+C trong terminal dotnet)
2. **Khởi động lại backend**:
   ```bash
   cd Backend/RetailPointBackend
   dotnet run
   ```

## ✅ Kiểm tra hoạt động

1. Mở trang Products trong frontend
2. Thêm sản phẩm mới
3. Nhập tên sản phẩm (ví dụ: "Xà phòng")
4. Nhấn nút **"🤖 Tìm ảnh AI"**
5. Hệ thống sẽ tự động tìm và tải hình ảnh phù hợp

## 🔍 Troubleshooting

### Lỗi 401 Unauthorized
- Kiểm tra Access Key đã được cấu hình đúng chưa
- Đảm bảo không sử dụng Secret Key (chỉ dùng Access Key)
- Khởi động lại backend sau khi cấu hình

### Lỗi Rate Limit (429)
- Unsplash miễn phí cho phép 50 requests/hour
- Nếu cần nhiều hơn, hãy đăng ký gói trả phí

### Không tìm thấy hình ảnh
- Thử tên sản phẩm bằng tiếng Anh
- Hoặc thử tên khác có ý nghĩa rõ ràng

## 🎯 Tính năng bổ sung

### Auto Image khi tạo sản phẩm
Bạn có thể bật tính năng tự động tìm hình ảnh khi tạo sản phẩm mới bằng cách thêm tham số `autoImage=true` trong request.

### Multiple Image Search
Sử dụng endpoint `/api/products/search-images` để lấy nhiều hình ảnh cùng lúc và cho phép người dùng chọn.

## 📊 Giới hạn sử dụng

- **Miễn phí**: 50 requests/hour
- **Plus ($9/tháng)**: 5,000 requests/hour  
- **Enterprise**: Unlimited

## 🔐 Bảo mật

**Lưu ý quan trọng**: 
- Không bao giờ chia sẻ Access Key
- Không commit Access Key vào source code
- Sử dụng environment variables cho production