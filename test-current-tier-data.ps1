# Test Current Customer Data with Tier Information
$baseUrl = "http://101.53.9.76:5273"

Write-Host "=== Testing Current Customer Data for Tier Benefits ===" -ForegroundColor Green

# Test with Ruby customer (ID 2)
$customerId = 2
Write-Host "`n1. Testing Ruby customer data (ID: $customerId)" -ForegroundColor Yellow

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/customers/$customerId" -Method GET -ContentType "application/json"
    
    Write-Host "✅ Customer API Response successful!" -ForegroundColor Green
    
    Write-Host "`n📊 Customer Info:" -ForegroundColor Cyan
    Write-Host "  Name: $($response.hoTen)"
    Write-Host "  Phone: $($response.soDienThoai)"
    Write-Host "  Hang: $($response.hangKhachHang)"
    Write-Host "  Total Spent: $($response.totalSpent.ToString('N0'))₫"
    Write-Host "  Loyalty Points: $($response.loyaltyPoints.ToString('N0'))"
    
    if ($response.customerTier) {
        Write-Host "`n🏆 Current Tier Info:" -ForegroundColor Green
        Write-Host "  Tier Name: $($response.customerTier.tierName)"
        Write-Host "  Discount Percentage: $($response.customerTier.discountPercentage)%"
        Write-Host "  Points Multiplier: $($response.customerTier.pointsMultiplier)x"
        Write-Host "  Tier Color: $($response.customerTier.tierColor)"
        
        # Calculate next tier
        $allTiers = @(
            @{ tierName = "Đồng"; discountPercentage = 0; pointsMultiplier = 1.0; minSpent = 0; description = "Hạng khách hàng cơ bản"; tierColor = "#CD7F32" },
            @{ tierName = "Bạc"; discountPercentage = 3; pointsMultiplier = 1.2; minSpent = 5000000; description = "Khách hàng thân thiết"; tierColor = "#C0C0C0" },
            @{ tierName = "Vàng"; discountPercentage = 5; pointsMultiplier = 1.5; minSpent = 20000000; description = "Khách hàng VIP"; tierColor = "#FFD700" },
            @{ tierName = "Kim cương"; discountPercentage = 10; pointsMultiplier = 2.0; minSpent = 50000000; description = "Khách hàng VVIP"; tierColor = "#B9F2FF" }
        )
        
        $currentTierIndex = -1
        for ($i = 0; $i -lt $allTiers.Count; $i++) {
            if ($allTiers[$i].tierName -eq $response.customerTier.tierName) {
                $currentTierIndex = $i
                break
            }
        }
        
        if ($currentTierIndex -ge 0 -and $currentTierIndex -lt ($allTiers.Count - 1)) {
            $nextTier = $allTiers[$currentTierIndex + 1]
            $spentRequired = $nextTier.minSpent - $response.totalSpent
            
            Write-Host "`n🎯 Next Tier: $($nextTier.tierName)" -ForegroundColor Blue
            Write-Host "  Discount: $($nextTier.discountPercentage)%"
            Write-Host "  Points Multiplier: $($nextTier.pointsMultiplier)x"
            Write-Host "  Min Spent Required: $($nextTier.minSpent.ToString('N0'))₫"
            Write-Host "  Additional Spending Needed: $($spentRequired.ToString('N0'))₫" -ForegroundColor Yellow
            
            $progress = ($response.totalSpent / $nextTier.minSpent) * 100
            Write-Host "  Progress: $($progress.ToString('N1'))%" -ForegroundColor Magenta
        } else {
            Write-Host "`n🎉 Customer has reached the highest tier!" -ForegroundColor Yellow
        }
    } else {
        Write-Host "`n❌ No tier information found" -ForegroundColor Red
    }
    
} catch {
    Write-Host "❌ Error calling API: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== Test Completed ===" -ForegroundColor Green