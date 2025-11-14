# Script để thay thế _notificationContext thành _context trong OrdersController
$filePath = "c:\shop\Backend\RetailPointBackend\Controllers\OrdersController.cs"

# Đọc nội dung file
$content = Get-Content $filePath -Raw

# Thay thế tất cả _notificationContext thành _context
$updatedContent = $content -replace '_notificationContext', '_context'

# Ghi lại file
$updatedContent | Set-Content $filePath -Encoding UTF8

Write-Host "Đã thay thế tất cả _notificationContext thành _context trong OrdersController.cs"