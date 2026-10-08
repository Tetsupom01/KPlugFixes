# KPlug Pose Bridge v0.2.0.0

## 目的

kPlug 3.6.0 の Pose Selector で利用可能な挿入体位を、Koikatu 本体側の H UI でも同じ集合として利用できるようにする。

## 実装

kPlug 自身の既存経路を利用する。

`ToolAnimSelector.GetAvailablePiston()`
→ canonical pose name
→ `ParserAnim.GetHAnim(canonical)`
→ `HSceneProc.AnimationListInfo`

初期同期は `ExtraAnimManager.Start` の Postfix、Hポイント/カテゴリ変更後の再同期は `HSceneProc.CreateListAnimationFileName(bool,int)` の Postfix で行う。

全canonical体位を解決できた場合だけ `lstUseAnimInfo[2]` を置き換える。1件でも解決できない場合は `SYNC_ABORT` として既存リストを変更しない。

## 実機確認

2026-10-08 の実機ログで以下を確認した。

- 床 `currentCats=[0]`: 64 → 77、requested=77 / finalCount=77 / canonicalUnique=77
- 壁 `currentCats=[8]`: 7 → 12、requested=12 / finalCount=12 / canonicalUnique=12
- 机 `currentCats=[7]`: 11 → 20、requested=20 / finalCount=20 / canonicalUnique=20
- `SYNC_ABORT` / Sync failed / Sync invocation failed は確認されていない
- ユーザーが選択した範囲の体位遷移は正常
- 全体位の総当たり確認は未実施。通常プレイ継続で副作用を監視する

## 判定

v0.2.0.0 を成功版として保存する。

## 対象範囲

- mode 2（挿入）のみ
- service / 3P / lesbian は本版の対象外
- 他の成功済み kPlug 修正 DLL には変更を加えない

## 成果物

`release/windows/KPlugPoseBridge_v0.2.0.0.zip`

ZIP SHA-256:

`df98829f15469b20868a4fe69f20f8c6a58f041dbf54e3c315b96f928f235a5c`
