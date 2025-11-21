# Script restart backend và kiểm tra hình ảnh
Write-Host "=== RESTART BACKEND VA KIEM TRA HINH ANH ===" -ForegroundColor Green

# Kiểm tra backend process đang chạy
Write-Host "Kiem tra backend process..." -ForegroundColor Yellow
$backendProcesses = Get-Process | Where-Object {$_.ProcessName -like "*dotnet*" -or $_.ProcessName -like "*RetailPoint*"}

if ($backendProcesses) {
    Write-Host "Tim thay backend processes:" -ForegroundColor Green
    $backendProcesses | ForEach-Object { 
        Write-Host "  PID: $($_.Id) - Name: $($_.ProcessName)" -ForegroundColor Cyan
    }
    
    Write-Host "Dung backend processes..." -ForegroundColor Yellow
    $backendProcesses | Stop-Process -Force
    Start-Sleep -Seconds 3
} else {
    Write-Host "Khong tim thay backend process nao dang chay" -ForegroundColor Yellow
}

# Kiểm tra và khởi động backend từ thư mục deploy
$deployPath = "C:\shop\backend-deploy"
$backendExe = Get-ChildItem $deployPath -Filter "*.exe" | Select-Object -First 1
$backendDll = Get-ChildItem $deployPath -Filter "*RetailPoint*.dll" | Select-Object -First 1

if ($backendExe) {
    Write-Host "Tim thay backend executable: $($backendExe.Name)" -ForegroundColor Green
    Write-Host "Khoi dong backend tu file exe..." -ForegroundColor Yellow
    
    # Chạy backend từ thư mục deploy
    Set-Location $deployPath
    Start-Process -FilePath $backendExe.FullName -WindowStyle Normal
} elseif ($backendDll) {
    Write-Host "Tim thay backend DLL: $($backendDll.Name)" -ForegroundColor Green
    Write-Host "Khoi dong backend tu DLL..." -ForegroundColor Yellow
    
    # Chạy backend từ thư mục deploy
    Set-Location $deployPath
    Start-Process "dotnet" -ArgumentList $backendDll.Name -WindowStyle Normal
} else {
    Write-Host "Khong tim thay backend executable hoac DLL trong $deployPath" -ForegroundColor Red
}

# Đợi một chút để backend khởi động
Write-Host "Doi backend khoi dong..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Kiểm tra kết nối backend
Write-Host "Kiem tra ket noi backend..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://101.53.9.76:5273/api" -Method GET -TimeoutSec 10
    Write-Host "Backend dang chay binh thuong - Status: $($response.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "Khong the ket noi den backend: $($_.Exception.Message)" -ForegroundColor Red
}

# Kiểm tra một số hình ảnh cụ thể
Write-Host "Kiem tra hinh anh cu the..." -ForegroundColor Yellow

$testImages = @(
    "0286348b-08f3-4a9b-80f5-be8eee36b0da.jpeg",
    "product_2dd03773-cf5b-4c54-85b5-e528e746ef8d.jpg",
    "4c57b286-1886-45e3-9a80-7e6d687dc294.jpg"
)

foreach ($imageName in $testImages) {
    $imageUrl = "http://101.53.9.76:5273/uploads/$imageName"
    Write-Host "Test: $imageUrl" -ForegroundColor Cyan
    
    try {
        $imageResponse = Invoke-WebRequest -Uri $imageUrl -Method HEAD -TimeoutSec 5
        Write-Host "  OK - Status: $($imageResponse.StatusCode)" -ForegroundColor Green
    } catch {
        Write-Host "  FAILED - $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "=== KET THUC ===" -ForegroundColor Green