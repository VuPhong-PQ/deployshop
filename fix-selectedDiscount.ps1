$filePath = "c:\shop\client\src\pages\sales.tsx"
$content = Get-Content $filePath -Raw

# Replace all occurrences of unsafe selectedDiscount property access
$content = $content -replace 'if \(selectedDiscount\) \{\s*formData\.append\(''discountRuleName'', selectedDiscount\.name\);\s*formData\.append\(''discountRuleType'', selectedDiscount\.discountType\);', 'if (selectedDiscount && ''name'' in selectedDiscount && ''discountType'' in selectedDiscount) {`r`n      formData.append(''discountRuleName'', selectedDiscount.name);`r`n      formData.append(''discountRuleType'', selectedDiscount.discountType);'

# Write back to file
$content | Set-Content $filePath -NoNewline

Write-Host "Fixed selectedDiscount property access in all functions"