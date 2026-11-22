# KPlugBenchEnterFix v1.0.0

[English](README_en.md) | [配布ZIP](../../release/windows/KPlugBenchEnterFix/KPlugBenchEnterFix_v1.0.0_Install.zip) | [ソース](../../src/KPlugBenchEnterFix/v1.0.0/KPlugBenchEnterFix_v1.0.0.cs)

**用途：** コイカツ（Koikatu）+ kPlug 3.6.0 のベンチフェラで、テンキーEnterの操作を補正する BepInEx / Harmony プラグインです。ゲーム本体・kPlug本体を改変しません。

- 待機中のEnterで元のフェラ開始処理を利用します。音声再生中に開始できない場合は要求を一度保留し、開始可能時に元処理へ渡します。
- 奉仕動作中のゲージ70％以下の無効Enterと、射精中の追加Enterを遮断します。
- ゲージ70％超のEnterによる射精要求と、口内選択後のEnterによる挿入移行を保持します。
- 服装やアニメーションを直接強制変更しません。

## 必要環境

実機確認：Koikatu 5.1 / kPlug 3.6.0 / BepInEx 5.4.23.5 / Unity 5.6.2f1 / Windows 11。**他のkPlug版での互換性は確認していません。** 依存ファイルの実機SHAは配布ZIP内`MANIFEST.json`を参照してください。Harmonyの参照・プラグイン競合には注意してください。

## 導入

1. **ゲームを終了**し、既存の同名DLLがあればバイト単位でバックアップします。
2. `HPointBenchEnterPistonGateTrial.dll`など旧試験DLLが `BepInEx/plugins/` 以下に残っていないことを確認します（**二重ロード禁止**）。
3. [v1.0.0 導入ZIP](../../release/windows/KPlugBenchEnterFix/KPlugBenchEnterFix_v1.0.0_Install.zip)をゲームルートに展開します。
4. 配置先は `BepInEx/plugins/KPlugFixes/KPlugBenchEnterFix.dll` です。`BepInEx/KPlugBenchEnterFix.log`の`READY hooks=...`で初期化・必須フックの適用を確認します。ロード表示のみでは合格としません。

**解除：** ゲームを終了して、上記の`KPlugBenchEnterFix.dll`だけを除去してください。他のKPlugFixes DLLや設定は変更しないでください。

## 実機検証の範囲

2026-11-22のWindows実機試験で、初回・再移動後の`Idle → WLoop`、70％超の射精要求、70％以下および射精中の追加Enter遮断、`Oral_Idle`到達とEnter受付、5つのHarmonyフック`READY`を記録で確認。音・画面については使用者が「問題なさそう」と評価。**効果音の厳密な回数計測や全モーション網羅は未実施**です。

## 既知の未解決事項

後段の口モーションからベンチフェラへ移動すると、移動先モーションが反映されないことがあります。旧Enter試験DLL撤去状態でも再現しました。**原因は未確定**であり、本プラグインはHポイントカテゴリ移動処理を修正しません。別の`HPointBenchRouteFix`の既存補正範囲を含む経路調査が必要です。

## 同一性と再現資料

- Runtime Version / Plugin ID: `1.0.0` / `tetsupom.kplug.benchenterfix`
- 実機確認済みDLL SHA-256: `DF1E541CA58B07D2CCCE1A87925BCAB4651E1C2C7DF24A45DB2CFBF29CE0C4CC`
- 実機確認済みソース SHA-256: `3C95474583C45FEBD672985EC620D5E5E4CFD41D26A5DBB1E740DCAC952A4DB8`
- 導入ZIP SHA-256: `8509D14394F3F1A9C138880176F64847FBC9AAD7B9CE3D2E4DEBD39CE422A412`
- [再現用スクリプト](../../scripts/KPlugBenchEnterFix/v1.0.0/) は当時の実機試験を再現するための参考資産です。**正式版の導入には使用しません**。元スクリプトのテスト用名称は履歴であり、本番導入物ではありません。
