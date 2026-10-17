# HPoint IK FixPack｜常用成功版の実機確定（2026-10-17）

- 配布版: FixPack v1.0.0（管理スクリプトHotfix v1.0.1適用）。2 DLL構成、DLLの再ビルドなし。
- 現行ゲーム: F:\illusion\Koikatu。kPlug SHA `34C13976108DB0A18A7AD6B7CFDA5517B4A9A83D85C9A909820144264C743855`。Assembly-CSharp SHA `0038281CAF8DF48A7903C55DC389642EEEB3F2A9114BD9D68AC11C8AC0396BC5`。
- RC1: `HPointIKStaleExtraIKGuard.dll` SHA `F167EF2459C695B4A7B1782141E9640E1C013D00B833D1E48D5B0C54FD4ECC54`。最終run 13:02〜13:09、対象判定4、suppressed 4、errors 0。
- 問題B: `ProblemBDeskEnterTransientResetGuard.dll` SHA `AC4DCD9D7A809C5A88EE2E9CC3DE60FC5AB4DFCBCA2C5B46B478DC69100F45A6`。同run skips 7、関連する安全側通過1、errors 0。
- CrossFader: 最終回収ログでゲート14件が正常解放。原因と断定しない別件の一瞬の姿勢復元が壁・寝に見えるが、ユーザーが実害なしと確認。
- ユーザー実機: 机バック愛撫→Enterの手のめり込み、およびHポイント移動時の大きな姿勢崩れ、ともに再発なしと回答。2026-10-17の最終判定で常用成功版として固定。
- 旧診断DLL `HPointEnterFlowDiag.dll` は隔離済み、FixPack Statusでリスト対象の診断・試験用DLLなし。
- インストール所有記録: 既存RC1は `preexisting_verified`、問題Bは `fixpack_installed`。FixPackアンインストール時は既存RC1を削除しない。
- 回収スクリプトv1.0.0にPowerShell変数 `$Dir` / `$dir` 衝突があった。Hotfix v1.0.1は `SaveEvidence()` で2箇所を `$script:Dir` に修正。ゲーム側DLL・ソースに差分なし。元のHotfix版で回収成功。
- 最終回収ZIP: `FixPackCollect_20261017_131457_547.zip` SHA `249FFD7E3C8221BB2DF8576AD0CF5D4E5661762B80C7F52F7F52FD779EF1AC69`。失敗回 `FixPackCollect_20261017_130952_943.zip` SHA `A7A47EF3A321A8A5E1A5BD91C4C523135D70A40A92C41AD223BED58D168309D7` は回収バグの比較資料。
- 継続課題: 将来1 DLLへ物理統合する場合は新しい試験版を作成し、現行2 DLL版を凍結して比較・復元可能にする。今回は統合しない。
- このフルパッケージは実機成功時のDLL、原本ソース、Hotfix済み管理スクリプトおよび最終証拠ログを収録。Hotfixスクリプト以外のバイナリは変更しない。ZIP全体は再梱包のため元配布物とはハッシュが異なる。
