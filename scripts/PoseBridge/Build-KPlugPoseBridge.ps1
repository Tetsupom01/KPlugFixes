param(
    [string]$GameRoot = "F:\illusion\Koikatu"
)

$ErrorActionPreference = "Stop"
$Here = Split-Path -Parent $MyInvocation.MyCommand.Path
$Src  = Join-Path $Here "KPlugPoseBridge.cs"
if (-not (Test-Path -LiteralPath $Src)) {
    $RepoSrc = Join-Path (Split-Path -Parent (Split-Path -Parent $Here)) "src\PoseBridge\KPlugPoseBridge_v0.2.0.1.cs"
    if (Test-Path -LiteralPath $RepoSrc) {
        $Src = $RepoSrc
    }
}
$Out  = Join-Path $Here "KPlugPoseBridge.dll"

$managed = Join-Path $GameRoot "Koikatu_Data\Managed"
$core    = Join-Path $GameRoot "BepInEx\core"

$refs = @(
    (Join-Path $core "BepInEx.dll"),
    (Join-Path $core "0Harmony.dll"),
    (Join-Path $managed "Assembly-CSharp.dll"),
    (Join-Path $managed "UnityEngine.dll")
)

foreach ($r in $refs) {
    if (-not (Test-Path -LiteralPath $r)) { throw "Required reference not found: $r" }
}

$cscCandidates = @(
    "$env:WINDIR\Microsoft.NET\Framework\v3.5\csc.exe",
    "$env:WINDIR\Microsoft.NET\Framework64\v3.5\csc.exe",
    "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe",
    "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
)

$csc = $cscCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $csc) { throw "csc.exe not found." }

Write-Host "Compiler: $csc"
Write-Host "Building KPlug Pose Bridge v0.2.0.1..."

$args = @("/nologo","/target:library","/optimize+","/out:$Out")
foreach ($r in $refs) { $args += "/reference:$r" }
$args += $Src

& $csc @args
if ($LASTEXITCODE -ne 0) { throw "Compile failed. ExitCode=$LASTEXITCODE" }
if (-not (Test-Path -LiteralPath $Out)) { throw "Build output missing: $Out" }

$hash = (Get-FileHash -LiteralPath $Out -Algorithm SHA256).Hash
Write-Host ""
Write-Host "[OK] Built:"
Write-Host $Out
Write-Host "SHA-256: $hash"
