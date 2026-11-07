@echo off
setlocal
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build-HPointBenchRouteFix.ps1"
set "RC=%ERRORLEVEL%"
echo RESULT_CODE=%RC%
pause
exit /b %RC%
