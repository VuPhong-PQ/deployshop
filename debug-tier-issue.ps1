# Debug script cho vấn đề hạng khách hàng
param(
    [string]$ServerUrl = "http://101.53.9.76:5273"
)

Write-Host "=== DEBUG HẠNG KHÁCH HÀNG CHÚ HẠNH ===" -ForegroundColor Red
Write-Host "Server: $ServerUrl" -ForegroundColor Yellow

# Tìm khách hàng Chú hạnh
Write-Host ""
Write-Host "🔍 BƯỚC 1: Tìm khách hàng Chú hạnh..." -ForegroundColor Cyan
try {
    $customers = Invoke-RestMethod -Uri "$ServerUrl/api/Customers" -Method GET
    $chuHanh = $customers | Where-Object { $_.hoTen -like "*hạnh*" -or $_.hoTen -like "*hanh*" }
    
    if (-not $chuHanh) {
        Write-Host "❌ Không tìm thấy khách hàng Chú hạnh!" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "Found customer:" -ForegroundColor Green
    Write-Host "   Customer ID: $($chuHanh.customerId)" -ForegroundColor White
    Write-Host "   Ten: $($chuHanh.hoTen)" -ForegroundColor White
    Write-Host "   Tier ID: $($chuHanh.tierId)" -ForegroundColor White
    Write-Host "   Hang Khach Hang (enum): $($chuHanh.hangKhachHang)" -ForegroundColor White
    Write-Host "   Loyalty Points: $($chuHanh.loyaltyPoints)" -ForegroundColor White
    Write-Host "   Total Spent: $($chuHanh.totalSpent)" -ForegroundColor White
    
    $customerId = $chuHanh.customerId
    
} catch {
    Write-Host "❌ Lỗi lấy danh sách khách hàng: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Kiểm tra thông tin loyalty chi tiết
Write-Host ""
Write-Host "🔍 BƯỚC 2: Kiểm tra loyalty status..." -ForegroundColor Cyan
try {
    $loyaltyStatus = Invoke-RestMethod -Uri "$ServerUrl/api/LoyaltySettings/customer-status/$customerId" -Method GET
    
    Write-Host "✅ Thông tin loyalty:" -ForegroundColor Green
    Write-Host "   Total Spent: $($loyaltyStatus.totalSpent)" -ForegroundColor White
    Write-Host "   Total Points: $($loyaltyStatus.totalPoints)" -ForegroundColor White
    
    if ($loyaltyStatus.currentTier) {
        Write-Host "   Current Tier ID: $($loyaltyStatus.currentTier.tierId)" -ForegroundColor Green
        Write-Host "   Current Tier Name: $($loyaltyStatus.currentTier.tierName)" -ForegroundColor Green
        Write-Host "   Tier Color: $($loyaltyStatus.currentTier.tierColor)" -ForegroundColor Green
        Write-Host "   Discount: $($loyaltyStatus.currentTier.discountPercentage)%" -ForegroundColor Green
    } else {
        Write-Host "   ❌ KHÔNG CÓ CURRENT TIER!" -ForegroundColor Red
    }
    
} catch {
    Write-Host "❌ Lỗi lấy loyalty status: $($_.Exception.Message)" -ForegroundColor Red
}

# Kiểm tra danh sách tiers
Write-Host ""
Write-Host "🔍 BƯỚC 3: Kiểm tra cấu hình tiers..." -ForegroundColor Cyan
try {
    $tiers = Invoke-RestMethod -Uri "$ServerUrl/api/CustomerTiers" -Method GET
    Write-Host "✅ Danh sách tiers:" -ForegroundColor Green
    foreach ($tier in $tiers | Sort-Object minSpent) {
        Write-Host "   ID $($tier.tierId): $($tier.tierName) - Chi tieu >= $($tier.minSpent), Diem >= $($tier.minPoints)" -ForegroundColor White
    }
} catch {
    Write-Host "❌ Lỗi lấy tiers: $($_.Exception.Message)" -ForegroundColor Red
}

# Force cập nhật tier cho khách hàng này
Write-Host ""
Write-Host "🔧 BƯỚC 4: Force update tier..." -ForegroundColor Cyan
try {
    # Gọi API update all tiers
    $updateResult = Invoke-RestMethod -Uri "$ServerUrl/api/LoyaltySettings/update-all-tiers" -Method POST
    Write-Host "✅ Update all tiers: $($updateResult.message)" -ForegroundColor Green
    
    # Gọi API fix enum mapping  
    $enumResult = Invoke-RestMethod -Uri "$ServerUrl/api/LoyaltySettings/fix-enum-mapping" -Method POST
    Write-Host "✅ Fix enum mapping: $($enumResult.message)" -ForegroundColor Green
    Write-Host "   Fixed: $($enumResult.fixedCount) customers" -ForegroundColor White
    
} catch {
    Write-Host "❌ Lỗi update: $($_.Exception.Message)" -ForegroundColor Red
}

# Kiểm tra lại sau update
Write-Host ""
Write-Host "🔍 BƯỚC 5: Kiểm tra lại sau update..." -ForegroundColor Cyan
try {
    # Lấy lại thông tin customer
    $customers = Invoke-RestMethod -Uri "$ServerUrl/api/Customers" -Method GET
    $chuHanhAfter = $customers | Where-Object { $_.customerId -eq $customerId }
    
    Write-Host "✅ Thông tin sau update:" -ForegroundColor Green
    Write-Host "   Tier ID: $($chuHanhAfter.tierId)" -ForegroundColor White
    Write-Host "   Hang Khach Hang (enum): $($chuHanhAfter.hangKhachHang)" -ForegroundColor White
    Write-Host "   Loyalty Points: $($chuHanhAfter.loyaltyPoints)" -ForegroundColor White
    Write-Host "   Total Spent: $($chuHanhAfter.totalSpent)" -ForegroundColor White
    
    # Lấy lại loyalty status
    $loyaltyAfter = Invoke-RestMethod -Uri "$ServerUrl/api/LoyaltySettings/customer-status/$customerId" -Method GET
    if ($loyaltyAfter.currentTier) {
        Write-Host "   Tier Name from Loyalty API: $($loyaltyAfter.currentTier.tierName)" -ForegroundColor Green
    }
    
} catch {
    Write-Host "❌ Lỗi kiểm tra lại: $($_.Exception.Message)" -ForegroundColor Red
}

# Kiểm tra nếu còn vấn đề
Write-Host ""
Write-Host "📋 PHÂN TÍCH VẤN ĐỀ:" -ForegroundColor Yellow

$customers = Invoke-RestMethod -Uri "$ServerUrl/api/Customers" -Method GET
$finalChuHanh = $customers | Where-Object { $_.customerId -eq $customerId }

if ($finalChuHanh.tierId -eq 3 -and $finalChuHanh.hangKhachHang -ne "Platinum") {
    Write-Host "❌ VẤN ĐỀ: TierId = 3 (Vàng) nhưng enum = $($finalChuHanh.hangKhachHang)" -ForegroundColor Red
    Write-Host "   Cần fix enum mapping trong database trực tiếp" -ForegroundColor Yellow
}
elseif ($finalChuHanh.tierId -ne 3) {
    Write-Host "❌ VẤN ĐỀ: TierId không đúng. Hiện tại = $($finalChuHanh.tierId), cần = 3" -ForegroundColor Red
    Write-Host "   Chi tiêu: $($finalChuHanh.totalSpent) (cần >= 20,000,000)" -ForegroundColor Yellow
    Write-Host "   Điểm: $($finalChuHanh.loyaltyPoints) (cần >= 2,000)" -ForegroundColor Yellow
}
else {
    Write-Host "✅ TierId và enum đã đúng!" -ForegroundColor Green
    Write-Host "   Vấn đề có thể ở frontend cache hoặc API mapping" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "💡 GIẢI PHÁP:" -ForegroundColor Yellow
Write-Host "1. Nếu TierId đúng nhưng enum sai → Cần update database trực tiếp" -ForegroundColor White
Write-Host "2. Nếu TierId sai → Kiểm tra logic UpdateCustomerTierAsync" -ForegroundColor White  
Write-Host "3. Nếu backend đúng → Frontend cần refresh/clear cache" -ForegroundColor White

Write-Host ""
Write-Host "=== DEBUG COMPLETED ===" -ForegroundColor Red