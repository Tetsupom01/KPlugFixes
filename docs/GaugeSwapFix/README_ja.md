# KPlugGaugeSwapFix v0.2.0

## 配布ZIPの使い方（追加の日本語案内）

- [配布ZIP v0.2.0](../../release/windows/GaugeSwapFix/KPlugGaugeSwapFix_v0.2.0.zip)
- [完全ソース](../../src/GaugeSwapFix/KPlugGaugeSwapFix_v0.2.0.cs)
- [公開SHA-256一覧](../../SHA256SUMS.txt)

この旧配布ZIPは**`KPlugGaugeSwapFix.dll`だけ**を収録しており、自動インストーラではありません。Koikatuを終了し、ZIPからDLLを取り出して`BepInEx/plugins/`配下のプラグイン読込場所に配置してください。既存の`KPlugGaugeSwapFix.dll`や旧診断DLLを確認し、重複ロードを避けてください。開発用ビルドスクリプトは`BepInEx/plugins/test/`に配置する設計ですが、配布ZIPにその自動配置機能はありません。

導入後、BepInExログで`GaugeSwapFix v0.2.0`のロードを確認し、Main girlをスワップして別カテゴリへ移動した際のFace/Gauge表示を確認してください。解除するにはゲームを終了し、**今回追加したDLLだけ**を外します。既存版を置き換えた場合は退避した原本から復元してください。ゲーム本体、kPlug.dll、他の修正DLLは変更しません。

## 目的

kPlug 3.6.0でMain girlをスワップした後、`寝 -> 椅子` などHのカテゴリ/アニメーションを変更すると、
実際の相手はスワップ後の女の子のままなのにFace/GaugeがH開始時の状態へ戻る問題を局所修正する。

## 今回の根拠

確認済みの処理経路:

- kPlugのスワップ処理は新Main girlのFaceを `HCtl.faceOverwrite` に保持する。
- kPlugのスワップ処理は新Main girlの値を `HFlag.gaugeFemale` へ反映する。
- PointMove/カテゴリ変更ではバニラ `HSceneProc.ChangeCategory` が呼ばれる。
- その先の `HSceneProc.ChangeAnimator` で `HSprite.InitHeroine(HFlag.lstHeroine)` 等の再初期化が走る。
- 実機ログではカテゴリ変更直後、`Dealer.Update` に入る前の段階ですでにGaugeが旧側へ変化していた。
- 同時にkPlugの `lastGaugeValues` にはスワップ後の値が残っていた。
- kPlugの既存Face再適用経路は `animAtPointMovEntry == Core.hAnim` 条件があり、カテゴリが変わる場合は再適用されない。

したがって、修正境界を `HSceneProc.ChangeAnimator` に限定する。

## 実装

Harmony Prefix/Postfixを `HSceneProc.ChangeAnimator` へ1組だけ適用する。

スワップ中 (`HCtl.faceOverwrite != null`) の場合のみ:

1. Prefixで現在の `gaugeFemale / gaugeMale` と `faceOverwrite` を退避。
2. バニラ `ChangeAnimator` は変更せず最後まで実行。
3. Postfixで同じ `HCtl` / 同じ `faceOverwrite` が継続中であることを確認。
4. 退避したGaugeを戻す。
5. kPlug既存の `ToolUI.ReplaceFaceImage(faceOverwrite)` を呼ぶ。

通常H、スワップしていない状態、`faceOverwrite2` のみの状態には介入しない。

## 変更しないもの

- `HSceneProc.ChangeAnimator` 本体の処理順
- `HFlag.lstHeroine`
- `Core.heroine`
- キャラクター本体
- アニメーション選択
- DynamicBone処理
- SmartUI状態
- kPlug.dll / Assembly-CSharp.dll 本体
- 既存のPiston/NullGuard/MyRoom修正

## ビルド条件

- Game root: `F:\illusion\Koikatu`
- kPlug 3.6.0 SHA-256:
  `34c13976108db0a18a7ad6b7cfda5517b4a9a83d85c9a909820144264c743855`
- Assembly-CSharp.dll SHA-256:
  `0038281caf8df48a7903c55dc389642eeeb3f2a9114bd9d68ac11c8ac0396bc5`

BATをダブルクリックしてビルド/配置する。

## トランザクション

- TEMPへのコンパイル成功前はゲーム側を変更しない。
- コンパイル成功後に旧GaugeSwap診断/修正DLLだけをOFFへ退避する。
- インストール失敗時は、この実行で退避したDLLを元へ戻す。

## 実機確認

1. H開始。
2. Iキーで女の子を呼ぶ。
3. 呼んだ女の子をMainへスワップ。
4. Face/Gaugeがスワップ後の女の子になっていることを確認。
5. 以前失敗した `寝 -> 椅子 -> 別カテゴリ` などを連続実行。
6. 各変更後もFace/Gaugeがスワップ後状態のままなら成功。

期待ログ:

```text
[GaugeSwapFix] v0.2.0 active. Target=HSceneProc::ChangeAnimator. Main-girl swap only.
[GaugeSwapFix] Rebound swapped main-girl Face/Gauge after ChangeAnimator. ...
```
