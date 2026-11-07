# HPointBenchRouteFix v1.0.0 — public repository release

**Date:** 2026-11-07  
**Runtime:** 1.0.0  
**Artifact:** [`HPointBenchRouteFix_v1.0.0_Install.zip`](../../release/windows/HPointBenchRouteFix/HPointBenchRouteFix_v1.0.0_Install.zip)

## Issue and root cause

Moving from a chair/sofa insertion animation to the explicitly chosen Bench Fellatio H-point may leave the insertion animation unchanged. A read-only diagnostic run showed that `HSceneProc.ChangeCategory()` returned via its `ROUTE_KEEP` branch when both sets were `[2,12]`, with no `ChangeAnimator()` invocation. This is a vanilla-game code path, but reproduction on a completely unmodded installation was **not** separately confirmed.

## Fix

A limited Harmony IL branch adjustment redirects the matching insertion-to-Bench Fellatio case to the stock animation-reselection path. Other H-point routes, IK, Reset, and CrossFader behaviors are not intentionally changed. The published Independent RC1 runtime removes the unnecessary kPlug/CrossFader activation dependencies; BepInEx and Harmony remain required.

## Verification and limitations

- Earlier v0.1.0 trial: four guarded activations in the user-provided run; user reported no apparent issue.
- Independent RC1 v1.0.0: installed and initially tested in the user's game; user reported no apparent issue.
- Public installer DLL is **byte-identical** to the initially tested Independent RC1 DLL. README and MANIFEST were revised for publication without recompiling it.
- **Not yet recorded:** second-launch behavior, final runtime log review, kPlug-free configuration, exhaustive H-point regressions.
- Separate H-point posture/IK and Enter input issues remain out of scope.

## Artifacts

- [Japanese guide](README_ja.md) / [English guide](README_en.md)
- [Install ZIP](../../release/windows/HPointBenchRouteFix/HPointBenchRouteFix_v1.0.0_Install.zip)
- [Complete C# source](../../src/HPointBenchRouteFix/v1.0.0/HPointBenchRouteFix_v1.0.0.cs)
- [Build PowerShell](../../scripts/HPointBenchRouteFix/v1.0.0/Build-HPointBenchRouteFix.ps1) / [Build BAT](../../scripts/HPointBenchRouteFix/v1.0.0/01_BuildRelease.bat)

DLL SHA-256: `170757DA0A4880CF2F8CF23C7E206C70BC6F9487F0051AAB470353D57A3408BD`  
ZIP SHA-256: `DB4EF44288838D754EDC64278240B4A1E03883B2D41B4ADEFF849CED7A79204F`  
Source SHA-256: `1BAF13FCE2CD85114A8A23187E2B74ACB8436AB6603D045C5E08808B193C3F90`
