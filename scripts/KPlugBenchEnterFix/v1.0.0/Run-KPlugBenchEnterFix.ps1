param(
    [ValidateSet('Audit','Deploy','Status','Collect')][string]$Action='Status'
)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$here=Split-Path -Parent $MyInvocation.MyCommand.Path
$game='F:\illusion\Koikatu'
$bep=Join-Path $game 'BepInEx'
$plugins=Join-Path $bep 'plugins'
$target=Join-Path $plugins 'KPlugFixes\KPlugBenchEnterFix.dll'
$cf=Join-Path $plugins 'KPlugFixes\KPlugCrossFaderCompatFix.dll'
$source=Join-Path $here 'KPlugBenchEnterFix.cs'
$stateFile=Join-Path $here 'InstallState.json'
$expectedSource='3C95474583C45FEBD672985EC620D5E5E4CFD41D26A5DBB1E740DCAC952A4DB8'
$expectedKPlug='34C13976108DB0A18A7AD6B7CFDA5517B4A9A83D85C9A909820144264C743855'
$expectedGame='0038281CAF8DF48A7903C55DC389642EEEB3F2A9114BD9D68AC11C8AC0396BC5'
$expectedCF='2392CCAFE5663EFC4FD80F8F72E9741796DE1DDD85D92F9A0E2C0F6C3E45ACFC'
$bench=Join-Path $plugins 'HPointBenchRouteFix.dll'
$expectedBench='170757DA0A4880CF2F8CF23C7E206C70BC6F9487F0051AAB470353D57A3408BD'
function Hash([string]$p){if(-not(Test-Path -LiteralPath $p -PathType Leaf)){throw "MISSING: $p"};return (Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToUpperInvariant()}
function AssertHash([string]$p,[string]$sha){$actual=Hash $p;if($actual -ne $sha){throw "SHA_MISMATCH: $p expected=$sha actual=$actual"}}
function GameIsClosed {if(Get-Process -Name 'Koikatu' -ErrorAction SilentlyContinue){throw 'STOP: Koikatu is running. Close the game first.'}}
function CheckBaseline {
    AssertHash (Join-Path $plugins 'kPlug\kPlug.dll') $expectedKPlug
    AssertHash (Join-Path $game 'Koikatu_Data\Managed\Assembly-CSharp.dll') $expectedGame
    AssertHash $cf $expectedCF
    AssertHash $bench $expectedBench
}
# Perform read-only signature checks against the exact SHA-pinned installed kPlug.dll.
# ReflectionOnlyLoadFrom never invokes a target game's code.
function FindExactMethod([type]$ownerType,[string]$methodName,[System.Reflection.BindingFlags]$flags,[string[]]$expectedParams) {
    # Compare metadata full names, not System.Type identity from different CLR reflection contexts.
    $matches=@()
    foreach($method in $ownerType.GetMethods($flags)) {
        if($method.Name -cne $methodName){continue}
        $actual=@($method.GetParameters() | ForEach-Object {$_.ParameterType.FullName})
        if($actual.Count -ne $expectedParams.Count){continue}
        $same=$true
        for($i=0;$i -lt $actual.Count;$i++) {
            if($actual[$i] -cne $expectedParams[$i]){$same=$false;break}
        }
        if($same){$matches+=,$method}
    }
    if($matches.Count -ne 1){throw ('HOOK_SIGNATURE_STOP: '+$ownerType.FullName+'.'+$methodName+' matches='+$matches.Count)}
    return $matches[0]
}
# Read-only metadata comparison against the exact SHA-pinned installed kPlug.dll.
function CheckHookSignatures {
    $kplugPath=Join-Path $plugins 'kPlug\kPlug.dll'
    AssertHash $kplugPath $expectedKPlug
    # ReflectionOnlyGetType resolves base classes and interface types. Without
    # a resolver, kPlug's UnityEngine dependency triggers a GetType exception.
    # Resolve from installed DLL files by assembly identity, NEVER execute game code.
    $roots=@(
        (Join-Path $game 'Koikatu_Data\Managed'),
        (Join-Path $plugins 'kPlug'),
        (Join-Path $bep 'core'),
        (Join-Path $plugins 'KPlugFixes'),
        (Join-Path $plugins 'test')
    )
    $resolverBlock={
        param($sender,$eventArgs)
        $requested=New-Object System.Reflection.AssemblyName($eventArgs.Name)
        # Prefer an assembly already loaded in the reflection-only context.
        foreach($loaded in [AppDomain]::CurrentDomain.ReflectionOnlyGetAssemblies()) {
            if($loaded.FullName -ceq $eventArgs.Name){return $loaded}
        }
        foreach($directory in $roots) {
            $candidate=Join-Path $directory ($requested.Name+'.dll')
            if(-not(Test-Path -LiteralPath $candidate -PathType Leaf)){continue}
            $identity=[System.Reflection.AssemblyName]::GetAssemblyName($candidate)
            if($identity.Name -cne $requested.Name){continue}
            # Fail closed rather than bind a different non-zero dependency version.
            if($requested.Version -ne $null -and $requested.Version.ToString() -ne '0.0.0.0' -and
               $identity.Version.ToString() -ne $requested.Version.ToString()){continue}
            return [System.Reflection.Assembly]::ReflectionOnlyLoadFrom($candidate)
        }
        # Framework references can live in the GAC, outside Koikatu's dirs.
        try {return [System.Reflection.Assembly]::ReflectionOnlyLoad($eventArgs.Name)}
        catch {return $null}
    }
    $resolver=[System.ResolveEventHandler]($resolverBlock.GetNewClosure())
    $domain=[AppDomain]::CurrentDomain
    $domain.add_ReflectionOnlyAssemblyResolve($resolver)
    try {
        $unityPath=Join-Path $game 'Koikatu_Data\Managed\UnityEngine.dll'
        if(-not(Test-Path -LiteralPath $unityPath -PathType Leaf)){throw 'HOOK_DEPENDENCY_STOP: UnityEngine.dll missing'}
        $unityAssembly=[System.Reflection.Assembly]::ReflectionOnlyLoadFrom($unityPath)
        if($unityAssembly.GetName().Name -cne 'UnityEngine'){throw 'HOOK_DEPENDENCY_STOP: UnityEngine identity mismatch'}
        $kplugAsm=[System.Reflection.Assembly]::ReflectionOnlyLoadFrom($kplugPath)
        $parser=$kplugAsm.GetType('kPlug.Inputs.HinputParser',$false)
        $combo=$kplugAsm.GetType('kPlug.CmpH.HComboCtrl',$false)
        $pose=$kplugAsm.GetType('kPlug.DefAndParse.Def+PoseMod',$false)
        if($null -eq $parser -or $null -eq $combo -or $null -eq $pose){throw 'HOOK_SIGNATURE_STOP: required kPlug types missing'}
        $st=[System.Reflection.BindingFlags]'Public,NonPublic,Static'
        $it=[System.Reflection.BindingFlags]'Public,NonPublic,Instance'
        $actionMethod=FindExactMethod $parser 'Action' $st @('System.Boolean','System.Boolean','System.Single','System.Boolean','System.Boolean')
        $chainMethod=FindExactMethod $parser 'Chain' $st @()
        $pistonMethod=FindExactMethod $combo 'ToPistonChain' $it @($pose.FullName,'System.Boolean')
        $clothes=$kplugAsm.GetType('kPlug.Tools.Clothes',$false)
        if($null -eq $clothes){throw 'HOOK_SIGNATURE_STOP: kPlug.Tools.Clothes missing'}
        $clothesMethod=FindExactMethod $clothes 'ShowKokan' $st @('ChaControl')
        if(-not $clothesMethod.IsStatic -or $clothesMethod.ReturnType.FullName -cne 'System.Void') {
            throw 'HOOK_SIGNATURE_STOP: static void kPlug.Tools.Clothes.ShowKokan(ChaControl) missing'
        }
        if(-not $actionMethod.IsStatic -or $actionMethod.ReturnType.FullName -cne 'System.Void') {
            throw 'HOOK_SIGNATURE_STOP: static void HinputParser.Action(bool,bool,float,bool,bool) missing'
        }
        if(-not $chainMethod.IsStatic -or $chainMethod.ReturnType.FullName -cne 'System.Void') {
            throw 'HOOK_SIGNATURE_STOP: static void HinputParser.Chain() missing'
        }
        if($pistonMethod.IsStatic -or $pistonMethod.ReturnType.FullName -cne 'System.Void') {
            throw 'HOOK_SIGNATURE_STOP: instance void HComboCtrl.ToPistonChain(PoseMod,bool) missing'
        }
        # SHA-pinned Assembly-CSharp metadata: the new Proc hook makes one
        # tightly scoped click write; fail closed on ANY unexpected field layout.
        $gamePath=Join-Path $game 'Koikatu_Data\Managed\Assembly-CSharp.dll'
        AssertHash $gamePath $expectedGame
        $gameAsm=[System.Reflection.Assembly]::ReflectionOnlyLoadFrom($gamePath)
        $houshi=$gameAsm.GetType('HHoushi',$false)
        $hbase=$gameAsm.GetType('HActionBase',$false)
        $hflag=$gameAsm.GetType('HFlag',$false)
        $hvoice=$gameAsm.GetType('HVoiceCtrl',$false)
        $cha=$gameAsm.GetType('ChaControl',$false)
        if($null -eq $houshi -or $null -eq $hbase -or $null -eq $hflag -or
           $null -eq $hvoice -or $null -eq $cha) {
             throw 'HOOK_SIGNATURE_STOP: Idle probe game types missing'
        }
        $nativeProc=FindExactMethod $houshi 'Proc' $it @()
        $animState=FindExactMethod $cha 'getAnimatorStateInfo' $it @('System.Int32')
        if($nativeProc.IsStatic) { throw 'HOOK_SIGNATURE_STOP: HHoushi.Proc expected instance, actual static' }
        if($nativeProc.ReturnType.FullName -cne 'System.Boolean') {
            throw ('HOOK_SIGNATURE_STOP: HHoushi.Proc expected bool, actual '+$nativeProc.ReturnType.FullName)
        }
        if($animState.IsStatic) { throw 'HOOK_SIGNATURE_STOP: ChaControl.getAnimatorStateInfo expected instance, actual static' }
        if($animState.ReturnType.FullName -cne 'UnityEngine.AnimatorStateInfo') {
            throw ('HOOK_SIGNATURE_STOP: ChaControl.getAnimatorStateInfo expected AnimatorStateInfo, actual '+$animState.ReturnType.FullName)
        }
        foreach($pair in @(
           @($hflag,'voiceWait','System.Boolean'),
           @($hbase,'female','ChaControl'),
           @($hbase,'voice','HVoiceCtrl'),
           @($houshi,'autoStart','System.Boolean'))) {
            $fd=$pair[0].GetField([string]$pair[1],$it)
            if($null -eq $fd -or $fd.FieldType.FullName -cne [string]$pair[2]) {
                throw ('HOOK_SIGNATURE_STOP: missing/changed field '+$pair[0].FullName+'.'+$pair[1])
            }
        }
        # One-shot native click replay relies on an enum-valued writable HFlag.click.
        # Verify the exact field and enum member names before installing the candidate.
        $clickField=$hflag.GetField('click',$it)
        if($null -eq $clickField -or -not $clickField.FieldType.IsEnum -or
           $clickField.IsInitOnly -or $clickField.IsLiteral -or
           $null -eq $clickField.FieldType.GetField('none',$st) -or
           $null -eq $clickField.FieldType.GetField('speedup',$st)) {
            throw 'HOOK_FIELD_STOP: HFlag.click enum none/speedup not writable/available'
        }
        $voicesField=$hvoice.GetField('nowVoices',$it)
        if($null -eq $voicesField -or -not $voicesField.FieldType.IsArray){
            throw 'HOOK_SIGNATURE_STOP: HVoiceCtrl.nowVoices must be an array'
        }
        return [ordered]@{
            GameSHA256=$expectedGame
            IdleStartProcSignature=$nativeProc.ToString()
            AnimatorStateSignature=$animState.ToString()
            KplugSHA256=$expectedKPlug
            ActionSignature=$actionMethod.ToString();ActionStatic=$actionMethod.IsStatic
            ChainSignature=$chainMethod.ToString();ChainStatic=$chainMethod.IsStatic
            PistonSignature=$pistonMethod.ToString();PistonStatic=$pistonMethod.IsStatic
            ClothesSignature=$clothesMethod.ToString();ClothesStatic=$clothesMethod.IsStatic
            Result='PASS: read-only metadata, dependency resolution, parameter/return/static signatures'
        }
    }
    catch {throw ('HOOK_METADATA_STOP: '+$_.Exception.Message)}
    finally {$domain.remove_ReflectionOnlyAssemblyResolve($resolver)}
}
function FindCompiler {
    foreach($p in @('C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe','C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe')){if(Test-Path -LiteralPath $p){return $p}}
    throw 'No legacy .NET Framework csc.exe.'
}
function FindMscorlib2 {
    foreach($p in @('C:\Windows\Microsoft.NET\Framework\v2.0.50727\mscorlib.dll','C:\Windows\Microsoft.NET\Framework64\v2.0.50727\mscorlib.dll')){if(Test-Path -LiteralPath $p){return $p}}
    throw 'No CLR 2.0 mscorlib found.'
}
function WriteJson([object]$obj,[string]$file){$obj|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $file -Encoding UTF8}
function GetState {
    if(-not(Test-Path -LiteralPath $stateFile)){throw 'InstallState.json missing. Deploy was not confirmed.'}
    $o=Get-Content -LiteralPath $stateFile -Raw -Encoding UTF8 | ConvertFrom-Json
    if($o.Tool -ne 'KPlugBenchEnterFix' -or $o.Version -ne '1.0.0' -or $o.GameRoot -ne $game -or $o.Target -ne $target){throw 'InstallState owner/path mismatch.'}
    return $o
}
function EnsureOwn([object]$state) {
    if(Test-Path -LiteralPath $target) {AssertHash $target $state.CandidateSHA256; return 'OWNED_PRESENT'}
    return 'ALREADY_REMOVED'
}
function Deploy {
    GameIsClosed
    $astTokens=$null;$astErrors=$null
    $scriptPath=Join-Path $here 'Run-KPlugBenchEnterFix.ps1'
    [void][System.Management.Automation.Language.Parser]::ParseFile($scriptPath,[ref]$astTokens,[ref]$astErrors)
    if($astErrors.Count -gt 0){throw ('PS_PARSE_STOP: '+(($astErrors | ForEach-Object {$_.Message}) -join '; '))}
    if(Test-Path -LiteralPath $stateFile){throw 'STOP: InstallState exists. Use Collect on that run, or unpack a new folder.'}
    CheckBaseline;AssertHash $source $expectedSource
    $hookSignatures=CheckHookSignatures
    Write-Host '[PRECHECK] kPlug Harmony target signatures: PASS'
    $cfCopies=@(Get-ChildItem -LiteralPath $plugins -Filter 'KPlugCrossFaderCompatFix.dll' -File -Recurse -ErrorAction Stop)
    if($cfCopies.Count -ne 1 -or $cfCopies[0].FullName -ne $cf){throw 'STOP: Duplicate or relocated CrossFader DLL; refuse to deploy.'}
    if(Test-Path -LiteralPath $target){throw "STOP: Existing release candidate DLL: $target"}
    $dups=@(Get-ChildItem -LiteralPath $plugins -Filter 'KPlugBenchEnterFix.dll' -Recurse -File -ErrorAction Stop)
    if($dups.Count -gt 0){throw 'STOP: Duplicate release candidate DLL in plugins.'}
    $oldTrials=@(Get-ChildItem -LiteralPath $plugins -Filter 'HPointBenchEnterPistonGateTrial.dll' -Recurse -File -ErrorAction Stop)
    if($oldTrials.Count -gt 0){throw 'STOP: Old v0.7.x plugin still installed; remove by its validated SHA before proceeding. No files modified.'}
    $old=@(Get-ChildItem -LiteralPath $plugins -Filter 'HPointBenchEnterPathDiag.dll' -Recurse -File -ErrorAction Stop)
    if($old.Count -gt 0){throw 'STOP: Old BenchEnterPathDiag older candidate still installed. Do not overlap diagnostics.'}
    $previous=@(Get-ChildItem -LiteralPath $plugins -Filter 'HPointBenchEnterLifecycleDiag.dll' -Recurse -File -ErrorAction Stop)
    if($previous.Count -gt 0){throw 'STOP: Previous lifecycle diagnostic is installed. Remove its owned DLL first.'}
    foreach($n in @('HPointBenchEnterNativeRouteTrial.dll','HPointBenchEnterFlowProbe.dll','HPointBenchCumRouteDiag.dll')) {
         $detected=@(Get-ChildItem -LiteralPath $plugins -Filter $n -File -Recurse -ErrorAction Stop)
        if($detected.Count -gt 0){throw ('STOP: prior bench older candidate still deployed: '+$n)}
    }
    # Put all release candidate under KPlugFixes; other plugins untouched.
    $out=Join-Path $here ('Output\Run_'+(Get-Date -Format 'yyyyMMdd_HHmmss_fff'))
    $build=Join-Path $out 'build';New-Item -ItemType Directory -Force -Path $build|Out-Null
    $csc=FindCompiler;$ms=FindMscorlib2
    $bepDll=Join-Path $bep 'core\BepInEx.dll'
    $harmony=Join-Path $bep 'core\0Harmony.dll'
    $unity=Join-Path $game 'Koikatu_Data\Managed\UnityEngine.dll'
    foreach($f in @($ms,$bepDll,$harmony,$unity)) {if(-not(Test-Path -LiteralPath $f)){throw "Required reference missing: $f"}}
    $dll=Join-Path $build 'KPlugBenchEnterFix.dll'
    $clog=Join-Path $build 'Compile.log'
    $args=@('/nologo','/noconfig','/nostdlib+','/target:library','/optimize+','/langversion:3','/codepage:65001',
        "/out:$dll", "/reference:$ms", "/reference:$bepDll", "/reference:$harmony", "/reference:$unity",$source)
    Write-Host '[1/3] Compile v1.0.0 bench Enter fix release candidate for CLR 2.0'
    # Native compiler warnings on stderr must reach the log, not trigger PowerShell terminating errors.
    $oldEAP=$ErrorActionPreference
    try {
        $ErrorActionPreference='Continue'
        & $csc @args 2>&1 | Out-File -LiteralPath $clog -Encoding UTF8
        $compileExit=$LASTEXITCODE
    } finally { $ErrorActionPreference=$oldEAP }
    if($compileExit -ne 0){throw "Compile failed (exit=$compileExit): $clog"}
    if(-not(Test-Path -LiteralPath $dll)){throw 'Compiler did not produce DLL.'}
    if((Get-Item -LiteralPath $dll).Length -le 0){throw 'Compiler produced an empty DLL.'}
    $asm=[System.Reflection.Assembly]::ReflectionOnlyLoadFrom($dll)
    $msRef=@($asm.GetReferencedAssemblies()|Where-Object{$_.Name -eq 'mscorlib'})
    if($msRef.Count -ne 1 -or $msRef[0].Version.Major -ne 2){throw 'Compiled DLL mscorlib must be v2.0.'}
    $candidateHash=Hash $dll
    # Inspect the actual CLR2 framework file and generated reference metadata before any deployment.
    $msName=[System.Reflection.AssemblyName]::GetAssemblyName($ms)
    if($msName.Name -ne 'mscorlib' -or $msName.Version.ToString() -ne '2.0.0.0'){throw 'CLR 2.0 mscorlib metadata mismatch.'}
    $allRefs=@($asm.GetReferencedAssemblies())
    $newerRefs=@($allRefs|Where-Object { $_.Name -match '^(mscorlib|System(\..*)?)$' -and $_.Version.Major -gt 2 })
    if($newerRefs.Count -gt 0){throw ('Unexpected framework reference: '+(($newerRefs|ForEach-Object{$_.FullName}) -join '; '))}
    $manifest=[ordered]@{
      Tool='KPlugBenchEnterFix';Version='1.0.0';Status='RC1_PREPARED_NOT_RUNTIME_VERIFIED'
      At=(Get-Date).ToString('o');GameRoot=$game;Purpose='One-shot deferred native Idle start request after voice0=breath; preserve v0.7.2 other operations'
      InputDlls=@(
         @{Path=(Join-Path $plugins 'kPlug\kPlug.dll');SHA256=$expectedKPlug},
         @{Path=(Join-Path $game 'Koikatu_Data\Managed\Assembly-CSharp.dll');SHA256=$expectedGame},
         @{Path=$cf;SHA256=$expectedCF},@{Path=$bench;SHA256=$expectedBench})
      Backup='Existing production DLLs are never modified; candidate target must not exist'
      BuildOutput=$dll;OutputSHA256=$candidateHash;DeploymentTarget=$target
      CompilerPath=$csc;CompilerVersion=(Get-Item -LiteralPath $csc).VersionInfo.FileVersion
      CompilerArgs=$args;CompilerExitCode=$compileExit;CompileLog=$clog
      MscorlibPath=$ms;MscorlibVersion=$msName.Version.ToString();MscorlibSHA256=(Hash $ms)
      SourcePath=$source;SourceSHA256=$expectedSource
      ReferenceDlls=@(@{Path=$bepDll;SHA256=(Hash $bepDll)},@{Path=$harmony;SHA256=(Hash $harmony)},@{Path=$unity;SHA256=(Hash $unity)})
      OutputAssemblyRefs=@($allRefs|ForEach-Object{$_.FullName})
      OutputMscorlib2Check='PASS';UnexpectedFrameworkRefCheck='PASS'
      HarmonyTargetSignatureCheck=$hookSignatures
      RuntimeHookCheck='PENDING: check READY Chain, ToPistonChain, Action and IdleClothesGate in H-scene'
      RuntimeLog= (Join-Path $bep 'KPlugBenchEnterFix.log')
      LogFallback= (Join-Path $bep 'LogOutput.log')
      NextStep='Status gate must be STATUS_OK before gameplay; then collect/remove'
      SuccessCondition='First Idle Enter once: native sound once, deferred click at breath, native WLoop transition, no insertion/clothes'
    }
    WriteJson $manifest (Join-Path $out 'MANIFEST.json')
    if($Action -eq 'Audit') {
        # AUDIT mode is read-only regarding the game; it never installs/removes a plugin.
        $manifest['Status']='OFFLINE_AUDIT_PASSED_PENDING_RUNTIME_TEST'
        WriteJson $manifest (Join-Path $out 'MANIFEST.json')
        $proof=Join-Path $out 'BenchEnterPreflight_AuditProof.zip'
        Compress-Archive -LiteralPath @((Join-Path $out 'MANIFEST.json'),$clog) -DestinationPath $proof -Force
        $verify=Join-Path $out ('audit_verify_'+[guid]::NewGuid().ToString('N'))
        try {
            Expand-Archive -LiteralPath $proof -DestinationPath $verify -Force
            foreach($f in @((Join-Path $out 'MANIFEST.json'),$clog)) {
                $readback=Join-Path $verify (Split-Path -Leaf $f)
                if(-not(Test-Path -LiteralPath $readback) -or (Hash $f) -ne (Hash $readback)) {
                    throw ('AUDIT_PROOF_READBACK_MISMATCH: '+$f)
                }
            }
        } finally {if(Test-Path -LiteralPath $verify){Remove-Item -LiteralPath $verify -Recurse -Force}}
        Write-Host '[AUDIT_ONLY_OK] Windows parser, source, baseline SHA, method/field signatures, compile, CLR2 refs, manifest and proof ZIP pass.'
        Write-Host "AUDIT_PROOF=$proof"
        Write-Host 'NO_GAME_DLL_DEPLOYED'
        return
    }
    Write-Host '[2/3] Prepare ownership state before installing a DLL'
    $targetFolder=Join-Path $plugins 'KPlugFixes'
    if(-not(Test-Path -LiteralPath $targetFolder -PathType Container)){throw 'STOP: expected KPlugFixes folder is missing; installed baseline CrossFader should exist here.'}
    CheckBaseline
    $state=[ordered]@{Tool='KPlugBenchEnterFix';Version='1.0.0';GameRoot=$game;Target=$target;CandidateSHA256=$candidateHash;SourceSHA256=$expectedSource;
      BaseCF_SHA256=$expectedCF;OutputFolder=$out;InstalledAt=(Get-Date).ToString('o');Collected=$false;CollectedAt='';DeployPhase='Prepared'}
    WriteJson $state $stateFile
    [System.IO.File]::Copy($dll,$target,$false) # no overwrite, even on a race
    AssertHash $target $candidateHash
    $state['DeployPhase']='Deployed';WriteJson $state $stateFile
    Write-Host '[3/3] DEPLOYED_OK';Write-Host "Target=$target";Write-Host "SHA256=$candidateHash"
}
function VerifyRuntimeLog([string]$logPath,[datetime]$installedAt) {
    if(-not(Test-Path -LiteralPath $logPath -PathType Leaf)){throw 'INCOMPLETE: READY log missing (start H scene)'}
    $item=Get-Item -LiteralPath $logPath
    if($item.Length -le 0){throw 'INCOMPLETE: EMPTY_RUNTIME_LOG'}
    if($item.LastWriteTime -lt $installedAt){throw 'INCOMPLETE: LOG_IS_STALE'}
    $logText=Get-Content -LiteralPath $logPath -Raw -Encoding UTF8
    if($logText -notmatch 'SESSION_START version=1\.0\.0'){throw 'INCOMPLETE: SESSION_VERSION_MISMATCH'}
    if($logText -match 'PATCH_NOT_APPLIED|GATE_ERROR_FAIL_OPEN|ACTION_GATE_ERROR_FAIL_OPEN|CHAIN_GATE_ERROR_FAIL_OPEN|CLOTHES_GATE_ERROR_FAIL_OPEN|CYCLE_MONITOR_ERROR|IDLE_START_PROBE_READ_ERROR|IDLE_START_REPLAY_ERROR'){
      throw 'INCOMPLETE: RUNTIME_HOOK_OR_GATE_ERROR'
    }
    if($logText -notmatch 'READY hooks=ChainScope\+ToPistonChain\+ActionLifecycleGate\+IdleClothesGate\+IdleStartProbe preCumGate=True owner=') {
      throw 'INCOMPLETE: READY_NOT_YET_SEEN (start H scene)'
    }
    return $logText
}
function Status {
    $s=GetState
    CheckBaseline
    if((EnsureOwn $s) -ne 'OWNED_PRESENT'){throw 'INCOMPLETE: NOT_INSTALLED'}
    $log=Join-Path $bep 'KPlugBenchEnterFix.log'
    $txt=VerifyRuntimeLog $log ([datetime]$s.InstalledAt)
    Write-Host 'STATUS_OK (v1.0.0 hooks ready)'
}
function Collect {
    GameIsClosed
    $s=GetState
    $state=EnsureOwn $s
    $out=[string]$s.OutputFolder
    if(-not(Test-Path -LiteralPath $out)){throw 'Run output folder missing.'}
    $stage=Join-Path $out 'collected'
    if(-not(Test-Path -LiteralPath $stage)){New-Item -ItemType Directory -Path $stage -Force|Out-Null}
    Write-Host '[1/4] Capture logs before removing release candidate'
    $named=[ordered]@{
      'KPlugBenchEnterFix.log'=(Join-Path $bep 'KPlugBenchEnterFix.log');
      'KPlugCrossFaderCompatFix.log'=(Join-Path $bep 'KPlugCrossFaderCompatFix.log');
      'HPointBenchRouteFix.log'=(Join-Path $bep 'HPointBenchRouteFix.log');
      'LogOutput.log'=(Join-Path $bep 'LogOutput.log');
      'CandidateSource.cs'=$source;
      'KPlugBenchEnterFix.dll'=(Join-Path $out 'build\KPlugBenchEnterFix.dll');
      'Compile.log'=(Join-Path $out 'build\Compile.log');
      'MANIFEST.json'=(Join-Path $out 'MANIFEST.json')
    }
    foreach($n in $named.Keys){if(Test-Path -LiteralPath $named[$n]){Copy-Item -LiteralPath $named[$n] -Destination (Join-Path $stage $n) -Force}}
    $builtCopy=Join-Path $stage 'KPlugBenchEnterFix.dll'
    if(-not(Test-Path -LiteralPath $builtCopy -PathType Leaf)){throw 'STOP: collected build DLL missing; do not remove installed candidate until byte-preserving copy exists.'}
    AssertHash $builtCopy $s.CandidateSHA256
    $diagCopy=Join-Path $stage 'KPlugBenchEnterFix.log'
    if(-not(Test-Path -LiteralPath $diagCopy)){
        $valid=$false
        [IO.File]::WriteAllText($diagCopy,'DIAGNOSTIC_LOG_MISSING: INVALID_RUN')
    }
    $txt=Get-Content -LiteralPath $diagCopy -Raw -Encoding UTF8
    $valid=$false
    try { $null=VerifyRuntimeLog $diagCopy ([datetime]$s.InstalledAt); $valid=$true }
    catch { Write-Host ('[WARN] '+$_.Exception.Message+'; collect and remove, run invalid.') }
    $hit=($txt -match 'IDLE_START_PENDING_QUEUED' -and $txt -match 'IDLE_START_PROBE_PROC')
    $visual=($txt -match 'VISUAL_F7' -or $txt -match 'VISUAL_F8')
    Write-Host '[2/4] Remove only SHA-owned release candidate DLL'
    if($state -eq 'OWNED_PRESENT') {Remove-Item -LiteralPath $target -Force; if(Test-Path -LiteralPath $target){throw 'Release candidate DLL still exists.'}}
    $baselineStatus='MATCHED'
    try { CheckBaseline } catch {
        $baselineStatus='MISMATCH_OR_MISSING'
        Write-Host '[WARN] Existing baseline SHA changed externally; not modifying it. Review captured evidence.'
    }
    $cfObserved=if(Test-Path -LiteralPath $cf){Hash $cf}else{'MISSING'}
    $s.Collected=$true;$s.CollectedAt=(Get-Date).ToString('o')
    WriteJson $s $stateFile
    Copy-Item -LiteralPath $stateFile -Destination (Join-Path $stage 'InstallState.json') -Force
    [IO.File]::WriteAllLines((Join-Path $stage 'Result.txt'),[string[]]@(
      $(if(-not $valid){'INVALID_CANDIDATE_NOT_READY'}elseif(-not $hit){'INCOMPLETE_NO_IDLE_START_REQUEST_EVENT'}elseif(-not $visual){'IDLE_START_REQUEST_RECORDED'}else{'IDLE_START_REQUEST_RECORDED'}),
      'NeedsVisualCorrelation=True','RuntimeLogic=v0.7.5 game-tested; identity-only new build; native_Proc_unchanged=True',
      ('ExistingBaseline='+$baselineStatus)))
    $proof=@("STATE=REMOVED", "GameRoot=$game", "CandidateBefore=$state", "CandidateExistsAfter="+(Test-Path -LiteralPath $target),
      "ObservedCrossFaderSHA256="+$cfObserved,"ExistingBaseline="+$baselineStatus,"CandidateSHA256="+$s.CandidateSHA256,"CollectedAt="+$s.CollectedAt)
    [IO.File]::WriteAllLines((Join-Path $stage 'RemovalProof.txt'),[string[]]$proof)
    $zip=Join-Path $out ('KPlugBenchEnterFix_CollectRemove_'+(Get-Date -Format 'yyyyMMdd_HHmmss_fff')+'.zip')
    Write-Host '[3/4] Archive captured logs and verify bytes'
    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -Force
    $verify=Join-Path $out ('verify_'+[guid]::NewGuid().ToString('N'))
    try {
      Expand-Archive -LiteralPath $zip -DestinationPath $verify -Force
      foreach($f in Get-ChildItem -LiteralPath $stage -File) {
        $other=Join-Path $verify $f.Name
        if(-not(Test-Path -LiteralPath $other) -or (Hash $f.FullName) -ne (Hash $other)){throw "Zip readback mismatch: $($f.Name)"}
      }
    }
    finally {if(Test-Path -LiteralPath $verify){Remove-Item -LiteralPath $verify -Recurse -Force}}
    if(-not $valid -or -not $hit -or $state -ne 'OWNED_PRESENT' -or $baselineStatus -ne 'MATCHED'){
      Write-Host '[4/4] COLLECTED_AND_REMOVED_INCOMPLETE'
      Write-Host "UPLOAD=$zip"
      throw 'INCOMPLETE: missing READY, Idle start event, owned DLL, or baseline match; run cannot be accepted.'
    }
    Write-Host '[4/4] COLLECTED_AND_REMOVED'
    Write-Host 'REVIEW_ARCHIVE_INCLUDES_TESTED_DLL=True'
    Write-Host "UPLOAD=$zip"
    Write-Host "OBSERVED_CF_SHA256=$cfObserved";Write-Host "BASELINE=$baselineStatus"
}
try {
   Write-Host "=== KPlugBenchEnterFix 1.0.0 | $Action ==="
   switch($Action){'Audit'{Deploy};'Deploy'{Deploy};'Status'{Status};'Collect'{Collect}}
} catch {
   Write-Host "[STOP] $($_.Exception.Message)" -ForegroundColor Red
   exit 1
}
