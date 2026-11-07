# KPlugNullGuardsFix v0.2.0｜日本語ガイド

kPlug 3.6.0のH処理、音声、MyRoom終了時などに発生する特定のnull参照・Destroy済みオブジェクト由来の例外を軽減するBepInEx/Harmonyプラグインです。`kPlug.dll`本体は変更しません。

[導入ZIP](../../release/windows/NullGuardsFix/KPlugNullGuardsFix_v0.2.0_KPlugFixes_Install.zip) ／ [完全C#ソース](../../src/NullGuardsFix/KPlugNullGuardsFix_v0.2.0.cs) ／ [SHA一覧](../../SHA256SUMS.txt)

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
