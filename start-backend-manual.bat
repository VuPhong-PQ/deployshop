@echo off
REM Start RetailPointBackend elevated and with working directory
powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath 'C:\shop\server\RetailPointBackend.exe' -ArgumentList '--urls http://0.0.0.0:5273' -WorkingDirectory 'C:\shop\server' -Verb RunAs"
exit /b 0
