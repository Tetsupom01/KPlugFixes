# KPlugCrossFaderCompatFix v1.2.0 — Release notes

**Released:** 2026-11-03  
[Current installer ZIP](../../release/windows/CrossFaderCompatFix/KPlugCrossFaderCompatFix_v1.2.0_KPlugFixes_Install.zip) | [Japanese installation guide](README_ja.md)

## Changes from v1.1.0

- Retains the first-request-wins animation-change gate and H-point MoveBoundary invalidation.
- Adds narrowly scoped stale ExtraIK suppression for H-point transitions following insertion states (`InsertIdle`, `WLoop`, `OUT_A`, and `Insert`).
- Combines CrossFader and ExtraIK diagnostics in `BepInEx/KPlugCrossFaderCompatFix.log`. Diagnostic logging is disabled by default; set `[Logging] EnableDiagnosticLogs = true` in the plugin config to enable it.
- Keeps the game's original state transitions and input handling.

## Verification and known limitations

- Tested in-game with diagnostics OFF and ON. In the recorded diagnostic run, the ExtraIK suppression executed six times (four `OUT_A` and two `InsertIdle`), with no plugin-internal errors.
- The published v1.2.0 DLL was confirmed to load and initialize after moving to `BepInEx/plugins/KPlugFixes/`. Full H-point regression tests on that exact binary have not been completed.
- Occasional transient pose displacement when pressing Enter after post-climax H-point moves, and intermittent Enter nonresponse, remain unresolved.
- The `Insert` suppression branch was not observed in the final diagnostic run.

## Install and rollback

1. Close Koikatu and back up any existing `KPlugCrossFaderCompatFix.dll`.
2. Extract the current ZIP at the game root. The DLL is placed at `BepInEx/plugins/KPlugFixes/KPlugCrossFaderCompatFix.dll`.
3. Remove or disable duplicate copies elsewhere under `BepInEx/plugins/` and confirm the BepInEx load message.
4. To roll back, exit the game, remove this DLL, and restore the backed-up version.

DLL SHA-256: `2392CCAFE5663EFC4FD80F8F72E9741796DE1DDD85D92F9A0E2C0F6C3E45ACFC`  
Source SHA-256: `029BE7B0733BD333939E950EB7B2C068139803CBBBDFB097DAAB07EA56FC772D`
