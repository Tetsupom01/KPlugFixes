# KPlugPoseBridge v0.2.0.1｜リリースノート

対象：Koikatu 5.1／kPlug 3.6.0／BepInEx 5.4.23.5

## 機能
- kPlug Pose Selectorで利用可能な挿入体位（mode 2）を、KoikatuのH画面の体位一覧へ同期。
- `ToolAnimSelector.GetAvailablePiston()`、`ParserAnim.GetHAnim()`を使用。
- Hリストの初期化前は同期を延期（`SYNC_DEFER`）。体位名の解決に失敗した場合は既存一覧を維持。

## 動作確認と制限
- v0.2.0.1の起動・パッチ初期化を確認。
- 床・壁・机の一覧同期を確認。ただしすべての体位の操作を網羅したものではありません。
- 対象は挿入体位（mode 2）に限定。service、3P、lesbianは含みません。

## ダウンロード
- [現行導入ZIP（日英README同梱）](../../release/windows/PoseBridge/KPlugPoseBridge_v0.2.0.1_KPlugFixes_Install.zip)（配置先：`BepInEx/plugins/KPlugFixes/KPlugPoseBridge.dll`）
- [完全C#ソース](../../src/PoseBridge/KPlugPoseBridge_v0.2.0.1.cs)
- [日本語ガイド](README_ja.md)

DLL SHA-256：`F1A35FF5A1849001D2705DD3F4C368FCB9A309170B51F244AEB1FD99583C9142`  
ZIP SHA-256：`76A985805C3817B2CAF4F69E787FDCC93125780619A66CE69F3D2EF2D7039BA4`

**English summary:** Synchronizes insertion-position choices (mode 2) between kPlug Pose Selector and the vanilla H scene. The current installer targets `BepInEx/plugins/KPlugFixes/`. Startup and patch activation are verified; full pose-by-pose regression testing remains incomplete.
