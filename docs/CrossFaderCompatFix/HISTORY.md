# CrossFaderCompatFix 調査・履歴

## 症状

CrossFader 0.11 有効時、H中にテンキーReturnをタイミング良く素早く複数回入力すると、native crashが発生することがあった。

native stackでは主に:

`Animator.set_runtimeAnimatorController -> HSceneProc.ChangeAnimator -> HSceneProc.Update`

へ到達していた。

## 確定した事実

### 静的解析

- `HSceneProc.Update` は `selectAnimationListInfo != null` かつ `voiceWait == false` の場合に `ChangeAnimator(selectAnimationListInfo, false)` を同期実行し、戻った直後に `selectAnimationListInfo = null` とする。
- `HFlag.selectAnimationListInfo` setter本体は単純なfield代入。
- CrossFaderの `SetPlayHook` は有効時に `Animator.CrossFadeInFixedTime` を利用する。
- CrossFaderの `InTransition()` は主にCrossFaderが保持する女性Animator layer 0の現在stateと `flags.nowAnimStateName` を基準にするため、Hに関係する全Animatorの安全完了判定ではない。

### 実機・ログ

- CrossFader OFFでは同系統クラッシュが再現しにくく、CrossFader ON時に問題が集中。
- v0.6.0で要求を保留し、`CrossFader.InTransition == false` 後に実行すると、保留要求の `ChangeAnimator` 直後に同じnative crashが再発した。
- したがってCrossFaderのfalse判定だけを安全条件にするのは不足。
- v0.7.0はクラッシュを止めたが、busyでない正常要求まで `Existing transition` を理由にDROPし、正常操作を壊した。
- v0.8.0では複数回の変更が安全待機 -> ChangeAnimator -> gate解除まで完走し、native crashなしでH終了・ゲーム終了できた。

## 主な試行

### v0.1 / v0.2

`HSceneProc.Update` 全体を遷移中に止める方式。

問題:
正常なH更新処理まで止めるため、設計境界が広すぎた。安全条件を増やしても解決しなかった。

### v0.4 / v0.5

kPlug側の `AskPistonChange` や `Keys.NextPose/PrevPose` をsingle-flight境界として試した。

結果:
実際のクラッシュ再現操作でその経路が発火せず、入力メソッドの選定が誤りだった。入力キー単位の追跡を打ち切った。

### v0.6

`HSceneProc.Update` の共通pending要求を、CrossFader遷移中だけ保留。

結果:
1回目は正常完走したが、2回目の保留要求をCrossFader false直後に実行するとnative crash。保留して後から実行する方式は不採用。

### v0.7

transition中のpending要求をDROPする方式。

結果:
native crashはしなかったが、正常入力までDROPし、次動作へ移行しないことがあった。採用不可。

### v0.8

first request wins。

- 最初の非nullsetter書き込みを受理
- gate中の別要求だけ拒否
- nullクリアは常時許可
- 最初の要求はCrossFader + 全H Animatorが2フレーム連続安定するまで保持
- 元のChangeAnimatorをそのまま実行
- 実行後も2フレーム連続安定までgate維持

結果:
複数回の遷移が正常完走し、旧クラッシュ操作でもnative crashなし。H/ゲーム終了も正常。

## 正式化

v0.8.0の機能ロジックを変更せず、版番号のみv1.0.0へ正式化。その後、v1.0.0 DLLを実機へ配置して再確認した。

【実機・ログ確認済み】

- `kPlug CrossFader Compat Fix 1.0.0` ロード成功
- `HSceneProc.Update` Patch適用成功
- 5回の `GATE START -> ALLOW CHANGE -> GATE RELEASE` が完走
- #2〜#5で `CrossFader=True` を検出し、待機後に変更を許可
- native crash / Access Violationなし
- ゲームは通常終了まで到達

これにより、現在の正式成功版を **KPlugCrossFaderCompatFix v1.0.0** とする。

正式版DLL SHA-256:

`1bd4cbcd316b4ec4132f453c732ddcbc95d12911349099b3fe786dbed9036cc7`

Windows配布ZIP SHA-256:

`50ce10e547e64f7fa564b8263d2271418e9853fb3b74b38cd1e8add8abd9a0ec`

正式版ソースパッケージSHA-256:

`3077ac3b8ea547c6c8e7a0cf96e5e11ab3aa1495e506ca7ac998f5cd5470f5e4`

## 未確定事項

v1.0.0最終成功ログでも `BLOCK WRITE` が0回だったため、後続setter書き込みを実際に拒否したケース自体は未観測。

また、CrossFader本体・`SetPlayHook`・`CrossFadeInFixedTime` は変更しておらず、ログ上CrossFaderの遷移状態も動作している。ただし最終テストは連打を含んでいたため、視覚的なクロスフェード品質だけを独立評価したテストは行っていない。
