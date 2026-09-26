# KPlugNullGuardsFix v0.2.0

Status: integration test BLOCKED.  
v0.1.0 core guards are runtime-tested and remain the stable version. The AtHome destroy guard was also runtime-tested as the standalone v0.1.1 plugin. During the first combined v0.2.0 runtime test, Koikatu later crashed with a native Access Violation in the HSceneProc.ChangeAnimator / Animator runtime-controller path. The crash was not in AtHomeCtrl.OnDestroy, so the integrated guard is not yet proven to be causal. Roll back and reproduce before any further change.

## 目的

kPlug 3.6.0で確認されているnull / destroyed object由来の限定的な例外を、kPlug.dll本体を直接改変せずBepInEx / Harmonyで防御する。

v0.2.0では、成功済みv0.1.0のNullGuardsに、旧 `KPlugAtHomeDestroyGuard v0.1.1` の処理を統合する。

## v0.1.0から継続する処理

- H処理系 Prefix guard: 9メソッド
- KokanBehavior Prefix guard: 2メソッド
- MenuCorner Prefix guard: 1メソッド
- Voice coroutine Transpiler: 1メソッド

v0.1.0実機確認済みDLL SHA-256:

`e1e585f3961126adda211d140535ab7590ed2d47c6f0ce97bbd2f146565a1c1e`

## v0.2.0追加: AtHome OnDestroy guard

対象:

`kPlug.CmpBase.AtHomeCtrl.OnDestroy()`

### 元の症状

ゲーム終了等の破棄処理時、`girlRoots` 内にUnity上ですでにDestroy済みのGameObjectが残っていると、
元の `OnDestroy()` がその要素に対して `GetComponentInChildren<ChaControl>()` を呼び、NullReferenceExceptionになる場合があった。

### 対応

Harmony Prefixで `AtHomeCtrl.OnDestroy()` の直前に `girlRoots` を確認する。

以下だけをリストから除去する。

- CLR上のnull要素
- UnityEngine.ObjectとしてDestroy済みのGameObject

その後、元の `AtHomeCtrl.OnDestroy()` は通常どおり最後まで実行する。

カメラ復帰、ChaControl処理、Destroy処理など元のOnDestroyロジックは置き換えない。

### standalone版で確認済み

旧 `KPlugAtHomeDestroyGuard v0.1.1` は、

```text
[AtHomeDestroyGuard] v0.1.1 active.
Destroyed girlRoots are removed before AtHomeCtrl.OnDestroy.
```

としてロードされ、そのテストログでは従来の `AtHomeCtrl.OnDestroy` NREは再発しなかった。

## v0.2.0構成

- Prefix guards: 13
- Voice Transpiler: 1

期待起動ログ:

```text
[NullGuardsFix] Voice guard injected at AudioSource.clip -> AudioClip.length path.
[NullGuardsFix] v0.2.0 active. 13 Prefix guards + Voice Transpiler. AtHome destroy guard included.
```

破棄対象が実際に見つかった場合:

```text
[NullGuardsFix] AtHome OnDestroy: removed N null/destroyed girlRoots entries.
```

## ビルド・導入

`scripts/NullGuardsFix/Build-KPlugNullGuardsFix_v0.2.0.bat`

を実行する。

処理:

1. TEMPへコンパイル
2. コンパイル成功後のみゲーム側を変更
3. 旧 `KPlugNullGuardsFix.dll` と standalone `KPlugAtHomeDestroyGuard.dll` を `.integrated_off` へ退避
4. `BepInEx/plugins/KPlugFixes/KPlugNullGuardsFix.dll` に統合版を配置
5. 配置失敗時は旧DLLをロールバック

旧DLLは削除しない。

## 最終実機確認

1. Koikatu起動
2. v0.2.0起動ログ確認
3. MyRoomへ1回入退室
4. 通常Hへ入り、アニメーション変更等を軽く確認
5. H終了
6. ゲームを通常終了
7. ログに `kPlug.CmpBase.AtHomeCtrl.OnDestroy` のNullReferenceExceptionがないことを確認

初回統合テストではH中の ChangeAnimator / Animator runtime-controller 経路でネイティブクラッシュが発生したため、v0.2.0は正式化しない。まずv0.1.0 + standalone AtHomeDestroyGuardへロールバックし、同じH操作で再現するか確認する。再現しなければv0.2.0統合が強い疑い、再現すればChangeAnimator/アニメーション経路を別件として調査する。
