# HỆ THỐNG QUẢN LÝ CẤU HÌNH CẤP ĐỘ KHÁCH HÀNG

## Tổng quan

Hệ thống cho phép tùy chỉnh hoàn toàn các thông số cho từng cấp độ khách hàng bao gồm:
- **Chi tiêu tối thiểu** (MinSpent): Số tiền tối thiểu khách hàng phải chi tiêu để đạt hạng
- **Điểm tối thiểu** (MinPoints): Số điểm tích lũy tối thiểu để đạt hạng
- **Hệ số điểm** (PointsMultiplier): Hệ số nhân điểm khi mua hàng (1.0x - 10.0x)
- **Giảm giá %** (DiscountPercentage): Phần trăm giảm giá tự động cho hạng (0-100%)
- **Màu sắc** (TierColor): Màu đại diện cho hạng (#RRGGBB)
- **Mô tả** (Description): Mô tả quyền lợi của hạng

## API Endpoints

### 1. Lấy cấu hình tổng thể
```
GET /api/TierConfiguration/settings
```

**Response:**
```json
{
  "tiers": [
    {
      "tierId": 1,
      "tierName": "Đồng",
      "minSpent": 0,
      "minPoints": 0,
      "pointsMultiplier": 1.0,
      "discountPercentage": 0,
      "description": "Hạng khách hàng cơ bản",
      "tierColor": "#CD7F32",
      "isActive": true
    }
  ],
  "config": {
    "isEnabled": true,
    "pointsPerCurrency": 1000,
    "minOrderAmountForPoints": 50000
  },
  "statistics": {
    "totalCustomers": 150,
    "activeTiers": 4
  }
}
```

### 2. Cập nhật hàng loạt cấu hình
```
PUT /api/TierConfiguration/batch-update
```

**Request Body:**
```json
[
  {
    "tierId": 1,
    "tierName": "Đồng",
    "minSpent": 0,
    "minPoints": 0,
    "pointsMultiplier": 1.0,
    "discountPercentage": 0,
    "description": "Hạng cơ bản",
    "tierColor": "#CD7F32",
    "isActive": true
  },
  {
    "tierId": 2,
    "tierName": "Bạc",
    "minSpent": 2000000,
    "minPoints": 200,
    "pointsMultiplier": 1.2,
    "discountPercentage": 3,
    "description": "Hạng thân thiết",
    "tierColor": "#C0C0C0",
    "isActive": true
  }
]
```

**Response:**
```json
{
  "message": "Cập nhật cấu hình hạng khách hàng thành công",
  "updatedTiers": 2,
  "warnings": [
    "Khoảng cách chi tiêu giữa hạng 'Đồng' và 'Bạc' có thể quá lớn"
  ]
}
```

### 3. Xác thực cấu hình
```
POST /api/TierConfiguration/validate
```

**Request Body:** Array của CustomerTierDto

**Response:**
```json
{
  "isValid": true,
  "errors": [],
  "warnings": [
    "Hạng 'Vàng' có điểm tối thiểu thấp hơn hạng trước"
  ],
  "suggestions": [
    "Đảm bảo các hạng cao hơn có quyền lợi tốt hơn",
    "Xem xét khoảng cách hợp lý giữa các hạng",
    "Nên có hạng cơ bản cho khách hàng mới (chi tiêu = 0)"
  ]
}
```

### 4. Reset về cấu hình mặc định
```
POST /api/TierConfiguration/reset-defaults
Header: X-Confirm-Reset: true
```

**Response:**
```json
{
  "message": "Đã reset về cấu hình hạng mặc định",
  "defaultTiers": [
    {
      "tierName": "Đồng",
      "minSpent": 0,
      "minPoints": 0,
      "pointsMultiplier": 1.0,
      "discountPercentage": 0
    },
    {
      "tierName": "Bạc",
      "minSpent": 5000000,
      "minPoints": 500,
      "pointsMultiplier": 1.2,
      "discountPercentage": 2
    },
    {
      "tierName": "Vàng",
      "minSpent": 20000000,
      "minPoints": 2000,
      "pointsMultiplier": 1.5,
      "discountPercentage": 5
    },
    {
      "tierName": "Kim cương",
      "minSpent": 50000000,
      "minPoints": 5000,
      "pointsMultiplier": 2.0,
      "discountPercentage": 10
    }
  ]
}
```

### 5. Xem trước tác động thay đổi
```
GET /api/TierConfiguration/preview-impact/{tierId}?newMinSpent=10000000&newMinPoints=1000
```

**Response:**
```json
{
  "tierName": "Vàng",
  "currentCriteria": {
    "minSpent": 20000000,
    "minPoints": 2000
  },
  "newCriteria": {
    "minSpent": 10000000,
    "minPoints": 1000
  },
  "impact": {
    "currentCustomers": 25,
    "qualifiedForNew": 45,
    "wouldLoseTier": 5,
    "netChange": 20
  }
}
```

## Validation Rules

### Quy tắc cơ bản:
- Chi tiêu tối thiểu ≥ 0
- Điểm tối thiểu ≥ 0
- Hệ số điểm: 0.1 - 10.0
- Giảm giá %: 0 - 100
- Màu sắc: định dạng hex (#RRGGBB)
- Tên hạng: không trùng lặp

### Quy tắc logic:
- Hạng cao hơn nên có quyền lợi tốt hơn
- Không có điều kiện trùng lặp giữa các hạng
- Nên có hạng cơ bản (chi tiêu = 0) cho khách hàng mới

### Cảnh báo:
- Khoảng cách quá lớn/nhỏ giữa các hạng
- Hạng cao có quyền lợi thấp hơn hạng thấp

## Sử dụng trong Frontend

### Ví dụ component React:

```jsx
const TierSettings = () => {
  const [tiers, setTiers] = useState([]);
  const [loading, setLoading] = useState(false);

  // Load cấu hình hiện tại
  const loadTierSettings = async () => {
    const response = await fetch('/api/TierConfiguration/settings');
    const data = await response.json();
    setTiers(data.tiers);
  };

  // Cập nhật cấu hình
  const updateTiers = async (updatedTiers) => {
    setLoading(true);
    try {
      const response = await fetch('/api/TierConfiguration/batch-update', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(updatedTiers)
      });
      
      const result = await response.json();
      if (response.ok) {
        alert('Cập nhật thành công!');
        if (result.warnings?.length > 0) {
          console.warn('Warnings:', result.warnings);
        }
        loadTierSettings();
      } else {
        alert('Lỗi: ' + result.errors?.join(', '));
      }
    } finally {
      setLoading(false);
    }
  };

  // Validate trước khi lưu
  const validateConfig = async (tierConfig) => {
    const response = await fetch('/api/TierConfiguration/validate', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(tierConfig)
    });
    
    const validation = await response.json();
    return validation;
  };

  return (
    <div>
      {/* UI component cho cấu hình tiers */}
    </div>
  );
};
```

## Quy trình thay đổi cấu hình

1. **Lấy cấu hình hiện tại**: `GET /api/TierConfiguration/settings`
2. **Validate thay đổi**: `POST /api/TierConfiguration/validate` 
3. **Xem trước tác động**: `GET /api/TierConfiguration/preview-impact/{id}`
4. **Áp dụng thay đổi**: `PUT /api/TierConfiguration/batch-update`
5. **Hệ thống tự động cập nhật hạng khách hàng** trong background

## Lưu ý quan trọng

- Thay đổi cấu hình sẽ tự động kích hoạt việc tính toán lại hạng cho tất cả khách hàng
- Khách hàng sẽ nhận thông báo khi được nâng hạng
- Nên sử dụng validation trước khi áp dụng thay đổi
- API preview-impact giúp đánh giá tác động trước khi thay đổi

## Ví dụ cấu hình thực tế

```json
[
  {
    "tierName": "Khách hàng mới",
    "minSpent": 0,
    "minPoints": 0,
    "pointsMultiplier": 1.0,
    "discountPercentage": 0,
    "tierColor": "#808080"
  },
  {
    "tierName": "Thân thiết",
    "minSpent": 1000000,
    "minPoints": 100,
    "pointsMultiplier": 1.2,
    "discountPercentage": 3,
    "tierColor": "#C0C0C0"
  },
  {
    "tierName": "VIP",
    "minSpent": 5000000,
    "minPoints": 500,
    "pointsMultiplier": 1.5,
    "discountPercentage": 7,
    "tierColor": "#FFD700"
  },
  {
    "tierName": "VVIP",
    "minSpent": 20000000,
    "minPoints": 2000,
    "pointsMultiplier": 2.0,
    "discountPercentage": 15,
    "tierColor": "#B9F2FF"
  }
]
```