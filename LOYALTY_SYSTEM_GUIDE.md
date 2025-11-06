# Hướng dẫn Hệ thống Tích điểm Thưởng

## Tổng quan

Hệ thống tích điểm thưởng cho phép khách hàng tích lũy điểm từ các giao dịch mua hàng và sử dụng điểm để nhận ưu đãi. Hệ thống được thiết kế dựa trên các mô hình thành công như KiotViet, PosApp và các hệ thống bán lẻ phổ biến.

## Cấu trúc Hệ thống

### 1. Cấu hình Tích điểm Cơ bản
- **Tỷ lệ tích điểm**: Cấu hình số tiền cần chi để nhận 1 điểm (mặc định: 1.000 VND = 1 điểm)
- **Đơn hàng tối thiểu**: Số tiền tối thiểu để được tích điểm
- **Điểm tối đa/đơn**: Giới hạn số điểm tối đa có thể nhận từ một đơn hàng
- **Hạn sử dụng điểm**: Thời gian điểm có hiệu lực (mặc định: 365 ngày)
- **Giá trị quy đổi**: Tỷ lệ đổi điểm thành tiền (mặc định: 100 điểm = 1.000 VND)

### 2. Cấp độ Khách hàng

#### Cấp Đồng (Mặc định)
- Chi tiêu tối thiểu: 0 VND
- Điểm tối thiểu: 0 điểm
- Hệ số tích điểm: x1.0
- Giảm giá: 0%

#### Cấp Bạc
- Chi tiêu tối thiểu: 5.000.000 VND
- Điểm tối thiểu: 500 điểm
- Hệ số tích điểm: x1.2
- Giảm giá: 2%

#### Cấp Vàng
- Chi tiêu tối thiểu: 20.000.000 VND
- Điểm tối thiểu: 2.000 điểm
- Hệ số tích điểm: x1.5
- Giảm giá: 5%

#### Cấp Kim cương
- Chi tiêu tối thiểu: 50.000.000 VND
- Điểm tối thiểu: 5.000 điểm
- Hệ số tích điểm: x2.0
- Giảm giá: 10%

### 3. Bonus Thời gian

#### Happy Hour (Giờ vàng)
- Thời gian: 17:00 - 19:00 (có thể tùy chỉnh)
- Hệ số nhân: x2.0
- Mục đích: Khuyến khích mua hàng vào giờ thấp điểm

#### Cuối tuần
- Áp dụng: Thứ 7 và Chủ nhật
- Hệ số nhân: x1.5
- Mục đích: Tăng doanh thu cuối tuần

#### Sinh nhật
- Áp dụng: Trong vòng 7 ngày quanh sinh nhật khách hàng
- Hệ số nhân: x3.0
- Mục đích: Tạo trải nghiệm đặc biệt cho khách hàng

## Cách sử dụng

### 1. Cấu hình Hệ thống (Dành cho Admin)

#### Truy cập Settings
1. Vào **Settings** → **Tích điểm**
2. Chọn tab **"Cài đặt chung"**

#### Cấu hình cơ bản
```
- Kích hoạt tích điểm: ON
- Tỷ lệ tích điểm: 1000 VND/điểm
- Đơn hàng tối thiểu: 50.000 VND
- Điểm hết hạn sau: 365 ngày
- Cho phép đổi điểm: ON
- Giá trị điểm: 1000 VND/điểm
- Tối đa đổi điểm: 50% hóa đơn
```

#### Cấu hình Bonus thời gian
1. Chọn tab **"Bonus thời gian"**
2. Bật/tắt các loại bonus:
   - **Happy Hour**: 17:00-19:00, x2.0
   - **Cuối tuần**: x1.5
   - **Sinh nhật**: x3.0, hiệu lực 7 ngày

### 2. Quy trình Tích điểm

#### Khi khách hàng mua hàng
1. **Chọn khách hàng** trong đơn hàng
2. Hệ thống **tự động tính điểm** dựa trên:
   - Tổng tiền hóa đơn
   - Cấp độ khách hàng
   - Bonus thời gian (nếu có)
3. **Hoàn thành đơn hàng** → Điểm được cộng tự động

#### Công thức tính điểm
```
Điểm = (Tổng tiền ÷ Tỷ lệ tích điểm) × Hệ số cấp độ × Bonus thời gian
```

**Ví dụ**: 
- Hóa đơn: 100.000 VND
- Khách VIP (x1.5)
- Mua vào Happy Hour (x2.0)
- Điểm nhận được: (100.000 ÷ 1.000) × 1.5 × 2.0 = 300 điểm

### 3. Sử dụng Điểm

#### Đổi điểm thành tiền
1. Trong **đơn hàng mới**, chọn khách hàng có điểm
2. Nhập **số điểm muốn sử dụng**
3. Hệ thống **tự động trừ** tiền từ tổng hóa đơn
4. **Giới hạn**: Tối đa 50% giá trị hóa đơn

#### Kiểm tra điểm khách hàng
1. Vào **Khách hàng** → Chọn khách hàng
2. Xem **số điểm hiện tại** và **lịch sử giao dịch**

## Tính năng Nâng cao

### 1. Tích điểm theo Danh mục
- Cấu hình **hệ số nhân riêng** cho từng danh mục sản phẩm
- Ví dụ: Thời trang x2.0, Điện tử x1.5

### 2. Tích điểm theo Sản phẩm
- Cấu hình **điểm thưởng đặc biệt** cho sản phẩm cụ thể
- Áp dụng cho **sản phẩm khuyến mãi** hoặc **thanh lý kho**

### 3. Chương trình Khuyến mãi
- Tạo **khuyến mãi có thời hạn**
- Mốc thành tích: Mua từ 5 sản phẩm nhận 100 điểm bonus
- Áp dụng cho **cấp độ khách hàng cụ thể**

## So sánh với Hệ thống Khác

### KiotViet
| Tính năng | KiotViet | Hệ thống của chúng ta |
|-----------|----------|----------------------|
| Tỷ lệ tích điểm | 1.000 VND = 1 điểm | ✓ Tùy chỉnh được |
| Cấp độ KH | ✓ | ✓ 4 cấp độ |
| Happy Hour | ✗ | ✓ |
| Sinh nhật bonus | ✓ | ✓ |
| Tích điểm theo danh mục | ✓ | ✓ |

### PosApp
| Tính năng | PosApp | Hệ thống của chúng ta |
|-----------|--------|----------------------|
| Tỷ lệ tích điểm | 10.000 VND = 1 điểm | ✓ Tùy chỉnh được |
| Đổi điểm | ✓ | ✓ |
| Hạn sử dụng | 1 năm | ✓ Tùy chỉnh |
| Bonus cuối tuần | ✗ | ✓ |

## Lợi ích

### Cho Cửa hàng
- **Tăng doanh thu**: Khuyến khích khách hàng quay lại
- **Dữ liệu khách hàng**: Thu thập thông tin mua sắm
- **Cạnh tranh**: Tạo lợi thế so với đối thủ
- **Loyalty**: Xây dựng lòng trung thành

### Cho Khách hàng
- **Tiết kiệm**: Nhận ưu đãi từ việc mua sắm
- **Đặc quyền**: Được ưu tiên theo cấp độ
- **Trải nghiệm**: Cảm giác được trân trọng
- **Minh bạch**: Theo dõi điểm và ưu đãi rõ ràng

## Troubleshooting

### Khách hàng không nhận được điểm
1. Kiểm tra **cấu hình tích điểm** đã bật chưa
2. Xác nhận **đơn hàng đã hoàn thành**
3. Kiểm tra **đơn hàng tối thiểu**
4. Xem **lịch sử giao dịch** trong hệ thống

### Điểm bị trừ nhầm
1. Vào **Lịch sử giao dịch điểm**
2. Tìm **giao dịch cần điều chỉnh**
3. Sử dụng tính năng **điều chỉnh thủ công**

### Cấp độ khách hàng không cập nhật
1. Hệ thống **tự động cập nhật** sau mỗi đơn hàng
2. Có thể **chạy lại** bằng API evaluate-customer
3. Kiểm tra **điều kiện tối thiểu** của cấp độ

## API Reference

### Tính điểm cho đơn hàng
```
GET /api/LoyaltyConfig/calculate-points?amount=100000&customerId=1
```

### Xử lý tích điểm khi hoàn thành đơn
```
POST /api/LoyaltyTransactions/process-order-points
{
  "orderId": 123,
  "staffId": 1
}
```

### Đổi điểm
```
POST /api/LoyaltyTransactions/redeem-points
{
  "customerId": 1,
  "pointsToRedeem": 100,
  "orderId": 124,
  "staffId": 1
}
```

## Kết luận

Hệ thống tích điểm thưởng đã được thiết kế toàn diện, linh hoạt và dễ sử dụng. Với các tính năng đa dạng và khả năng tùy chỉnh cao, hệ thống sẽ giúp cửa hàng tăng doanh thu và xây dựng lòng trung thành của khách hàng một cách hiệu quả.