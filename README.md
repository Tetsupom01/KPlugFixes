# KPlugFixes｜Koikatu用BepInExプラグイン

[English README](README_en.md)

KoikatuのHシーンなどで確認した不具合の修正と、一部処理の最適化を配布しています。必要な機能だけ選んで導入できます。**ゲーム本体、kPlug、CrossFader、Sideloaderなど第三者のDLLは配布していません。**

## プラグイン一覧

### kPlug関連の修正（6種類）

| プラグイン | 主な機能 | 現行版・ダウンロード |
| --- | --- | --- |
| **CrossFaderCompatFix** | アニメーション切替競合とHポイント移動後の旧ExtraIK残留を抑制 | [v1.2.0 ZIP](release/windows/CrossFaderCompatFix/KPlugCrossFaderCompatFix_v1.2.0_KPlugFixes_Install.zip) ／ [説明](docs/CrossFaderCompatFix/README_ja.md) |
| **PoseBridge** | kPlug Pose Selectorの挿入体位を本体H画面の一覧にも表示 | [v0.2.0.1 ZIP](release/windows/PoseBridge/KPlugPoseBridge_v0.2.0.1_KPlugFixes_Install.zip) ／ [説明](docs/PoseBridge/README_ja.md) |
| **GaugeSwapFix** | Mainの女の子を入れ替えた後のFace/Gauge表示を維持 | [v0.2.0 ZIP](release/windows/GaugeSwapFix/KPlugGaugeSwapFix_v0.2.0_KPlugFixes_Install.zip) ／ [説明](docs/GaugeSwapFix/README_ja.md) |
| **NullGuardsFix** | 特定のnull参照・破棄済みオブジェクト例外を軽減 | [v0.2.0 ZIP](release/windows/NullGuardsFix/KPlugNullGuardsFix_v0.2.0_KPlugFixes_Install.zip) ／ [説明](docs/NullGuardsFix/README_ja.md) |
| **PistonWaitFix** | 挿入モーション切替後の待機状態による操作不能を防止 | [v1.0.0 ZIP](release/windows/PistonTransitionFix/KPlugPistonWaitFix_v1.0.0_KPlugFixes_Install.zip) ／ [説明](docs/PistonTransitionFix/README_ja.md) |
| **PistonAutoResume** | 切替後に元のGo処理でピストン動作を自動再開 | [v0.2.0 ZIP](release/windows/PistonTransitionFix/KPlugPistonAutoResume_v0.2.0_KPlugFixes_Install.zip) ／ [説明](docs/PistonTransitionFix/README_ja.md) |

**PistonAutoResumeにはPistonWaitFix v1.0.0が必要です。**

### Koikatu本体側の独立パッチ（2種類）

| プラグイン | 主な機能 | 現行版・ダウンロード |
| --- | --- | --- |
| **SideloaderHListNameCache** | H体位リストのアセット名一覧の反復取得をキャッシュ | [v0.1.0 ZIP](release/windows/HListNameCache/Sideloader_HListNameCache_v0.1.0_KPlugFixes_Install.zip) ／ [説明](docs/HListNameCache/README_ja.md) |
| **HPointBenchRouteFix** | 椅子・ソファ挿入から「ベンチフェラ」へ切り替わらない問題を補正 | [v1.0.0 ZIP](release/windows/HPointBenchRouteFix/HPointBenchRouteFix_v1.0.0_Install.zip) ／ [説明](docs/HPointBenchRouteFix/README_ja.md) |

これら2種類はkPlugのAPIへの直接依存を持ちません。ただし、**kPlugを使用しない環境での実機動作は未確認**です。

## インストール

1. **Koikatuを終了**し、既存の同名DLLがあればバックアップします。
2. 必要なプラグインの**導入ZIPをゲーム本体フォルダーへ展開**します。
3. `BepInEx/plugins/`以下で同名DLLや旧試験版の重複ロードがないことを確認します。
4. ゲーム起動後、`BepInEx/LogOutput.log`などのログでロードを確認します。個別の確認方法は各説明書を参照してください。

配布ZIPの配置先は次のとおりです。

```text
Koikatu/
└─ BepInEx/
   └─ plugins/
      ├─ KPlugFixes/
      │  ├─ KPlugCrossFaderCompatFix.dll
      │  ├─ KPlugPoseBridge.dll
      │  ├─ KPlugGaugeSwapFix.dll
      │  ├─ KPlugNullGuardsFix.dll
      │  ├─ KPlugPistonWaitFix.dll
      │  ├─ KPlugPistonAutoResume.dll
      │  └─ SideloaderHListNameCache.dll
      └─ HPointBenchRouteFix.dll
```

**すべてを導入する必要はありません。** それぞれのZIPには該当プラグインのDLLが入っています。終了後の解除は対象DLLだけを外し、以前の版があればバックアップから復元してください。

## 環境と既知の制限

主な検証環境：Koikatu 5.1、kPlug 3.6.0、BepInEx 5.4.23.5、Unity 5.6.2f1、Windows 11。プラグインによって追加条件があります。

- **CrossFaderCompatFix：** CrossFader 0.11向け。絶頂後の移動に続くEnter時の一瞬の位置ズレ、低頻度のEnter無反応は未解決です。
- **PoseBridge：** 挿入体位（mode 2）だけが対象で、全体位の網羅的確認は未実施です。
- **HListNameCache：** 特定6アセットが対象です。kPlugなしの動作は未検証です。
- **HPointBenchRouteFix：** 特定のベンチフェラ切替だけが対象です。再起動後の動作・最終ログ・kPlugなしの動作は未確認です。
- 一部の再梱包ZIPは、同一DLLの実機検証とは別に**ZIP展開手順自体の実機検証が未完了**です。詳細は個別READMEで確認してください。

## ソース・詳細情報

[公開ソース](src/) ／ [ビルドスクリプト](scripts/) ／ [機能別ドキュメント](docs/) ／ [配布ZIP一覧](release/windows/) ／ [SHA-256一覧](SHA256SUMS.txt)

旧版のZIPやソースは比較・復元用として保管しています。**新しく導入する場合は上表の現行版リンクを使用してください。**
