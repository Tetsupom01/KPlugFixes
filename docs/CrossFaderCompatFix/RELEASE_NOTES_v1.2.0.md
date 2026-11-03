# KPlugCrossFaderCompatFix v1.2.0 — official build

Date: 2026-11-03 (JST)

## Changes from v1.1.0
- Preserve v1.1.0 CrossFader gate and HPoint MoveBoundary invalidation.
- Integrate narrowly scoped stale ExtraIK suppression for post-insertion HPoint transitions.
- Support old states: InsertIdle, WLoop, OUT_A, Insert (Insert branch not observed in final test).
- Unified optional diagnostics: [Logging] EnableDiagnosticLogs=false by default; when true, both CrossFader and ExtraIK details append to BepInEx/KPlugCrossFaderCompatFix.log.
- Official display-label cleanup: old runtime banners `v1.1.0`, `1.1.0-trial`, and `integrated v0.1.1` now identify the current release. Historical source ancestry and internal Harmony ownership ID remain unchanged.

## Validation and limitations
- The pre-label-cleanup v1.2.0 candidate DLL was tested on 2026-11-03 in normal logging OFF and diagnostic logging ON configurations.
- Recent diagnostic logging ON run: 6 ExtraIK suppressions (4 OUT_A, 2 InsertIdle), 0 internal errors; logging merged successfully.
- User reports no persistent pose collapse in tested runs; occasional transient first-Enter hand/body offset and intermittent Enter nonresponse remain unresolved.
- Final distributed DLL has **only managed metadata string substitutions** versus that tested candidate. No PE header, method IL, runtime configuration, or data outside the managed metadata heaps was modified. This post-label-cleanup binary has NOT been separately smoke-tested in-game.
- Avoid claiming all conditions permanently fixed or that all states have been exercised.

## Provenance / SHA-256
- Previously tested candidate DLL SHA-256: `2D3F04355D1064C45E1EE421735C563E0CDBDE2F99E6EAC52F0F0C0C8DBFA782`
- Official label-cleaned DLL SHA-256: `2392CCAFE5663EFC4FD80F8F72E9741796DE1DDD85D92F9A0E2C0F6C3E45ACFC`
- Official revised source SHA-256: `029BE7B0733BD333939E950EB7B2C068139803CBBBDFB097DAAB07EA56FC772D`
- Reproducibility: This DLL was made by in-place equal-length .NET metadata literal substitutions of the tested candidate. Recompiling source may yield a binary with a different SHA; compare logic/IL semantics rather than expecting byte identity.

## Installation
1. Exit Koikatsu. Backup the existing BepInEx/plugins/KPlugCrossFaderCompatFix.dll.
2. Extract the installation ZIP into the Koikatsu game root, preserving the folder structure.
3. Keep pre-existing config unless intentionally enabling diagnostics.
4. Restart game and confirm BepInEx loads v1.2.0 and the MoveBoundary / ExtraIK patches initialize.
5. Rollback by restoring the previously backed-up DLL (v1.1.0 baseline SHA recorded in project documentation).
