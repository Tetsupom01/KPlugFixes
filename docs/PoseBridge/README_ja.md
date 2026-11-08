# PoseBridge v0.2.0.1｜日本語ガイド

kPlugのPose SelectorとKoikatu本体H画面の挿入体位一覧を連携するBepInExプラグインです。

[導入ZIP](../../release/windows/PoseBridge/KPlugPoseBridge_v0.2.0.1_KPlugFixes_Install.zip) ／ [完全C#ソース](../../src/PoseBridge/KPlugPoseBridge_v0.2.0.1.cs) ／ [リリースノート](RELEASE_NOTES_v0.2.0.1.md)

## 修正する不具合と効果

| 発生していた不具合 | 対処内容 | 改善効果 |
| --- | --- | --- |
| kPlugのPose Selectorで選べる挿入体位（mode 2）が、Koikatu本体のH体位一覧に反映されない | kPlugの既存の体位解決処理を使って一覧を同期し、解決できない体位は元の一覧を維持する | 対象の挿入体位を本体H画面から選択できるようにする |

## インストール・解除

1. Koikatuを終了し、既存の`KPlugPoseBridge.dll`があればバックアップします。
2. 導入ZIPをゲーム本体フォルダーへ展開します。配置先は`BepInEx/plugins/KPlugFixes/KPlugPoseBridge.dll`です。
3. `BepInEx/plugins/`直下などにある同名DLLを残して二重ロードしないよう確認します。
4. 起動ログでプラグインのロードとパッチ初期化を確認し、H体位一覧を確認します。

解除はゲームを終了してこのDLLを外し、以前の版があればバックアップから復元します。

## 動作と対応範囲

- `ToolAnimSelector.GetAvailablePiston()`と`ParserAnim.GetHAnim()`を使って体位一覧を同期します。
- 初期化より前に呼び出された場合は同期を延期し、必要な体位を解決できない場合も元の一覧を保持します。
- **挿入体位（mode 2）のみ**が対象です。service、3P、lesbianは対象外です。
- Koikatu 5.1、kPlug 3.6.0、BepInEx 5.4.23.5を基準に検証しています。

## 実機検証・既知の制限

v0.2.0.1は実機でロードと初期化を確認しました。床・壁・机の一覧同期および選択した体位への遷移は検証していますが、**全体位の網羅的な動作確認は未実施**です。

DLL SHA-256：`F1A35FF5A1849001D2705DD3F4C368FCB9A309170B51F244AEB1FD99583C9142`  
導入ZIP SHA-256：`76A985805C3817B2CAF4F69E787FDCC93125780619A66CE69F3D2EF2D7039BA4`

[旧v0.2.0.0配布ZIP](../../release/windows/PoseBridge/KPlugPoseBridge_v0.2.0.0.zip)は比較・復元用です。新規導入には上記のv0.2.0.1を使用してください。
