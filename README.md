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

**KPlugNullGuardsFix v0.1.0**

Reproduces the null-guard changes previously embedded in a modified kPlug build without modifying `kPlug.dll` itself.

Runtime patch set:

- 9 H-process guards
- 2 KokanBehavior guards
- 1 MenuCorner guard
- 1 Voice coroutine transpiler

Startup result:

`12 Prefix guards + Voice Transpiler`

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
├─ SHA256SUMS.txt
├─ .gitignore
│
├─ src/
│  ├─ GaugeSwapFix/
│  │  └─ KPlugGaugeSwapFix_v0.2.0.cs
│  ├─ NullGuardsFix/
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
│  └─ PistonTransitionFix/
│
└─ release/
   └─ generated packages only
```

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
KPlugNullGuardsFix.dll
e1e585f3961126adda211d140535ab7590ed2d47c6f0ce97bbd2f146565a1c1e

KPlugPistonWaitFix.dll
c134bde2967194477c686e26ebca12010c3d81653e97a7b226e33a6b7294eabd

KPlugPistonAutoResume.dll
0abaa27f88469dcb9e73a1f83878da522e61f3a73cd3a7df9aad4ad668290f4f
```

The successful runtime-tested KPlugGaugeSwapFix v0.2.0 binary hash was not recorded at the time of testing, so no replacement value is invented here.

## Migration note

`KPlugPistonTransitionFix` was originally maintained as a separate repository.  
Its source, build scripts, release script, and documentation are now also preserved under `PistonTransitionFix/` in this repository.
