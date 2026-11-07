[CmdletBinding()]
param([string]$GameRoot='F:\illusion\Koikatu')
Set-StrictMode -Version 2.0
$ErrorActionPreference='Stop'
$Root=$PSScriptRoot
$Source=Join-Path $Root 'HPointBenchRouteFix_v1.0.0.cs'
$RequiredSourceSha='1BAF13FCE2CD85114A8A23187E2B74ACB8436AB6603D045C5E08808B193C3F90'
$RequiredA='0038281CAF8DF48A7903C55DC389642EEEB3F2A9114BD9D68AC11C8AC0396BC5'
function Need([string]$p){if(-not [IO.File]::Exists($p)){throw "STOP: Missing file $p"}}
function Hash([string]$p){Need $p; return (Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToUpperInvariant()}
function AssertHash([string]$p,[string]$expected){$h=Hash $p;if($h -ne $expected){throw "STOP: SHA mismatch $p expected=$expected actual=$h"};Write-Host "SHA_OK $p"}
function FindCsc {
  foreach($p in @('C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe','C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe')){
    if([IO.File]::Exists($p)){return $p}
  }
  throw 'STOP: Compatible csc.exe missing'
}
function FindMscorlib2 {
  foreach($p in @('C:\Windows\Microsoft.NET\Framework\v2.0.50727\mscorlib.dll','C:\Windows\Microsoft.NET\Framework64\v2.0.50727\mscorlib.dll')){
    if([IO.File]::Exists($p) -and [Reflection.AssemblyName]::GetAssemblyName($p).Version.ToString() -eq '2.0.0.0'){return $p}
  }
  throw 'STOP: CLR2 mscorlib unavailable; no fallback to CLR4'
}
try {
  if(@(Get-Process -Name 'Koikatu*' -ErrorAction SilentlyContinue).Count -gt 0){throw 'STOP: Close Koikatu before build.'}
  AssertHash $Source $RequiredSourceSha
  $A=Join-Path $GameRoot 'Koikatu_Data\Managed\Assembly-CSharp.dll'
  AssertHash $A $RequiredA
  $Bep=Join-Path $GameRoot 'BepInEx\core\BepInEx.dll'
  $Harmony=Join-Path $GameRoot 'BepInEx\core\0Harmony.dll'
  $Unity=Join-Path $GameRoot 'Koikatu_Data\Managed\UnityEngine.dll'
  foreach($p in @($Bep,$Harmony,$Unity)){Need $p}
  $Csc=FindCsc; $Mscorlib=FindMscorlib2
  $Run=Join-Path $Root ('Output\Build_'+(Get-Date -Format 'yyyyMMdd_HHmmss'))
  $Stage=Join-Path $Run 'stage'
  $DllFolder=Join-Path $Stage 'BepInEx\plugins'
  New-Item -ItemType Directory -Path $DllFolder -Force|Out-Null
  $Dll=Join-Path $DllFolder 'HPointBenchRouteFix.dll'
  $Log=Join-Path $Run 'Compile.log'
  $Args=@('/nologo','/noconfig','/nostdlib+','/target:library','/optimize+','/langversion:3','/codepage:65001',"/out:$Dll","/reference:$Mscorlib","/reference:$Bep","/reference:$Harmony","/reference:$Unity",$Source)
  & $Csc @Args *> $Log
  if($LASTEXITCODE -ne 0){throw "STOP: C# compile failed exit=$LASTEXITCODE log=$Log"}
  Need $Dll
  if((Get-Item -LiteralPath $Dll).Length -lt 1024){throw 'STOP: DLL too small'}
  # Read metadata from memory: ReflectionOnlyLoadFrom keeps the output DLL locked until this PowerShell process ends.
  $DllBytes=[IO.File]::ReadAllBytes($Dll)
  $refs=@([Reflection.Assembly]::ReflectionOnlyLoad($DllBytes).GetReferencedAssemblies())
  $DllBytes=$null
  $core=@($refs|Where-Object{$_.Name -eq 'mscorlib'})
  if($core.Count -ne 1 -or $core[0].Version.ToString() -ne '2.0.0.0'){throw 'STOP: mscorlib 2.0 AssemblyRef mismatch'}
  foreach($r in $refs){if($r.Name -eq 'System.Core' -or $r.Name -eq 'Microsoft.CSharp'){throw "STOP: unsupported reference $($r.Name)"}}
  $DllSha=Hash $Dll
  Copy-Item -LiteralPath (Join-Path $Root 'INSTALL_README_ja.txt') -Destination (Join-Path $Stage 'README_ja.txt')
  Copy-Item -LiteralPath (Join-Path $Root 'INSTALL_README_en.txt') -Destination (Join-Path $Stage 'README_en.txt')
  $Manifest=@(
    'ReleasePackage=HPointBenchRouteFix_v1.0.0',
    'Status=INDEPENDENT_RC1_BUILT_NOT_GAME_TESTED',
    'RuntimeVersion=1.0.0',
    'CandidateRevision=Independent_RC1',
    'PluginDll=BepInEx/plugins/HPointBenchRouteFix.dll',
    ('DllSHA256='+$DllSha),
    ('SourceSHA256='+(Hash $Source)),
    ('AssemblyCSharpSHA256='+(Hash $A)),
    ('Csc='+$Csc),
    ('Mscorlib2='+$Mscorlib),
    ('Mscorlib2SHA256='+(Hash $Mscorlib)),
    ('BepInExSHA256='+(Hash $Bep)),
    ('HarmonySHA256='+(Hash $Harmony)),
    ('UnityEngineSHA256='+(Hash $Unity)),
    ('AssemblyRefs='+ (($refs|ForEach-Object{$_.Name+' '+$_.Version.ToString()}) -join '; ')),
    'TestRequired=Two launches; chair insertion to bench fella; no UI or HPoint regression'
  )
  [IO.File]::WriteAllLines((Join-Path $Run 'MANIFEST.txt'),$Manifest,[Text.Encoding]::UTF8)
  [IO.File]::WriteAllLines((Join-Path $Stage 'MANIFEST.txt'),$Manifest,[Text.Encoding]::UTF8)
  $Zip=Join-Path $Run 'HPointBenchRouteFix_v1.0.0_Independent_RC1_Install_UNTESTED.zip'
  # Build the ZIP with explicit forward-slash entry paths; Compress-Archive may
  # generate environment-dependent separators, causing the exact-path check to fail.
  Add-Type -AssemblyName System.IO.Compression -ErrorAction Stop
  Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction Stop
  $Entries=@(
    @{ Path=$Dll; Entry='BepInEx/plugins/HPointBenchRouteFix.dll' }
    @{ Path=(Join-Path $Stage 'README_ja.txt'); Entry='README_ja.txt' }
    @{ Path=(Join-Path $Stage 'README_en.txt'); Entry='README_en.txt' }
    @{ Path=(Join-Path $Stage 'MANIFEST.txt'); Entry='MANIFEST.txt' }
  )
  foreach($pair in $Entries){Need ([string]$pair.Path)}
  $ArchiveOut=[IO.Compression.ZipFile]::Open($Zip,[IO.Compression.ZipArchiveMode]::Create)
  try {
    foreach($pair in $Entries){
      $created=[IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
        $ArchiveOut,[string]$pair.Path,[string]$pair.Entry,[IO.Compression.CompressionLevel]::Optimal)
      if($null -eq $created){throw 'STOP: ZIP entry create failed'}
    }
  }finally{$ArchiveOut.Dispose()}
  Need $Zip
  $Archive=[IO.Compression.ZipFile]::OpenRead($Zip)
  try {
    $names=@($Archive.Entries|ForEach-Object{$_.FullName})
    $expected=@('BepInEx/plugins/HPointBenchRouteFix.dll','README_ja.txt','README_en.txt','MANIFEST.txt')
    if($names.Count -ne $expected.Count){throw 'STOP: ZIP entry count mismatch'}
    foreach($n in $expected){if(@($names|Where-Object{$_ -ceq $n}).Count -ne 1){throw ('STOP: ZIP entry path mismatch: '+$n+' actual='+($names -join '; '))}}
    $entry=$Archive.GetEntry('BepInEx/plugins/HPointBenchRouteFix.dll')
    if($null -eq $entry){throw 'STOP: ZIP DLL path missing'}
    $stream=$entry.Open()
    try {$sh=[Security.Cryptography.SHA256]::Create();try{$ins=[BitConverter]::ToString($sh.ComputeHash($stream)).Replace('-','')}finally{$sh.Dispose()}}finally{$stream.Dispose()}
    if($ins -ne $DllSha){throw 'STOP: ZIP DLL SHA mismatch'}
  }finally{$Archive.Dispose()}
  Write-Host "BUILD_PACKAGE_OK sha=$DllSha"
  Write-Host "UNTESTED_ZIP=$Zip"
  Write-Host 'NEXT: Send the output ZIP for inspection; DLL is not yet game-tested. Do not deploy without the planned test/restore.'
  exit 0
} catch {
  Write-Host ('[STOP] '+$_.Exception.Message) -ForegroundColor Red
  exit 1
}
