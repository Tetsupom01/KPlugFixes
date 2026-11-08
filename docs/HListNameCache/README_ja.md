# Sideloader HList Name Cache v0.1.0

KoikatuのH体位リストで、アセット名一覧の重複取得を減らすBepInEx/Harmonyプラグインです。Koikatu本体の`AssetBundleCheck.GetAllAssetName()`を対象とし、**kPlugのAPIを直接使用しません**。

[導入ZIP](../../release/windows/HListNameCache/Sideloader_HListNameCache_v0.1.0_Standalone_Install.zip) ／ [SHA-256一覧](../../SHA256SUMS.txt)

## 最適化する処理と効果

| 重複していた処理 | 対処内容 | 改善効果 |
| --- | --- | --- |
| H体位一覧の処理で、同じアセット名一覧を繰り返し取得する | 対象6アセットの該当する`AssetBundleCheck.GetAllAssetName()`呼び出し結果をキャッシュし、配列を複製して返す | 重複したアセット名取得処理を削減し、一覧処理の無駄を減らす |

## 機能

対象は`abdata/h/list/`配下の6つのBundle（`hs2suite.unity3d`、`mmbath.unity3d`、`mmhouse01.unity3d`、`poolmap.unity3d`、`purpleroom.unity3d`、`sakuraroom.unity3d`）です。

`AssetBundleCheck.GetAllAssetName(string,bool,string,bool)`のうち、`withExtension=false`、manifestが空、`isAllCheck=false`の呼び出しだけをキャッシュします。取得結果の配列は複製して返し、対象外の呼び出しやアセットの読み込み・解放処理は変更しません。

## 導入・解除

1. Koikatuを終了し、既存の`SideloaderHListNameCache.dll`がある場合はバックアップします。
2. [導入ZIP](../../release/windows/HListNameCache/Sideloader_HListNameCache_v0.1.0_Standalone_Install.zip)をゲーム本体フォルダーへ展開します。DLLの配置先は`BepInEx/plugins/SideloaderHListNameCache.dll`です。
3. `BepInEx/plugins/`配下の別の場所に同名DLLを残さず、起動ログの`Loading [Sideloader H List Name Cache 0.1.0]`と`[HListNameCache] PATCH_OK`を確認します。
4. H体位一覧の表示・選択、Hポイント移動が正常か確認します。

解除するときはゲームを終了し、このDLLを取り外します。以前の版を使用していた場合はバックアップから戻してください。他のプラグインは削除しないでください。

## 対応環境・検証範囲

- 実機確認環境：Koikatu 5.1、kPlug 3.6.0、Sideloader、BepInEx/Harmony。
- **kPlugへの直接的なコード依存はありませんが、kPlugなしでの動作は未検証です。**
- 同一DLLの実機テスト2回で、対象Bundleごとにキャッシュヒット852回・480回を記録。H操作・体位一覧・Hポイント移動も確認しました。
- DLL本体を変更せず`BepInEx/plugins/`直下へ移動した後の正常動作は利用者確認済みです。ただし、本配布ZIPを展開する操作そのものと起動ログ原本の独立照合は未実施です。既存のHポイント移動後の姿勢崩れを修正するプラグインではありません。

DLL SHA-256：`17F571ACB7901776F1069C1B218E85EDB67918C50A1CE844837C7DA3616429C4`  
導入ZIP SHA-256：`80A95B2F86BA5BC4B0B2EE5904EF807AF2F70DF68F310C2516C9AEC953839FBF`

## ソース・以前の配布物

旧配置の[従来の配布ZIP](../../release/windows/HListNameCache/Sideloader_HListNameCache_v0.1.0_KPlugFixes_Install.zip)は再導入用ではなく過去版の参照・復元用です。

[ソース・ビルドスクリプト同梱の凍結パッケージ](../../release/windows/HListNameCache/Sideloader_HListNameCache_v0.1.0_Stable_Frozen.zip)は、導入ZIPとは構成が異なります。ゲームルートへ直接展開する目的には使用しないでください。両方のZIPからDLLを重複導入しないでください。
