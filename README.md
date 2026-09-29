# KPlugFixes

Independent BepInEx / Harmony fixes for **Koikatu + kPlug 3.6.0**.

This repository groups stable fixes by issue while keeping each runtime plugin as a separate DLL.  
Each fix can therefore be built, tested, enabled, updated, or rolled back independently.

## Included fixes

### GaugeSwapFix

**KPlugGaugeSwapFix v0.2.0**

Fixes a kPlug H-scene issue where, after inviting a girl with the I-key and swapping her into Main, changing H animation/category can restore the Face/Gauge display to the original H-start state even though the actual partner remains the swapped girl.

Patch boundary:

`HSceneProc.ChangeAnimator`

The vanilla method completes normally. The plugin preserves the current swapped Face/Gauge state across that reinitialization boundary.

### NullGuardsFix

**KPlugNullGuardsFix v0.2.0**

Reproduces the null-guard changes previously embedded in a modified kPlug build without modifying `kPlug.dll` itself.

Runtime patch set:

- 9 H-process guards
- 2 KokanBehavior guards
- 1 MenuCorner guard
- 1 AtHome OnDestroy destroyed-object guard
- 1 Voice coroutine transpiler

Startup result:

`13 Prefix guards + Voice Transpiler`

v0.2.0 integrates the previously standalone AtHomeDestroyGuard v0.1.1. Final integrated runtime verification completed successfully: the plugin loaded with all 13 Prefix guards plus the Voice Transpiler, MyRoom entry/exit completed normally, the game exited normally, and no AtHomeCtrl.OnDestroy NullReferenceException was observed.

### CrossFaderCompatFix

**KPlugCrossFaderCompatFix v1.0.0**

Prevents a native H-scene crash seen with CrossFader 0.11 when animation-change requests overlap around `HSceneProc.ChangeAnimator`.

The fix uses a first-request-wins gate: the first request is preserved until CrossFader and all relevant H Animators are stable, later conflicting non-null requests are rejected while the gate is active, and the original `ChangeAnimator` path is then allowed to run unchanged.

CrossFader itself is not disabled or rewritten; its `SetPlayHook` / `CrossFadeInFixedTime` path remains intact.

Runtime-success logic was verified as v0.8.0 and formalized as v1.0.0 with no functional source changes. Investigation history and verification limits are documented under `docs/CrossFaderCompatFix/`.

### PistonTransitionFix

Contains two independent plugins:

**KPlugPistonWaitFix v1.0.0**

Fixes the kPlug insertion-animation transition path where Numpad 7 / Backspace can leave `AI_Main.InAnswer` waiting indefinitely and make later animation switching unresponsive.

**KPlugPistonAutoResume v0.2.0**

Optionally resumes piston motion through the normal game Go path after a piston/insertion animation change.  
It waits for the real Animator state and cancels the pending resume when the H context changes.

## Repository structure

```text
KPlugFixes/
├─ README.md
├─ RELEASE_NOTES_v1.0.0.md
├─ RELEASE_NOTES_v1.0.1.md
├─ RELEASE_NOTES_v1.1.0.md
├─ SHA256SUMS.txt
├─ .gitignore
│
├─ src/
│  ├─ GaugeSwapFix/
│  │  └─ KPlugGaugeSwapFix_v0.2.0.cs
│  ├─ NullGuardsFix/
│  │  ├─ KPlugNullGuardsFix_v0.2.0.cs
│  │  └─ KPlugNullGuardsFix_v0.1.0.cs
│  └─ PistonTransitionFix/
│     ├─ WaitFix/
│     │  └─ KPlugPistonWaitFix_v1.0.0.cs
│     └─ AutoResume/
│        └─ KPlugPistonAutoResume_v0.2.0.cs
│
├─ scripts/
│  ├─ GaugeSwapFix/
│  ├─ NullGuardsFix/
│  └─ PistonTransitionFix/
│
├─ docs/
│  ├─ GaugeSwapFix/
│  ├─ NullGuardsFix/
│  ├─ PistonTransitionFix/
│  └─ CrossFaderCompatFix/
│     ├─ README_ja.md
│     └─ HISTORY.md
│
└─ release/
   ├─ CrossFaderCompatFix/
   │  └─ KPlugCrossFaderCompatFix_v1.0.0-source.zip
   └─ windows/
      ├─ KPlugGaugeSwapFix_v0.2.0.zip
      ├─ KPlugNullGuardsFix_v0.2.0.zip
      ├─ KPlugPistonWaitFix_v1.0.0.zip
      ├─ KPlugPistonAutoResume_v0.2.0.zip
      ├─ KPlugCrossFaderCompatFix_v1.0.0.zip\n      └─ archive/\n         └─ KPlugCrossFaderCompatFix_v0.8.0-runtime-tested.zip
```

## Prebuilt Windows binaries

For users who do not compile from source, exact runtime-tested DLLs are provided as individual ZIP files under `release/windows/`.

| Package | Runtime DLL version | DLL SHA-256 | ZIP SHA-256 |
| --- | --- | --- | --- |
| `KPlugGaugeSwapFix_v0.2.0.zip` | v0.2.0 | `7039f65e09ff3de19b56a1c152a3156b147bf17b70e817862fde5a1684ee8cc9` | `7bae97d1a68ead58cba5a467fd44708ae0021a8102f9801c981e7bf0dd21c77b` |
| `KPlugNullGuardsFix_v0.2.0.zip` | v0.2.0 | `da40fb1db618baccb229d36bbe4f557df499a0f7227b720bd3298090887cd087` | `650bf318eafb1b7cbe7c0f7094b7c73d49caf3e014ca3d5ab0af89194d803033` |
| `KPlugPistonWaitFix_v1.0.0.zip` | v1.0.0 | `c134bde2967194477c686e26ebca12010c3d81653e97a7b226e33a6b7294eabd` | `955de4cc6c72d0e852cd4f3b75e7e13bf6efd9094d84cc5d4096427adb3be48a` |
| `KPlugPistonAutoResume_v0.2.0.zip` | v0.2.0 | `0abaa27f88469dcb9e73a1f83878da522e61f3a73cd3a7df9aad4ad668290f4f` | `a7e8a19766570f441f6d059053eeb0333ae266b7b99824a0e75c72793b5c1c48` |
| `KPlugCrossFaderCompatFix_v1.0.0.zip` | v1.0.0 current installed build | `1bd4cbcd316b4ec4132f453c732ddcbc95d12911349099b3fe786dbed9036cc7` | `50ce10e547e64f7fa564b8263d2271418e9853fb3b74b38cd1e8add8abd9a0ec` |

Each ZIP contains the corresponding project-produced DLL with its normal runtime filename.

CrossFaderCompatFix now has a **v1.0.0 Windows binary** matching the formal v1.0.0 source metadata. The DLL was collected from the user's current Koikatu installation on 2026-09-30. Its internal plugin/version strings report v1.0.0. The exact v1.0.0 binary has not yet been independently runtime-verified after this rebuild, so the previous v0.8.0 runtime-tested DLL is preserved under `release/windows/archive/` as historical evidence rather than overwritten.

The independent MyRoom mannequin Harmony fix is not included yet because it has not reached its formal runtime-verified release state.

## Build policy

The source is intentionally kept compatible with the legacy .NET Framework compiler used by this Koikatu environment.

Behavior fixes remain separate DLLs. Repository-level consolidation does **not** mean runtime DLL consolidation.

Build scripts keep the established default game root:

```text
F:\illusion\Koikatu
```

The scripts under each feature folder reference the corresponding source folder in this repository.

## Tested environment

Main project environment:

- Koikatu 5.1
- kPlug 3.6.0
- BepInEx 5.4.23.5
- Unity 5.6.2f1
- CLR 2.0.50727.1433
- HF Patch v4.1
- Windows 11

Current virgin kPlug 3.6.0 reference SHA-256:

`34c13976108db0a18a7ad6b7cfda5517b4a9a83d85c9a909820144264c743855`

Assembly-CSharp.dll reference SHA-256:

`0038281caf8df48a7903c55dc389642eeeb3f2a9114bd9d68ac11c8ac0396bc5`

Historical PistonTransitionFix release documentation is preserved under `docs/PistonTransitionFix/`.

## Known successful DLL hashes

```text
KPlugNullGuardsFix v0.1.0 historical known-good DLL
e1e585f3961126adda211d140535ab7590ed2d47c6f0ce97bbd2f146565a1c1e

KPlugPistonWaitFix.dll
c134bde2967194477c686e26ebca12010c3d81653e97a7b226e33a6b7294eabd

KPlugPistonAutoResume.dll
0abaa27f88469dcb9e73a1f83878da522e61f3a73cd3a7df9aad4ad668290f4f
```

`KPlugGaugeSwapFix v0.2.0` and `KPlugNullGuardsFix v0.2.0` hashes were later recovered directly from the active runtime-tested game environment and are recorded in the prebuilt Windows package section above. CrossFaderCompatFix v1.0.0 is the current installed binary; its exact DLL SHA-256 is `1bd4cbcd316b4ec4132f453c732ddcbc95d12911349099b3fe786dbed9036cc7`.

## Migration note

`KPlugPistonTransitionFix` was originally maintained as a separate repository.  
Its source, build scripts, release script, and documentation are now also preserved under `PistonTransitionFix/` in this repository.


## Distribution policy

Stable fixes should provide both reproducible source and prebuilt Windows binaries.

For each stable runtime fix, preserve:

- source code
- build/deploy scripts
- documentation and investigation history
- the runtime-tested Windows `.dll` when available
- SHA-256 for each distributed DLL
- a release package for users who do not compile from source

Prebuilt binaries are limited to DLLs produced by this project. Third-party binaries such as kPlug, BepInEx, Harmony, Unity, or game assemblies are not redistributed here.

A DLL is not labeled runtime-tested unless that exact binary, or an explicitly documented byte-identical build, was tested. Missing hashes are left missing rather than reconstructed or guessed.
