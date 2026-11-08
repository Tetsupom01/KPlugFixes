# KPlugFixes｜コイカツ（Koikatu）用BepInEx不具合修正パッチ

[English README](README_en.md)

コイカツ（Koikatu）＋kPlug 3.6.0向けの**不具合修正・安定化用BepInExパッチ集**です。Hシーンの操作不能やアニメーション切替の不具合、音声再生・MyRoom終了時の例外に対処し、クラッシュにつながり得るエラーのリスク低減とH体位リストの重複処理削減を図ります。必要なプラグインだけ選んで導入できます。**ゲーム本体、kPlug、CrossFader、Sideloaderなど第三者のDLLは配布していません。**

## プラグイン一覧

### kPlug関連の修正（6種類）

| プラグイン | 主な機能 | 現行版・ダウンロード |
| --- | --- | --- |
| **CrossFaderCompatFix** | Hポイント移動後の姿勢崩れやアニメーション切替要求の競合を抑制し、H中のモーション切替を安定させる | [v1.2.0 ZIP](release/windows/CrossFaderCompatFix/KPlugCrossFaderCompatFix_v1.2.0_KPlugFixes_Install.zip) ／ [説明](docs/CrossFaderCompatFix/README_ja.md) |
| **PoseBridge** | kPlugの挿入体位が本体H体位一覧に表示されない問題に対応し、本体画面から選べるようにする | [v0.2.0.1 ZIP](release/windows/PoseBridge/KPlugPoseBridge_v0.2.0.1_KPlugFixes_Install.zip) ／ [説明](docs/PoseBridge/README_ja.md) |
| **GaugeSwapFix** | 女の子をMainへ入れ替えた後の体位変更でFace/Gauge表示が元に戻る問題を修正し、表示を維持する | [v0.2.0 ZIP](release/windows/GaugeSwapFix/KPlugGaugeSwapFix_v0.2.0_KPlugFixes_Install.zip) ／ [説明](docs/GaugeSwapFix/README_ja.md) |
| **NullGuardsFix** | H処理・音声再生・MyRoom終了時に発生するNullReferenceExceptionなどを防止し、ゲームの異常動作やクラッシュのリスクを低減 | [v0.2.0 ZIP](release/windows/NullGuardsFix/KPlugNullGuardsFix_v0.2.0_KPlugFixes_Install.zip) ／ [説明](docs/NullGuardsFix/README_ja.md) |
| **PistonWaitFix** | テンキー7・Backspaceで挿入モーションを切り替えた後の操作不能を防止し、次のH操作を受け付けられるようにする | [v1.0.0 ZIP](release/windows/PistonTransitionFix/KPlugPistonWaitFix_v1.0.0_KPlugFixes_Install.zip) ／ [説明](docs/PistonTransitionFix/README_ja.md) |
| **PistonAutoResume** | 挿入モーション切替後に停止したピストン動作を、元のGo処理で自動再開する | [v0.2.0 ZIP](release/windows/PistonTransitionFix/KPlugPistonAutoResume_v0.2.0_KPlugFixes_Install.zip) ／ [説明](docs/PistonTransitionFix/README_ja.md) |

**PistonAutoResumeにはPistonWaitFix v1.0.0が必要です。**

### Koikatu本体側の独立パッチ（2種類）

| プラグイン | 主な機能 | 現行版・ダウンロード |
| --- | --- | --- |
| **SideloaderHListNameCache** | H体位リストでのアセット名一覧の繰り返し取得をキャッシュし、対象6アセットの重複処理を削減する | [v0.1.0 ZIP](release/windows/HListNameCache/Sideloader_HListNameCache_v0.1.0_Standalone_Install.zip) ／ [説明](docs/HListNameCache/README_ja.md) |
| **HPointBenchRouteFix** | 椅子・ソファからベンチフェラを選んでも前の挿入モーションが残る問題を修正し、正しいモーションへ切り替える | [v1.0.0 ZIP](release/windows/HPointBenchRouteFix/HPointBenchRouteFix_v1.0.0_Install.zip) ／ [説明](docs/HPointBenchRouteFix/README_ja.md) |

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
      │  └─ KPlugPistonAutoResume.dll
      ├─ HPointBenchRouteFix.dll
      └─ SideloaderHListNameCache.dll
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
