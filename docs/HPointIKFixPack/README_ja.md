# HPoint IK FixPack v1.0.0｜常用成功版（統合管理・2 DLL／管理Hotfix v1.0.1）

2026-10-17。**1つのパッケージで2つの成功バイナリを管理します。DLLの物理的な結合・再コンパイルは行いません。** 本版は管理スクリプトの回収・アンインストール共通証拠採取をHotfix v1.0.1へ修正した保全用フルパッケージです。実機で元のFixPack v1.0.0の導入を確認し、Hotfix v1.0.1で回収成功。旧診断DLL除去後の通常構成で再発なしを実機確認済みです。

## 収録する実機成功DLL

| 機能 | DLL | SHA-256 | 元Runtime表示 |
|---|---|---|---|
| 問題A／Hポイント移動後の姿勢崩れ | `HPointIKStaleExtraIKGuard.dll` | `F167EF2459C695B4A7B1782141E9640E1C013D00B833D1E48D5B0C54FD4ECC54` | v0.2.0 EXP RC1 |
| 問題B／机バック愛撫→テンキーEnter | `ProblemBDeskEnterTransientResetGuard.dll` | `AC4DCD9D7A809C5A88EE2E9CC3DE60FC5AB4DFCBCA2C5B46B478DC69100F45A6` | v0.1.0 RC0 |

表示名に EXP/RC0 が残るのは、成功済みDLLを1バイトも変えないためです。**FixPackの配布Versionは1.0.0**です。ソース・旧単独パッケージ・代表結果は `src/`、`provenance/` に保存しています。

## 通常導入（ゲームを完全終了）

1. ZIPを**専用のフォルダーへ展開し、その場所に保存し続ける**。既存RC1パッケージは削除せず保存する。
2. `01_Install.bat`：kPlug 3.6.0とAssembly-CSharpのSHA、Harmony版、内蔵DLLとソースのSHAを照合。
3. 既にRC1が所定場所にあれば**SHAが完全一致するときのみそのまま再利用**し、問題Bだけ新規導入。RC1が無ければ両DLLを配置。問題Bが既に存在する場合は、そのSHAが一致していても勝手に上書き／所有権移管しないため安全停止。以前の問題B単独Releaseを利用中なら、その**元の** `04_Uninstall.bat` で撤去してから本FixPackを導入。
4. `[INSTALLED]` と `[SUCCESS]` を確認。`02_Status.bat` にて両DLLのSHAと、**当該ゲーム起動時刻の**各READYログを確認する。
5. 机バック愛撫初期表示→テンキーEnter→挿入遷移、机／壁／寝を含むHポイント移動、Return等の通常操作を確認。壁・寝の一瞬の復元表示は今回の修正範囲外。
6. ゲームを終了し `03_Collect.bat`。BATと同じフォルダーの `FixPackCollect_日時.zip` を送付。回収時にDLLを削除しません。

配置先は2つとも `F:\illusion\Koikatu\BepInEx\plugins\KPlugFixes\`。kPlug.dll、Assembly-CSharp.dll、RC1バイナリを再ビルド・直接修正しません。

**旧診断DLL** `HPointEnterFlowDiag.dll`／`ProblemBEnterIKWriterTrace.dll` が存在する場合、Statusに `NORMAL_MODE_PENDING` と表示します。FixPackは自動削除しません。各診断の**元パッケージが持つ回収・撤去BAT**でのみ除去し、通常構成の確認を行います。古いログのREADYを今回起動の成功と誤認しないでください。

## 管理コマンド

- `01_Install.bat`：SHA照合と一括導入（既存RC1は検証のみ）。成功DLLを再ビルドしない。
- `02_Status.bat`：双方の設置・SHA／専用ログ／診断DLLを確認。
- `03_Collect.bat`：両DLL、状態JSON、専用ログと出力ログを同梱したZIP。管理Hotfix v1.0.1適用済み。撤去なし。各実行ログは `Results/モード_日時/Operation.log`。
- `04_Uninstall.bat`：最初に証拠ZIPを生成・読取検査し、FixPackが**今回配置した**DLLだけを除去。従来RC1が先に存在した場合はRC1を必ず保護。

全ての操作でゲームの完全終了を必要とするのはInstall／Collect／Uninstallです。Statusは起動中も可能。バイナリ所有情報 `FixPack_InstallState.json` は**削除・移動しないでください**。アンインストール先はRC1の従来マニフェストを検索して勝手に書き換えません。

## 既知の範囲と将来方針

RC1の問題AとRC0の問題Bを個別確認済み。FixPack一括管理インストーラと、回収Hotfix v1.0.1を実機成功確認。旧診断DLLなしで2026-10-17 13:02〜13:09に両パッチ動作、ユーザーが問題A/Bの再発なしと後続操作正常を確認したため、**常用成功版として確定**。回収Hotfixを反映したこの再梱包版の新規導入は独立には未試験（修正差分は管理スクリプト2箇所のみ）。壁・寝で一瞬見える姿勢復元は実害がなく対象外。CrossFader由来かは未確定。

将来1 DLLへ物理統合する際は、Harmony IDとHook順序、各成功機能、基準SHA、rollbackを明示した**新規試験版**で検証します。今回の凍結版は統合後も戻せるよう保存します。
