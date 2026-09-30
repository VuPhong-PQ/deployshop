@echo off
cls
echo =========================================================================
echo    DANG KY TU DONG KHOI DONG BACKEND API KHI RESTART SERVER (SYSTEM BOOT)
echo =========================================================================
echo.

net session >nul 2>&1
if %errorLevel% NEQ 0 (
    echo [THONG BAO] Dang yeu cau quyen Administrator...
    powershell -Command "Start-Process '%~0' -Verb RunAs"
    exit /b
)

C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe -ExecutionPolicy Bypass -NoProfile -File "C:\shop\Deploy\setup-auto-start.ps1"

echo.
echo =========================================================================
echo  HOAN TAT! Task Scheduler da duoc dang ky tu dong khoi dong cung Windows.
echo =========================================================================
pause
