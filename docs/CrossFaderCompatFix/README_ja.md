# CrossFaderCompatFix

## 正式版

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

実機成功版は v0.8.0。v1.0.0はそのソースと機能ロジック同一で、プラグイン版番号のみ正式化している。

最終確認では:

- 複数回のHアニメーション変更が `GATE START -> ALLOW CHANGE -> GATE RELEASE` まで完走
- 旧クラッシュ再現操作でも native crash / Access Violation なし
- Hシーン終了およびゲーム終了まで正常
- CrossFader 0.11 はロード済み
- 実動作中に `CrossFader=True` / Animator transition を検出して待機
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

`release/CrossFaderCompatFix/KPlugCrossFaderCompatFix_v1.0.0-source.zip`

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

v1.0.0は正式版ソース。実機検証済みロジックはv0.8.0と同一だが、版番号変更後のv1.0.0 DLLそのものの実機SHA-256はまだ記録されていない。未取得値を推測で補完しない。
