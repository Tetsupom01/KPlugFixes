@echo off
setlocal
cd /d "%~dp0"

echo ==========================================
echo   KPlugNullGuardsFix v0.2.0
echo   NullGuards + AtHome Destroy Guard
echo ==========================================
echo.
echo This will build and install the integrated version.
echo Old standalone DLLs are renamed, not deleted.
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0build_KPlugNullGuardsFix_v0.2.0.ps1" -Install

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
