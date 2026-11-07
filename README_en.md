[日本語](README.md) | English

> **Updated installers (2026-11-05):** All seven current packages (six kPlug-related fixes plus the separate Sideloader HList Name Cache optimization) have game-root installation ZIPs placing each unchanged DLL in `BepInEx/plugins/KPlugFixes/`. They include Japanese `README.txt`, English `README_EN.txt`, and `SHA256SUMS.txt`. Earlier ZIPs remain for historical reproduction; do not duplicate plugin DLLs across directories. The newly repackaged installers have not been separately runtime-tested.

# KPlugFixes

Six independent BepInEx / Harmony **kPlug-related fixes**, one separately classified **H-list asset-name caching optimization**, and one **standalone vanilla Koikatu H-point route correction**, validated within a **Koikatu + kPlug 3.6.0** environment. HListNameCache does not patch kPlug itself; operation without kPlug is untested.

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

**KPlugCrossFaderCompatFix v1.2.0** — current adopted version (2026-11-03).

Preserves the v1.1.0 first-request-wins gate, stability wait, and H-point MoveBoundary invalidation. Adds narrowly scoped stale ExtraIK suppression for H-point transitions after insertion motions, with guards for `InsertIdle`, `WLoop`, `OUT_A`, and `Insert` (the `Insert` branch was not observed in the final run). The original state transitions and input handling remain in place.

Diagnostics are **OFF by default**. Set `[Logging] EnableDiagnosticLogs = true` in `BepInEx/config/tetsupom.kplug.crossfader.compatfix.cfg` and restart to record CrossFader and ExtraIK messages together in `BepInEx/KPlugCrossFaderCompatFix.log`.

The pre-label-cleanup v1.2.0 candidate passed startup and both logging OFF/ON tests on 2026-11-03. The recent logging ON run recorded six ExtraIK suppressions and no internal errors; the user reported no persistent pose collapse in that run. **Known unresolved issues:** intermittent first-Enter transient pose displacement after post-climax H-point moves and occasional Enter nonresponse. The official binary differs from the tested candidate only in equal-length managed metadata strings; the exact label-cleaned binary has subsequently been confirmed to load once and initialize in-game after the KPlugFixes folder migration, but full H-point regression testing on that final binary remains unperformed.

- [Current installer ZIP](release/windows/CrossFaderCompatFix/KPlugCrossFaderCompatFix_v1.2.0_KPlugFixes_Install.zip)
- [Current complete source](src/CrossFaderCompatFix/v1.2.0/KPlugCrossFaderCompatFix_v1.2.0.cs)
- [Source and build-material ZIP](release/source/CrossFaderCompatFix/KPlugCrossFaderCompatFix_v1.2.0-source.zip)
- [v1.2.0 release notes](docs/CrossFaderCompatFix/RELEASE_NOTES_v1.2.0.md)
- [Public technical overview](docs/CrossFaderCompatFix/README_ja.md)
- [Previous v1.1.0 installer](release/windows/CrossFaderCompatFix/KPlugCrossFaderCompatFix_v1.1.0_Install.zip) remains available.

### PoseBridge

[v0.2.0.1 release notes](docs/PoseBridge/RELEASE_NOTES_v0.2.0.1.md)

**KPlugPoseBridge v0.2.0.1 (currently loaded on user's game; verified startup)**

v0.2.0.1 [complete source](src/PoseBridge/KPlugPoseBridge_v0.2.0.1.cs) and [game-root installation ZIP](release/windows/PoseBridge/KPlugPoseBridge_v0.2.0.1_KPlugFixes_Install.zip) are available. The ZIP contains the byte-identical DLL collected from the runtime on 2026-10-31 (DLL SHA-256 `F1A35FF5A1849001D2705DD3F4C368FCB9A309170B51F244AEB1FD99583C9142`). Startup/patch active was verified; full pose-by-pose retesting of this exact binary was not performed. The v0.2.0.0 installer remains historical.

Synchronizes the vanilla/main H insertion-position list (mode 2) with the same canonical pose set used by kPlug Pose Selector.

The plugin reuses kPlug's own resolution path:

`ToolAnimSelector.GetAvailablePiston()` → `ParserAnim.GetHAnim(canonical)`

It synchronizes once after `ExtraAnimManager.Start` and again after `HSceneProc.CreateListAnimationFileName(bool,int)`, so H-point/category changes rebuild the vanilla list to the kPlug-visible set.

Runtime verification on 2026-10-08 confirmed successful synchronization for floor (77), wall (12), and desk (20) sets, with no `SYNC_ABORT` / sync failure in the supplied log. Poses selected during the test behaved normally; exhaustive selection of every pose was not performed.

### Sideloader HList Name Cache

**SideloaderHListNameCache v0.1.0 — H-list optimization, not a kPlug fix (tested DLL; frozen baseline).**

This independent BepInEx/Harmony plugin caches asset-name arrays for six exact `abdata/h/list/` bundles via `AssetBundleCheck.GetAllAssetName()`. It does **not** patch `kPlug.dll`, and its original C# source has no direct kPlug API or namespace references. The runtime-tested environment included kPlug 3.6.0 and Sideloader; **operation without kPlug has not been verified**. Two runtime sessions confirmed 852 and 480 cache hits **per bundle**, respectively. The second test confirmed normal H controls, pose-list selection and H-point movement. This plugin does not address the separate intermittent HPointMove pose-collapse issue.

- [Current published KPlugFixes-layout installer](release/windows/HListNameCache/Sideloader_HListNameCache_v0.1.0_KPlugFixes_Install.zip) — extract into the Koikatu game root; the DLL goes to `BepInEx/plugins/KPlugFixes/SideloaderHListNameCache.dll`. This directory is a packaging convention, **not a kPlug dependency**. An independently placed installer targeting `BepInEx/plugins/` has been prepared but is **not published as the current release** because that packaging has not been runtime-tested. There is no need to relocate an existing, working, byte-identical DLL solely for classification.
- [Historical frozen archive (DLL, source, install/removal scripts, records)](release/windows/HListNameCache/Sideloader_HListNameCache_v0.1.0_Stable_Frozen.zip) — retained byte-for-byte unchanged; not a game-root overlay ZIP.
- [Technical README in Japanese](docs/HListNameCache/README_ja.md).

The **DLL** inside the new ZIP is byte-identical to the successfully runtime-tested frozen build. The **new ZIP installation** and the **historical installer script** have not been separately tested on Windows.

### PistonTransitionFix

[Japanese installation guide](docs/PistonTransitionFix/README_ja.md)

Contains two independent plugins:

**KPlugPistonWaitFix v1.0.0**

Fixes the kPlug insertion-animation transition path where Numpad 7 / Backspace can leave `AI_Main.InAnswer` waiting indefinitely and make later animation switching unresponsive.

**KPlugPistonAutoResume v0.2.0**

Optionally resumes piston motion through the normal game Go path after a piston/insertion animation change.  
It waits for the real Animator state and cancels the pending resume when the H context changes.

### HPointBenchRouteFix (vanilla H-point selection fix; independent)

**HPointBenchRouteFix v1.0.0** fixes a case where selecting *Bench Fellatio* from an insertion pose keeps the previous animation. Diagnostic logs traced this to a same-category `HSceneProc.ChangeCategory()` preserve-motion branch. The fix reuses the stock animation-selection path. **kPlug and CrossFader are not code-level dependencies.** Operation without kPlug has not been runtime-tested.

- [v1.0.0 installer ZIP](release/windows/HPointBenchRouteFix/HPointBenchRouteFix_v1.0.0_Install.zip)
- [Japanese guide](docs/HPointBenchRouteFix/README_ja.md) / [English guide](docs/HPointBenchRouteFix/README_en.md)
- [Complete C# source](src/HPointBenchRouteFix/v1.0.0/HPointBenchRouteFix_v1.0.0.cs) / [Release notes](docs/HPointBenchRouteFix/RELEASE_NOTES_v1.0.0.md)

The user installed the Independent RC1 binary and initially reported no apparent issue. The public DLL is byte-identical. A second launch, final runtime log inspection, and kPlug-free testing remain undocumented.

## Repository structure

```text
src/<FixName>/                  complete C# source (historic versions retained)
docs/<FixName>/                 documentation, validation and history
scripts/<FixName>/              reproducible build/deploy scripts where available
release/windows/<FixName>/      no-build Windows installation ZIPs
release/source/CrossFaderCompatFix/  historical v1.0.0 source/build archive
SHA256SUMS.txt                  current and preserved release checksums
```

Within `release/windows/`, packages are grouped by the fix they belong to. Historical frozen releases retain their exact ZIP bytes and SHA-256; reorganizing paths does not constitute a rebuild. Top-level `RELEASE_NOTES_v*.md` are historical repository release notes, not declarations of the latest individual plugin versions.

## Installation directory (current seven packages)

Each current ZIP is extracted into the **Koikatu game root**, placing its individual DLL in this directory. Install only the fixes/optimizations you need, rather than all seven automatically:

```text
Koikatu/
└─ BepInEx/
   └─ plugins/
      └─ KPlugFixes/
         ├─ KPlugCrossFaderCompatFix.dll
         ├─ KPlugPoseBridge.dll
         ├─ KPlugGaugeSwapFix.dll
         ├─ KPlugNullGuardsFix.dll
         ├─ KPlugPistonWaitFix.dll
         ├─ KPlugPistonAutoResume.dll
         └─ SideloaderHListNameCache.dll
```

Close the game, locate and back up any previous copy of the same DLL, extract the current ZIP to the game root, and ensure there is no duplicate DLL elsewhere under `BepInEx/plugins/`. Check `BepInEx/LogOutput.log` after restarting. All packages contain unchanged binaries from their respective tested releases; the **packaging layout is not a new runtime validation**. HListNameCache is an independent H-list optimization with no direct kPlug API dependency (not a kPlug.dll patch). Its present `KPlugFixes/` layout is only a distribution choice; it has been runtime-tested with kPlug 3.6.0 present, and a kPlug-free setup is not yet verified.

## Standalone HPointBenchRouteFix installation

Unlike the seven `KPlugFixes/` packages above, this patch is installed to `BepInEx/plugins/HPointBenchRouteFix.dll`. Extract its [ZIP](release/windows/HPointBenchRouteFix/HPointBenchRouteFix_v1.0.0_Install.zip) at the game root after closing Koikatu. Avoid duplicate copies under any plugin directory, including `KPlugFixes/`. At startup inspect `BepInEx/HPointBenchRouteFix.log` for `READY`.

## Prebuilt Windows binaries

Each plugin has its own self-contained ZIP in `release/windows/<FixName>/`. The following checksums identify the published packages. The four newly repackaged legacy fixes contain byte-identical DLL payloads; their installer layout has not been separately tested in-game. Historical DLL-only ZIPs remain available at their original paths.

| Package | DLL SHA-256 | ZIP SHA-256 | Verification |
| --- | --- | --- | --- |
| [HPointBenchRouteFix v1.0.0](release/windows/HPointBenchRouteFix/HPointBenchRouteFix_v1.0.0_Install.zip) | `170757da0a4880cf2f8cf23c7e206c70bc6f9487f0051aab470353d57a3408bd` | `db4ef44288838d754edc64278240b4a1e03883b2d41b4adeff849ced7a79204f` | User-reported initial game run OK; final DLL unchanged; second-launch/log/kPlug-free tests pending |
| [CrossFaderCompatFix v1.2.0](release/windows/CrossFaderCompatFix/KPlugCrossFaderCompatFix_v1.2.0_KPlugFixes_Install.zip) | `2392ccafe5663efc4fd80f8f72e9741796de1ddd85d92f9a0e2c0f6c3e45acfc` | `f769ee644700fc52c8d59a14f5f80bd9103df2be9543947b6201506ddde499d1` | Pre-label-cleanup candidate tested OFF/ON; final DLL startup/patch activation verified after migration; full H-point regression retest incomplete; Enter issues remain |
| [CrossFaderCompatFix v1.1.0](release/windows/CrossFaderCompatFix/KPlugCrossFaderCompatFix_v1.1.0_Install.zip) | `1f6f359255d3501545f1b97e25dc059436fa24c3eeb704ec9db93e80256b4154` | `d826bfc59e8994fefbb862f135ad77fab8354c0e6d003f9e79d230bd02e581a1` | Label-cleaned DLL: startup/patch/MoveBoundary READY verified; separate H-point move retest pending |
| [CrossFaderCompatFix v1.0.0 (historical)](release/windows/CrossFaderCompatFix/KPlugCrossFaderCompatFix_v1.0.0.zip) | `1bd4cbcd316b4ec4132f453c732ddcbc95d12911349099b3fe786dbed9036cc7` | `50ce10e547e64f7fa564b8263d2271418e9853fb3b74b38cd1e8add8abd9a0ec` | Prior runtime-tested baseline |
| [GaugeSwapFix v0.2.0 (KPlugFixes installer)](release/windows/GaugeSwapFix/KPlugGaugeSwapFix_v0.2.0_KPlugFixes_Install.zip) | `7039f65e09ff3de19b56a1c152a3156b147bf17b70e817862fde5a1684ee8cc9` | `ad7eec9aafe1dce177bd5567e41cc9b187d413f3a2fc675f20537f1e9459db00` | Original runtime-tested DLL retained byte-for-byte; new game-root ZIP not separately installed/tested |
| [NullGuardsFix v0.2.0 (KPlugFixes installer)](release/windows/NullGuardsFix/KPlugNullGuardsFix_v0.2.0_KPlugFixes_Install.zip) | `da40fb1db618baccb229d36bbe4f557df499a0f7227b720bd3298090887cd087` | `295c959aac2a194fd61d0128544d399ca7e17401fa10b9c224ae6c47b897c81e` | Original runtime-tested DLL retained byte-for-byte; new game-root ZIP not separately installed/tested |
| [PistonWaitFix v1.0.0 (KPlugFixes installer)](release/windows/PistonTransitionFix/KPlugPistonWaitFix_v1.0.0_KPlugFixes_Install.zip) | `c134bde2967194477c686e26ebca12010c3d81653e97a7b226e33a6b7294eabd` | `13153506c868289d4f9df47ba1bc8acf170b37224411928168aa38c72c90df55` | Original runtime-tested DLL retained byte-for-byte; new game-root ZIP not separately installed/tested |
| [PistonAutoResume v0.2.0 (KPlugFixes installer)](release/windows/PistonTransitionFix/KPlugPistonAutoResume_v0.2.0_KPlugFixes_Install.zip) | `0abaa27f88469dcb9e73a1f83878da522e61f3a73cd3a7df9aad4ad668290f4f` | `9d5e9d399b0ce110782ff527b969271d80a679869c5d8ce375baed2497397940` | Original runtime-tested DLL retained byte-for-byte; new game-root ZIP not separately installed/tested |
| [PoseBridge v0.2.0.1 (current)](release/windows/PoseBridge/KPlugPoseBridge_v0.2.0.1_KPlugFixes_Install.zip) | `f1a35ff5a1849001d2705dd3f4c368fcb9a309170b51f244aeb1fd99583c9142` | `76a985805c3817b2caf4f69e787fdcc93125780619a66ce69f3d2ef2d7039ba4` | Runtime-collected exact DLL, startup patch active |
| [PoseBridge v0.2.0.0 (historical)](release/windows/PoseBridge/KPlugPoseBridge_v0.2.0.0.zip) | Not separately recorded | `df98829f15469b20868a4fe69f20f8c6a58f041dbf54e3c315b96f928f235a5c` | Historical installer, not current |
| [HListNameCache v0.1.0 (H-list optimization; KPlugFixes installer)](release/windows/HListNameCache/Sideloader_HListNameCache_v0.1.0_KPlugFixes_Install.zip) | `17f571acb7901776f1069c1b218e85edb67918c50a1ce844837c7da3616429c4` | `1e05cdf4e64fef04f8695155e3cf4276bb2deff529700d8165d999eea3961202` | Original runtime-tested DLL unchanged; new ZIP not separately installed/tested; kPlug-free operation untested |

**Historical HListNameCache frozen archive (source and scripts, not a game-root overlay):** [v0.1.0 Stable Frozen](release/windows/HListNameCache/Sideloader_HListNameCache_v0.1.0_Stable_Frozen.zip).

**Historical DLL-only ZIPs retained (manual placement):** [GaugeSwapFix](release/windows/GaugeSwapFix/KPlugGaugeSwapFix_v0.2.0.zip), [NullGuardsFix](release/windows/NullGuardsFix/KPlugNullGuardsFix_v0.2.0.zip), [PistonWaitFix](release/windows/PistonTransitionFix/KPlugPistonWaitFix_v1.0.0.zip), [PistonAutoResume](release/windows/PistonTransitionFix/KPlugPistonAutoResume_v0.2.0.zip).

The independent MyRoom mannequin Harmony fix is not included because its formal runtime-verified release state is not established. No third-party game files or other vendors' DLLs are distributed here.

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

`KPlugGaugeSwapFix v0.2.0` and `KPlugNullGuardsFix v0.2.0` hashes were later recovered directly from the active runtime-tested game environment and are recorded in the prebuilt Windows package section above. CrossFaderCompatFix historical v1.0.0 was runtime-tested; its DLL SHA-256 is `1bd4cbcd316b4ec4132f453c732ddcbc95d12911349099b3fe786dbed9036cc7`.

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
