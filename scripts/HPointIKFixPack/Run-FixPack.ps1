param(
  [ValidateSet('Install','Status','Collect','Uninstall')][string]$Mode = 'Status',
  [string]$GameRoot = 'F:\illusion\Koikatu'
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0
$Here = Split-Path -Parent $MyInvocation.MyCommand.Path
$GameRoot = [IO.Path]::GetFullPath($GameRoot).TrimEnd('\')
$Root = Join-Path $GameRoot 'BepInEx\plugins'
$Dir = Join-Path $Root 'KPlugFixes'
$State = Join-Path $Here 'FixPack_InstallState.json'
$Results = Join-Path $Here 'Results'
$Names = @('HPointIKStaleExtraIKGuard.dll','ProblemBDeskEnterTransientResetGuard.dll')
$H = @{
 'HPointIKStaleExtraIKGuard.dll'='F167EF2459C695B4A7B1782141E9640E1C013D00B833D1E48D5B0C54FD4ECC54'
 'ProblemBDeskEnterTransientResetGuard.dll'='AC4DCD9D7A809C5A88EE2E9CC3DE60FC5AB4DFCBCA2C5B46B478DC69100F45A6'
 'HPointIKStaleExtraIKGuard.cs'='B9E155DEF6C166FE58ED36E128425092E11AB4BF180F687F63BEA1A5165A6B47'
 'ProblemBDeskEnterTransientResetGuard.cs'='2B6E7A9C6F6A505722105B36CB59852C211CDBA75108DC4D972F3D93C3943493'
 'kPlug'='34C13976108DB0A18A7AD6B7CFDA5517B4A9A83D85C9A909820144264C743855'
 'Assembly-CSharp'='0038281CAF8DF48A7903C55DC389642EEEB3F2A9114BD9D68AC11C8AC0396BC5'
}
$TargetRC1=Join-Path $Dir $Names[0]
$TargetB=Join-Path $Dir $Names[1]
function Sha([string]$p){
 if(-not (Test-Path -LiteralPath $p -PathType Leaf)){throw "MISSING_FILE: $p"}
 return (Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToUpperInvariant()
}
function AssertHash([string]$p,[string]$want,[string]$label){
 $got=Sha $p
 if($got -cne $want){throw "SHA_MISMATCH $label wanted=$want actual=$got path=$p"}
 Write-Host "[OK] $label SHA256=$got"
}
function AssertClosed{
 foreach($name in @('Koikatu','KoikatuVR','Koikatu_x64','Koikatsu')) {
   if(@(Get-Process -Name $name -ErrorAction SilentlyContinue).Count -gt 0){throw "GAME_RUNNING: $name. Close game first."}
 }
}
function EnsureDir([string]$p){if(-not(Test-Path -LiteralPath $p -PathType Container)){New-Item -ItemType Directory -Path $p -ErrorAction Stop|Out-Null}}
function WriteUtf8([string]$p,[string]$s){[IO.File]::WriteAllText($p,$s,(New-Object Text.UTF8Encoding($false)))}
function AssertBaseline {
 AssertHash (Join-Path $Root 'kPlug\kPlug.dll') $H['kPlug'] 'kPlug'
 AssertHash (Join-Path $GameRoot 'Koikatu_Data\Managed\Assembly-CSharp.dll') $H['Assembly-CSharp'] 'Assembly-CSharp'
}
function AssertPackage{
 foreach($n in $Names){AssertHash (Join-Path $Here (Join-Path 'bin' $n)) $H[$n] "Bundled $n"}
 foreach($n in @('HPointIKStaleExtraIKGuard.cs','ProblemBDeskEnterTransientResetGuard.cs')){AssertHash (Join-Path $Here (Join-Path 'src' $n)) $H[$n] "Frozen source $n"}
}
function AssertRuntimeDeps{
 foreach($p in @((Join-Path $GameRoot 'BepInEx\core\0Harmony.dll'),(Join-Path $GameRoot 'BepInEx\core\BepInEx.dll'))){if(-not(Test-Path -LiteralPath $p -PathType Leaf)){throw "RUNTIME_DEP_MISSING: $p"}}
 $v=[Reflection.AssemblyName]::GetAssemblyName((Join-Path $GameRoot 'BepInEx\core\0Harmony.dll')).Version
 if($v.Major -ne 2 -or $v.Minor -ne 9){throw "HARMONY_BASELINE_UNEXPECTED: $v"}
 Write-Host "[OK] Harmony $v"
}
function AssertNoDuplicate {
 foreach($name in $Names){
   $dest=Join-Path $Dir $name
   $dups=@(Get-ChildItem -LiteralPath $Root -Recurse -File -Filter $name -ErrorAction Stop | Where-Object { -not [string]::Equals($_.FullName,$dest,[StringComparison]::OrdinalIgnoreCase) })
   if($dups.Count -gt 0){throw ('DUPLICATE_PLUGIN: '+($dups.FullName -join '; '))}
 }
}
function Diagnostics {
 $diagnostics=@('HPointEnterFlowDiag.dll','ProblemBEnterIKWriterTrace.dll','HPointIKTargetFlowDiag.dll','HPointIKTargetRestoreFix.dll','HPointIKPreApplyRestore.dll','HPointIKStateBranchDiag.dll','HPointILRuntimeProbe.dll')
 $found=@()
 foreach($n in $diagnostics){if(Test-Path -LiteralPath (Join-Path $Dir $n) -PathType Leaf){$found += $n}}
 if($found.Count -gt 0){Write-Host ('[NORMAL_MODE_PENDING] External diagnostic/experimental DLLs: '+($found -join ', ')) -ForegroundColor Yellow;Write-Host '[ACTION] Remove only with THEIR ORIGINAL ownership-aware CollectRemove packages. This FixPack never removes them.'}
 else{Write-Host '[OK] Listed HPoint diagnostics/experimental DLLs absent (not a global plugin audit).'}
}
function ReadState{
 if(-not(Test-Path -LiteralPath $State -PathType Leaf)){throw 'FIXPACK_STATE_MISSING: no ownership record. No destructive operation permitted.'}
 $m=Get-Content -LiteralPath $State -Raw -Encoding UTF8|ConvertFrom-Json
 if($m.package -cne 'HPointIKFixPack_v1.0.0' -or -not [string]::Equals([string]$m.gameRoot,$GameRoot,[StringComparison]::OrdinalIgnoreCase) -or
  $m.rc1Sha -cne $H[$Names[0]] -or $m.problemBSha -cne $H[$Names[1]] -or
  @('preexisting_verified','fixpack_installed') -cnotcontains [string]$m.rc1Origin -or $m.problemBOrigin -cne 'fixpack_installed'){
  throw 'FIXPACK_STATE_INVALID: refusing action.'
 }
 AssertHash $TargetRC1 $H[$Names[0]] 'Installed RC1'
 AssertHash $TargetB $H[$Names[1]] 'Installed ProblemB'
 return $m
}
function CopyIf([string]$s,[string]$dest,[string]$name){if(Test-Path -LiteralPath $s -PathType Leaf){Copy-Item -LiteralPath $s -Destination (Join-Path $dest $name) -ErrorAction Stop;return $true};return $false}
function MakeZip([string]$dir,[string]$target,[string[]]$required){
 Compress-Archive -Path (Join-Path $dir '*') -DestinationPath $target -CompressionLevel Optimal -ErrorAction Stop
 Add-Type -AssemblyName System.IO.Compression.FileSystem
 $z=[IO.Compression.ZipFile]::OpenRead($target)
 try{
  foreach($name in $required){if(@($z.Entries|Where-Object {$_.Name -ceq $name}).Count -ne 1){throw "ZIP_ENTRY_MISSING: $name"}}
  foreach($entry in $z.Entries){$s=$entry.Open();try{$buf=New-Object byte[] 65536;$total=[long]0;while(($k=$s.Read($buf,0,$buf.Length)) -gt 0){$total+=$k};if($total -ne $entry.Length){throw "ZIP_SIZE_MISMATCH: $($entry.Name)"}}finally{$s.Dispose()}}
 }finally{$z.Dispose()}
 Write-Host "[ZIP_OK] $target SHA256=$(Sha $target)"
}
function InstallPack{
 AssertClosed; AssertPackage; AssertBaseline; AssertRuntimeDeps
 if(-not(Test-Path -LiteralPath $Root -PathType Container)){throw "PLUGIN_ROOT_MISSING: $Root"}
 if(Test-Path -LiteralPath $State -PathType Leaf){throw 'FIXPACK_ALREADY_REGISTERED: use 02_Status; no overwrite.'}
 AssertNoDuplicate
 $rExists=Test-Path -LiteralPath $TargetRC1 -PathType Leaf
 $bExists=Test-Path -LiteralPath $TargetB -PathType Leaf
 if($rExists){AssertHash $TargetRC1 $H[$Names[0]] 'Existing frozen RC1'}
 if($bExists){Write-Host "[EXISTING] ProblemB SHA256=$(Sha $TargetB)";throw 'PROBLEMB_EXISTS_UNMANAGED: do not overwrite or adopt. If installed by older single release, uninstall with its own 04_Uninstall.bat first.'}
 if(-not $rExists){
  foreach($n in @('HPointIKTargetFlowDiag.dll','HPointIKTargetRestoreFix.dll','HPointIKPreApplyRestore.dll','HPointIKStateBranchDiag.dll','HPointILRuntimeProbe.dll')){if(Test-Path -LiteralPath (Join-Path $Dir $n) -PathType Leaf){throw "RC1_CONFLICT: $n"}}
 }
 $origin=if($rExists){'preexisting_verified'}else{'fixpack_installed'}
 EnsureDir $Dir
 $pending=@()
 if(-not $rExists){$pending += $Names[0]}
 $pending += $Names[1]
 foreach($n in $pending){if(Test-Path -LiteralPath (Join-Path $Dir ($n+'.fixpack_staging')) -PathType Leaf){throw "UNKNOWN_STAGE_EXISTS: $n"}}
 $created=@();$staged=@();$stateWritten=$false
 try{
  foreach($n in $pending){
   $temp=Join-Path $Dir ($n+'.fixpack_staging');Copy-Item -LiteralPath (Join-Path $Here (Join-Path 'bin' $n)) -Destination $temp -ErrorAction Stop
   $staged += $temp;AssertHash $temp $H[$n] "Staged $n"
  }
  AssertClosed;AssertBaseline
  foreach($n in $pending){$dest=Join-Path $Dir $n;$temp=Join-Path $Dir ($n+'.fixpack_staging')
    if(Test-Path -LiteralPath $dest -PathType Leaf){throw "DEST_APPEARED: $dest"}
    [IO.File]::Move($temp,$dest);$created += $dest;AssertHash $dest $H[$n] "Installed $n"
  }
  if($origin -eq 'preexisting_verified'){AssertHash $TargetRC1 $H[$Names[0]] 'Still frozen external RC1'}
  $m=[ordered]@{package='HPointIKFixPack_v1.0.0';gameRoot=$GameRoot;rc1Sha=$H[$Names[0]];problemBSha=$H[$Names[1]];rc1Origin=$origin;problemBOrigin='fixpack_installed';runtimeRC1='0.2.0 EXP RC1';runtimeProblemB='0.1.0 RC0';rebuildPerformed=$false;installedAt=(Get-Date).ToString('o')}
  $j=$m|ConvertTo-Json -Depth 5
  WriteUtf8 $State $j;$stateWritten=$true
  Write-Host "[INSTALLED] FixPack v1.0.0; RC1 origin=$origin; ProblemB installed. No rebuild."
  if($origin -eq 'preexisting_verified'){Write-Host '[PRESERVED] Preexisting RC1 and its original ownership manifest untouched.'}
  Diagnostics
  Write-Host '[NEXT] Check current-launch RC1 and ProblemB READY logs, then run the standard desk/point transition test.'
 }catch{
  $err=$_; $unsafe=$false
  foreach($p in $staged){if(Test-Path -LiteralPath $p -PathType Leaf){$n=([IO.Path]::GetFileName($p) -replace '\.fixpack_staging$','');if((Sha $p) -ceq $H[$n]){Remove-Item -LiteralPath $p -Force -ErrorAction Stop}else{$unsafe=$true;Write-Host "[ROLLBACK_STOP] Unknown staging file preserved: $p"}}}
  foreach($p in $created){if(Test-Path -LiteralPath $p -PathType Leaf){$n=[IO.Path]::GetFileName($p);if((Sha $p) -ceq $H[$n]){Remove-Item -LiteralPath $p -Force -ErrorAction Stop;Write-Host "[ROLLBACK] Removed new $n"}else{$unsafe=$true;Write-Host "[ROLLBACK_STOP] Unexpected target content preserved: $p"}}}
  if($stateWritten -and -not $unsafe -and (Test-Path -LiteralPath $State -PathType Leaf)){Remove-Item -LiteralPath $State -Force -ErrorAction Stop}
  if($unsafe){Write-Host '[STOP] Investigate preserved files before retry.'}
  throw $err
 }
}
function StatusPack{
 AssertPackage;AssertBaseline;AssertNoDuplicate
 if(-not(Test-Path -LiteralPath $State -PathType Leaf)){
  foreach($n in $Names){$p=Join-Path $Dir $n;if(Test-Path -LiteralPath $p -PathType Leaf){Write-Host "[PRESENT_UNMANAGED] $n SHA256=$(Sha $p)"}else{Write-Host "[ABSENT] $n"}}
  Write-Host '[PENDING] No FixPack ownership state; use 01_Install or investigate existing DLL first.'
 }else{
  $m=ReadState;Write-Host "[OK] FixPack v1.0.0 state verified; RC1 origin=$($m.rc1Origin); both exact successful DLLs."
 }
 foreach($item in @(@{n='RC1';p=(Join-Path $Dir 'HPointIKStaleExtraIKGuard.log')},@{n='ProblemB';p=(Join-Path $Dir 'ProblemBDeskEnterTransientResetGuard.log')})){
  if(Test-Path -LiteralPath $item.p -PathType Leaf){Write-Host "[LOG] $($item.n) LastWrite=$((Get-Item -LiteralPath $item.p).LastWriteTime.ToString('o'))";Get-Content -LiteralPath $item.p -Tail 12 -ErrorAction Stop|ForEach-Object{Write-Host $_}}
  else{Write-Host "[PENDING] $($item.n) log missing until game loads."}
 }
 Write-Host '[NOTE] Check READY timestamp is from the CURRENT game start; old log cannot prove startup.'
 Diagnostics
}
function SaveEvidence([string]$dest){
 EnsureDir $dest
 foreach($n in $Names){$p=Join-Path $script:Dir $n;if(Test-Path -LiteralPath $p -PathType Leaf){Copy-Item -LiteralPath $p -Destination (Join-Path $dest ($n+'.installed')) -ErrorAction Stop;AssertHash (Join-Path $dest ($n+'.installed')) $H[$n] "Evidence $n"}}
 CopyIf $State $dest 'FixPack_InstallState.json'|Out-Null
 foreach($n in @('HPointIKStaleExtraIKGuard.log','ProblemBDeskEnterTransientResetGuard.log')){CopyIf (Join-Path $script:Dir $n) $dest $n|Out-Null}
 foreach($i in @(@{p=(Join-Path $GameRoot 'BepInEx\LogOutput.log');n='LogOutput.log'},@{p=(Join-Path $GameRoot 'output_log.txt');n='output_log_root.txt'},@{p=(Join-Path $GameRoot 'Koikatu_Data\output_log.txt');n='output_log_data.txt'})){CopyIf $i.p $dest $i.n|Out-Null}
}
function CollectPack{
 AssertClosed;AssertBaseline;$m=ReadState
 $stamp=Get-Date -Format 'yyyyMMdd_HHmmss_fff';EnsureDir $Results
 $dir=Join-Path $Results ('Collect_'+$stamp);SaveEvidence $dir
 $missing=@();foreach($n in @('HPointIKStaleExtraIKGuard.log','ProblemBDeskEnterTransientResetGuard.log')){if(-not(Test-Path -LiteralPath (Join-Path $dir $n) -PathType Leaf)){$missing += $n}}
 $result=if($missing.Count -gt 0){'INCOMPLETE_MISSING_LOG'}else{'COLLECTED_BOTH_LOGS'}
 WriteUtf8 (Join-Path $dir 'Result.txt') "CaptureStatus=$result`r`nMissing=$($missing -join ',')`r`nRC1Origin=$($m.rc1Origin)`r`nNoDLLRemoved=True`r`n"
 $zip=Join-Path $Here ('FixPackCollect_'+$stamp+'.zip')
 MakeZip $dir $zip @('FixPack_InstallState.json','HPointIKStaleExtraIKGuard.dll.installed','ProblemBDeskEnterTransientResetGuard.dll.installed','Result.txt')
 Write-Host "[COLLECT] $result; both DLLs KEPT installed. Upload $zip"
}
function UninstallPack{
 AssertClosed;AssertBaseline;$m=ReadState
 $stamp=Get-Date -Format 'yyyyMMdd_HHmmss_fff';EnsureDir $Results
 $dir=Join-Path $Results ('PreUninstall_'+$stamp);SaveEvidence $dir
 $summary="Action=PreUninstall`r`nRC1Origin=$($m.rc1Origin)`r`nWillRemoveProblemB=True`r`nWillRemoveRC1=$($m.rc1Origin -ceq 'fixpack_installed')`r`n"
 WriteUtf8 (Join-Path $dir 'Result.txt') $summary
 $zip=Join-Path $Here ('FixPackPreUninstall_'+$stamp+'.zip')
 MakeZip $dir $zip @('FixPack_InstallState.json','HPointIKStaleExtraIKGuard.dll.installed','ProblemBDeskEnterTransientResetGuard.dll.installed','Result.txt')
 AssertClosed;AssertHash $TargetRC1 $H[$Names[0]] 'RC1 before uninstall';AssertHash $TargetB $H[$Names[1]] 'ProblemB before uninstall'
 Remove-Item -LiteralPath $TargetB -Force -ErrorAction Stop
 if($m.rc1Origin -ceq 'fixpack_installed'){
   AssertHash $TargetRC1 $H[$Names[0]] 'Pack-owned RC1 before uninstall';Remove-Item -LiteralPath $TargetRC1 -Force -ErrorAction Stop
   Write-Host '[REMOVED] Both DLLs installed by this FixPack.'
 }else{AssertHash $TargetRC1 $H[$Names[0]] 'Preserved external RC1';Write-Host '[PRESERVED] Original external RC1, ownership and original package remain intact.'}
 Remove-Item -LiteralPath $State -Force -ErrorAction Stop
 AssertBaseline
 Write-Host "[UNINSTALLED] Recovery evidence ZIP: $zip"
}
$success=$false;$transcript=$false;$runLog=''
try {
 EnsureDir $Results
 $runFolder=Join-Path $Results ($Mode+'_'+(Get-Date -Format 'yyyyMMdd_HHmmss_fff'))
 EnsureDir $runFolder
 $runLog=Join-Path $runFolder 'Operation.log'
 Start-Transcript -LiteralPath $runLog -ErrorAction Stop|Out-Null;$transcript=$true
 Write-Host "=== HPoint IK FixPack v1.0.0 | $Mode | GameRoot=$GameRoot ==="
 switch($Mode){'Install'{InstallPack};'Status'{StatusPack};'Collect'{CollectPack};'Uninstall'{UninstallPack}}
 $success=$true;Write-Host '[SUCCESS]'
} catch {Write-Host ('[STOP] '+$_.Exception.Message) -ForegroundColor Red;Write-Host '[NOTE] No unverified DLL is deleted. Keep existing Results/ and send Operation.log.'}
finally {if($transcript){try{Stop-Transcript|Out-Null}catch{}}}
if($runLog -ne ''){Write-Host "OperationLog=$runLog"}
if($success){exit 0}else{exit 1}

