# kPlug Piston Transition Fix

[日本語版README](README_ja.md) | English

BepInEx plugins for Koikatu + kPlug 3.6.0 that fix insertion-animation switching issues and optionally resume piston motion automatically after using Numpad 7 / Backspace.

This project contains two separate plugins with different roles:

- **KPlugPistonWaitFix v1.0.0**  
  Fixes an issue where animation navigation can stop responding after changing an insertion-state animation with Numpad 7 / Backspace.
- **KPlugPistonAutoResume v0.2.0**  
  Automatically resumes piston motion through the game's normal Go route after switching insertion/piston animations while motion is active.

## Target environment

Validated with:

- Koikatu 5.1
- kPlug 3.6.0
- BepInEx 5.4.23.5
- Unity 5.6.2f1
- CLR 2.0.50727.1433

Validated `kPlug.dll` SHA-256:

`880b34324bf1563a1e09a02a6c0960918270fae7463132d47e5fc813bcf4607f`

## Installation — current KPlugFixes layout

Download the current game-root installation ZIPs from this repository:

- [KPlugPistonWaitFix v1.0.0 KPlugFixes installer](../../release/windows/PistonTransitionFix/KPlugPistonWaitFix_v1.0.0_KPlugFixes_Install.zip)
- [KPlugPistonAutoResume v0.2.0 KPlugFixes installer](../../release/windows/PistonTransitionFix/KPlugPistonAutoResume_v0.2.0_KPlugFixes_Install.zip)

Each current ZIP contains the unchanged original DLL, Japanese and English READMEs and a SHA256 manifest. Exit Koikatu, back up any existing version, and extract the `BepInEx` folder into your Koikatu game root. The resulting layout (only install the plugins you need) is:

```text
Koikatu/
└─ BepInEx/
   └─ plugins/
      └─ KPlugFixes/
         ├─ KPlugPistonWaitFix.dll
         └─ KPlugPistonAutoResume.dll
```

AutoResume requires WaitFix v1.0.0 and does not replace it. Check other plugin folders for duplicate copies, then confirm loading in BepInEx/LogOutput.log. To uninstall, exit the game, remove only the DLLs installed by these ZIPs, and restore previous versions from your backups if needed.

The former single-DLL ZIPs are preserved for historical use: [WaitFix](../../release/windows/PistonTransitionFix/KPlugPistonWaitFix_v1.0.0.zip), [AutoResume](../../release/windows/PistonTransitionFix/KPlugPistonAutoResume_v0.2.0.zip). The new game-root ZIP layout has not been separately tested on Windows; the DLL payloads are byte-identical to the previous packages.

## Auto Resume configuration

After the first launch, the following config file is created:

```text
BepInEx/config/local.kplug.pistonautoresume.cfg
```

Main settings:

```ini
[Piston Auto Resume]

Enabled = true
ResumeDelaySeconds = 0.8
```

`ResumeDelaySeconds` controls how long Auto Resume waits after the real Animator state reaches `InsertIdle` / `A_InsertIdle` before triggering the normal Go action.

- `0.0` = fastest
- `0.8` = default
- up to `3.0` seconds
- set `Enabled = false` to disable Auto Resume

If another H-scene command or animation change occurs while Auto Resume is waiting, the scheduled resume is cancelled instead of overwriting that action.

## Startup check

When both plugins load successfully, the BepInEx log should contain:

```text
[PistonWaitFix] v1.0.0 active.
[PistonAutoResume] v0.2.0 active.
```

## Uninstallation

Exit Koikatu and remove only the DLL(s) installed with the current packages:

```text
BepInEx/plugins/KPlugFixes/KPlugPistonWaitFix.dll
BepInEx/plugins/KPlugFixes/KPlugPistonAutoResume.dll
```

The older `BepInEx/plugins/KPlugPistonTransitionFix/` location belongs to historical manual installation. Check it for duplicate DLLs when migrating; do not delete unrelated files.

To remove the Auto Resume configuration as well:

```text
BepInEx/config/local.kplug.pistonautoresume.cfg
```

## Repository structure

```text
src/
├─ WaitFix/
│  └─ KPlugPistonWaitFix_v1.0.0.cs
└─ AutoResume/
   └─ KPlugPistonAutoResume_v0.2.0.cs

scripts/
├─ build_KPlugPistonWaitFix_v1.0.0.ps1
├─ build_KPlugPistonAutoResume_v0.2.0.ps1
└─ Make-Release.ps1

docs/
├─ KPlugPistonWaitFix_v1.0.0_README_ja.txt
├─ KPlugPistonAutoResume_v0.2.0_README_ja.txt
└─ README_配布用.txt
```

Japanese documentation is available in the `docs/` folder.

## Building from source

The included PowerShell build scripts use the following game path by default:

```text
F:\illusion\Koikatu
```

If your installation is elsewhere, edit `$GameRoot` in the script.

## Creating a release ZIP

If you already have tested DLLs installed in your game folder:

```powershell
cd scripts
.\Make-Release.ps1
```

The generated ZIP is placed in the repository's `release` folder.

## Technical notes

Wait Fix patches only the `WaitUntil` predicate used by `AI_Main.IE_AskPistonChange`. It does not directly modify `Assembly-CSharp.dll` or `kPlug.dll`.

Auto Resume waits until the actual female Animator reaches `InsertIdle` / `A_InsertIdle`, then uses the game's normal Go path. It also cancels the pending resume when the context changes, so user input is not overwritten.
