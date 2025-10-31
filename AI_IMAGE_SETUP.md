# Hướng dẫn cài đặt AI Image Search

Tính năng AI Image Search cho phép hệ thống tự động tìm kiếm và tải hình ảnh phù hợp cho sản phẩm dựa trên tên sản phẩm.

## Cách lấy Unsplash Access Key

1. Truy cập [Unsplash Developers](https://unsplash.com/developers)
2. Đăng ký tài khoản hoặc đăng nhập
3. Tạo một ứng dụng mới (New Application)
4. Điền thông tin ứng dụng:
   - Application name: `RetailPoint Image Search`
   - Description: `AI image search for retail products`
5. Chấp nhận các điều khoản sử dụng
6. Sao chép `Access Key` từ trang ứng dụng

## Cài đặt

1. Mở file `Backend/RetailPointBackend/appsettings.json`
2. Thay thế `YOUR_UNSPLASH_ACCESS_KEY_HERE` bằng Access Key thực tế:

```json
{
  "UnsplashAccessKey": "your-actual-access-key-here"
}
```

3. Khởi động lại backend server

## Cách sử dụng

1. Khi thêm hoặc sửa sản phẩm, nhập tên sản phẩm
2. Nhấn nút "🤖 AI Tìm ảnh" 
3. Hệ thống sẽ tự động tìm kiếm và tải hình ảnh phù hợp từ Unsplash
4. Hình ảnh sẽ được lưu vào thư mục `wwwroot/uploads` và hiển thị trong form

## Lưu ý

- API miễn phí của Unsplash có giới hạn 50 requests/hour
- Hình ảnh được tìm kiếm dựa trên tên sản phẩm, nên đặt tên sản phẩm rõ ràng
- Nếu không tìm thấy hình ảnh phù hợp, có thể upload hình ảnh thủ công
- Hình ảnh tải về sẽ có kích thước phù hợp cho hiển thị sản phẩm

## Troubleshooting

- **Lỗi "Unsplash access key not configured"**: Kiểm tra lại Access Key trong appsettings.json
- **Lỗi "No image found"**: Thử đổi tên sản phẩm rõ ràng hơn
- **Lỗi "Failed to download image"**: Kiểm tra kết nối internet và quyền ghi file

## API Endpoints

- `POST /api/products/search-image`: Tìm và tải 1 hình ảnh
- `POST /api/products/search-images`: Tìm nhiều hình ảnh (để người dùng chọn)

Body request:
```json
{
  "productName": "Tên sản phẩm",
  "limit": 5  // optional, chỉ cho search-images
}
```