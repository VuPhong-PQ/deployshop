# Script Deploy Du An Shop Len IIS Duoi Dang Virtual Application /shop
# Mac dinh Physical Path: C:\shop\dist

param(
    [string]$TargetDir = "C:\shop\dist",
    [string]$SiteName = "Default Web Site",
    [string]$AppPoolName = "ShopAppPool",
    [string]$VirtualPath = "/shop"
)

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host " TRIEN KHAI DU AN SHOP THANH VIRTUAL APP /shop " -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan

# 1. Kiem tra thu muc nguon dist
if (-not (Test-Path $TargetDir)) {
    Write-Error "Chua co thu muc build dist tai $TargetDir. Vui long chay 'npm run build' truoc."
    exit 1
}

Write-Host "[1/3] Thu muc dist san sang tai: $TargetDir" -ForegroundColor Green

# 2. Kiem tra module WebAdministration va tao AppPool / Virtual App tren IIS
Write-Host "[2/3] Cau hinh IIS AppPool va Virtual Application..." -ForegroundColor Yellow

try {
    Import-Module WebAdministration -ErrorAction Stop

    # Tao AppPool neu chua co
    if (-not (Test-Path "IIS:\AppPools\$AppPoolName")) {
        Write-Host "   - Tao AppPool moi: $AppPoolName" -ForegroundColor Cyan
        New-Item -Path "IIS:\AppPools\$AppPoolName" | Out-Null
        Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "managedRuntimeVersion" -Value ""
    } else {
        Write-Host "   - AppPool $AppPoolName da ton tai." -ForegroundColor Cyan
    }

    # Tao Virtual Application /shop duoi Default Web Site neu chua co
    $VirtualAppPath = "IIS:\Sites\$SiteName$VirtualPath"
    if (-not (Test-Path $VirtualAppPath)) {
        Write-Host "   - Tao Virtual Application '$VirtualPath' duoi '$SiteName'..." -ForegroundColor Cyan
        New-WebApplication -Site $SiteName -Name "shop" -PhysicalPath $TargetDir -ApplicationPool $AppPoolName | Out-Null
    } else {
        Write-Host "   - Virtual Application '$VirtualPath' da ton tai. Cap nhat PhysicalPath va AppPool..." -ForegroundColor Cyan
        Set-ItemProperty -Path $VirtualAppPath -Name "physicalPath" -Value $TargetDir
        Set-ItemProperty -Path $VirtualAppPath -Name "applicationPool" -Value $AppPoolName
    }

    # Restart AppPool
    Write-Host "[3/3] Khoi dong lai Application Pool: $AppPoolName..." -ForegroundColor Yellow
    Restart-WebAppPool -Name $AppPoolName
    Write-Host "Cau hinh IIS hoan tat thanh cong!" -ForegroundColor Green

} catch {
    Write-Warning "PowerShell hien tai khong co quyen Admin de ghi truc tiep vao IIS config: $($_.Exception.Message)"
    Write-Host ""
    Write-Host "=========================================================================" -ForegroundColor Yellow
    Write-Host " HUONG DAN MAP VIRTUAL APP /shop TREN IIS MANAGER (CHI MAT 30 GIAY):" -ForegroundColor Yellow
    Write-Host "=========================================================================" -ForegroundColor Yellow
    Write-Host "1. Mo 'IIS Manager' (Go inetmgr trong Start menu)." -ForegroundColor White
    Write-Host "2. Mo cay thu muc trai: Sites -> Dai 'Default Web Site'." -ForegroundColor White
    Write-Host "3. Nhap chuot phai vao 'Default Web Site' -> Chon 'Add Application...'." -ForegroundColor White
    Write-Host "4. Nhap cac thong tin sau:" -ForegroundColor White
    Write-Host "   - Alias: shop" -ForegroundColor Cyan
    Write-Host "   - Application Pool: (Bam Select -> Chon 'DefaultAppPool' hoac tao 'ShopAppPool')" -ForegroundColor Cyan
    Write-Host "   - Physical path: C:\shop\dist" -ForegroundColor Cyan
    Write-Host "5. Bam OK." -ForegroundColor White
    Write-Host "6. Truy cap ngay: http://101.53.9.75/shop" -ForegroundColor Green
    Write-Host "=========================================================================" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "==================================================" -ForegroundColor Green
Write-Host " LINK KIEM TRA TRUY CAP: " -ForegroundColor Green
Write-Host " - Trang Web NPP cu:  http://101.53.9.75" -ForegroundColor White
Write-Host " - Trang Web Shop moi: http://101.53.9.75/shop" -ForegroundColor White
Write-Host "==================================================" -ForegroundColor Green
