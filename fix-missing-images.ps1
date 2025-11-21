# Script khôi phục hình ảnh bị mất
Write-Host "=== SCRIPT KHOI PHUC HINH ANH BI MAT ===" -ForegroundColor Green

# Đường dẫn nguồn (nơi có đầy đủ hình ảnh)
$sourceUploadPath = "C:\shop\Backend\RetailPointBackend\wwwroot\uploads"
$backendDeployPath = "C:\shop\backend-deploy\wwwroot\uploads"
$deployPackagePath = "C:\shop\deploy_package_20251119_113230\wwwroot\uploads"

# Kiểm tra đường dẫn nguồn
Write-Host "Kiem tra duong dan nguon..." -ForegroundColor Yellow
if (Test-Path $sourceUploadPath) {
    $sourceFiles = Get-ChildItem $sourceUploadPath -File | Measure-Object
    Write-Host "Tim thay $($sourceFiles.Count) files trong thu muc nguon: $sourceUploadPath" -ForegroundColor Green
} else {
    Write-Host "Khong tim thay thu muc nguon: $sourceUploadPath" -ForegroundColor Red
    exit 1
}

# Danh sách các đường dẫn cần khôi phục
$targetPaths = @(
    $backendDeployPath,
    $deployPackagePath,
    "C:\shop\server\wwwroot\uploads"
)

Write-Host "Bat dau khoi phuc hinh anh..." -ForegroundColor Yellow

foreach ($targetPath in $targetPaths) {
    Write-Host "Xu ly: $targetPath" -ForegroundColor Cyan
    
    # Tạo thư mục nếu chưa có
    if (!(Test-Path $targetPath)) {
        New-Item -ItemType Directory -Path $targetPath -Force | Out-Null
        Write-Host "  Da tao thu muc: $targetPath" -ForegroundColor Green
    }
    
    # Copy tất cả files từ nguồn
    try {
        Copy-Item "$sourceUploadPath\*" -Destination $targetPath -Recurse -Force
        $copiedFiles = Get-ChildItem $targetPath -File | Measure-Object
        Write-Host "  Da copy $($copiedFiles.Count) files vao: $targetPath" -ForegroundColor Green
    } catch {
        Write-Host "  Loi khi copy vao $targetPath : $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Kiểm tra các files bị thiếu từ error log
Write-Host "Kiem tra cac files cu the bi thieu..." -ForegroundColor Yellow

$missingFiles = @(
    "4c57b286-1886-45e3-9a80-7e6d687dc294.jpg",
    "3af5a8f6-822f-4a18-9d33-c54eb30584e7.jpg", 
    "43c5ee48-f189-40ab-a5db-5f7226981f75.jpg",
    "81b5c7ec-c024-42b7-b9b6-f46119665937.jpg",
    "dd1c6164-f4e3-4c1e-add5-010084026723.jpg",
    "ed0987aa-c1fd-45f9-98f6-393382b102e3.jpg"
)

foreach ($fileName in $missingFiles) {
    Write-Host "Tim kiem file: $fileName" -ForegroundColor Cyan
    
    # Tìm file trong tất cả thư mục uploads
    $foundFiles = Get-ChildItem -Path "C:\shop" -Recurse -Name $fileName -ErrorAction SilentlyContinue
    
    if ($foundFiles) {
        Write-Host "  Tim thay file tai: $foundFiles" -ForegroundColor Green
        
        # Copy file này vào tất cả thư mục đích
        foreach ($targetPath in $targetPaths) {
            if (Test-Path $targetPath) {
                $fullFoundPath = Join-Path "C:\shop" $foundFiles[0]
                $targetFile = Join-Path $targetPath $fileName
                
                try {
                    Copy-Item $fullFoundPath -Destination $targetFile -Force
                    Write-Host "    Da copy vao: $targetPath" -ForegroundColor Green
                } catch {
                    Write-Host "    Loi copy vao $targetPath : $($_.Exception.Message)" -ForegroundColor Red
                }
            }
        }
    } else {
        Write-Host "  Khong tim thay file: $fileName" -ForegroundColor Red
    }
}

# Kiểm tra quyền truy cập
Write-Host "Kiem tra quyen truy cap thu muc..." -ForegroundColor Yellow
foreach ($targetPath in $targetPaths) {
    if (Test-Path $targetPath) {
        try {
            $acl = Get-Acl $targetPath
            Write-Host "  Co the doc quyen truy cap: $targetPath" -ForegroundColor Green
        } catch {
            Write-Host "  Loi quyen truy cap: $targetPath" -ForegroundColor Red
        }
    }
}

# Hướng dẫn tiếp theo
Write-Host "=== HUONG DAN TIEP THEO ===" -ForegroundColor Green
Write-Host "1. Khoi dong lai backend service" -ForegroundColor Yellow
Write-Host "2. Kiem tra lai website de xem anh da hien thi chua" -ForegroundColor Yellow  
Write-Host "3. Neu van loi, kiem tra cau hinh static files trong Program.cs" -ForegroundColor Yellow
Write-Host "4. Dam bao backend dang chay tu dung thu muc deploy" -ForegroundColor Yellow

Write-Host "=== HOAN THANH ===" -ForegroundColor Green