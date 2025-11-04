# Script to fix Vietnamese encoding in sales.tsx
$filePath = "c:\shop\client\src\pages\sales.tsx"

# Read content and fix common Vietnamese encoding issues
$content = Get-Content $filePath -Raw -Encoding UTF8

# Fix Vietnamese characters
$fixes = @{
    'KhÃ¡ch vÃ£ng lai' = 'Khách vãng lai'
    'ÄÆ¡n hÃ ng' = 'Đơn hàng'
    'Ä'Æ°á»£c' = 'được'
    'cáº­p nháº­t' = 'cập nhật'
    'Tá»•ng' = 'Tổng'
    'â‚«' = '₫'
    'Gá»­i' = 'Gửi'
    'toÃ n bá»™' = 'toàn bộ'
    'hiá»‡n táº¡i' = 'hiện tại'
    'giá» hÃ ng' = 'giỏ hàng'
    'bao gá»"m' = 'bao gồm'
    'items cÅ©' = 'items cũ'
    'má»›i' = 'mới'
    'Lá»—i' = 'Lỗi'
    'KhÃ´ng' = 'Không'
    'tÃ¬m tháº¥y' = 'tìm thấy'
    'thÃ´ng tin' = 'thông tin'
    'Ä'á»ƒ' = 'để'
    'Äang' = 'Đang'
    'cho' = 'cho'
    'Táº¡o' = 'Tạo'
    'form-data' = 'form-data'
    'Ä'Æ¡n hÃ ng' = 'đơn hàng'
}

foreach ($key in $fixes.Keys) {
    $content = $content -replace [regex]::Escape($key), $fixes[$key]
}

# Write back with UTF-8 BOM
$content | Out-File $filePath -Encoding UTF8 -NoNewline

Write-Host "Fixed Vietnamese encoding in sales.tsx"