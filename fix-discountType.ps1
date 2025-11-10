$filePath = "c:\shop\client\src\pages\sales.tsx"
$content = Get-Content $filePath -Raw

# Replace discountType access with type
$content = $content -replace "'discountType' in selectedDiscount", "'type' in selectedDiscount"
$content = $content -replace "selectedDiscount\.discountType", "selectedDiscount.type"

# Write back to file
$content | Set-Content $filePath -NoNewline

Write-Host "Fixed discountType to type in sales.tsx"