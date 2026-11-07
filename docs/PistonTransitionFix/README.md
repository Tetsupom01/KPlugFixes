# kPlug Piston Transition Fix

[日本語ガイド](README_ja.md) | English

Two BepInEx/Harmony plugins for Koikatu + kPlug 3.6.0, addressing separate problems when switching insertion/piston animations using Numpad 7 or Backspace.

| Plugin | Purpose | Installer ZIP |
| --- | --- | --- |
| **KPlugPistonWaitFix v1.0.0** | Avoids an indefinite wait that can leave later H animation navigation unresponsive | [Download](../../release/windows/PistonTransitionFix/KPlugPistonWaitFix_v1.0.0_KPlugFixes_Install.zip) |
| **KPlugPistonAutoResume v0.2.0** | Resumes piston motion through the game's normal Go action after an animation change | [Download](../../release/windows/PistonTransitionFix/KPlugPistonAutoResume_v0.2.0_KPlugFixes_Install.zip) |

**AutoResume requires WaitFix v1.0.0; it does not replace WaitFix.**

## Install / uninstall

1. Close Koikatu and back up any existing copies of either DLL.
2. Extract the ZIPs for the plugins you need into the Koikatu game root. Both install to `BepInEx/plugins/KPlugFixes/`.
3. Remove or disable duplicate copies under other `BepInEx/plugins/` directories; confirm loading in `BepInEx/LogOutput.log`.
4. To uninstall, exit the game, remove only the installed DLLs and restore any backed-up version if needed.

```text
Koikatu/
└─ BepInEx/
   └─ plugins/
      └─ KPlugFixes/
         ├─ KPlugPistonWaitFix.dll
         └─ KPlugPistonAutoResume.dll
```

## AutoResume settings

On first run the plugin creates `BepInEx/config/local.kplug.pistonautoresume.cfg`:

```ini
[Piston Auto Resume]
Enabled = true
ResumeDelaySeconds = 0.8
```

The delay can be set from `0.0` (fastest) to `3.0` seconds; default is `0.8`. Setting `Enabled = false` disables automatic resume. The plugin waits for the real Animator state `InsertIdle` or `A_InsertIdle` and cancels a pending resume when another H command takes precedence.

To clear settings on uninstall, remove only `BepInEx/config/local.kplug.pistonautoresume.cfg` after closing the game.

## Compatibility / technical details

Validated environment: Koikatu 5.1, kPlug 3.6.0, BepInEx 5.4.23.5, Unity 5.6.2f1, CLR 2.0.50727.1433. Compatibility with other versions is not guaranteed.

WaitFix changes only the `WaitUntil` predicate in `AI_Main.IE_AskPistonChange` and preserves the stock movement pause. AutoResume uses the game's existing Go processing path. Neither directly modifies game or kPlug binaries.

Successful startup log lines:

```text
[PistonWaitFix] v1.0.0 active.
[PistonAutoResume] v0.2.0 active.
```

The current installer ZIPs contain the original DLLs, but their extraction workflow has not been separately runtime-tested.

## Source and scripts

- [WaitFix C# source](../../src/PistonTransitionFix/WaitFix/KPlugPistonWaitFix_v1.0.0.cs)
- [AutoResume C# source](../../src/PistonTransitionFix/AutoResume/KPlugPistonAutoResume_v0.2.0.cs)
- [WaitFix build BAT](../../scripts/PistonTransitionFix/Build-KPlugPistonWaitFix_v1.0.0.bat)
- [AutoResume build BAT](../../scripts/PistonTransitionFix/Build-KPlugPistonAutoResume_v0.2.0.bat)
- [Japanese WaitFix details](KPlugPistonWaitFix_v1.0.0_README_ja.txt) / [Japanese AutoResume details](KPlugPistonAutoResume_v0.2.0_README_ja.txt)
- [Published checksums](../../SHA256SUMS.txt)

Build scripts may use a test installation path; check and configure the game root before running them.
