# Sideloader HList Name Cache v0.1.0

KoikatuのH体位リストで、アセット名一覧の重複取得を減らすBepInEx/Harmonyプラグインです。Koikatu本体の`AssetBundleCheck.GetAllAssetName()`を対象とし、**kPlugのAPIを直接使用しません**。

[導入ZIP](../../release/windows/HListNameCache/Sideloader_HListNameCache_v0.1.0_KPlugFixes_Install.zip) ／ [SHA-256一覧](../../SHA256SUMS.txt)

## 機能

対象は`abdata/h/list/`配下の6つのBundle（`hs2suite.unity3d`、`mmbath.unity3d`、`mmhouse01.unity3d`、`poolmap.unity3d`、`purpleroom.unity3d`、`sakuraroom.unity3d`）です。

`AssetBundleCheck.GetAllAssetName(string,bool,string,bool)`のうち、`withExtension=false`、manifestが空、`isAllCheck=false`の呼び出しだけをキャッシュします。取得結果の配列は複製して返し、対象外の呼び出しやアセットの読み込み・解放処理は変更しません。

## 導入・解除

1. Koikatuを終了し、既存の`SideloaderHListNameCache.dll`がある場合はバックアップします。
2. [導入ZIP](../../release/windows/HListNameCache/Sideloader_HListNameCache_v0.1.0_KPlugFixes_Install.zip)をゲーム本体フォルダーへ展開します。DLLの配置先は`BepInEx/plugins/KPlugFixes/SideloaderHListNameCache.dll`です。
3. `BepInEx/plugins/`配下の別の場所に同名DLLを残さず、起動ログの`Loading [Sideloader H List Name Cache 0.1.0]`と`[HListNameCache] PATCH_OK`を確認します。
4. H体位一覧の表示・選択、Hポイント移動が正常か確認します。

解除するときはゲームを終了し、このDLLを取り外します。以前の版を使用していた場合はバックアップから戻してください。他のプラグインは削除しないでください。

## 対応環境・検証範囲

- 実機確認環境：Koikatu 5.1、kPlug 3.6.0、Sideloader、BepInEx/Harmony。
- **kPlugへの直接的なコード依存はありませんが、kPlugなしでの動作は未検証です。**
- 同一DLLの実機テスト2回で、対象Bundleごとにキャッシュヒット852回・480回を記録。H操作・体位一覧・Hポイント移動も確認しました。
- この配布ZIPの展開操作そのものは実機未検証です。既存のHポイント移動後の姿勢崩れを修正するプラグインではありません。

DLL SHA-256：`17F571ACB7901776F1069C1B218E85EDB67918C50A1CE844837C7DA3616429C4`  
導入ZIP SHA-256：`1E05CDF4E64FEF04F8695155E3CF4276BB2DEFF529700D8165D999EEA3961202`

## ソース・以前の配布物

[ソース・ビルドスクリプト同梱の凍結パッケージ](../../release/windows/HListNameCache/Sideloader_HListNameCache_v0.1.0_Stable_Frozen.zip)は、導入ZIPとは構成が異なります。ゲームルートへ直接展開する目的には使用しないでください。両方のZIPからDLLを重複導入しないでください。
