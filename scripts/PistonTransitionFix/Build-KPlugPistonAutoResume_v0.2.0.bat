@echo off
setlocal
cd /d "%~dp0"

echo ==========================================
echo   KPlugPistonAutoResume v0.2.0
echo ==========================================
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0build_KPlugPistonAutoResume_v0.2.0.ps1"

set "RC=%ERRORLEVEL%"

echo.
if not "%RC%"=="0" (
    echo FAILED. Exit code: %RC%
) else (
    echo SUCCESS.
)

echo.
pause
exit /b %RC%
