# Fix Vietnamese encoding
$filePath = "c:\shop\client\src\pages\sales.tsx"
$content = Get-Content $filePath -Raw -Encoding UTF8

# Simple replacements
$content = $content -replace 'KhÃ¡ch vÃ£ng lai', 'Khách vãng lai'
$content = $content -replace 'ÄÆ¡n hÃ ng', 'Đơn hàng'
$content = $content -replace 'cáº­p nháº­t', 'cập nhật'
$content = $content -replace 'Tá»•ng', 'Tổng'
$content = $content -replace 'â‚«', '₫'
$content = $content -replace 'Lá»—i', 'Lỗi'
$content = $content -replace 'KhÃ´ng', 'Không'
$content = $content -replace 'tÃ¬m tháº¥y', 'tìm thấy'
$content = $content -replace 'thÃ´ng tin', 'thông tin'
$content = $content -replace 'Äang', 'Đang'

$content | Out-File $filePath -Encoding UTF8 -NoNewline
Write-Host "Fixed encoding"