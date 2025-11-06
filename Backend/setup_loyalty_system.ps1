# Script để chạy migration và thiết lập hệ thống tích điểm
# Chạy từ thư mục Backend/RetailPointBackend

Write-Host "Đang chuẩn bị migration cho hệ thống tích điểm..." -ForegroundColor Green

# Tạo migration mới cho hệ thống tích điểm
Write-Host "Tạo migration..." -ForegroundColor Yellow
dotnet ef migrations add AddLoyaltySystem

# Cập nhật database
Write-Host "Cập nhật database..." -ForegroundColor Yellow
dotnet ef database update

# Chạy script khởi tạo dữ liệu
Write-Host "Khởi tạo dữ liệu tích điểm..." -ForegroundColor Yellow
sqlcmd -S "TEST-PC\KTEAM" -d "RetailPoint" -U "sa" -P "sa@123" -i "..\create_loyalty_system.sql"

Write-Host "Hoàn thành thiết lập hệ thống tích điểm!" -ForegroundColor Green
Write-Host ""
Write-Host "Các tính năng đã được thêm vào:" -ForegroundColor Cyan
Write-Host "✓ Cấu hình tích điểm cơ bản" -ForegroundColor White
Write-Host "✓ Hệ thống cấp độ khách hàng (Đồng, Bạc, Vàng, Kim cương)" -ForegroundColor White
Write-Host "✓ Tích điểm theo thời gian (Happy Hour, Cuối tuần, Sinh nhật)" -ForegroundColor White
Write-Host "✓ Tích điểm theo danh mục và sản phẩm" -ForegroundColor White
Write-Host "✓ Lịch sử giao dịch điểm" -ForegroundColor White
Write-Host "✓ Chương trình khuyến mãi đặc biệt" -ForegroundColor White
Write-Host ""
Write-Host "Truy cập Settings > Tích điểm để cấu hình!" -ForegroundColor Yellow