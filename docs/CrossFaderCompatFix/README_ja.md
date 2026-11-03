# CrossFaderCompatFix｜日本語ガイド

## 現行版：v1.2.0（2026-11-03）

Koikatu＋kPlug 3.6.0＋CrossFader 0.11向けです。Hポイント移動後に旧ExtraIKが残って姿勢が崩れる現象を抑えるため、従来のCrossFader待機処理・MoveBoundary処理に、限定的なExtraIK抑制を追加しています。

- **推奨導入ZIP（日本語README付き）：** [v1.2.0／KPlugFixesフォルダ配置](../../release/windows/CrossFaderCompatFix/KPlugCrossFaderCompatFix_v1.2.0_KPlugFixes_Install.zip)
- [C#ソース](../../src/CrossFaderCompatFix/v1.2.0/KPlugCrossFaderCompatFix_v1.2.0.cs)
- [技術リリースノート](RELEASE_NOTES_v1.2.0.md)
- **配置先：** `BepInEx/plugins/KPlugFixes/KPlugCrossFaderCompatFix.dll`
- **DLL SHA-256：** `2392CCAFE5663EFC4FD80F8F72E9741796DE1DDD85D92F9A0E2C0F6C3E45ACFC`
- **新ZIP SHA-256：** `f769ee644700fc52c8d59a14f5f80bd9103df2be9543947b6201506ddde499d1`

**インストール：** ゲーム終了→旧DLLをバックアップ→ZIPをゲーム本体フォルダへ展開。既存の`BepInEx/plugins/`直下などに同名DLLがある場合、重複してロードされないよう確認してください。復元時はゲームを終了してから旧DLLを戻します。

**実機確認：** v1.2.0の統合ExtraIK対策は実機で発火を確認し、ログON/OFFを検証済みです。表記調整後の正式DLLは、フォルダ移行後にBepInExで1回ロードされ、初期化READYを確認しています。最終DLLでのすべてのHポイント操作の網羅的な再試験は未実施です。

**残課題：** 絶頂後のHポイント移動の直後にEnterを押すと一瞬座標がずれることがあり、Enter無反応も低頻度で残っています。

**診断ログ：** `BepInEx/config/tetsupom.kplug.crossfader.compatfix.cfg` の `[Logging] EnableDiagnosticLogs = true` で有効化し、ゲーム再起動。CrossFaderとExtraIKは `BepInEx/KPlugCrossFaderCompatFix.log` に記録されます。通常運用では `false` に戻してください。

旧v1.2.0の`KPlugCrossFaderCompatFix_v1.2.0_Install.zip`は、`plugins`直下への配置だった歴史的ファイルです。新規導入では上記の新ZIPを使用してください。

---

## 過去の正式版：v1.1.0（2026-10-31）

既存のfirst request winsとAnimator遷移時の安全待機を維持したまま、Hポイント移動境界を越えて残る旧pending要求だけを条件付きで失効させる修正を追加した。

- 現行ソース： [KPlugCrossFaderCompatFix_v1.1.0.cs](../../src/CrossFaderCompatFix/v1.1.0/KPlugCrossFaderCompatFix_v1.1.0.cs)
- 現行導入ZIP： [KPlugCrossFaderCompatFix_v1.1.0_Install.zip](../../release/windows/CrossFaderCompatFix/KPlugCrossFaderCompatFix_v1.1.0_Install.zip)
- 導入先： `BepInEx/plugins/KPlugCrossFaderCompatFix.dll`
- DLL SHA-256： `1F6F359255D3501545F1B97E25DC059436FA24C3EEB704EC9DB93E80256B4154`
- 導入ZIP SHA-256： `D826BFC59E8994FEFBB862F135AD77FAB8354C0E6D003F9E79D230BD02E581A1`
- 元のCrossFader 0.11、kPlug 3.6.0、元ChangeAnimator、入力処理は変更しない。

**確認範囲：** 旧試験版v1.1.0で移動元の旧要求を3件失効し、移動先要求の受理とゲート解除を確認した。上記の`trial`表記除去版も2026-10-31にロード・Patch適用・`[MOVE_BOUNDARY READY] version=1.1.0`を確認。ただし、ラベル除去後のDLLによるHポイント移動再テストは、別途未確認である。試験版と表記除去版の機能変更はない。

詳細な試行ログ・内部検証記録は非公開Libraryで管理し、GitHubには完全ソースと配布物・利用者向けの説明を置く。

---

## 正式版（歴史的なv1.0.0の記録）

**KPlugCrossFaderCompatFix v1.0.0**

Koikatu + kPlug 3.6.0 + CrossFader 0.11 環境で、H中にアニメーション変更要求が短時間に重なった際、`HSceneProc.ChangeAnimator` 内の `Animator.runtimeAnimatorController` 更新で native Access Violation が発生する問題を抑止する互換パッチ。

## 採用した設計

基本方針は **first request wins**。

- `HFlag.selectAnimationListInfo` の最初の非null要求を受理してgate開始
- gate中の別の非null要求はsetter Prefixで拒否
- vanillaのnullクリアは必ず許可
- 最初の要求は `HSceneProc.Update` 内で安全状態になるまで保持
- 安全判定は、CrossFaderの `InTransition() == false` かつ、Hの女性・男性Animator全layerで `IsInTransition == false`
- 上記を2フレーム連続確認後、元の `ChangeAnimator` をそのまま実行
- 実行後も同じ安全条件を2フレーム連続確認するまでgateを保持

変更しないもの:

- `voiceWait`
- `click`
- `HSceneProc.ChangeAnimator` 本体
- `Animator.runtimeAnimatorController` setter
- CrossFader本体
- CrossFaderの `SetPlayHook`
- `Animator.CrossFadeInFixedTime`
- kPlugの入力処理

## 実機確認

**v1.0.0 DLLは当時の実機確認済み成功版（現在は歴史的比較基準）。** v0.8.0で確立した機能ロジックを変更せず正式化した版を、そのまま実機へ入れて確認した。

v1.0.0最終確認では:

- BepInExが `kPlug CrossFader Compat Fix 1.0.0` をロード
- `HSceneProc.Update` Patch適用成功、`v1.0.0 active` を確認
- 5回のHアニメーション変更が `GATE START -> ALLOW CHANGE -> GATE RELEASE` まで完走
- #2〜#5では `CrossFader=True` を検出し、安全状態まで待機してから変更を許可
- native crash / Access Violation なし
- Hシーン終了後、ゲームの通常終了まで到達
- 最終確認ログの `BLOCK WRITE` は0回

最後の点から、後続setter書き込みの実ブロック発生そのものは最終確認ログでは未観測。したがって「ブロックが発生したからクラッシュを防げた」とまでは断定しない。確認できた事実は、要求を安全状態まで保持する経路で連続遷移が正常完走したこと。

## 対象環境

- Koikatu 5.1
- kPlug 3.6.0
- BepInEx 5.4.23.5
- CrossFader 0.11
- Unity 5.6.2f1
- CLR 2.0.50727.1433
- HF Patch v4.1
- Windows 11

## ソースパッケージ

`release/source/CrossFaderCompatFix/KPlugCrossFaderCompatFix_v1.0.0-source.zip`

SHA-256:

`3077ac3b8ea547c6c8e7a0cf96e5e11ab3aa1495e506ca7ac998f5cd5470f5e4`

パッケージ内容:

- `KPlugCrossFaderCompatFix_v1.0.0.cs`
- `Build-Deploy-KPlugCrossFaderCompatFix_v1.0.0.ps1`
- `Run-KPlugCrossFaderCompatFix_v1.0.0.bat`
- `README.txt`
- `SHA256SUMS.txt`

## ビルド

BATを実行するとPowerShell経由でFramework v4 `csc.exe`を使用してビルドし、既存DLLをSHA確認付きでバックアップしてからテスト用フォルダへ配置する。

既定配置先:

`F:\illusion\Koikatu\BepInEx\plugins\test\KPlugCrossFaderCompatFix.dll`

ビルド時に生成されるDLLのSHA-256はMANIFESTへ記録する。

## 正本上の注意

v1.0.0は正式版ソースかつ実機確認済みDLL。Windows配布DLL SHA-256は `1bd4cbcd316b4ec4132f453c732ddcbc95d12911349099b3fe786dbed9036cc7`、配布ZIP SHA-256は `50ce10e547e64f7fa564b8263d2271418e9853fb3b74b38cd1e8add8abd9a0ec`。
