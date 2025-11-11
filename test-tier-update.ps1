# Test CustomerTier Update API
$baseUrl = "http://101.53.9.76:5273"

Write-Host "=== Testing CustomerTier Update API ===" -ForegroundColor Green

# Test updating tier ID 4 (Kim cương theo hình)
$tierId = 4
$updateData = @{
    TierId = 4
    TierName = "Kim cương"
    MinSpent = 50000000
    MinPoints = 5000
    PointsMultiplier = 2.0
    DiscountPercentage = 10.0
    Description = "Khách hàng VVIP"
    TierColor = "#B9F2FF"
    IsActive = $true
}

Write-Host "`n1. Testing PUT /api/CustomerTierManagement/$tierId" -ForegroundColor Yellow

try {
    $json = $updateData | ConvertTo-Json -Depth 10
    Write-Host "Request body:" -ForegroundColor Cyan
    Write-Host $json -ForegroundColor Gray
    
    $response = Invoke-RestMethod -Uri "$baseUrl/api/CustomerTierManagement/$tierId" -Method PUT -Body $json -ContentType "application/json"
    
    Write-Host "✅ Update successful!" -ForegroundColor Green
    Write-Host "Response: $($response | ConvertTo-Json)" -ForegroundColor Green
    
} catch {
    Write-Host "❌ Error updating tier: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $errorStream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($errorStream)
        $errorContent = $reader.ReadToEnd()
        Write-Host "Error details: $errorContent" -ForegroundColor Red
    }
}

Write-Host "`n2. Testing GET to verify current tier data" -ForegroundColor Yellow

try {
    $currentTiers = Invoke-RestMethod -Uri "$baseUrl/api/CustomerTierManagement" -Method GET -ContentType "application/json"
    $kimCuongTier = $currentTiers | Where-Object { $_.tierName -eq "Kim cương" }
    
    if ($kimCuongTier) {
        Write-Host "✅ Current Kim cương tier data:" -ForegroundColor Green
        Write-Host "  ID: $($kimCuongTier.tierId)"
        Write-Host "  Name: $($kimCuongTier.tierName)"
        Write-Host "  Min Spent: $($kimCuongTier.minSpent.ToString('N0'))"
        Write-Host "  Min Points: $($kimCuongTier.minPoints)"
        Write-Host "  Points Multiplier: $($kimCuongTier.pointsMultiplier)"
        Write-Host "  Discount %: $($kimCuongTier.discountPercentage)"
        Write-Host "  Description: $($kimCuongTier.description)"
        Write-Host "  Color: $($kimCuongTier.tierColor)"
        Write-Host "  Active: $($kimCuongTier.isActive)"
    } else {
        Write-Host "⚠️ Kim cương tier not found" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "❌ Error getting tiers: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== Test Completed ===" -ForegroundColor Green