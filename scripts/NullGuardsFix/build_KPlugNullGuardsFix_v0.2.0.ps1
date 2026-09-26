param(
    [string]$GameRoot = "F:\illusion\Koikatu",
    [switch]$Install
)

$ErrorActionPreference = "Stop"

$ExpectedKPlugSha256 = "34c13976108db0a18a7ad6b7cfda5517b4a9a83d85c9a909820144264c743855"

$WorkDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ScriptsRoot = Split-Path -Parent $WorkDir
$RepoRoot = Split-Path -Parent $ScriptsRoot

$SrcCandidates = @(
    (Join-Path $RepoRoot "src\NullGuardsFix\KPlugNullGuardsFix_v0.2.0.cs"),
    (Join-Path $WorkDir "KPlugNullGuardsFix_v0.2.0.cs")
)
$Src = $SrcCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if (!$Src) {
    throw "Source not found. Checked:`n  " + ($SrcCandidates -join "`n  ")
}

$BuildDir = Join-Path $WorkDir "build\v0.2.0"
$BuildDll = Join-Path $BuildDir "KPlugNullGuardsFix.dll"
$TempDll = Join-Path $env:TEMP "KPlugNullGuardsFix_v0.2.0.dll"

$DstDir = Join-Path $GameRoot "BepInEx\plugins\KPlugFixes"
$DstDll = Join-Path $DstDir "KPlugNullGuardsFix.dll"

$KPlug = Join-Path $GameRoot "BepInEx\plugins\kPlug\kPlug.dll"
$BepInEx = Join-Path $GameRoot "BepInEx\core\BepInEx.dll"
$UnityEngine = Join-Path $GameRoot "Koikatu_Data\Managed\UnityEngine.dll"

$HarmonyCandidates = @(
    (Join-Path $GameRoot "BepInEx\core\0Harmony.dll"),
    (Join-Path $GameRoot "BepInEx\plugins\0Harmony.dll"),
    (Join-Path $GameRoot "BepInEx\core\BepInEx.Harmony.dll")
)

$cscCandidates = @(
    "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)

$OldPluginCandidates = @(
    (Join-Path $GameRoot "BepInEx\plugins\test\KPlugNullGuardsFix.dll"),
    (Join-Path $GameRoot "BepInEx\plugins\test\KPlugAtHomeDestroyGuard.dll"),
    (Join-Path $GameRoot "BepInEx\plugins\KPlugAtHomeDestroyGuard.dll"),
    (Join-Path $GameRoot "BepInEx\plugins\kPlug\KPlugAtHomeDestroyGuard.dll")
)

foreach ($path in @($KPlug, $BepInEx, $UnityEngine)) {
    if (!(Test-Path $path)) {
        throw "Required file not found: $path"
    }
}

$ActualKPlugSha256 = (Get-FileHash -Algorithm SHA256 $KPlug).Hash.ToLowerInvariant()
if ($ActualKPlugSha256 -ne $ExpectedKPlugSha256) {
    throw @"
Unexpected kPlug.dll SHA-256.
Expected: $ExpectedKPlugSha256
Actual:   $ActualKPlugSha256
No build/install was performed.
"@
}

$Harmony = $HarmonyCandidates |
    Where-Object { Test-Path $_ } |
    Select-Object -First 1

if (!$Harmony) {
    throw "Harmony assembly not found. Checked:`n  " +
        ($HarmonyCandidates -join "`n  ")
}

$csc = $cscCandidates |
    Where-Object { Test-Path $_ } |
    Select-Object -First 1

if (!$csc) {
    throw "csc.exe not found."
}

New-Item -ItemType Directory -Force -Path $BuildDir | Out-Null

if (Test-Path $BuildDll) {
    Remove-Item -Force $BuildDll
}
if (Test-Path $TempDll) {
    Remove-Item -Force $TempDll
}

Write-Host ""
Write-Host "Building KPlugNullGuardsFix v0.2.0..."
Write-Host "Source : $Src"
Write-Host "Harmony: $Harmony"
Write-Host ""

& $csc /nologo /target:library /optimize+ `
    /out:"$TempDll" `
    /reference:"$BepInEx" `
    /reference:"$UnityEngine" `
    /reference:"$Harmony" `
    "$Src"

if ($LASTEXITCODE -ne 0 -or !(Test-Path $TempDll)) {
    if (Test-Path $TempDll) {
        Remove-Item -Force $TempDll
    }
    throw "Build failed. Existing game/plugin state was not changed."
}

Copy-Item -Force $TempDll $BuildDll
$BuildSha = (Get-FileHash $BuildDll -Algorithm SHA256).Hash.ToLowerInvariant()

Write-Host ""
Write-Host "Build succeeded:"
Write-Host "  $BuildDll"
Write-Host "SHA-256:"
Write-Host "  $BuildSha"

if (!$Install) {
    Remove-Item -Force $TempDll

    Write-Host ""
    Write-Host "Build only. Game folder was NOT modified."
    Write-Host "To install deliberately, run:"
    Write-Host ('  powershell.exe -ExecutionPolicy Bypass -File "' +
        $MyInvocation.MyCommand.Path + '" -Install')
    exit 0
}

$MovedThisRun = @()
$BackupDst = $null

try {
    New-Item -ItemType Directory -Force -Path $DstDir | Out-Null

    if (Test-Path $DstDll) {
        $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
        $BackupDst = $DstDll + ".backup_" + $stamp
        Copy-Item -Force $DstDll $BackupDst

        Write-Host ""
        Write-Host "Existing integrated DLL backed up:"
        Write-Host "  $BackupDst"
    }

    foreach ($path in $OldPluginCandidates) {
        if (!(Test-Path $path)) {
            continue
        }

        $disabled = $path + ".integrated_off"
        if (Test-Path $disabled) {
            $disabled = $path + "." +
                (Get-Date -Format "yyyyMMdd-HHmmss") +
                ".integrated_off"
        }

        Move-Item -Force $path $disabled
        $MovedThisRun += [PSCustomObject]@{
            Original = $path
            Disabled = $disabled
        }

        Write-Host ""
        Write-Host "Disabled old standalone plugin:"
        Write-Host "  $path"
        Write-Host "  -> $disabled"
    }

    Copy-Item -Force $TempDll $DstDll

    if (!(Test-Path $DstDll)) {
        throw "Integrated DLL copy failed."
    }

    $InstalledSha =
        (Get-FileHash $DstDll -Algorithm SHA256).Hash.ToLowerInvariant()

    if ($InstalledSha -ne $BuildSha) {
        throw "Installed DLL hash does not match build output."
    }
}
catch {
    Write-Host ""
    Write-Host "Install failed. Rolling back..."

    if (Test-Path $DstDll) {
        Remove-Item -Force $DstDll
    }

    if ($BackupDst -and (Test-Path $BackupDst)) {
        Copy-Item -Force $BackupDst $DstDll
        Write-Host "Restored previous integrated DLL."
    }

    for ($i = $MovedThisRun.Count - 1; $i -ge 0; $i--) {
        $x = $MovedThisRun[$i]

        if ((Test-Path $x.Disabled) -and
            !(Test-Path $x.Original)) {
            Move-Item -Force $x.Disabled $x.Original
            Write-Host "Restored:"
            Write-Host "  $($x.Original)"
        }
    }

    if (Test-Path $TempDll) {
        Remove-Item -Force $TempDll
    }

    throw
}

Remove-Item -Force $TempDll

Write-Host ""
Write-Host "SUCCESS"
Write-Host ""
Write-Host "Installed:"
Write-Host "  $DstDll"
Write-Host "SHA-256:"
Write-Host "  $InstalledSha"
Write-Host ""
Write-Host "Expected startup logs:"
Write-Host "  [NullGuardsFix] Voice guard injected ..."
Write-Host "  [NullGuardsFix] v0.2.0 active. 13 Prefix guards + Voice Transpiler. AtHome destroy guard included."
Write-Host ""
Write-Host "Verification:"
Write-Host "  1) Start Koikatu and confirm the two startup lines above."
Write-Host "  2) Enter/leave MyRoom at least once."
Write-Host "  3) Quit the game normally."
Write-Host "  4) Confirm there is no NullReferenceException at kPlug.CmpBase.AtHomeCtrl.OnDestroy."
Write-Host "  5) Confirm normal H entry / animation change / H exit still work."
