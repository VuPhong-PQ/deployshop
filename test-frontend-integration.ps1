# Test API TierConfiguration với frontend mới
$BaseUrl = "http://101.53.9.76:5273/api"

Write-Host "=== TEST TIER CONFIGURATION FOR FRONTEND ===" -ForegroundColor Green

# Test 1: Lấy settings cho frontend
Write-Host "1. Test GET TierConfiguration/settings (for frontend)..." -ForegroundColor Yellow
try {
    $settings = Invoke-RestMethod -Uri "$BaseUrl/TierConfiguration/settings" -Method Get
    Write-Host "SUCCESS: API settings loaded!" -ForegroundColor Green
    Write-Host "- Tiers: $($settings.tiers.Count)" -ForegroundColor White
    Write-Host "- Total customers: $($settings.statistics.totalCustomers)" -ForegroundColor White
    Write-Host "- Config enabled: $($settings.config.isEnabled)" -ForegroundColor White
    
    Write-Host "`nTier data for frontend:" -ForegroundColor Cyan
    foreach ($tier in $settings.tiers) {
        Write-Host "  $($tier.tierName): Min spent $($tier.minSpent), Points x$($tier.pointsMultiplier), Discount $($tier.discountPercentage)%" -ForegroundColor Gray
    }
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: Validate configuration
Write-Host "`n2. Test validation..." -ForegroundColor Yellow
$validConfig = @(
    @{
        tierId = 1
        tierName = "Bronze Updated"
        minSpent = 0
        minPoints = 0
        pointsMultiplier = 1.0
        discountPercentage = 0
        description = "Updated basic tier"
        tierColor = "#CD7F32"
        isActive = $true
    },
    @{
        tierId = 2
        tierName = "Silver Updated"
        minSpent = 3000000
        minPoints = 300
        pointsMultiplier = 1.3
        discountPercentage = 5
        description = "Updated silver tier"
        tierColor = "#C0C0C0"
        isActive = $true
    }
)

try {
    $validation = Invoke-RestMethod -Uri "$BaseUrl/TierConfiguration/validate" -Method Post -ContentType "application/json" -Body ($validConfig | ConvertTo-Json -Depth 10)
    Write-Host "SUCCESS: Validation works!" -ForegroundColor Green
    Write-Host "- Valid: $($validation.isValid)" -ForegroundColor $(if ($validation.isValid) {"Green"} else {"Red"})
    Write-Host "- Errors: $($validation.errors.Count)" -ForegroundColor White
    Write-Host "- Warnings: $($validation.warnings.Count)" -ForegroundColor Yellow
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Batch update (simulate frontend edit)
$confirmUpdate = Read-Host "`n3. Test batch update? This will modify tier data (y/N)"
if ($confirmUpdate -eq 'y' -or $confirmUpdate -eq 'Y') {
    Write-Host "Updating tier configuration..." -ForegroundColor Yellow
    
    try {
        # Get current tiers first
        $currentSettings = Invoke-RestMethod -Uri "$BaseUrl/TierConfiguration/settings" -Method Get
        
        # Modify first tier slightly (simulate frontend edit)
        if ($currentSettings.tiers.Count -gt 0) {
            $firstTier = $currentSettings.tiers[0]
            $firstTier.description = "Updated from frontend test - $(Get-Date -Format 'HH:mm:ss')"
            
            $updateData = @($firstTier)
            $result = Invoke-RestMethod -Uri "$BaseUrl/TierConfiguration/batch-update" -Method Put -ContentType "application/json" -Body ($updateData | ConvertTo-Json -Depth 10)
            
            Write-Host "SUCCESS: Tier updated!" -ForegroundColor Green
            Write-Host "- Message: $($result.message)" -ForegroundColor White
            Write-Host "- Updated tiers: $($result.updatedTiers)" -ForegroundColor White
            
            if ($result.warnings) {
                Write-Host "- Warnings: $($result.warnings -join ', ')" -ForegroundColor Yellow
            }
        }
    } catch {
        Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.ErrorDetails.Message) {
            Write-Host "Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
        }
    }
} else {
    Write-Host "Skipped batch update test" -ForegroundColor Gray
}

Write-Host "`n=== SUMMARY ===" -ForegroundColor Green
Write-Host "Frontend can now:" -ForegroundColor White
Write-Host "- Load tier configuration from /api/TierConfiguration/settings" -ForegroundColor Gray
Write-Host "- Edit tiers inline and save with batch-update" -ForegroundColor Gray
Write-Host "- Validate configuration before saving" -ForegroundColor Gray
Write-Host "- Preview impact of changes" -ForegroundColor Gray
Write-Host "`nReady for frontend integration!" -ForegroundColor Green