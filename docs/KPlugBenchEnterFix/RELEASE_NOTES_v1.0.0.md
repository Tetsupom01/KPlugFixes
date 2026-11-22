# KPlugBenchEnterFix v1.0.0 — Release notes

**Formal adoption:** 2026-11-22. Koikatu 5.1 / kPlug 3.6.0 / BepInEx 5.4.23.5 / Unity 5.6.2f1 / Windows 11.

## Scope

- Corrects bench Enter lifecycle: Idle start, one qualifying request above 70%, rejection of invalid/redundant Enter, preserving the post-climax oral choice and insertion transition.
- Uses original kPlug state transitions rather than forcing animations.
- Keeps HPointBenchRouteFix and all other existing plugins unchanged.

## Validation and hash protection

- Original v0.7.5 gameplay patch was tested before identity-only v1.0.0 naming changes.
- v1.0.0 compiled with CLR 2.0 references; runtime `READY` and five Harmony targets confirmed.
- Initial and return-to-bench Idle starts, qualifying climax and oral-choice transitions confirmed by runtime log.
- Visual/audio assessment was user-reported, not instrumented; no claim of exhaustive testing.
- Tested DLL SHA-256: `DF1E541CA58B07D2CCCE1A87925BCAB4651E1C2C7DF24A45DB2CFBF29CE0C4CC`.
- Source SHA-256: `3C95474583C45FEBD672985EC620D5E5E4CFD41D26A5DBB1E740DCAC952A4DB8`.

## Known limitation

An oral-to-bench H-point movement may fail to switch animation. This also occurs without the prior Enter test plugin; the cause is not established. The issue is outside this patch's stated functionality.

No game, kPlug, or other third-party DLL is redistributed.
