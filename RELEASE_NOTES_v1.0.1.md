# KPlugFixes v1.0.1

Stable maintenance update.

## NullGuardsFix

### KPlugNullGuardsFix v0.2.0

Promoted to the current stable version.

v0.2.0 integrates the previously standalone `KPlugAtHomeDestroyGuard v0.1.1` into `KPlugNullGuardsFix`.

Runtime patch set:

- 9 H-process Prefix guards
- 2 KokanBehavior Prefix guards
- 1 MenuCorner Prefix guard
- 1 AtHomeCtrl.OnDestroy destroyed-object Prefix guard
- 1 Voice coroutine Transpiler

Expected startup log:

```text
[NullGuardsFix] Voice guard injected at AudioSource.clip -> AudioClip.length path.
[NullGuardsFix] v0.2.0 active. 13 Prefix guards + Voice Transpiler. AtHome destroy guard included.
```

### Final runtime verification

Verified with the project Koikatu + kPlug 3.6.0 environment.

Confirmed:

- v0.2.0 loaded successfully
- 13 Prefix guards + Voice Transpiler active
- MyRoom entry/exit completed normally
- game exited normally
- no `kPlug.CmpBase.AtHomeCtrl.OnDestroy` NullReferenceException
- no `NullGuardsFix patch failed`
- no `InvalidProgramException`
- no AccessViolation / Fatal / Crash in the final verification run

The final verification run did not emit `AtHome OnDestroy: removed N ...`, meaning no null/destroyed `girlRoots` entries required removal during that run.

### Version history

- v0.1.0 remains preserved as the previous known-good standalone NullGuards version.
- standalone `KPlugAtHomeDestroyGuard v0.1.1` is superseded by v0.2.0 integration.
- v0.2.0 is the current stable version.

Historical v0.1.0 DLL SHA-256:

`e1e585f3961126adda211d140535ab7590ed2d47c6f0ce97bbd2f146565a1c1e`

The runtime-tested v0.2.0 DLL SHA-256 was not captured in the verification log, so no replacement or rebuilt hash is substituted.

## Other fixes

No runtime behavior changes were made to:

- KPlugGaugeSwapFix v0.2.0
- KPlugPistonWaitFix v1.0.0
- KPlugPistonAutoResume v0.2.0
