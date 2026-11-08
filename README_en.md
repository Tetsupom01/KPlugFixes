# KPlugFixes — BepInEx plugins for Koikatu

[日本語README](README.md)

This repository provides individual fixes for Koikatu H-scene issues and a targeted H-list optimization. Install **only the plugins you need**. No third-party game, kPlug, CrossFader, Sideloader, or BepInEx DLLs are redistributed.

## Available plugins

### kPlug-related fixes (6 plugins)

| Plugin | Purpose | Current download |
| --- | --- | --- |
| **CrossFaderCompatFix** | Reduces pose displacement after H-point moves and conflicting animation requests, stabilizing H-scene transitions | [v1.2.0 ZIP](release/windows/CrossFaderCompatFix/KPlugCrossFaderCompatFix_v1.2.0_KPlugFixes_Install.zip) / [Details](docs/CrossFaderCompatFix/README_ja.md) |
| **PoseBridge** | Makes kPlug insertion poses missing from the standard H-scene pose list selectable there | [v0.2.0.1 ZIP](release/windows/PoseBridge/KPlugPoseBridge_v0.2.0.1_KPlugFixes_Install.zip) / [Details](docs/PoseBridge/README_ja.md) |
| **GaugeSwapFix** | Prevents Face/Gauge displays from reverting after changing an H animation with a swapped Main partner | [v0.2.0 ZIP](release/windows/GaugeSwapFix/KPlugGaugeSwapFix_v0.2.0_KPlugFixes_Install.zip) / [Details](docs/GaugeSwapFix/README_ja.md) |
| **NullGuardsFix** | Prevents selected NullReferenceExceptions during H-scene processing, voice playback and MyRoom shutdown, reducing the risk of game instability and crashes | [v0.2.0 ZIP](release/windows/NullGuardsFix/KPlugNullGuardsFix_v0.2.0_KPlugFixes_Install.zip) / [Details](docs/NullGuardsFix/README_ja.md) |
| **PistonWaitFix** | Prevents H controls becoming unresponsive after switching insertion animations with Numpad 7 or Backspace | [v1.0.0 ZIP](release/windows/PistonTransitionFix/KPlugPistonWaitFix_v1.0.0_KPlugFixes_Install.zip) / [Details](docs/PistonTransitionFix/README.md) |
| **PistonAutoResume** | Automatically restarts piston movement through the normal Go action after insertion-animation changes | [v0.2.0 ZIP](release/windows/PistonTransitionFix/KPlugPistonAutoResume_v0.2.0_KPlugFixes_Install.zip) / [Details](docs/PistonTransitionFix/README.md) |

**PistonAutoResume requires PistonWaitFix v1.0.0.**

### Independent Koikatu-side plugins (2 plugins)

| Plugin | Purpose | Current download |
| --- | --- | --- |
| **SideloaderHListNameCache** | Caches repeated H-list asset-name lookups, reducing duplicate processing for six selected bundles | [v0.1.0 ZIP](release/windows/HListNameCache/Sideloader_HListNameCache_v0.1.0_Standalone_Install.zip) / [Details](docs/HListNameCache/README_ja.md) |
| **HPointBenchRouteFix** | Fixes the old insertion animation remaining when selecting Bench Fellatio from a chair or sofa | [v1.0.0 ZIP](release/windows/HPointBenchRouteFix/HPointBenchRouteFix_v1.0.0_Install.zip) / [English details](docs/HPointBenchRouteFix/README_en.md) |

These two plugins do not call kPlug APIs directly. Runtime operation without kPlug **has not been verified**.

## Installation

1. **Exit Koikatu** and back up any existing DLL of the same name.
2. Extract the installer ZIP for each selected plugin into your **Koikatu game root**.
3. Check `BepInEx/plugins/` for duplicate copies or older trial versions.
4. Start the game and check `BepInEx/LogOutput.log` for a single load of each installed plugin; consult the individual guides for additional checks.

The current ZIP packages install to:

```text
Koikatu/
└─ BepInEx/
   └─ plugins/
      ├─ KPlugFixes/
      │  ├─ KPlugCrossFaderCompatFix.dll
      │  ├─ KPlugPoseBridge.dll
      │  ├─ KPlugGaugeSwapFix.dll
      │  ├─ KPlugNullGuardsFix.dll
      │  ├─ KPlugPistonWaitFix.dll
      │  ├─ KPlugPistonAutoResume.dll
      ├─ HPointBenchRouteFix.dll
      └─ SideloaderHListNameCache.dll
```

Each ZIP includes only the relevant plugin DLL; not all eight are required. To uninstall, exit the game, remove only the DLL installed for that feature, and restore a previous version from backup if needed.

## Compatibility and limitations

Main test environment: Koikatu 5.1, kPlug 3.6.0, BepInEx 5.4.23.5, Unity 5.6.2f1, Windows 11. Individual plugins may have additional requirements.

- **CrossFaderCompatFix:** For CrossFader 0.11. Transient pose offset when pressing Enter after post-climax H-point moves and occasional Enter nonresponse remain unresolved.
- **PoseBridge:** Insertion poses (mode 2) only. Not every pose has been individually exercised.
- **HListNameCache:** Limited to six specific asset bundles. Operation without kPlug has not been tested.
- **HPointBenchRouteFix:** Applies only to the targeted Bench Fellatio transition. Second launch, final logs and kPlug-free operation are not verified.
- For some repackaged ZIPs, the **installation process itself has not been separately tested**, even though the enclosed DLLs are unchanged from previously tested builds. See individual guides.

## Source and technical documentation

[Source code](src/) | [Build scripts](scripts/) | [Feature documentation](docs/) | [Installer ZIPs](release/windows/) | [Checksums](SHA256SUMS.txt)

Older packages are retained for comparison and rollback. For new installations, use the current versions linked in the tables above.
