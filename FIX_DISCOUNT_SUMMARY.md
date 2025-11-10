# 🔧 BẢNG FIX: GIẢM GIÁ TỰ ĐỘNG CHO KHÁCH HÀNG CÓ HẠNG

## 🚨 Vấn đề đã sửa
**Vấn đề cũ:** Hệ thống tự động áp dụng giảm giá cho khách hàng có hạng (Ruby/VIP/Premium) ngay cả khi không chọn giảm giá trong giỏ hàng. Điều này dẫn đến:
- Cart không hiển thị giảm giá nhưng order lại có giảm giá
- Khách hàng bị giảm giá mà không biết
- Logic không nhất quán giữa frontend và backend

**Fix mới:** Chỉ áp dụng giảm giá khi người dùng chủ động chọn. Nếu không chọn giảm giá thì chỉ tích điểm thôi.

## 📝 Chi tiết các thay đổi

### 1. Backend Changes (c:\shop\Backend\RetailPointBackend\Controllers\OrdersController.cs)

**Trước (Lines 97-109):**
```csharp
// Áp dụng giảm giá theo hạng khách hàng (nếu chưa có giảm giá thủ công)
if (customer.CustomerTier != null && order.DiscountAmount == 0)
{
    var tierDiscountAmount = order.SubTotal * (customer.CustomerTier.DiscountPercentage / 100);
    order.DiscountAmount = tierDiscountAmount;
    order.TotalAmount = order.SubTotal + order.TaxAmount - tierDiscountAmount;
    
    _logger.LogInformation("Applied tier discount for customer {CustomerId}: {DiscountPercentage}% = {DiscountAmount}", 
        customer.CustomerId, customer.CustomerTier.DiscountPercentage, tierDiscountAmount);
}
```

**Sau (Lines 97-109):**
```csharp
// KHÔNG tự động áp dụng giảm giá theo hạng khách hàng
// Giảm giá sẽ chỉ được áp dụng khi frontend gửi lên rõ ràng
// hoặc thông qua hệ thống discount selector

_logger.LogInformation("Order for customer {CustomerId} ({CustomerName}) - Tier: {TierName}. Discount will only be applied if explicitly selected.", 
    customer.CustomerId, customer.HoTen, customer.CustomerTier?.TierName ?? "None");
```

### 2. Frontend Changes (c:\shop\client\src\pages\sales.tsx)

**A. Cập nhật hàm getCustomerDiscountInfo:**
```typescript
// Trước: discountPercentage (hiển thị như đang áp dụng)
// Sau: availableDiscountPercentage (hiển thị như có thể áp dụng)

return {
  tierName,
  availableDiscountPercentage, // Thông tin có thể áp dụng, không phải đang áp dụng
  tierColor,
  loyaltyPoints
};
```

**B. Cập nhật UI hiển thị:**
```typescript
// Trước: "Giảm X%" (màu xanh lá - như đang áp dụng)
// Sau: "Có thể giảm X%" (màu xanh dương - chỉ thông tin)

{discountInfo.availableDiscountPercentage > 0 && (
  <div className="text-xs text-blue-600 font-medium bg-blue-100 px-2 py-1 rounded-full">
    Có thể giảm {discountInfo.availableDiscountPercentage}%
  </div>
)}
```

### 3. DiscountSelector Enhancement (c:\shop\client\src\components\DiscountSelector.tsx)

**A. Thêm support cho customer tier discounts:**
```typescript
// Tạo discount rules ảo cho hạng khách hàng
const getCustomerTierDiscounts = (): DiscountRule[] => {
  if (!selectedCustomer || selectedCustomer.hangKhachHang === 'Thuong') {
    return [];
  }
  
  // Tạo discount rule cho từng hạng khách hàng
  switch (selectedCustomer.hangKhachHang) {
    case 'VIP': return [{ discountId: -1, name: 'Giảm giá hạng VIP', type: 1, value: 0.15 }];
    case 'Premium': return [{ discountId: -1, name: 'Giảm giá hạng Premium', type: 1, value: 0.10 }];
    case 'Platinum': return [{ discountId: -1, name: 'Giảm giá hạng Platinum', type: 1, value: 0.20 }];
  }
};
```

**B. Cải thiện UI:**
```typescript
// Phân biệt rõ ràng giữa "Giảm giá hạng khách hàng" và "Chương trình giảm giá"
<span className="bg-blue-100 text-blue-800 px-2 py-0.5 text-xs rounded-full">Hạng KH</span>
<span className="bg-gray-100 text-gray-800 px-2 py-0.5 text-xs rounded-full">Chương trình</span>
```

## ✅ Kết quả mong đợi

### Scenario 1: Khách Ruby KHÔNG chọn giảm giá
- **Cart:** Hiển thị "Có thể giảm 15%" (màu xanh dương)
- **Order:** Tổng tiền: 500,000₫, Giảm giá: 0₫
- **Points:** 750 điểm (Ruby x1.5 multiplier)
- **Result:** ✅ Chỉ tích điểm, không giảm giá

### Scenario 2: Khách Ruby CÓ chọn "Giảm giá hạng Ruby"
- **Cart:** Hiển thị discount được chọn
- **Order:** Tạm tính: 500,000₫, Giảm giá: 75,000₫, Thành tiền: 425,000₫
- **Points:** 750 điểm
- **Result:** ✅ Vừa giảm giá vừa tích điểm

### Scenario 3: Khách vãng lai
- **Cart:** Không có tùy chọn giảm giá hạng
- **Order:** Tổng tiền: 500,000₫, Giảm giá: 0₫
- **Points:** 0 điểm
- **Result:** ✅ Bình thường

## 🔍 Cách kiểm tra

1. **Mở trang bán hàng:** http://101.53.9.76:3000/sales
2. **Chọn khách hàng Ruby/VIP:** Xem hiển thị "Có thể giảm X%" thay vì "Giảm X%"
3. **Thêm sản phẩm vào cart:** Kiểm tra tổng tiền không tự động giảm
4. **Không chọn giảm giá và thanh toán:** Kiểm tra order không có giảm giá nhưng vẫn tích điểm
5. **Chọn "Giảm giá hạng Ruby" và thanh toán:** Kiểm tra order có giảm giá và tích điểm

## 📋 Test Cases

| Khách hàng | Hạng | Chọn giảm giá | Tổng bill | Giảm giá | Điểm tích lũy | Kết quả |
|------------|------|---------------|-----------|----------|---------------|---------|
| Ruby       | VIP  | Không         | 500k      | 0        | 750           | ✅ Chỉ tích điểm |
| Ruby       | VIP  | Có            | 500k      | 75k      | 750           | ✅ Giảm giá + tích điểm |
| Premium    | Premium | Không      | 300k      | 0        | 360           | ✅ Chỉ tích điểm |
| Premium    | Premium | Có         | 300k      | 30k      | 360           | ✅ Giảm giá + tích điểm |
| Vãng lai   | Thường | N/A         | 200k      | 0        | 0             | ✅ Không có quyền lợi |

## 🎯 Lợi ích của fix

1. **Minh bạch hơn:** Khách hàng biết rõ được giảm giá hay không
2. **Kiểm soát tốt hơn:** Nhân viên quyết định có áp dụng giảm giá hay không
3. **Tách biệt rõ ràng:** Hệ thống tích điểm và giảm giá hoạt động độc lập
4. **UX nhất quán:** Frontend và backend xử lý giống nhau
5. **Dễ audit:** Có thể theo dõi được đơn hàng nào được áp dụng giảm giá và tại sao

## 🔄 Rollback (nếu cần)

Nếu cần rollback về behavior cũ, chỉ cần:

1. **Restore OrdersController.cs:**
```csharp
// Thêm lại logic tự động áp dụng giảm giá
if (customer.CustomerTier != null && order.DiscountAmount == 0)
{
    var tierDiscountAmount = order.SubTotal * (customer.CustomerTier.DiscountPercentage / 100);
    order.DiscountAmount = tierDiscountAmount;
    order.TotalAmount = order.SubTotal + order.TaxAmount - tierDiscountAmount;
}
```

2. **Restore sales.tsx:**
```typescript
// Đổi lại từ availableDiscountPercentage về discountPercentage
// Đổi màu từ blue về green
```

Tuy nhiên, recommend giữ fix này vì nó tốt hơn cho UX và business logic.