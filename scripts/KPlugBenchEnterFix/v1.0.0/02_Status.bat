@echo off
setlocal
cd /d "%~dp0"
echo === KPlugBenchEnterFix v1.0.0 RC1 - Status ===
set "PS1=%~dp0Run-KPlugBenchEnterFix.ps1"
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$tok=$null;$err=$null;[void][System.Management.Automation.Language.Parser]::ParseFile($env:PS1,[ref]$tok,[ref]$err);if($err.Count -gt 0){$err|ForEach-Object {Write-Output $_};exit 1};Write-Output 'PS_PARSE_OK'"
if errorlevel 1 (
  echo [STOP] PowerShell parse failed; no deployment.
  pause
  exit /b 1
)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%PS1%" -Action Status
set "RC=%errorlevel%"
echo === COMPLETE exit=%RC% ===
pause
exit /b %RC%
