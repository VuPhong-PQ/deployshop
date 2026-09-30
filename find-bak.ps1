Write-Host "=== TIM FILE .BAK MOI NHAT TRONG DU AN C:\SHOP ===" -ForegroundColor Cyan
Write-Host ""

$bakFiles = Get-ChildItem -Path "c:\shop" -Filter "*.bak" -Recurse -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending

if ($bakFiles) {
    Write-Host "Tim thay $($bakFiles.Count) file .bak trong du an. Danh sach 15 file gan day nhat:" -ForegroundColor Yellow
    Write-Host ""

    $num = 0
    foreach ($file in $bakFiles) {
        $num = $num + 1
        if ($num -gt 15) { break }
        $sizeMB = [math]::Round($file.Length / 1MB, 2)
        $dateStr = $file.LastWriteTime.ToString("dd/MM/yyyy HH:mm:ss")
        $fileName = $file.Name
        $fullName = $file.FullName
        
        if ($num -eq 1) {
            Write-Host "[MOI NHAT] Top 1: $fileName" -ForegroundColor Green
            Write-Host "   - Duong dan: $fullName" -ForegroundColor Green
            Write-Host "   - Ngay tao: $dateStr" -ForegroundColor Green
            Write-Host "   - Dung luong: $sizeMB MB" -ForegroundColor Green
            Write-Host ""
        } else {
            Write-Host "Top $num - $fileName" -ForegroundColor White
            Write-Host "   - Duong dan: $fullName" -ForegroundColor Gray
            Write-Host "   - Ngay tao: $dateStr | Dung luong: $sizeMB MB" -ForegroundColor Gray
            Write-Host ""
        }
    }
} else {
    Write-Host "Khong tim thay file .bak nao" -ForegroundColor Red
}
