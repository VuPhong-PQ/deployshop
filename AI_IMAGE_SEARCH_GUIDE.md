# Hướng dẫn cài đặt tính năng AI Image Search

## Tổng quan
Tính năng AI Image Search tự động tìm kiếm và tải hình ảnh phù hợp cho sản phẩm dựa trên tên sản phẩm, sử dụng API Unsplash.

## Cài đặt

### 1. Lấy Unsplash Access Key

1. Truy cập [Unsplash Developers](https://unsplash.com/developers)
2. Đăng ký tài khoản hoặc đăng nhập
3. Tạo một ứng dụng mới:
   - Click "New Application"
   - Chấp nhận điều khoản sử dụng
   - Điền thông tin ứng dụng:
     - Application name: "RetailPoint Image Search"
     - Description: "Tự động tìm hình ảnh cho sản phẩm"
4. Sau khi tạo xong, copy "Access Key"

### 2. Cấu hình Backend

Mở file `Backend/RetailPointBackend/appsettings.json` và thay thế:
```json
{
  "UnsplashAccessKey": "YOUR_UNSPLASH_ACCESS_KEY_HERE"
}
```

Thay `YOUR_UNSPLASH_ACCESS_KEY_HERE` bằng Access Key vừa lấy từ Unsplash.

### 3. Cấu hình Development (tuỳ chọn)

Để bảo mật key trong môi trường development, tạo file `appsettings.Development.json`:
```json
{
  "UnsplashAccessKey": "your_actual_key_here"
}
```

## Cách sử dụng

### 1. Tự động tìm hình ảnh khi thêm sản phẩm

1. Mở trang "Sản phẩm"
2. Click "Thêm sản phẩm"
3. Nhập tên sản phẩm (ví dụ: "Áo thun nam")
4. Trong phần "Hình ảnh", click nút "🤖 AI Tìm ảnh"
5. Hệ thống sẽ tự động tìm và tải hình ảnh phù hợp

### 2. Tìm nhiều hình ảnh để chọn

1. Click "🔍 Xem thêm ảnh"
2. Chọn hình ảnh phù hợp từ danh sách
3. Hình ảnh được chọn sẽ tự động upload vào hệ thống

## API Endpoints mới

### 1. Tìm và tải một hình ảnh
```
POST /api/products/search-image
Content-Type: application/json

{
  "productName": "Áo thun nam"
}

Response:
{
  "imageUrl": "/uploads/product_uuid.jpg"
}
```

### 2. Tìm nhiều hình ảnh
```
POST /api/products/search-images
Content-Type: application/json

{
  "productName": "Áo thun nam",
  "limit": 5
}

Response:
{
  "images": [
    "https://images.unsplash.com/photo-1...",
    "https://images.unsplash.com/photo-2...",
    ...
  ]
}
```

## Tính năng

### ✅ Đã triển khai
- Tự động tìm kiếm hình ảnh dựa trên tên sản phẩm
- Tải và lưu hình ảnh vào thư mục uploads
- Giao diện người dùng thân thiện
- Xử lý lỗi và thông báo trạng thái
- API endpoints cho cả tìm một ảnh và nhiều ảnh
- Tích hợp với form thêm/sửa sản phẩm

### 🚀 Tính năng tương lai (có thể mở rộng)
- Tích hợp với nhiều nguồn ảnh (Pexels, Google Images)
- Tự động resize và tối ưu ảnh
- AI phân loại và gợi ý ảnh theo danh mục
- Lưu cache ảnh để tăng tốc độ
- Batch processing cho import Excel

## Troubleshooting

### Lỗi "Không tìm thấy hình ảnh"
- Kiểm tra Unsplash Access Key
- Thử tên sản phẩm khác (bằng tiếng Anh tốt hơn)
- Kiểm tra kết nối internet

### Lỗi "Upload ảnh thất bại"
- Kiểm tra quyền ghi thư mục `wwwroot/uploads`
- Kiểm tra dung lượng ổ cứng
- Kiểm tra log server để xem chi tiết lỗi

### Lỗi API
- Kiểm tra Access Key có đúng không
- Kiểm tra giới hạn request của Unsplash (50 requests/hour cho free tier)

## Giới hạn

### Unsplash Free Tier
- 50 requests/hour
- Phải hiển thị credit cho photographer (đã tự động xử lý)
- Chỉ dành cho ứng dụng non-commercial hoặc demo

### Để nâng cấp
- Đăng ký Unsplash+ để có giới hạn cao hơn
- Hoặc tích hợp thêm Pexels API làm backup
- Hoặc sử dụng Google Images API (có phí)

## Bảo mật

- Không commit Access Key vào git
- Sử dụng Environment Variables trong production
- Giới hạn rate limiting để tránh abuse
- Validate input để tránh injection attacks