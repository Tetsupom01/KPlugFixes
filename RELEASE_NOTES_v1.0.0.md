# KPlugFixes v1.0.0

Initial consolidated repository release.

This repository collects successful kPlug fixes while keeping each runtime DLL independent.

## GaugeSwapFix

### KPlugGaugeSwapFix v0.2.0

- Fixes Face/Gauge state reverting after a Main-girl swap when the H category/animation changes.
- Patches only `HSceneProc.ChangeAnimator` with one Prefix/Postfix pair.
- Leaves vanilla animator reinitialization intact.
- Reapplies existing kPlug `faceOverwrite` and restores the preserved gauges afterward.
- Runtime verification completed successfully.

## NullGuardsFix

### KPlugNullGuardsFix v0.1.0

- Reproduces the successful null guards formerly embedded in a directly modified kPlug build.
- Uses 12 Prefix guards plus one Voice coroutine transpiler.
- Does not modify `kPlug.dll`.
- Runtime verification completed successfully.

Known-good DLL SHA-256:

`e1e585f3961126adda211d140535ab7590ed2d47c6f0ce97bbd2f146565a1c1e`

## PistonTransitionFix

Migrated from the original `KPlugPistonTransitionFix` repository.

### KPlugPistonWaitFix v1.0.0

- Fixes the Numpad 7 / Backspace insertion-animation transition wait condition.
- Preserves the original kPlug coroutine path and only expands the WaitUntil completion condition.

Known-good DLL SHA-256:

`c134bde2967194477c686e26ebca12010c3d81653e97a7b226e33a6b7294eabd`

### KPlugPistonAutoResume v0.2.0

- Optional automatic resume after a piston/insertion animation change.
- Waits for the actual InsertIdle / A_InsertIdle animator state.
- Uses the normal Go route.
- Cancels when another H action or transition supersedes the pending resume.

Known-good DLL SHA-256:

`0abaa27f88469dcb9e73a1f83878da522e61f3a73cd3a7df9aad4ad668290f4f`

## Repository organization

Sources, scripts, and documentation are grouped by feature:

```text
src/<Feature>/
scripts/<Feature>/
docs/<Feature>/
```

The plugins remain separate DLLs by design.

## GaugeSwap binary note

The runtime-tested KPlugGaugeSwapFix v0.2.0 DLL hash was not recorded during the successful test. No rebuilt DLL is substituted as the historical known-good binary.
