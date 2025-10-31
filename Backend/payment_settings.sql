-- Bảng lưu cài đặt thanh toán cho từng cửa hàng
CREATE TABLE PaymentSettings (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    StoreId NVARCHAR(64) NOT NULL,
    PaymentMethod NVARCHAR(100) NULL,
    BankAccount NVARCHAR(50) NULL,
    BankName NVARCHAR(100) NULL,
    QrApi NVARCHAR(200) NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE()
);

-- Đảm bảo mỗi StoreId chỉ có 1 bản ghi
CREATE UNIQUE INDEX IX_PaymentSettings_StoreId ON PaymentSettings(StoreId);
