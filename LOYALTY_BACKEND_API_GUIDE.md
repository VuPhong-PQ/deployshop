# HỆ THỐNG TÍCH ĐIỂM & NÂNG HẠNG TỰ ĐỘNG

## Tổng quan
Hệ thống tích điểm được tích hợp vào backend để tự động:
- Tính điểm cho khách hàng khi mua hàng
- Nâng hạng khách hàng tự động khi đủ điều kiện  
- Tạo thông báo khi nâng hạng
- Quản lý cài đặt tích điểm và hạng khách hàng

## API Endpoints

### 1. Quản lý Cài đặt Tích điểm

#### GET /api/LoyaltySettings
Lấy toàn bộ cài đặt tích điểm và danh sách hạng khách hàng.

**Response:**
```json
{
  "config": {
    "isEnabled": true,
    "pointsPerCurrency": 1000,
    "minOrderAmountForPoints": 50000,
    "pointExpiryDays": 365,
    "happyHourEnabled": false,
    "weekendBonusEnabled": false,
    "birthdayBonusEnabled": false
  },
  "tiers": [
    {
      "tierId": 1,
      "tierName": "Đồng",
      "minSpent": 0,
      "minPoints": 0,
      "pointsMultiplier": 1.0,
      "discountPercentage": 0,
      "tierColor": "#CD7F32"
    }
  ]
}
```

#### PUT /api/LoyaltySettings
Cập nhật cài đặt tích điểm và hạng khách hàng.

**Request Body:** Cùng format như GET response

### 2. Thông tin Khách hàng

#### GET /api/LoyaltySettings/customer-status/{customerId}
Lấy thông tin tích điểm và hạng của khách hàng.

**Response:**
```json
{
  "customerId": 1,
  "customerName": "Nguyễn Văn A",
  "totalSpent": 5500000,
  "totalPoints": 550,
  "currentTier": {
    "tierName": "Bạc",
    "discountPercentage": 2,
    "pointsMultiplier": 1.2
  },
  "nextTier": {
    "tierName": "Vàng",
    "minSpent": 20000000,
    "minPoints": 2000
  },
  "progress": {
    "spentToNext": 14500000,
    "pointsToNext": 1450,
    "progressPercentage": 27.5
  }
}
```

### 3. Tính toán Điểm

#### POST /api/LoyaltySettings/calculate-points
Tính điểm cho một đơn hàng.

**Request:**
```json
{
  "amount": 100000,
  "customerId": 1
}
```

**Response:**
```json
{
  "points": 120,
  "basePoints": 100,
  "multiplier": 1.2,
  "bonusInfo": ["Hạng Bạc x1.2"],
  "formula": "100,000 ÷ 1,000 × 1.2 = 120 điểm"
}
```

### 4. Quản trị Hệ thống

#### POST /api/LoyaltySettings/update-all-tiers
Cập nhật hạng cho tất cả khách hàng trong hệ thống.

## Cấu hình Hạng Khách hàng Mặc định

### Hạng Đồng
- **Chi tiêu tối thiểu:** 0 VNĐ
- **Điểm tối thiểu:** 0 điểm
- **Hệ số điểm:** 1.0x
- **Giảm giá:** 0%

### Hạng Bạc  
- **Chi tiêu tối thiểu:** 5,000,000 VNĐ
- **Điểm tối thiểu:** 500 điểm
- **Hệ số điểm:** 1.2x
- **Giảm giá:** 2%

### Hạng Vàng
- **Chi tiêu tối thiểu:** 20,000,000 VNĐ
- **Điểm tối thiểu:** 2,000 điểm  
- **Hệ số điểm:** 1.5x
- **Giảm giá:** 5%

### Hạng Kim cương
- **Chi tiêu tối thiểu:** 50,000,000 VNĐ
- **Điểm tối thiểu:** 5,000 điểm
- **Hệ số điểm:** 2.0x
- **Giảm giá:** 10%

## Logic Nâng Hạng Tự động

Hệ thống tự động kiểm tra và nâng hạng khách hàng khi:

1. **Khi tạo đơn hàng mới** (status = pending)
2. **Khi hoàn thành đơn hàng** (status = completed)
3. **Khi cập nhật trạng thái đơn hàng**

### Điều kiện nâng hạng:
- Khách hàng phải đạt **CẢ HAI** điều kiện: chi tiêu tối thiểu VÀ điểm tối thiểu
- Hệ thống chọn hạng cao nhất mà khách hàng đủ điều kiện
- Tự động tạo thông báo khi nâng hạng

### Tính điểm:
```
Điểm cơ bản = Số tiền đơn hàng ÷ PointsPerCurrency
Điểm cuối cùng = Điểm cơ bản × Hệ số hạng khách hàng × Bonus khác
```

### Bonus điểm:
- **Happy Hour:** Nhân x2.0 (có thể cấu hình)
- **Cuối tuần:** Nhân x1.5 (có thể cấu hình)  
- **Sinh nhật:** Nhân x3.0 trong vòng 7 ngày (có thể cấu hình)

## Tích hợp với Đơn hàng

### Khi tạo đơn hàng mới:
1. Tự động tính điểm dự kiến
2. Chạy background task tích điểm
3. Cập nhật hạng khách hàng nếu cần

### Khi hoàn thành đơn hàng:
1. Xác nhận tích điểm cho khách hàng
2. Cập nhật tổng chi tiêu
3. Kiểm tra nâng hạng
4. Tạo thông báo nâng hạng

### Khi hủy/hoàn trả đơn hàng:
1. Trừ lại điểm đã tích
2. Cập nhật lại tổng chi tiêu
3. Hạ hạng khách hàng nếu cần

## Thông báo Nâng hạng

Khi khách hàng được nâng hạng, hệ thống tự động tạo thông báo:
```json
{
  "title": "🎉 Chúc mừng bạn đã được nâng hạng!",
  "message": "Chúc mừng bạn đã được nâng hạng từ Bạc lên Vàng! Bạn sẽ được hưởng 5% giảm giá và nhận thêm 1.5x điểm thưởng.",
  "type": "tier_upgrade"
}
```

## Database Tables

### LoyaltyConfigs
Cấu hình hệ thống tích điểm.

### CustomerTiers  
Định nghĩa các hạng khách hàng.

### LoyaltyTransactions
Lịch sử giao dịch điểm của khách hàng.

### Customers
- `TotalSpent`: Tổng chi tiêu
- `LoyaltyPoints`: Điểm hiện tại
- `TierId`: Hạng hiện tại

## Testing

Chạy script test:
```powershell
cd c:\shop
.\test-loyalty-api.ps1
```

Script sẽ kiểm tra:
- API endpoints hoạt động
- Logic tính điểm
- Thông tin khách hàng
- Hệ thống thông báo

## Troubleshooting

### Lỗi thường gặp:

1. **Khách hàng không được tích điểm:**
   - Kiểm tra đơn hàng có CustomerId không
   - Kiểm tra trạng thái đơn hàng = "completed"
   - Kiểm tra cấu hình IsEnabled = true

2. **Không nâng hạng tự động:**
   - Kiểm tra điều kiện MinSpent và MinPoints
   - Chạy POST /api/LoyaltySettings/update-all-tiers

3. **Tính điểm sai:**
   - Kiểm tra cấu hình PointsPerCurrency
   - Kiểm tra hệ số hạng khách hàng
   - Kiểm tra các bonus được kích hoạt