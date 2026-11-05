# KPlugFixes（日本語ガイド）

[English README](README_en.md)

**Koikatu＋kPlug 3.6.0環境で検証した、kPlug関連の不具合修正6種類と、独立したHリスト最適化1種類**を公開しています。必要なものだけ選べるよう、機能ごとにDLLを分けています。HListNameCacheはkPlug.dll本体を修正するパッチではありません。ゲーム本体、kPlug本体、CrossFader本体を配布・改造するものではありません。

## まずどれを入れる？

### kPlug関連の不具合修正（6種類）

| プラグイン | 何を改善する？ | 現行版・入手先 |
| --- | --- | --- |
| **CrossFaderCompatFix** | Hポイント移動後に姿勢が崩れたままになる問題を低減。CrossFaderとのアニメーション切替競合にも対応 | [v1.2.0 日本語README同梱・KPlugFixes配置版](release/windows/CrossFaderCompatFix/KPlugCrossFaderCompatFix_v1.2.0_KPlugFixes_Install.zip) ／ [日本語README](docs/CrossFaderCompatFix/README_ja.md) |
| **PoseBridge** | kPlugの体位選択にある挿入体位を、本体のH画面の体位一覧にも反映 | [v0.2.0.1 日本語README同梱・KPlugFixes配置版](release/windows/PoseBridge/KPlugPoseBridge_v0.2.0.1_KPlugFixes_Install.zip) ／ [日本語README](docs/PoseBridge/README_ja.md) |
| **GaugeSwapFix** | H中に相手を入れ替えた際の顔・ゲージ表示の巻き戻りを抑制 | [v0.2.0・KPlugFixes配置版](release/windows/GaugeSwapFix/KPlugGaugeSwapFix_v0.2.0_KPlugFixes_Install.zip) ／ [日本語README](docs/GaugeSwapFix/README_ja.md) |
| **NullGuardsFix** | kPlugの特定のNullReference例外を、kPlug.dllを直接変更せず保護 | [v0.2.0・KPlugFixes配置版](release/windows/NullGuardsFix/KPlugNullGuardsFix_v0.2.0_KPlugFixes_Install.zip) ／ [日本語README](docs/NullGuardsFix/README_ja.md) |
| **PistonWaitFix** | 特定の挿入モーション切替後に操作できなくなる状態への対策 | [v1.0.0・KPlugFixes配置版](release/windows/PistonTransitionFix/KPlugPistonWaitFix_v1.0.0_KPlugFixes_Install.zip) ／ [日本語README](docs/PistonTransitionFix/README_ja.md) |
| **PistonAutoResume** | アニメーション切替後、条件が整ったら元のゲームのGo経路でピストン動作を再開 | [v0.2.0・KPlugFixes配置版](release/windows/PistonTransitionFix/KPlugPistonAutoResume_v0.2.0_KPlugFixes_Install.zip) ／ [日本語README](docs/PistonTransitionFix/README_ja.md) |
### Hリスト取得の最適化（1種類・別カテゴリ）

kPlugの不具合修正ではなく、Koikatuの`AssetBundleCheck.GetAllAssetName()`の対象を限定した最適化です。**kPlug 3.6.0が入った環境で実機検証済み**ですが、kPlugなしの環境での動作は未検証です。

| プラグイン | 何を改善する？ | 現行版・入手先 |
| --- | --- | --- |
| **SideloaderHListNameCache** | H体位一覧の特定6アセットについて、名前一覧の反復取得をキャッシュ（kPlug本体の修正ではない） | [v0.1.0・KPlugFixes配置版](release/windows/HListNameCache/Sideloader_HListNameCache_v0.1.0_KPlugFixes_Install.zip) ／ [日本語README](docs/HListNameCache/README_ja.md) |

旧凍結保全パッケージ（ソース・専用スクリプトを収録）：[Sideloader HListNameCache v0.1.0](release/windows/HListNameCache/Sideloader_HListNameCache_v0.1.0_Stable_Frozen.zip)。現行のゲームルート展開用ZIPとは別の形式です。

**現行推奨ZIPについて：** 上記7種類はすべて、ゲーム本体へ展開すると `BepInEx/plugins/KPlugFixes/` に各DLLが配置される形式です。各ZIPは日本語／英語READMEとSHA一覧を同梱しています。旧DLL単体ZIPやHListNameCache凍結保全ZIPは履歴として残し、成功済みDLL本体を変更していません。**新規に作成した再梱包ZIPは、実機での展開・起動を別途確認していません。**

**注意：** これらを全部入れる必要はありません。適用したい症状に対応するDLLだけを選んでください。各ZIPの導入手順・検証状況を確認してください。

## KPlugFixes配置版ZIPのインストール方法

現在の7種類の推奨ZIPは、各DLLを次の共通フォルダへ配置します。**必要なプラグインだけを選んでください。** 下図は配置先をまとめて示したもので、全7種類の同時導入を求めるものではありません。

```text
Koikatu（ゲーム本体フォルダ）
└─ BepInEx
   └─ plugins
      └─ KPlugFixes
         ├─ KPlugCrossFaderCompatFix.dll
         ├─ KPlugPoseBridge.dll
         ├─ KPlugGaugeSwapFix.dll
         ├─ KPlugNullGuardsFix.dll
         ├─ KPlugPistonWaitFix.dll
         ├─ KPlugPistonAutoResume.dll
         └─ SideloaderHListNameCache.dll
```

1. **Koikatuを完全に終了**してください。
2. 既に使用中のDLLがあればバックアップしてください。`BepInEx/plugins/`直下や他のサブフォルダに同名DLLがある場合、先に配置を確認してください。
3. 上表から必要なZIPを保存し、**ZIPの中にある `BepInEx` フォルダごと、ゲーム本体フォルダへ展開**してください。
4. 旧配置先の同名DLLを放置しないでください。**同じプラグインを2か所に置くと、重複ロードや競合の原因となります。** 不明なファイルは削除せずバックアップ・確認してください。
5. ゲームを再起動して `BepInEx/LogOutput.log` を確認してください。各プラグインの`Loading`が**1回だけ**記録されていれば、起動時ロードの確認になります。

既に旧配置で使用している方は、新版ZIPを重ねて入れるだけでは重複する可能性があります。旧DLLの所在を確認し、安全に移行してください。**今回のフォルダ整理は、DLLの中身や動作の変更ではありません。**

## 確認済みの範囲・残っている問題

- **CrossFaderCompatFix v1.2.0：** 統合ExtraIK抑制を実機ログで確認済み。診断ログON/OFFと、移行後の起動・初期化も確認しています。姿勢崩れの改善を確認しましたが、**絶頂後の移動に続くEnter時の一瞬の位置ズレ**と、**低頻度のEnter無反応**は未解決です。
- **PoseBridge v0.2.0.1：** 移行後に起動・パッチ有効化を確認済み。体位一覧の同期は一部カテゴリで確認していますが、すべての体位の網羅的な動作確認はしていません。
- **SideloaderHListNameCache v0.1.0：** 凍結版に収録したDLL本体は実機成功確認済みです。旧凍結版の専用Windowsインストールスクリプト、および今回の新しいゲームルート展開用ZIPは**それぞれ実機導入未検証**です。kPlugなしでの動作も未検証です。

### CrossFaderの診断ログ

通常は記録をOFFにします。詳細調査が必要なときだけ、`BepInEx/config/tetsupom.kplug.crossfader.compatfix.cfg` の設定を切り替えます。

```ini
[Logging]
EnableDiagnosticLogs = true
```

ゲームを再起動すると、CrossFaderとExtraIKの詳細が`BepInEx/KPlugCrossFaderCompatFix.log`に出力されます。確認後は`false`へ戻してください。

## 元へ戻すには

Koikatuを終了し、今回導入した同名DLLを外してください。導入前に使っていた版がある場合は**保全したバックアップから復元**します。他のプラグインやゲーム本体のファイルは削除しないでください。

## ソース・詳細・更新履歴

- [CrossFader 詳細（日本語）](docs/CrossFaderCompatFix/README_ja.md)／[ソース](src/CrossFaderCompatFix/v1.2.0/KPlugCrossFaderCompatFix_v1.2.0.cs)／[リリースノート](docs/CrossFaderCompatFix/RELEASE_NOTES_v1.2.0.md)
- [PoseBridge 詳細（日本語）](docs/PoseBridge/README_ja.md)／[ソース](src/PoseBridge/KPlugPoseBridge_v0.2.0.1.cs)／[リリースノート](docs/PoseBridge/RELEASE_NOTES_v0.2.0.1.md)
- [GaugeSwapFix 日本語README](docs/GaugeSwapFix/README_ja.md)／[ソース](src/GaugeSwapFix/KPlugGaugeSwapFix_v0.2.0.cs)
- [NullGuardsFix 日本語README](docs/NullGuardsFix/README_ja.md)／[ソース](src/NullGuardsFix/KPlugNullGuardsFix_v0.2.0.cs)
- [Piston系 日本語README](docs/PistonTransitionFix/README_ja.md)／[WaitFixソース](src/PistonTransitionFix/WaitFix/KPlugPistonWaitFix_v1.0.0.cs)／[AutoResumeソース](src/PistonTransitionFix/AutoResume/KPlugPistonAutoResume_v0.2.0.cs)
- [HListNameCache（Hリスト最適化）日本語README](docs/HListNameCache/README_ja.md)／[旧凍結保全パッケージ（ソース同梱）](release/windows/HListNameCache/Sideloader_HListNameCache_v0.1.0_Stable_Frozen.zip)
- [SHA-256一覧](SHA256SUMS.txt)：配布物の同一性確認に使用します
- [英語の技術説明](README_en.md)

過去のZIPは再現・比較のため残しています。**新規導入時は上表のリンクを優先**し、旧配置版を重複して入れないでください。