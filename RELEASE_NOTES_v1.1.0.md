# KPlugFixes v1.1.0

Feature addition: **KPlugCrossFaderCompatFix v1.0.0**.

## CrossFaderCompatFix

### Problem

With Koikatu + kPlug 3.6.0 + CrossFader 0.11, rapid/timed H-animation change requests could lead to a native Access Violation while `HSceneProc.ChangeAnimator` was replacing an Animator runtime controller.

### Final design

v1.0.0 uses a narrow first-request-wins gate.

- first non-null `HFlag.selectAnimationListInfo` request is accepted
- later different non-null setter writes are blocked while the gate is active
- vanilla null clear is always allowed
- the first request is preserved in `HSceneProc.Update` until CrossFader and all relevant H Animators are stable for 2 consecutive frames
- the original `HSceneProc.ChangeAnimator` then runs unchanged
- the gate remains active until the same stable condition is observed for 2 consecutive frames after the change

The fix does not modify the CrossFader body, `SetPlayHook`, `Animator.CrossFadeInFixedTime`, the `ChangeAnimator` body, or the runtimeAnimatorController setter.

### Runtime verification

The successful runtime implementation was v0.8.0. v1.0.0 is functionally identical source with only the plugin version metadata formalized.

Observed in the final verification run:

- multiple animation changes completed through `GATE START -> ALLOW CHANGE -> GATE RELEASE`
- no native crash / Access Violation
- H scene and game exited normally
- CrossFader 0.11 loaded and reported active transition state during H changes
- final verification showed `blockedWrites=0`

The last item means the final run did not directly exercise a later setter write being blocked. No stronger claim is made.

### Source package

`release/CrossFaderCompatFix/KPlugCrossFaderCompatFix_v1.0.0-source.zip`

SHA-256:

`3077ac3b8ea547c6c8e7a0cf96e5e11ab3aa1495e506ca7ac998f5cd5470f5e4`

The exact v1.0.0 rebuilt DLL SHA-256 is intentionally not invented. It should be recorded from the first formal v1.0.0 build manifest.

## Prebuilt Windows binaries

No-build Windows packages are provided individually under `release/windows/`:

- `KPlugGaugeSwapFix_v0.2.0.zip`
- `KPlugNullGuardsFix_v0.2.0.zip`
- `KPlugPistonWaitFix_v1.0.0.zip`
- `KPlugPistonAutoResume_v0.2.0.zip`
- `KPlugCrossFaderCompatFix_v0.8.0-runtime-tested.zip`

Each package contains the exact DLL recovered from the active runtime-tested Koikatu environment on 2026-09-29.

CrossFaderCompatFix is deliberately distributed as the exact runtime-tested v0.8.0 DLL. The v1.0.0 package in `release/CrossFaderCompatFix/` remains source-only until a v1.0.0 DLL is built and verified.

The independent MyRoom mannequin Harmony fix is not included because it has not yet reached its formal runtime-verified release state.
