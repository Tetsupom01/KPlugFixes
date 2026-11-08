# KPlugGaugeSwapFix v0.2.0｜日本語ガイド

kPlug 3.6.0のHシーンで、女の子をMainへ入れ替えた際のFace/Gauge表示を補正するプラグインです。実際の相手を変更するパッチではありません。

[導入ZIP](../../release/windows/GaugeSwapFix/KPlugGaugeSwapFix_v0.2.0_KPlugFixes_Install.zip) ／ [C#ソース](../../src/GaugeSwapFix/KPlugGaugeSwapFix_v0.2.0.cs) ／ [SHA-256一覧](../../SHA256SUMS.txt)

## 修正する不具合と効果

| 発生していた不具合 | 対処内容 | 改善効果 |
| --- | --- | --- |
| 女の子をMainへ入れ替えた後、Hカテゴリやアニメーションを変更するとFace/Gauge表示が入れ替え前へ戻る | 同じMainが続いている場合だけ、変更前のFace/Gauge表示を復元する | 相手を入れ替えた後も正しいFace/Gauge表示を維持できる |

## インストール・解除

1. Koikatuを終了して既存の`KPlugGaugeSwapFix.dll`をバックアップします。
2. 導入ZIPをゲーム本体フォルダーへ展開します。配置先は`BepInEx/plugins/KPlugFixes/KPlugGaugeSwapFix.dll`です。
3. 別の`BepInEx/plugins/`フォルダーに同名DLLが残っていないことを確認し、BepInExログでロードを確認します。

解除するときはゲームを終了し、このDLLだけを外して必要に応じてバックアップを復元してください。

## 修正内容

対象は`HSceneProc.ChangeAnimator`です。Mainが入れ替わっている場合だけ、変更前のFace/Gaugeを保存し、ゲーム本来のアニメーション切替を実行した後、**同じMain状態が続いている場合に限って**表示を復元します。kPlugやKoikatu本体のDLLを直接書き換えるものではありません。通常のHや、Mainを入れ替えていない場合には介入しません。

## 動作確認

Koikatu＋kPlug 3.6.0の環境で、Iキーで女の子を呼びMainへ入れ替えた後、寝→椅子などのカテゴリ変更時にFace/Gaugeが維持されることを確認しています。新しいZIPへの再梱包はDLLの変更を伴いませんが、ZIPの展開操作そのものは実機未検証です。

起動ログの例：

```text
[GaugeSwapFix] v0.2.0 active. Target=HSceneProc::ChangeAnimator. Main-girl swap only.
```

[以前のDLL単体配布ZIP](../../release/windows/GaugeSwapFix/KPlugGaugeSwapFix_v0.2.0.zip)は手動配置形式です。新規導入には上記のゲームルート導入ZIPを使用してください。
