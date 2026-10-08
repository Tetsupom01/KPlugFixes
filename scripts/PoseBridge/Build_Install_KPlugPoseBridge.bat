@echo off
setlocal EnableExtensions

set "ROOT=F:\illusion\Koikatu"
set "HERE=%~dp0"
set "PS1=%HERE%Build-KPlugPoseBridge.ps1"
set "DLL=%HERE%KPlugPoseBridge.dll"
set "PLUGIN=%ROOT%\BepInEx\plugins\KPlugPoseBridge.dll"

echo ============================================================
echo KPlug Pose Bridge v0.2.0.0
echo Build + Install
echo mode=2 / sync vanilla main UI to kPlug GetAvailablePiston
echo ============================================================
echo Root: %ROOT%
echo.

if not exist "%PS1%" (
  echo [ERROR] Build script not found:
  echo %PS1%
  echo.
  pause
  exit /b 1
)

powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%PS1%" -GameRoot "%ROOT%"
if errorlevel 1 (
  echo.
  echo [ERROR] Build failed.
  pause
  exit /b 1
)

if not exist "%DLL%" (
  echo.
  echo [ERROR] DLL was not created:
  echo %DLL%
  echo.
  pause
  exit /b 1
)

if exist "%PLUGIN%" (
  copy /y "%PLUGIN%" "%PLUGIN%.bak" >nul
  echo Previous bridge backed up:
  echo %PLUGIN%.bak
)

copy /y "%DLL%" "%PLUGIN%" >nul
if errorlevel 1 (
  echo.
  echo [ERROR] Install failed.
  pause
  exit /b 1
)

echo.
echo [OK] Installed:
echo %PLUGIN%
echo.
echo TEST:
echo   1. Enter normal H at an ordinary floor point.
echo   2. Open the VANILLA insertion-position list without opening Pose Selector.
echo   3. Confirm the list looks like the Pose Selector set; floor expected around 77.
echo   4. Select one vanilla-origin pose and one kPlug-added pose from the vanilla UI.
echo   5. Move to a desk or wall H point.
echo   6. Re-open the vanilla insertion list and confirm it changes to that location.
echo   7. If possible compare with U Pose Selector at the same point.
echo   8. Exit normally and send output_log.txt plus screenshots if useful.
echo.
echo Expected log: [PoseBridge] SYNC_OK
echo If SYNC_ABORT appears, the bridge leaves the existing list untouched.
echo.
pause
exit /b 0
