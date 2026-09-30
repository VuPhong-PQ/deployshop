@echo off
cls
echo =========================================================================
echo    TRIEN KHAI VIRTUAL APPLICATION /shop TREN IIS
echo =========================================================================
echo.

C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe -ExecutionPolicy Bypass -NoProfile -File "C:\shop\Deploy\add-iis-app.ps1"

echo.
echo =========================================================================
echo  HOAN TAT! Kiem tra truy cap ngay: http://101.53.9.75/shop
echo =========================================================================
pause
