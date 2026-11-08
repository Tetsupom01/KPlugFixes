# KPlugNullGuardsFix v0.2.0｜日本語ガイド

kPlug 3.6.0のH処理、音声、MyRoom終了時などに発生する特定のnull参照・Destroy済みオブジェクト由来の例外を軽減するBepInEx/Harmonyプラグインです。`kPlug.dll`本体は変更しません。

[導入ZIP](../../release/windows/NullGuardsFix/KPlugNullGuardsFix_v0.2.0_KPlugFixes_Install.zip) ／ [完全C#ソース](../../src/NullGuardsFix/KPlugNullGuardsFix_v0.2.0.cs) ／ [SHA一覧](../../SHA256SUMS.txt)

## 修正する不具合と効果

kPlug 3.6.0で発生していた、ゲームの動作を不安定にする処理エラーを修正します。

| 発生していた不具合 | 対処内容 | 改善効果 |
|---|---|---|
| Hシーンの終了・状態切替後に、H関連処理が不適切に実行される | Hシーンの状態を確認し、不要な処理を停止 | H操作に関連する例外を抑制 |
| 音声再生時、存在しないAudioSourceや音声クリップを参照してエラーが発生する | 音声データの有効性を確認してから参照 | 音声処理のNullReferenceExceptionを防止 |
| MyRoom終了時、破棄済みのキャラクターオブジェクトを参照してエラーが発生する | 無効なオブジェクトを除外してから終了処理を実行 | MyRoom退出・ゲーム終了時の例外を抑制 |

**これらの修正により、Hシーン・音声再生・MyRoom終了時の動作を安定させ、エラーによるゲームの異常動作やクラッシュのリスクを低減します。**

kPlug.dll本体を変更せず、BepInEx/Harmonyプラグインとして対処します。

## インストール・解除

1. Koikatuを終了し、既存の`KPlugNullGuardsFix.dll`をバックアップします。
2. 旧`KPlugAtHomeDestroyGuard.dll`が有効なら退避します。v0.2.0に同じ対象の保護処理を統合しているため、同時に有効化しないでください。
3. 導入ZIPをゲーム本体フォルダーへ展開します。配置先は`BepInEx/plugins/KPlugFixes/KPlugNullGuardsFix.dll`です。
4. 他のフォルダーにある同名DLLを残さず、起動ログでロードを確認します。

解除するときはゲームを終了し、このDLLを外します。以前の版があればバックアップから復元してください。kPlug本体やほかのプラグインは削除しないでください。

## 対象処理

- H処理のnull guard：9メソッド
- `KokanBehavior`：2メソッド
- `MenuCorner`：1メソッド
- `AtHomeCtrl.OnDestroy()`：1メソッド（Destroy済み／nullの`girlRoots`だけを除外）
- Voice coroutine：1メソッドのTranspiler

`AtHomeCtrl.OnDestroy()`の元処理は通常どおり実行します。ガードに該当しない正常な処理を置き換えるものではありません。

## 動作確認・制限

Koikatu＋kPlug 3.6.0環境で、プラグイン起動、MyRoom入退室、ゲーム通常終了を確認しています。確認した実行では`AtHomeCtrl.OnDestroy`のNullReferenceExceptionおよびプラグイン内部のパッチ適用エラーは記録されませんでした。null要素を実際に取り除く場面の網羅的な再現は行っていません。

起動ログの例：

```text
[NullGuardsFix] v0.2.0 active. 13 Prefix guards + Voice Transpiler. AtHome destroy guard included.
```

現行のゲームルート導入ZIPは成功済みDLLを再梱包したもので、ZIP展開操作の実機テストは未実施です。

[旧DLL単体配布ZIP](../../release/windows/NullGuardsFix/KPlugNullGuardsFix_v0.2.0.zip)は手動配置用です。
