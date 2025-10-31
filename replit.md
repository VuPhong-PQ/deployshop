# Hệ Thống POS Web Bán Hàng

## Tổng Quan Dự Án
Hệ thống POS web toàn diện với tích hợp hóa đơn điện tử, được xây dựng bằng React và Express.js. Hệ thống hỗ trợ quản lý bán hàng, kho, khách hàng, nhân viên và các tính năng nâng cao khác.

## Các Tính Năng Chính
- ✅ Quản lý sản phẩm và kho
- ✅ Giỏ hàng và xử lý đơn hàng  
- ✅ Thanh toán đa dạng (tiền mặt, thẻ, QR, ví điện tử)
- ✅ Quản lý khách hàng & CRM
- ✅ Báo cáo doanh thu & phân tích
- ✅ Tích hợp hóa đơn điện tử
- ✅ Quản lý nhân viên/phân quyền
- ✅ Chương trình khuyến mãi, voucher
- ✅ Quản lý vận chuyển
- ✅ Tích hợp đa nền tảng
- ✅ Bảo mật & phân quyền
- ✅ Đa chi nhánh/chuỗi
- ✅ Quản lý đơn hàng online/offline

## Kiến Trúc Dự Án
- **Frontend**: React với Vite, TypeScript, Tailwind CSS, Shadcn/UI
- **Backend**: Express.js với TypeScript
- **Database**: PostgreSQL với Drizzle ORM
- **State Management**: TanStack Query
- **Routing**: Wouter
- **Forms**: React Hook Form với Zod validation

## Cấu Trúc Thư Mục
```
├── client/src/
│   ├── components/ui/     # Shadcn UI components
│   ├── pages/            # Các trang chính
│   ├── lib/              # Utilities và configs
│   └── hooks/            # Custom hooks
├── server/
│   ├── routes.ts         # API routes
│   └── storage.ts        # Data storage interface
├── shared/
│   └── schema.ts         # Database schema và types
└── package.json
```

## Người Dùng Ưu Tiên
- Ngôn ngữ: Tiếng Việt
- Giao diện: Đơn giản, dễ sử dụng
- Tối ưu cho: Cửa hàng nhỏ đến trung bình tại Việt Nam

## Thay Đổi Gần Đây
- [26/08/2025] Khởi tạo dự án với full-stack JavaScript setup
- [26/08/2025] Định nghĩa schema database cho POS system
- [26/08/2025] Hoàn thiện trang Sales với POS đầy đủ chức năng
- [26/08/2025] Xây dựng trang Products với quản lý CRUD hoàn chỉnh
- [26/08/2025] Phát triển trang Customers với hệ thống CRM
- [26/08/2025] Triển khai trang Inventory với quản lý kho toàn diện
- [26/08/2025] Hoàn thiện Dashboard với báo cáo và thống kê real-time