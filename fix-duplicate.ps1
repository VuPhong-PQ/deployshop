$content = Get-Content "c:\shop\client\src\pages\sales.tsx"
$newContent = @()
$foundFirst = $false

foreach ($line in $content) {
    if ($line -match 'const \[selectedDiscount, setSelectedDiscount\] = useState<Discount') {
        if (-not $foundFirst) {
            $foundFirst = $true
            # Skip this duplicate line - don't add it
        } else {
            # This is the second occurrence, also skip it
        }
    } else {
        $newContent += $line
    }
}

$newContent | Set-Content "c:\shop\client\src\pages\sales.tsx"
Write-Host "Removed duplicate selectedDiscount declaration"