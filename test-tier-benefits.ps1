# Test Customer Tier Benefits API
$baseUrl = "http://101.53.9.76:5273"

Write-Host "=== Testing Customer Tier Benefits API ===" -ForegroundColor Green

# Test with Ruby customer (ID 2)
$customerId = 2
Write-Host "`n1. Testing Ruby customer tier benefits (ID: $customerId)" -ForegroundColor Yellow

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/customers/$customerId/tier-benefits" -Method GET -ContentType "application/json"
    
    Write-Host "✅ API Response successful!" -ForegroundColor Green
    
    Write-Host "`n📊 Customer Stats:" -ForegroundColor Cyan
    Write-Host "  Total Spent: $($response.customerStats.totalSpent.ToString('N0'))₫"
    Write-Host "  Total Points: $($response.customerStats.totalPoints.ToString('N0'))"
    
    if ($response.currentTier) {
        Write-Host "`n🏆 Current Tier: $($response.currentTier.tierName)" -ForegroundColor Green
        Write-Host "  Discount: $($response.currentTier.discountPercentage)%"
        Write-Host "  Points Multiplier: $($response.currentTier.pointsMultiplier)x"
        Write-Host "  Description: $($response.currentTier.description)"
        Write-Host "  Min Spent Required: $($response.currentTier.minSpent.ToString('N0'))₫"
        Write-Host "  Min Points Required: $($response.currentTier.minPoints)"
    }
    
    if ($response.nextTier) {
        Write-Host "`n🎯 Next Tier: $($response.nextTier.tierName)" -ForegroundColor Blue
        Write-Host "  Discount: $($response.nextTier.discountPercentage)%"
        Write-Host "  Points Multiplier: $($response.nextTier.pointsMultiplier)x"
        Write-Host "  Description: $($response.nextTier.description)"
        Write-Host "  Min Spent Required: $($response.nextTier.minSpent.ToString('N0'))₫"
        Write-Host "  Additional Spending Needed: $($response.nextTier.spentRequired.ToString('N0'))₫" -ForegroundColor Yellow
        
        # Calculate progress percentage
        $progress = ($response.customerStats.totalSpent / $response.nextTier.minSpent) * 100
        Write-Host "  Progress: $($progress.ToString('N1'))%" -ForegroundColor Magenta
    } else {
        Write-Host "`n🎉 This customer has reached the highest tier!" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "❌ Error calling API: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Response: $($_.Exception.Response)" -ForegroundColor Red
}

Write-Host "`n2. Testing with another customer (ID: 1)" -ForegroundColor Yellow

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/customers/1/tier-benefits" -Method GET -ContentType "application/json"
    
    Write-Host "✅ API Response successful!" -ForegroundColor Green
    
    Write-Host "`n📊 Customer Stats:" -ForegroundColor Cyan
    Write-Host "  Total Spent: $($response.customerStats.totalSpent.ToString('N0'))₫"
    Write-Host "  Total Points: $($response.customerStats.totalPoints.ToString('N0'))"
    
    if ($response.currentTier) {
        Write-Host "`n🏆 Current Tier: $($response.currentTier.tierName)" -ForegroundColor Green
        Write-Host "  Discount: $($response.currentTier.discountPercentage)%"
        Write-Host "  Points Multiplier: $($response.currentTier.pointsMultiplier)x"
    }
    
    if ($response.nextTier) {
        Write-Host "`n🎯 Next Tier: $($response.nextTier.tierName)" -ForegroundColor Blue
        Write-Host "  Additional Spending Needed: $($response.nextTier.spentRequired.ToString('N0'))₫"
    }
    
} catch {
    Write-Host "❌ Error calling API: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== Test Completed ===" -ForegroundColor Green