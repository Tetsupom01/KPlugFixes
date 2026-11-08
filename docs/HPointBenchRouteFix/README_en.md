# HPointBenchRouteFix v1.0.0 — Koikatu H-point animation selection fix

[日本語](README_ja.md) | [Installer ZIP](../../release/windows/HPointBenchRouteFix/HPointBenchRouteFix_v1.0.0_Install.zip) | [Complete source](../../src/HPointBenchRouteFix/v1.0.0/HPointBenchRouteFix_v1.0.0.cs) | [Release notes](RELEASE_NOTES_v1.0.0.md)

## Problems addressed and results

| Problem | How it works | Result |
| --- | --- | --- |
| Selecting **Bench Fellatio** from a chair or sofa insertion animation can leave the previous insertion animation playing | Narrowly adjusts the matching-category preserve-animation branch and uses the game's original animation-reselection path | Correctly transitions to the selected Bench Fellatio animation |

## How it works

This plugin narrowly adjusts the stock `HSceneProc.ChangeCategory()` preserve-animation branch for matching category sets `[2,12]` and uses the game's normal animation-reselection path. It does not replace animator, IK, or Reset processing.

**Requires BepInEx and Harmony.** kPlug and CrossFader are not startup dependencies; runtime behavior without kPlug has not been verified.

## Install / uninstall

1. Exit Koikatu and back up any existing `HPointBenchRouteFix.dll`.
2. Extract the [ZIP](../../release/windows/HPointBenchRouteFix/HPointBenchRouteFix_v1.0.0_Install.zip) to the Koikatu game root. The target path is `BepInEx/plugins/HPointBenchRouteFix.dll`.
3. Remove or disable other copies of that DLL and `HPointBenchRouteFixTrial.dll` to avoid duplicate loading.
4. Check `BepInEx/HPointBenchRouteFix.log` for `READY` after startup. `PATCH_NOT_APPLIED` means the patch is not active.

To uninstall, exit the game and remove this DLL. No original game DLL is modified.

## Compatibility / verification

- Target: Koikatu 5.1, `Assembly-CSharp.dll` SHA-256 `0038281CAF8DF48A7903C55DC389642EEEB3F2A9114BD9D68AC11C8AC0396BC5`
- Runtime: BepInEx and Harmony (build references: BepInEx 5.4.23.5, 0Harmony 2.9.0.0)
- The targeted reload path was recorded four times in trial tests. The same v1.0.0 DLL as in this ZIP was installed for an initial game check with no issue reported.
- **Not verified:** behavior after a second launch, final runtime logs, operation without kPlug, or exhaustive regression coverage of all H-point routes.
- Other pose collapse, IK, and Enter input issues are outside this fix's scope.

DLL SHA-256: `170757DA0A4880CF2F8CF23C7E206C70BC6F9487F0051AAB470353D57A3408BD`  
Installer ZIP SHA-256: `DB4EF44288838D754EDC64278240B4A1E03883B2D41B4ADEFF849CED7A79204F`  
Source SHA-256: `1BAF13FCE2CD85114A8A23187E2B74ACB8436AB6603D045C5E08808B193C3F90`

[Build PowerShell](../../scripts/HPointBenchRouteFix/v1.0.0/Build-HPointBenchRouteFix.ps1) | [Build BAT](../../scripts/HPointBenchRouteFix/v1.0.0/01_BuildRelease.bat) | [Checksum list](../../SHA256SUMS.txt)

For source builds, use the `-GameRoot` option to specify your Koikatu installation. Game files and third-party DLLs are not distributed.
