# KPlugNullGuardsFix v0.2.0

Status: 正式版・実機確認済み。  
v0.1.0のNullGuards本体と、standalone版KPlugAtHomeDestroyGuard v0.1.1の処理を統合したv0.2.0を、最終実機テスト通過後の現行Stableとする。

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

最終確認では以下を実施した。

1. Koikatu起動
2. v0.2.0起動ログ確認
3. MyRoomへ入退室
4. 通常終了
5. ログ確認

結果:

- `[NullGuardsFix] v0.2.0 active. 13 Prefix guards + Voice Transpiler. AtHome destroy guard included.` を確認
- MyRoom入退室正常
- ゲーム通常終了
- `kPlug.CmpBase.AtHomeCtrl.OnDestroy` のNullReferenceExceptionなし
- `NullGuardsFix patch failed` なし
- `InvalidProgramException` なし
- AccessViolation / Fatal / Crash なし

なお、今回の正常終了ログでは `AtHome OnDestroy: removed N ...` は出ていない。
これは今回の終了時には除去対象となるnull / Destroy済み `girlRoots` が存在しなかったことを意味する。

## 正式判定

**KPlugNullGuardsFix v0.2.0 を正式成功版・現行Stableとして固定する。**

v0.1.0は履歴・ロールバック用として保持する。

v0.1.0既知SHA-256:

`e1e585f3961126adda211d140535ab7590ed2d47c6f0ce97bbd2f146565a1c1e`

v0.2.0は実機成功確認済みだが、成功実機DLLのSHA-256は確認ログに記録されていないため、値を推定・再生成して記載しない。
