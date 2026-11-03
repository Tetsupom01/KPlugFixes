# KPlugPoseBridge v0.2.0.1｜リリースノート

対象：Koikatu 5.1、kPlug 3.6.0、BepInEx 5.4.23.5（確認済み環境）。

## 機能
- kPlug Pose Selectorで利用可能な挿入体位（mode 2）をKoikatu本体のH画面の体位一覧へ同期する。
- 既存の`ToolAnimSelector.GetAvailablePiston()`と`ParserAnim.GetHAnim()`を利用する。
- `ExtraAnimManager.Start`より早い`CreateListAnimationFileName`では一覧の同期を延期し、既存一覧を保護する（`SYNC_DEFER`）。
- 必要な体位名を解決できない場合は一覧を書き換えない。

## 実機検証・既知の制限
- v0.2.0.1のDLLを2026-10-31の実機から回収し、起動・パッチ有効化を確認。
- 2026-11-03、`BepInEx/plugins/KPlugFixes/`への移動後も、同一DLLがBepInExで1回ロードされ初期化することを確認。
- 床・壁・机の同期動作は従来の実機確認で検証したが、v0.2.0.1の全体位を網羅した実機操作は未実施。
- 本版の対象は挿入体位（mode 2）のみで、service・3P・lesbianは対象外。

## 配布
- [最新版・日本語／英語README同梱ZIP](../../release/windows/PoseBridge/KPlugPoseBridge_v0.2.0.1_KPlugFixes_Install.zip)
- [完全ソース](../../src/PoseBridge/KPlugPoseBridge_v0.2.0.1.cs)
- DLL SHA-256：`F1A35FF5A1849001D2705DD3F4C368FCB9A309170B51F244AEB1FD99583C9142`
- ZIP SHA-256：`76A985805C3817B2CAF4F69E787FDCC93125780619A66CE69F3D2EF2D7039BA4`
- ZIPにはゲーム本体、kPlug、BepInExなどの第三者バイナリを含めない。
- v0.2.0.0およびv0.2.0.1の旧配置ZIPは履歴として維持。

## English summary
KPlugPoseBridge v0.2.0.1 synchronizes the insertion-position list (mode 2) used by the vanilla H scene with kPlug Pose Selector. The current installer targets `BepInEx/plugins/KPlugFixes/` and includes Japanese and English installation guides. Startup and patch activation are verified; exhaustive pose-by-pose regression testing has not been performed.
