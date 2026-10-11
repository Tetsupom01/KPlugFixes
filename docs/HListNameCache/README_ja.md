# Sideloader HList Name Cache v0.1.0 — frozen runtime-tested build

## Purpose and boundary
Independent BepInEx/Harmony plugin for Koikatu + kPlug 3.6.0. Avoid redundant asset-name-list enumeration for exactly six `abdata/h/list/` bundles:
`hs2suite.unity3d`, `mmbath.unity3d`, `mmhouse01.unity3d`, `poolmap.unity3d`, `purpleroom.unity3d`, `sakuraroom.unity3d`.

Patch target: `AssetBundleCheck.GetAllAssetName(string, bool, string, bool)`, only with `withExtension=false`, empty/null manifest, and `isAllCheck=false`. First successful nonempty result is cloned and saved as a string-array; repeat calls return clones. Actual AssetBundle/TextAsset loading and unload, all other paths and arguments, Sideloader.dll and Assembly-CSharp.dll remain unchanged. This is independent of PoseBridge and does not fix pose collapse.

## Runtime evidence (2026-10-12)
Two in-game sessions verified cache effectiveness, absence of patch-origin errors, and user-confirmed normal H controls, pose-list display/selection and H-point movement.
- Test 1: original list calls 1 per bundle, cache hits 852 per bundle, Streaming 56 per bundle.
- Test 2: original list calls 1 per bundle, cache hits 480 per bundle (2,880 total), Streaming 86 per bundle.
- Remaining Streaming is not claimed safe to suppress.
- Intermittent pose collapse immediately after HPointMove, possibly associated with wall points, **remains a separate unresolved issue**; switching to the next motion restores the pose. Do not attribute this to or claim it was corrected by this cache.

## Immutable distributable
[`release/windows/Sideloader_HListNameCache_v0.1.0_Stable_Frozen.zip`](../../release/windows/Sideloader_HListNameCache_v0.1.0_Stable_Frozen.zip)

ZIP SHA-256: `2cf2ba5af6602e49ee8bdc8dffe8197c0e3e500c31911ce2b95b98864b8195bc`
Embedded tested `SideloaderHListNameCache.dll` SHA-256: `17f571acb7901776f1069c1b218e85edb67918c50a1ce844837c7da3616429c4`
Embedded `SideloaderHListNameCache.cs` SHA-256: `d873cc477af79ad90d69e976095f25e2e1b16f129906ec24d3c0cca98c93f76d`

The archive contains the **exact** DLL used in the second successful session (not a newly rebuilt approximation), source, BAT/PowerShell one-step installer and collector/remover, README, SHA256 list and success baseline. The Windows stable installer itself has **not yet been separately runtime-tested**; it checks expected hashes and refuses unsafe overwrite. The experimental DLL/pointer was reported removed after the test by its collector.

Required baseline SHA-256:
- `Sideloader.dll`: `a72e28585ff0bc62e13e077bf2e4077bca5a9d7ca2c41ad43dc8b48e667bbe76`
- `Assembly-CSharp.dll`: `0038281caf8df48a7903c55dc389642eeeb3f2a9114bd9d68ac11c8ac0396bc5`

No third-party game or kPlug DLL is bundled. Source and scripts are preserved unchanged inside the frozen ZIP. Do not replace the successful binary by recompiling without a separate test and new version.
