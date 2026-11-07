# HPointBenchRouteFix v1.0.0 — Release notes

**Released:** 2026-11-07  
[Installer ZIP](../../release/windows/HPointBenchRouteFix/HPointBenchRouteFix_v1.0.0_Install.zip) | [Japanese guide](README_ja.md) | [English guide](README_en.md)

## Fix

Selecting Bench Fellatio after chair/sofa insertion can leave the insertion animation active. In the tested environment, `HSceneProc.ChangeCategory()` took its `ROUTE_KEEP` branch for equal category sets `[2,12]` instead of executing the normal animation switch. The patch redirects only the matching case to the game's stock reselection route.

The implementation does not intentionally alter other H-point transitions, IK, Reset, or CrossFader behavior. BepInEx and Harmony are required; kPlug and CrossFader are not direct runtime dependencies.

## Verification and limitations

The targeted branch was exercised four times in the trial run, and the final v1.0.0 DLL was installed for an initial in-game check without reported issues. Operation without kPlug, a second launch, final runtime-log review, and exhaustive H-point regression tests remain unverified. Reproduction on a completely unmodded game was not separately established.

Other H-point posture and Enter input problems are outside this fix's scope.

## Artifacts

- [Complete C# source](../../src/HPointBenchRouteFix/v1.0.0/HPointBenchRouteFix_v1.0.0.cs)
- [Build PowerShell](../../scripts/HPointBenchRouteFix/v1.0.0/Build-HPointBenchRouteFix.ps1) / [Build BAT](../../scripts/HPointBenchRouteFix/v1.0.0/01_BuildRelease.bat)

DLL SHA-256: `170757DA0A4880CF2F8CF23C7E206C70BC6F9487F0051AAB470353D57A3408BD`  
ZIP SHA-256: `DB4EF44288838D754EDC64278240B4A1E03883B2D41B4ADEFF849CED7A79204F`  
Source SHA-256: `1BAF13FCE2CD85114A8A23187E2B74ACB8436AB6603D045C5E08808B193C3F90`
