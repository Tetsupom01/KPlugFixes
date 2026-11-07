# HPointBenchRouteFix v1.0.0（Koikatu本体側のHPoint切替補正）

[English](README_en.md) ｜ [導入ZIP](../../release/windows/HPointBenchRouteFix/HPointBenchRouteFix_v1.0.0_Install.zip) ｜ [完全C#ソース](../../src/HPointBenchRouteFix/v1.0.0/HPointBenchRouteFix_v1.0.0.cs) ｜ [リリースノート](RELEASE_NOTES_v1.0.0.md)

## 対象と修正

KoikatuのHPoint移動メニューで「ベンチフェラ」を選んでも、以前の椅子・ソファの挿入モーションを維持してしまうケースを修正します。ゲーム本体の`HSceneProc.ChangeCategory()`が同じカテゴリ集合`[2,12]`の移動をモーション維持扱いにすることをログで確認しました。限定条件に一致した場合だけ、**元のゲームのモーション再選択経路**へ進ませます。独自のAnimator操作、IK再設定、Reset停止は行いません。

本パッチは**kPlug固有の問題を直すものではなく、kPlug・CrossFaderを起動上の必須依存にしない独立したBepInEx/Harmonyプラグイン**です。ただし、kPlugを外した環境での実機確認は未実施です。

## 導入

1. ゲームを完全に終了し、同名DLLの配置場所を確認してバックアップします。
2. [v1.0.0導入ZIP](../../release/windows/HPointBenchRouteFix/HPointBenchRouteFix_v1.0.0_Install.zip)をゲームルートへ展開すると、`BepInEx/plugins/HPointBenchRouteFix.dll`に配置されます。
3. `BepInEx/plugins/KPlugFixes/HPointBenchRouteFix.dll`など別の場所の旧DLL、および`HPointBenchRouteFixTrial.dll`と同時にロードしないでください。
4. 起動後、`BepInEx/HPointBenchRouteFix.log`の`READY`を確認します。`PATCH_NOT_APPLIED`の場合は対応環境・DLL整合を再確認してください。

解除はゲーム終了後、導入した`BepInEx/plugins/HPointBenchRouteFix.dll`を取り除きます。ゲーム本体DLLは上書きしません。

## 互換性・検証状態

- 対象：Koikatu 5.1／対象`Assembly-CSharp.dll`のSHA-256が`0038281CAF8DF48A7903C55DC389642EEEB3F2A9114BD9D68AC11C8AC0396BC5`と一致する構成。
- 依存：BepInEx、Harmony。ビルド参照はBepInEx 5.4.23.5、0Harmony 2.9.0.0。
- v0.1.0試験版：対象切替補正4回をログで確認し、ユーザーが問題なしと評価。
- v1.0.0 Independent RC1：ユーザーが実機へ導入し「問題なさそう」と評価。**公開DLLはそのRC1とバイト単位で一致**。README・MANIFESTの公開状態のみ更新。
- **未記録：2回目起動後の動作、最終実機ログ照合、kPlugなし環境での動作。** すべてのHPoint体位での網羅的な回帰確認は行っていません。
- ほかのHPoint移動時の姿勢崩れ、IK、Enter無反応などは対象外。

## ファイルの照合

- DLL SHA-256：`170757DA0A4880CF2F8CF23C7E206C70BC6F9487F0051AAB470353D57A3408BD`
- 導入ZIP SHA-256：`DB4EF44288838D754EDC64278240B4A1E03883B2D41B4ADEFF849CED7A79204F`
- ソース SHA-256：`1BAF13FCE2CD85114A8A23187E2B74ACB8436AB6603D045C5E08808B193C3F90`

[ビルド用PowerShell](../../scripts/HPointBenchRouteFix/v1.0.0/Build-HPointBenchRouteFix.ps1) ／ [起動BAT](../../scripts/HPointBenchRouteFix/v1.0.0/01_BuildRelease.bat) ／ [リポジトリ全体のSHA一覧](../../SHA256SUMS.txt)

ビルドスクリプトには当初の実機テストで使用したゲームルートの既定値が設定されています。他の環境では`-GameRoot`でご自身のKoikatuフォルダーを指定してください。元のゲームおよび依存DLLは同梱していません。
