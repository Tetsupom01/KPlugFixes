# KPlug Pose Bridge

## 最新の実機版：v0.2.0.1

2026-10-31の起動ログで`KPlug Pose Bridge 0.2.0.1`のロードと、`[PoseBridge] v0.2.0.1 active`を確認。

- [v0.2.0.1 完全ソース](../../src/PoseBridge/KPlugPoseBridge_v0.2.0.1.cs)
- `ExtraAnimManager.Start`前の`CreateListAnimationFileName`で、早期同期を行わず既存リストを保持する`SYNC_DEFER`対応を含む
- [v0.2.0.1 導入ZIP](../../release/windows/PoseBridge/KPlugPoseBridge_v0.2.0.1_Install.zip)：2026-10-31実機で回収したDLLを無改変で同梱\n- DLL SHA-256：`F1A35FF5A1849001D2705DD3F4C368FCB9A309170B51F244AEB1FD99583C9142`\n- 導入ZIP SHA-256：`35359C57BD06F4926BD41B75F7B0CDDADB4BD4CDD8071C4023D25DF5B83135FF`\n- 起動・パッチ適用は確認済み。回収したバイナリ単体で全体位を網羅した実機テストは未実施。旧v0.2.0.0 ZIPは履歴版
- 成功済み実機DLLを不用意に再ビルド・上書きしない

## 過去の成功版：v0.2.0.0

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

`release/windows/PoseBridge/KPlugPoseBridge_v0.2.0.0.zip`

ZIP SHA-256:

`df98829f15469b20868a4fe69f20f8c6a58f041dbf54e3c315b96f928f235a5c`
