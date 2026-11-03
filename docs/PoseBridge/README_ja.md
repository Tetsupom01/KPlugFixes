# PoseBridge｜日本語ガイド

## 現行版：v0.2.0.1

kPlugのPose Selectorで選べる挿入体位（mode 2）を、Koikatu本体H画面の体位一覧にも反映するプラグインです。既存のkPlug内部処理を利用し、取得・変換に失敗するときは元の一覧を保持します。

- **推奨導入ZIP（日本語README付き）：** [v0.2.0.1／KPlugFixesフォルダ配置](../../release/windows/PoseBridge/KPlugPoseBridge_v0.2.0.1_KPlugFixes_Install.zip)
- [C#ソース](../../src/PoseBridge/KPlugPoseBridge_v0.2.0.1.cs)
- **配置先：** `BepInEx/plugins/KPlugFixes/KPlugPoseBridge.dll`
- **DLL SHA-256：** `F1A35FF5A1849001D2705DD3F4C368FCB9A309170B51F244AEB1FD99583C9142`
- **新ZIP SHA-256：** `929A45F7F3B222194B4F2DACB9FC99E7CB8D48D3C4FE59161457800718E1AEF2`

**インストール：** ゲーム終了→同名DLLをバックアップ→ZIPをゲーム本体フォルダへ展開。旧配置先`BepInEx/plugins/`直下などの同名DLLを残さないよう確認してください。

**確認済み：** 実機でv0.2.0.1のロードとHarmony有効化を確認し、2026-11-03の`KPlugFixes`フォルダ移行後も1回ロードされて正常に初期化されました。挿入体位一覧の同期は一部カテゴリで検証しましたが、すべての体位を網羅したわけではありません。

旧`KPlugPoseBridge_v0.2.0.1_Install.zip`は`plugins`直下に配置される歴史的パッケージです。新規導入では上記の新ZIPを推奨します。

---

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
