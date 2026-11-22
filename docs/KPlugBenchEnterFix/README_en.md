# KPlugBenchEnterFix v1.0.0

[日本語](README_ja.md) | [Installation ZIP](../../release/windows/KPlugBenchEnterFix/KPlugBenchEnterFix_v1.0.0_Install.zip) | [Source code](../../src/KPlugBenchEnterFix/v1.0.0/KPlugBenchEnterFix_v1.0.0.cs)

BepInEx/Harmony fix for Numpad Enter handling in the bench oral-service animation of Koikatu with kPlug 3.6.0. Does not modify the original game or kPlug DLLs.

- Uses the original start handling from Idle. If a voice line prevents immediate start, it retains one request and hands it back to the original routine when conditions permit.
- Blocks Enter inputs at or below 70% gauge and additional Enter requests during climax; preserves the qualifying Enter above 70% and transition from post-climax oral choice to insertion.
- Does not directly force animation or clothing states.

**Test environment:** Koikatu 5.1, kPlug 3.6.0, BepInEx 5.4.23.5, Unity 5.6.2f1, Windows 11. Other kPlug versions have not been validated. See `MANIFEST.json` within the install package for reference DLL hashes.

**Installation:** Exit the game. Back up any existing same-named DLL, and ensure no older `HPointBenchEnterPistonGateTrial.dll` exists under `BepInEx/plugins/`. Extract the [Install ZIP](../../release/windows/KPlugBenchEnterFix/KPlugBenchEnterFix_v1.0.0_Install.zip) to the Koikatu game directory. DLL destination: `BepInEx/plugins/KPlugFixes/KPlugBenchEnterFix.dll`. Verify `READY hooks=...` in `BepInEx/KPlugBenchEnterFix.log` after launch; a plugin loading message alone is insufficient.

**Removal:** Exit the game and remove ONLY `BepInEx/plugins/KPlugFixes/KPlugBenchEnterFix.dll`. Leave other plugins and settings intact.

**Validation:** The exact DLL was built and tested on Windows on November 22, 2026. Logs confirm initial and return-to-bench start, qualifying climax request, blocking invalid/redundant Enter, reaching the oral-choice state, and accepting Enter there. User-reported visuals and audio appeared acceptable. Audio counts and every animation path were **not** exhaustively instrumented.

**Known separate issue:** Moving from a later oral animation back to bench can leave the prior animation active. It also occurred without the prior Enter test DLL. Root cause is not established, and this plugin does not modify H-point category routing.

**Hashes:** DLL `DF1E541CA58B07D2CCCE1A87925BCAB4651E1C2C7DF24A45DB2CFBF29CE0C4CC`; source `3C95474583C45FEBD672985EC620D5E5E4CFD41D26A5DBB1E740DCAC952A4DB8`; installation ZIP `8509D14394F3F1A9C138880176F64847FBC9AAD7B9CE3D2E4DEBD39CE422A412`.

[Historical build/test scripts](../../scripts/KPlugBenchEnterFix/v1.0.0/) are for reproducibility, **not** installation; the scripts may retain test-era labels.
