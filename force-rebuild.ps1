# Force rebuild va deploy backend
Write-Host "Force rebuild backend with latest changes..." -ForegroundColor Green

Set-Location "c:\shop"

# Clean va rebuild
Write-Host "Cleaning..." -ForegroundColor Yellow
dotnet clean Backend\RetailPointBackend

Write-Host "Rebuilding..." -ForegroundColor Yellow  
dotnet build Backend\RetailPointBackend --configuration Release

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build SUCCESS!" -ForegroundColor Green
    Write-Host "Now copy files to IIS and restart" -ForegroundColor Cyan
} else {
    Write-Host "Build FAILED!" -ForegroundColor Red
}