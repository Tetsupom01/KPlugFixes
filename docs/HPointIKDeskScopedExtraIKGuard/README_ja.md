# HPointIK DeskScoped ExtraIK Guard v0.2.0 RC1 (Frozen)

Status: **runtime-tested RC1; archive metadata registered; exact ZIP/DLL binary not yet committed to this repository**. Recorded on 2026-10-17 (JST).

## Purpose and scope

Addresses issue A: after moving to desk-back caress (`kha_f_05`), one or both female hands/arms could remain in an incorrect pose. During a `HSceneProc.ChangeAnimator` whose destination is precisely desk-back caress, the old insertion-motion extra-IK check could fire after the vanilla `kha_f_05` load and apply the generic `kha_f_00` data. The RC1 guard suppresses **only the one matching** `ExtraAnimManager.SetIkTargets(false)` call when the required old/new states and stale extra-IK conditions match. A failed or mismatched condition is fail-open (original behavior preserved). This is not a global extra-IK disabling fix.

The frozen RC1 DLL is byte-identical to the successful v0.2.0 EXP build, so its internal `EXPERIMENT`/EXP string is retained intentionally. **Do not recompile or change the predicate and represent the output as the same RC1.** FlowDiag is a separate diagnostic tool, not part of the RC1 runtime installation.

## Frozen artifact identities (from installation / collection records)

| Item | Name / SHA-256 |
|---|---|
| Intended exact distribution ZIP | `HPointIK_DeskScopedExtraIKGuard_v0.2.0_RC1_Frozen_OneStep.zip` |
| ZIP SHA-256 | `DDAAF270E707FD9B8F7074FB4641565511E2E2D5065E13C2B5566477CFB4F951` |
| Installed / tested plugin DLL | `HPointIKStaleExtraIKGuard.dll` |
| DLL SHA-256 | `F167EF2459C695B4A7B1782141E9640E1C013D00B833D1E48D5B0C54FD4ECC54` |
| Frozen source SHA-256 | `B9E155DEF6C166FE58ED36E128425092E11AB4BF180F687F63BEA1A5165A6B47` |
| kPlug.dll reference SHA-256 (not redistributed) | `34C13976108DB0A18A7AD6B7CFDA5517B4A9A83D85C9A909820144264C743855` |
| Assembly-CSharp.dll reference SHA-256 (not redistributed) | `0038281CAF8DF48A7903C55DC389642EEEB3F2A9114BD9D68AC11C8AC0396BC5` |

The target Windows installation path is `BepInEx\\plugins\\KPlugFixes\\HPointIKStaleExtraIKGuard.dll`. The game-local installed file and collection manifest were reported byte-identical by the OneStep operation log. The exact ZIP file was listed in the project's Library but its bytes were unavailable to the GitHub registration session: **no recreated binary, placeholder ZIP, or invented checksum has been uploaded**. When the original is available, verify the ZIP and embedded DLL and commit the unchanged ZIP under `release/windows/` before treating this registration as a binary backup.

## Runtime evidence

On 2026-10-17 the EXP build plus FlowDiag observed three `SUPPRESS_DESK_EXTRA` events (09:13:35, 09:15:01, 09:16:29 desk transitions); post-suppression manual observations showed hand target world Y approximately L=0.6735 m and R=0.6722 m. The exact same DLL was then installed as RC1, without FlowDiag. In the **09:32:19 start session only**, the RC1 collection recorded:

```text
END animatorCalls=26 deskCalls=6 checks=6 suppressed=4 passed=0 errors=0
```

Four qualifying desk transitions were suppressed; two non-qualifying desk calls retained their original behavior. The tester reported that multiple transitions were almost entirely normal. These are finite-run results, not an exhaustive test across all characters and H points.

## Separate outstanding issue B — DO NOT fold into RC1

When desk-back caress starts in a correct pose, pressing Numpad Enter still always briefly draws hands intersecting the desk before the next motion settles. The same transient existed before RC1. Its direct cause is unconfirmed. Investigate Enter -> `ChangeAnimator(mode=sonyu)` -> Animator/IK/CrossFader/Solver ordering with a separately deployed, read-only, bounded diagnostic if existing evidence is insufficient. Do not expand RC1, force `oriTargs` restoration, modify baseline game DLLs, or overwrite other successful fixes in an attempt to address issue B.

## Preservation rules

- Preserve the SHA-matched RC1 DLL, source, installer/rollback scripts and frozen OneStep ZIP without changes.
- Check baseline DLL hashes and only install the original verified package; installation/removal must not touch unrelated fixes.
- Do not redistribute kPlug.dll, game assemblies, BepInEx or Harmony binaries.
- Source of this registration: `HPointIK_RC1_EnterTransition_引き継ぎ_20261017.md` and the dated EXP verification and RC1 collection records. SHA claims above are transcribed from those records, not an independent GitHub-side ZIP byte verification.
