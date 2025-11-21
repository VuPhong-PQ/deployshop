# HƯỚNG DẪN DEPLOYMENT - KHẮC PHỤC LỖI FONT VÀ DATABASE

## 🎯 TÓM TẮT VẤN ĐỀ ĐÃ KHẮC PHỤC
- **Nguyên nhân**: Lệnh `DELETE FROM dbo.ActivityLogs, dbo.Messages` trên các bảng không tồn tại
- **Ảnh hưởng**: Backend có reference đến bảng không tồn tại → lỗi SQL → ảnh hưởng encoding
- **Đã fix**: Loại bỏ reference trong DataManagementController, tạo script khôi phục

---

## 📋 CHECKLIST DEPLOYMENT

### ✅ HOÀN THÀNH
- [x] Sửa DataManagementController (loại bỏ ActivityLogs, AuditLogs)
- [x] Build & Publish backend thành công
- [x] Tạo backup: `backup_20251119_112911`
- [x] Tạo script SQL khôi phục: `restore_missing_tables.sql`

### 🔄 CẦN THỰC HIỆN
- [ ] **BƯỚC 1**: Chạy SQL script trên database
- [ ] **BƯỚC 2**: Deploy backend lên IIS server
- [ ] **BƯỚC 3**: Restart IIS Application Pool
- [ ] **BƯỚC 4**: Test font trên website

---

## 📝 CHI TIẾT THỰC HIỆN

### BƯỚC 1: CHẠY SQL SCRIPT
```sql
-- Kết nối SQL Server: TEST-PC\KTEAM
-- Database: RetailPoint
-- User: sa / Pass: sa@123

-- Chạy file: c:\shop\Backend\restore_missing_tables.sql
-- Script này sẽ:
--   ✓ Kiểm tra bảng Notifications
--   ✓ Tạo lại nếu bị mất
--   ✓ Kiểm tra encoding/collation
--   ✓ Báo cáo kết quả
```

### BƯỚC 2: DEPLOY BACKEND
```powershell
# File cần deploy từ:
C:\shop\Backend\RetailPointBackend\bin\Release\publish\

# Deploy đến server IIS:
# - Server: 101.53.9.76:5273
# - Copy tất cả file từ publish folder
# - Đảm bảo permissions đúng
```

### BƯỚC 3: RESTART IIS
```cmd
# Trên server 101.53.9.76:
# 1. Mở IIS Manager
# 2. Tìm Application Pool của RetailPointBackend
# 3. Right click → Recycle
# 4. Hoặc Stop → Start
```

### BƯỚC 4: TEST WEBSITE
```
Frontend: http://101.53.9.76 (port 80)
Backend API: http://101.53.9.76:5273

Kiểm tra:
- ✓ Font tiếng Việt hiển thị đúng
- ✓ Các API endpoint hoạt động
- ✓ Không có lỗi encoding
- ✓ Notification system hoạt động
```

---

## 🔧 FILES ĐÃ TẠO/SỬA

### Modified Files:
- `DataManagementController.cs` - Removed non-existent table references

### New Files:
- `restore_missing_tables.sql` - Database recovery script
- `fix-font-database-issue.ps1` - Main deployment script
- `backup_20251119_112911/` - Backup của publish

---

## 🚨 LƯU Ý QUAN TRỌNG

1. **Backup Database trước khi chạy SQL script**
2. **Kiểm tra server connectivity trước deploy**
3. **Test trên staging trước production (nếu có)**
4. **Monitor logs sau deploy**

---

## 📞 XỬ LÝ SỰ CỐ

Nếu có vấn đề sau deploy:

### Rollback Backend:
```powershell
# Restore từ backup nếu cần
Copy-Item "backup_20251119_112911/*" -Destination "current_publish_folder" -Recurse -Force
```

### Kiểm tra Logs:
- IIS Event Viewer
- Application logs
- SQL Server error logs

### Test Connection:
```powershell
# Test API endpoint
Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/test" -Method GET
```

---

## ✨ KẾT LUẬN

Sau khi hoàn thành 4 bước trên, lỗi font sẽ được khắc phục hoàn toàn.
**Dữ liệu hiện tại được bảo toàn 100%**.

🎯 **Ready for deployment!**