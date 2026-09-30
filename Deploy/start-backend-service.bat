@echo off
title RetailPoint Backend Service (Port 5273)
color 0B

echo =========================================================================
echo    KHOI DONG BACKEND API TRAP SHOP (PORT 5273)
echo =========================================================================

cd /d "C:\shop\server"
powershell -NoProfile -ExecutionPolicy Bypass -Command "$proc = Get-Process RetailPointBackend -ErrorAction SilentlyContinue; if (-not $proc) { Start-Process -FilePath 'C:\shop\server\RetailPointBackend.exe' -ArgumentList '--urls http://0.0.0.0:5273' -WorkingDirectory 'C:\shop\server' -WindowStyle Hidden; Write-Host 'Backend API started on http://localhost:5273' -ForegroundColor Green } else { Write-Host 'Backend API already running' -ForegroundColor Yellow }"

echo Backend API dang chay tren: http://localhost:5273
pause
