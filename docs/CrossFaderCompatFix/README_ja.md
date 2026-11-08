# CrossFaderCompatFix v1.2.0｜日本語ガイド

Koikatu＋kPlug 3.6.0＋CrossFader 0.11向けのBepInEx/Harmony互換パッチです。

[導入ZIP](../../release/windows/CrossFaderCompatFix/KPlugCrossFaderCompatFix_v1.2.0_KPlugFixes_Install.zip) ／ [C#ソース](../../src/CrossFaderCompatFix/v1.2.0/KPlugCrossFaderCompatFix_v1.2.0.cs) ／ [リリースノート](RELEASE_NOTES_v1.2.0.md)

## 修正する不具合と効果

| 発生していた不具合 | 対処内容 | 改善効果 |
| --- | --- | --- |
| H中に複数のアニメーション切替要求が重なり、切替処理が競合する | 切替要求を調整し、安全な切替状態まで待機する | アニメーション切替時の競合や状態の不整合を抑制 |
| Hポイント移動後に移動前のExtraIKが残り、女性の姿勢が崩れる | 移動境界を検出し、対象状態で古いExtraIKの適用を限定的に抑制する | 移動後の姿勢崩れを軽減 |

## インストール・解除

1. Koikatuを終了し、既存の`KPlugCrossFaderCompatFix.dll`があればバックアップします。
2. 導入ZIPをゲーム本体フォルダーに展開します。配置先は`BepInEx/plugins/KPlugFixes/KPlugCrossFaderCompatFix.dll`です。
3. `BepInEx/plugins/`直下など別フォルダーに同名DLLを残して二重ロードしないよう確認します。
4. ゲーム起動後にBepInExログでv1.2.0のロードとパッチ初期化を確認します。

解除するときはゲームを終了し、該当DLLを取り外して必要に応じて旧版をバックアップから復元します。

## 診断ログ

通常は無効です。詳細を確認する場合は、`BepInEx/config/tetsupom.kplug.crossfader.compatfix.cfg`で設定してゲームを再起動してください。

```ini
[Logging]
EnableDiagnosticLogs = true
```

ログは`BepInEx/KPlugCrossFaderCompatFix.log`へ出力されます。確認後は`false`へ戻してください。

## 確認範囲と既知の制限

- ExtraIK抑制と診断ログのON/OFFを実機で確認。公開v1.2.0 DLLはフォルダー移行後の起動・初期化を確認しています。
- 全Hポイント移動経路について、公開DLLでの網羅的な回帰試験は未実施です。
- **絶頂後のHポイント移動に続くEnter操作での一瞬の位置ズレ**と、**低頻度のEnter無反応**は未解決です。
- kPlug・CrossFader本体やゲームDLLを上書きするものではありません。

DLL SHA-256：`2392CCAFE5663EFC4FD80F8F72E9741796DE1DDD85D92F9A0E2C0F6C3E45ACFC`  
導入ZIP SHA-256：`F769EE644700FC52C8D59A14F5F80BD9103DF2BE9543947B6201506DDDE499D1`

## 以前の配布版

[v1.1.0のソース](../../src/CrossFaderCompatFix/v1.1.0/KPlugCrossFaderCompatFix_v1.1.0.cs) ／ [v1.1.0導入ZIP](../../release/windows/CrossFaderCompatFix/KPlugCrossFaderCompatFix_v1.1.0_Install.zip) ／ [v1.0.0ソース・ビルド資産](../../release/source/CrossFaderCompatFix/KPlugCrossFaderCompatFix_v1.0.0-source.zip)

旧ZIPは現行版とは配置先が異なる場合があります。新規導入には上記v1.2.0の導入ZIPを使用してください。
