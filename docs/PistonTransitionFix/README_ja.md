# kPlug Piston Transition Fix｜日本語README

[English README](README.md)

Koikatu＋kPlug 3.6.0向けの**独立した2つのBepInExプラグイン**です。既存の日本語説明と英語READMEに基づき、公開済みのDLL単体ZIPを安全に導入するための案内をまとめています。

## 対象プラグイン

| 名前 | 用途 | ダウンロード | 個別の日本語説明 |
| --- | --- | --- | --- |
| **KPlugPistonWaitFix v1.0.0** | 挿入中にテンキー7／Backspaceでアニメーションを切り替えた後、入力が反応しなくなる問題を修正。元の`IE_AskPistonChange`の待機判定を限定修正 | [ZIP](../../release/windows/PistonTransitionFix/KPlugPistonWaitFix_v1.0.0.zip) | [詳しいREADME](KPlugPistonWaitFix_v1.0.0_README_ja.txt) |
| **KPlugPistonAutoResume v0.2.0** | 挿入モーション切替後に、元のGo処理を使ってピストン動作を自動再開。別操作があれば予約した再開を取り消す | [ZIP](../../release/windows/PistonTransitionFix/KPlugPistonAutoResume_v0.2.0.zip) | [詳しいREADME](KPlugPistonAutoResume_v0.2.0_README_ja.txt) |

**AutoResumeはWaitFixを置き換えません。** AutoResumeを使用する場合はWaitFix v1.0.0を維持してください。

## 動作確認の基準環境

- Koikatu 5.1／kPlug 3.6.0
- BepInEx 5.4.23.5／Unity 5.6.2f1／CLR 2.0.50727.1433
- 既存配布説明に記録された検証用`kPlug.dll` SHA-256：`880b34324bf1563a1e09a02a6c0960918270fae7463132d47e5fc813bcf4607f`

このSHAは当時の検証環境を示すもので、現在のkPlug.dllを置き換える指示ではありません。他バージョンの互換性を保証しません。

## 既存ZIPの導入方法

**各ZIPにはDLLが1個だけ入っています。** ゲーム本体フォルダへZIPをそのまま展開しても自動配置されません。

1. Koikatuを完全終了します。
2. 既存の`KPlugPistonWaitFix.dll`と`KPlugPistonAutoResume.dll`の場所を調べ、使用中なら先にバックアップを取ります。
3. ZIPからDLLを取り出し、既存の配布ドキュメントにある次の配置先へ置きます。

```text
Koikatu/
└─ BepInEx/
   └─ plugins/
      └─ KPlugPistonTransitionFix/
         ├─ KPlugPistonWaitFix.dll
         └─ KPlugPistonAutoResume.dll
```

4. `BepInEx/plugins/test/`等の別フォルダに同名DLLを残さず、二重ロードを防止します。不明なファイルや他のプラグインは勝手に削除しないでください。
5. ゲーム起動後、BepInExログで以下を確認します。

```text
[PistonWaitFix] v1.0.0 active.
[PistonAutoResume] v0.2.0 active.
```

既存の**開発用ビルドスクリプト**は`BepInEx/plugins/test/`へ配置する仕様です。上記の配布時配置先と混同しないでください。

## AutoResumeの設定と安全動作

初回起動後の設定ファイル：`BepInEx/config/local.kplug.pistonautoresume.cfg`

```ini
[Piston Auto Resume]
Enabled = true
ResumeDelaySeconds = 0.8
```

`ResumeDelaySeconds`は0.0秒（最短）から最大3.0秒まで。既定値は0.8秒です。自動再開を停止するには`Enabled = false`を設定してください。

女性側の実Animatorが`InsertIdle`または`A_InsertIdle`へ到達した後に待機します。待機中に別H操作・別のモーション変更・別AI処理が入ると再開予約を取り消します。WaitFixは元処理の一時停止`ForceCoupleIdle(true)`を変更しません。

## アンインストール・復元

ゲームを終了し、**自分が導入した対象DLLだけ**を取り外します。元の版があった場合は保存しておいたバックアップから復元します。AutoResume設定が不要なら`local.kplug.pistonautoresume.cfg`を削除できます。kPlug本体や他のパッチは変更しません。

## ソース・参考資料

- [WaitFix v1.0.0 ソース](../../src/PistonTransitionFix/WaitFix/KPlugPistonWaitFix_v1.0.0.cs)
- [AutoResume v0.2.0 ソース](../../src/PistonTransitionFix/AutoResume/KPlugPistonAutoResume_v0.2.0.cs)
- [従来の日本語配布用説明](README_配布用.txt)
- [英語の総合README](README.md)
- [公開SHA-256一覧](../../SHA256SUMS.txt)

本書は既存配布物への補足説明です。配布ZIPや実機成功済みDLLを変更するものではありません。
