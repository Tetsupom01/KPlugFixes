# Sideloader HList Name Cache v0.1.0｜実機成功版（固定）

## 目的と修正範囲
Koikatu + kPlug 3.6.0向け、PoseBridgeとは独立したBepInEx/Harmonyプラグイン。Hリストのアセット名一覧の不要な反復取得を抑える。

対象は `abdata/h/list/` 配下の次の6 Bundleのみ：
`hs2suite.unity3d`、`mmbath.unity3d`、`mmhouse01.unity3d`、`poolmap.unity3d`、`purpleroom.unity3d`、`sakuraroom.unity3d`。

`AssetBundleCheck.GetAllAssetName(string,bool,string,bool)` に対し、`withExtension=false`、manifestがnull/空、`isAllCheck=false`の場合だけキャッシュする。初回の正常かつ非空の文字列配列を複製して保持し、以後も複製して返す。Bundle本体・TextAssetの読み込み／解放、他のパス・引数、Sideloader.dll・Assembly-CSharp.dllには変更を加えない。

## 2026-10-12 実機確認
2回の実機検証でキャッシュ効果を確認。プラグイン由来のエラーは確認されず、2回目はユーザーがH操作・体位リストの表示と選択・Hポイント移動に問題なしと確認した。

| 計測 | 各Bundle初回取得 | 各Bundleのキャッシュヒット | 各BundleのStreaming |
|---|---:|---:|---:|
| 1回目 | 1 | 852 | 56 |
| 2回目 | 1 | 480 | 86 |

残存Streamingを省略してよいとは判定していない。HPointMove直後に姿勢が崩れる低頻度不具合は別件として未解決。壁ポイントとの関連は仮説段階で、次のモーションに入ると復旧する。キャッシュDLLの原因・修正効果とは断定しない。

## 固定成果物
`release/windows/Sideloader_HListNameCache_v0.1.0_Stable_Frozen.zip`

- ZIP SHA-256：`2CF2BA5AF6602E49EE8BDC8DFFE8197C0E3E500C31911CE2B95B98864B8195BC`
- 同梱の実機確認済み `SideloaderHListNameCache.dll` SHA-256：`17F571ACB7901776F1069C1B218E85EDB67918C50A1CE844837C7DA3616429C4`
- 同梱ソース `SideloaderHListNameCache.cs` SHA-256：`D873CC477AF79AD90D69E976095F25E2E1B16F129906EC24D3C0CCA98C93F76D`

ZIPには**2回目の実機成功時と同一のDLL**（再ビルド品ではない）、ソース、BAT＋PowerShellによる一括導入・回収／撤去、README、SHA256一覧、成功条件を収録。ソースとスクリプトはZIP内に原本のまま保全する。

**固定版導入スクリプト自体のWindows実機動作は未検証。** 設計上は基準SHA照合と既存対象ファイルの上書き拒否を行う。実験版DLLおよびポインタは、2回目の回収スクリプト上、撤去成功と記録された。

要求する既存基準SHA-256：
- `Sideloader.dll`：`A72E28585FF0BC62E13E077BF2E4077BCA5A9D7CA2C41AD43DC8B48E667BBE76`
- `Assembly-CSharp.dll`：`0038281CAF8DF48A7903C55DC389642EEEB3F2A9114BD9D68AC11C8AC0396BC5`

ゲーム本体、kPlug、Sideloader、BepInEx等の第三者DLLは再配布しない。成功版を再ビルド品で上書きする場合は、別版として改めて実機検証する。
