# 🔧 Staff Page API Endpoints Fix

## 🚨 Vấn đề đã phát hiện

Trang Staff có nhiều lỗi 404 vì các API endpoints sử dụng relative URLs (`/api/...`) thay vì full URLs với base server.

## ✅ Endpoints đã sửa

### Staff Management
- ✅ `GET /api/staff` → `GET http://101.53.9.76:5273/api/staff`
- ✅ `POST /api/staff` → `POST http://101.53.9.76:5273/api/staff`
- ✅ `PUT /api/staff/{id}` → `PUT http://101.53.9.76:5273/api/staff/{id}`
- ✅ `DELETE /api/staff/{id}` → `DELETE http://101.53.9.76:5273/api/staff/{id}`

### Role Management
- ✅ `GET /api/role` → `GET http://101.53.9.76:5273/api/role`
- ✅ `POST /api/role` → `POST http://101.53.9.76:5273/api/role`
- ✅ `PUT /api/role/{id}` → `PUT http://101.53.9.76:5273/api/role/{id}`
- ✅ `DELETE /api/role/{id}` → `DELETE http://101.53.9.76:5273/api/role/{id}`

### Permission Management
- ✅ `GET /api/permission` → `GET http://101.53.9.76:5273/api/permission`
- ✅ `POST /api/role/assign-permission` → `POST http://101.53.9.76:5273/api/role/assign-permission`
- ✅ `POST /api/role/remove-permission` → `POST http://101.53.9.76:5273/api/role/remove-permission`

### Store Management (đã đúng từ trước)
- ✅ `GET http://101.53.9.76:5273/api/stores`
- ✅ `GET http://101.53.9.76:5273/api/staffstores/staff-assignments`
- ✅ `POST http://101.53.9.76:5273/api/staffstores/assign`
- ✅ `DELETE http://101.53.9.76:5273/api/staffstores/unassign`

## 🔍 Testing

Sau khi sửa, cần test các chức năng:

### Staff Management
1. **Xem danh sách nhân viên** → Kiểm tra load được data
2. **Thêm nhân viên mới** → Test form create
3. **Sửa thông tin nhân viên** → Test form edit
4. **Xóa nhân viên** → Test soft/hard delete

### Role Management  
1. **Xem danh sách vai trò** → Kiểm tra load được data
2. **Thêm vai trò mới** → Test create role
3. **Sửa vai trò** → Test edit role
4. **Xóa vai trò** → Test delete role

### Permission Management
1. **Xem quyền hạn của vai trò** → Load permissions for role
2. **Toggle quyền hạn** → Test assign/remove permissions
3. **Chọn tất cả quyền** → Test bulk assign
4. **Hủy tất cả quyền** → Test bulk remove

### Store Assignment
1. **Xem assignment hiện tại** → Load staff-store assignments
2. **Gán nhân viên vào cửa hàng** → Test assign
3. **Hủy gán nhân viên** → Test unassign

## 🐛 Debugging Tips

Nếu vẫn gặp lỗi 404:

1. **Check Network tab** trong DevTools
2. **Verify endpoint** trong backend Controller
3. **Check CORS settings** nếu cần
4. **Verify backend server** đang chạy trên port 5273

## 📝 Code Pattern

Thay vì:
```typescript
const response = await fetch('/api/staff');
```

Sử dụng:
```typescript
const response = await fetch('http://101.53.9.76:5273/api/staff');
```

## 🎯 Expected Result

Sau khi fix:
- ❌ Network tab không còn lỗi 404
- ✅ Data load thành công
- ✅ CRUD operations hoạt động bình thường
- ✅ UI hiển thị đầy đủ thông tin

---

**🚀 Next**: Test từng chức năng để ensure tất cả hoạt động đúng!