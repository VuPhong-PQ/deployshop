$filePath = "c:\shop\client\src\pages\sales.tsx"
$content = Get-Content $filePath

# Tìm line có selectedDiscountAmount và thêm selectedDiscount trước nó
$newContent = @()
$added = $false

for ($i = 0; $i -lt $content.Length; $i++) {
    $line = $content[$i]
    
    # Nếu tìm thấy dòng selectedDiscountAmount và chưa thêm selectedDiscount
    if ($line -match 'const \[selectedDiscountAmount' -and -not $added) {
        # Thêm dòng selectedDiscount trước
        $newContent += "  const [selectedDiscount, setSelectedDiscount] = useState<DiscountRule | Discount | null>(null);"
        $added = $true
    }
    
    $newContent += $line
}

$newContent | Set-Content $filePath
Write-Host "Added selectedDiscount declaration"