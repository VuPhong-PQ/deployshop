-- Script kiem tra va cap nhat san pham bi thieu hinh

-- Lay danh sach cac file name tu ImageUrl
-- Vi du: http://101.53.9.76:5273/uploads/abc.jpg -> abc.jpg

-- Cap nhat: Set ImageUrl = NULL cho cac san pham co file khong ton tai
-- De frontend hien thi placeholder image

-- Danh sach cac file dang co trong uploads (can nhap thu cong hoac tu script)
-- Tam thoi set null de hien thi placeholder

UPDATE Products 
SET ImageUrl = NULL 
WHERE ImageUrl IS NOT NULL 
AND ImageUrl NOT LIKE '%placeholder%'
AND REVERSE(SUBSTRING(REVERSE(ImageUrl), 1, CHARINDEX('/', REVERSE(ImageUrl)) - 1)) NOT IN (
    '0286348b-08f3-4a9b-80f5-be8eee36b0da.jpeg',
    '05832da4-b657-433e-9e80-d6a9fde565db.jpg',
    '064fca59-34d9-43d4-aa66-02c29aae0ef6.jpeg'
    -- ... them cac file khac
);

-- Hoac don gian hon: Set tat ca ImageUrl = NULL de dung placeholder
-- UPDATE Products SET ImageUrl = NULL;

SELECT COUNT(*) as UpdatedCount FROM Products WHERE ImageUrl IS NULL;
