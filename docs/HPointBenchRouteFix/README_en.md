# HPointBenchRouteFix v1.0.0 (Koikatu vanilla H-point route correction)

[日本語](README_ja.md) | [Install ZIP](../../release/windows/HPointBenchRouteFix/HPointBenchRouteFix_v1.0.0_Install.zip) | [Complete C# source](../../src/HPointBenchRouteFix/v1.0.0/HPointBenchRouteFix_v1.0.0.cs) | [Release notes](RELEASE_NOTES_v1.0.0.md)

## What it fixes

On some H-point moves, selecting **Bench Fellatio** from a chair/sofa insertion scene preserves the old insertion animation. Diagnostics traced the failure to the stock `HSceneProc.ChangeCategory()` route: when the category set remains `[2,12]`, the game may keep the current animation and skip `ChangeAnimator()`. This patch narrows the exception and takes the existing built-in animation reload path, without introducing new animator or IK operations.

**This is not a kPlug-specific fix.** The plugin requires BepInEx and Harmony, not kPlug or CrossFader as code-level prerequisites. A kPlug-free installation has **not** been runtime-tested.

## Install / uninstall

1. Close the game. Locate and back up any existing `HPointBenchRouteFix.dll` before installing.
2. Extract the [installer ZIP](../../release/windows/HPointBenchRouteFix/HPointBenchRouteFix_v1.0.0_Install.zip) at the Koikatu game root, preserving its folders. The DLL is installed to `BepInEx/plugins/HPointBenchRouteFix.dll`.
3. Remove or isolate any other copy such as `BepInEx/plugins/KPlugFixes/HPointBenchRouteFix.dll` or `HPointBenchRouteFixTrial.dll`, so it cannot be loaded twice.
4. Check `BepInEx/HPointBenchRouteFix.log` for `READY` at startup. `PATCH_NOT_APPLIED` means the patch was not activated.

To uninstall, close the game and remove the installed DLL. No stock game, kPlug, or CrossFader DLL is overwritten.

## Compatibility and validation

- Target: Koikatu 5.1 with `Assembly-CSharp.dll` SHA-256 `0038281CAF8DF48A7903C55DC389642EEEB3F2A9114BD9D68AC11C8AC0396BC5`.
- Runtime references: BepInEx and Harmony (build references: BepInEx 5.4.23.5, 0Harmony 2.9.0.0).
- The earlier v0.1.0 trial recorded four matching reload events and was assessed by the user as having no apparent issue.
- The v1.0.0 Independent RC1 DLL was installed and reported to behave normally on an initial game run. **The publicly distributed DLL is byte-identical to that game-installed RC1**. Only README and MANIFEST status text changed for public distribution.
- **Not yet documented:** a second launch, final runtime log review, a kPlug-free installation, or exhaustive regression coverage across all H-point transitions.
- Other pose collapse, IK and Enter input issues are outside this patch's scope.

## Checksums

- DLL SHA-256: `170757DA0A4880CF2F8CF23C7E206C70BC6F9487F0051AAB470353D57A3408BD`
- Installer ZIP SHA-256: `DB4EF44288838D754EDC64278240B4A1E03883B2D41B4ADEFF849CED7A79204F`
- C# source SHA-256: `1BAF13FCE2CD85114A8A23187E2B74ACB8436AB6603D045C5E08808B193C3F90`

[Build script](../../scripts/HPointBenchRouteFix/v1.0.0/Build-HPointBenchRouteFix.ps1) | [Build BAT](../../scripts/HPointBenchRouteFix/v1.0.0/01_BuildRelease.bat) | [Repository SHA list](../../SHA256SUMS.txt)

The build script retains the original test environment's default game-root path; pass `-GameRoot` for your own installation path. The original game and dependency DLLs are not distributed.
