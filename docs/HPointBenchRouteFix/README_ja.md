# HPointBenchRouteFix v1.0.0｜Koikatu Hポイント切替補正

[English](README_en.md) ／ [導入ZIP](../../release/windows/HPointBenchRouteFix/HPointBenchRouteFix_v1.0.0_Install.zip) ／ [完全C#ソース](../../src/HPointBenchRouteFix/v1.0.0/HPointBenchRouteFix_v1.0.0.cs) ／ [リリースノート](RELEASE_NOTES_v1.0.0.md)

## 修正する不具合と効果

| 発生していた不具合 | 対処内容 | 改善効果 |
| --- | --- | --- |
| 椅子・ソファで挿入モーション中に「ベンチフェラ」を選んでも、直前の挿入モーションが残る | カテゴリ集合が一致する場合のモーション維持判定を限定的に調整し、本来のモーション再選択処理を利用する | 「ベンチフェラ」のモーションへ正しく切り替わる |

## 修正の仕組み

`HSceneProc.ChangeCategory()`でカテゴリ集合`[2,12]`が一致する場合のモーション維持判定を限定的に調整し、ゲーム本来のモーション再選択処理を利用します。ほかのAnimator・IK・Reset処理は変更しません。

**BepInEx・Harmonyが必要です。** kPlug・CrossFaderは起動時の必須依存ではありません。ただし、kPlugなしでの動作は実機未確認です。

## インストール・解除

1. Koikatuを終了し、同名DLLがある場合はバックアップします。
2. [導入ZIP](../../release/windows/HPointBenchRouteFix/HPointBenchRouteFix_v1.0.0_Install.zip)をゲーム本体フォルダーへ展開します。配置先は`BepInEx/plugins/HPointBenchRouteFix.dll`です。
3. 旧配置フォルダーに同名DLLや`HPointBenchRouteFixTrial.dll`を残して二重ロードしないよう確認します。
4. 起動後、`BepInEx/HPointBenchRouteFix.log`の`READY`を確認してください。`PATCH_NOT_APPLIED`が出る場合は対応環境を確認します。

解除するときはゲームを終了し、このDLLを外します。ゲーム本体DLLは変更しません。

## 対応環境・検証範囲

- 対象：Koikatu 5.1、`Assembly-CSharp.dll` SHA-256 `0038281CAF8DF48A7903C55DC389642EEEB3F2A9114BD9D68AC11C8AC0396BC5`
- 参照：BepInEx 5.4.23.5、Harmony（ビルド参照：0Harmony 2.9.0.0）
- 対象の切替処理を試験版ログで4回確認。公開DLLと同一のv1.0.0 DLLを実機へ導入した初回確認でも、問題は報告されていません。
- **未確認：** 再起動後の動作、最終ログ、kPlugなしの実機動作、全Hポイント経路の網羅的な回帰確認。
- 姿勢崩れ・IK・Enter無反応など、別のHポイント不具合は修正対象外です。

DLL SHA-256：`170757DA0A4880CF2F8CF23C7E206C70BC6F9487F0051AAB470353D57A3408BD`  
導入ZIP SHA-256：`DB4EF44288838D754EDC64278240B4A1E03883B2D41B4ADEFF849CED7A79204F`  
ソース SHA-256：`1BAF13FCE2CD85114A8A23187E2B74ACB8436AB6603D045C5E08808B193C3F90`

[ビルドPowerShell](../../scripts/HPointBenchRouteFix/v1.0.0/Build-HPointBenchRouteFix.ps1) ／ [ビルドBAT](../../scripts/HPointBenchRouteFix/v1.0.0/01_BuildRelease.bat) ／ [SHA一覧](../../SHA256SUMS.txt)

ビルドする場合は、PowerShellの`-GameRoot`でKoikatuのインストール先を指定してください。ゲーム本体・第三者DLLは配布していません。
