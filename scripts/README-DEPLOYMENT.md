# Hướng dẫn Triển khai và Khắc phục sự cố RetailPoint Backend

## Mục lục
1. [Tổng quan](#tổng-quan)
2. [Khắc phục nhanh - Backend không chạy](#khắc-phục-nhanh)
3. [Khắc phục - Lỗi kết nối SQL Server](#khắc-phục-sql)
4. [Cài đặt Backend như Windows Service](#cài-đặt-service)
5. [Cài đặt Giám sát tự động](#cài-đặt-giám-sát)
6. [Các lệnh thường dùng](#các-lệnh-thường-dùng)

---

## Tổng quan

RetailPoint Backend là một .NET 9 Web API chạy trên Windows Server, kết nối với SQL Server.

**Thông tin mặc định:**
- **Backend URL**: `http://101.53.9.76:5273`
- **Health check**: `http://101.53.9.76:5273/weatherforecast`
- **Login API**: `POST http://101.53.9.76:5273/api/staff/login`
- **SQL Server**: `TEST-PC\KTEAM` (database: `RetailPoint`)

---

## Khắc phục nhanh

### Bước 1: Kiểm tra backend có đang chạy không

```powershell
# Kiểm tra process
Get-Process -Name "RetailPointBackend" -ErrorAction SilentlyContinue

# Hoặc kiểm tra bằng HTTP
Invoke-WebRequest -Uri "http://127.0.0.1:5273/weatherforecast" -UseBasicParsing
```

### Bước 2: Khởi động lại backend

**Cách 1: Nếu đã cài đặt như Service**
```powershell
# Xem trạng thái
Get-Service -Name "RetailPointBackend"

# Khởi động lại
Restart-Service -Name "RetailPointBackend" -Force
```

**Cách 2: Chạy trực tiếp exe**
```powershell
# Dừng process cũ (nếu có)
Stop-Process -Name "RetailPointBackend" -Force -ErrorAction SilentlyContinue

# Khởi động
cd "C:\shop\backend-deploy"
Start-Process -FilePath ".\RetailPointBackend.exe" -WindowStyle Hidden
```

**Cách 3: Sử dụng script có sẵn**
```powershell
cd C:\shop\scripts
.\start-backend.ps1
```

### Bước 3: Kiểm tra lại

```powershell
# Chờ 5 giây
Start-Sleep -Seconds 5

# Test health
Invoke-WebRequest -Uri "http://127.0.0.1:5273/weatherforecast" -UseBasicParsing

# Test login (thay đổi username/password nếu cần)
$body = @{ username = "admin"; password = "admin123" } | ConvertTo-Json
Invoke-RestMethod -Uri "http://127.0.0.1:5273/api/staff/login" -Method POST -Body $body -ContentType "application/json"
```

---

## Khắc phục SQL

### Bước 1: Kiểm tra SQL Server service

```powershell
# Tìm SQL Server service (ví dụ: MSSQL$KTEAM cho named instance)
Get-Service -Name "MSSQL*"

# Kiểm tra trạng thái
Get-Service -Name "MSSQL`$KTEAM"
```

### Bước 2: Khởi động SQL Server nếu đã dừng

```powershell
# Khởi động SQL Server
Start-Service -Name "MSSQL`$KTEAM"

# Khởi động SQL Browser (cần cho named instance)
Start-Service -Name "SQLBrowser" -ErrorAction SilentlyContinue
```

### Bước 3: Test kết nối SQL

```powershell
# Sử dụng script có sẵn
cd C:\shop\scripts
.\check-sql-and-restart.ps1 -AppSettingsPath "C:\shop\backend-deploy\appsettings.json"

# Hoặc với tự động restart
.\check-sql-and-restart.ps1 -AppSettingsPath "C:\shop\backend-deploy\appsettings.json" -RestartSqlService -RestartBackend -BackendExePath "C:\shop\backend-deploy\RetailPointBackend.exe"
```

### Bước 4: Kiểm tra connection string

Mở file `appsettings.json` và kiểm tra:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=TEST-PC\\KTEAM;Database=RetailPoint;User Id=sa;Password=sa@123;MultipleActiveResultSets=True;TrustServerCertificate=True;"
  }
}
```

**Các vấn đề thường gặp:**
- Sai tên server/instance
- Sai username/password
- SQL Server chưa bật TCP/IP
- Firewall chặn port 1433

---

## Cài đặt Service

Cài đặt backend như Windows Service để tự động khởi động khi server reboot:

```powershell
# Chạy với quyền Administrator
cd C:\shop\scripts
.\install-backend-service.ps1 -BackendExePath "C:\shop\backend-deploy\RetailPointBackend.exe"
```

Sau khi cài đặt:
- Service sẽ tự động khởi động khi Windows boot
- Service sẽ tự restart nếu bị crash
- Logs được lưu tại `C:\shop\backend-deploy\logs\`

---

## Cài đặt Giám sát

Cài đặt scheduled task để tự động kiểm tra và restart nếu cần:

```powershell
# Chạy với quyền Administrator
cd C:\shop\scripts

# Cài đặt scheduled task (chạy mỗi 5 phút)
.\install-scheduled-task.ps1 -HealthUrl "http://127.0.0.1:5273/weatherforecast" -IntervalMinutes 5
```

Sau khi cài đặt:
- Task sẽ chạy mỗi 5 phút
- Tự động restart backend nếu không phản hồi
- Logs được lưu tại `C:\shop\logs\health-monitor.log`

---

## Các lệnh thường dùng

### Quản lý Backend Service

```powershell
# Xem trạng thái
Get-Service -Name "RetailPointBackend"

# Khởi động
Start-Service -Name "RetailPointBackend"

# Dừng
Stop-Service -Name "RetailPointBackend"

# Khởi động lại
Restart-Service -Name "RetailPointBackend" -Force

# Xem logs
Get-Content "C:\shop\backend-deploy\logs\service-stdout.log" -Tail 50
Get-Content "C:\shop\backend-deploy\logs\service-stderr.log" -Tail 50
```

### Quản lý SQL Server

```powershell
# Xem trạng thái tất cả SQL services
Get-Service -Name "MSSQL*", "SQL*"

# Khởi động lại SQL Server (thay KTEAM bằng tên instance của bạn)
Restart-Service -Name "MSSQL`$KTEAM" -Force

# Khởi động SQL Browser
Start-Service -Name "SQLBrowser"
```

### Quản lý Health Monitor Task

```powershell
# Xem trạng thái
Get-ScheduledTask -TaskName "RetailPoint-HealthMonitor"

# Chạy ngay
Start-ScheduledTask -TaskName "RetailPoint-HealthMonitor"

# Tắt task
Disable-ScheduledTask -TaskName "RetailPoint-HealthMonitor"

# Bật task
Enable-ScheduledTask -TaskName "RetailPoint-HealthMonitor"

# Xem logs
Get-Content "C:\shop\logs\health-monitor.log" -Tail 100
```

### Test nhanh API

```powershell
# Health check
Invoke-WebRequest -Uri "http://127.0.0.1:5273/weatherforecast" -UseBasicParsing

# Test login
$body = @{ username = "admin"; password = "admin123" } | ConvertTo-Json
Invoke-RestMethod -Uri "http://127.0.0.1:5273/api/staff/login" -Method POST -Body $body -ContentType "application/json"
```

---

## Troubleshooting

### Lỗi "Internal server error" khi login

1. Kiểm tra SQL Server đang chạy
2. Kiểm tra connection string trong `appsettings.json`
3. Kiểm tra bảng `Staffs` có dữ liệu user
4. Xem logs backend để biết chi tiết lỗi

### Backend không start được

1. Kiểm tra port 5273 có đang được sử dụng không:
   ```powershell
   netstat -ano | findstr :5273
   ```
2. Kiểm tra .NET runtime đã cài đặt:
   ```powershell
   dotnet --list-runtimes
   ```
3. Xem logs trong thư mục `logs\`

### SQL Server không kết nối được

1. Kiểm tra SQL Server service đang chạy
2. Kiểm tra SQL Browser service (cho named instance)
3. Kiểm tra firewall cho phép port 1433
4. Kiểm tra SQL Server Configuration Manager - TCP/IP đã bật

---

## Liên hệ hỗ trợ

- **Admin**: Vũ Phong
- **ĐT**: 0907 999 841
- **Email**: vuphongpq@gmail.com
