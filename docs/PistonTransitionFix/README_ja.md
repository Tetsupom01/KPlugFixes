# kPlug Piston Transition Fix｜日本語ガイド

[English README](README.md)

Koikatu＋kPlug 3.6.0向けの2つのBepInEx/Harmonyプラグインです。挿入モーションの切替に関係する別々の問題を扱います。

| プラグイン | 用途 | 導入ZIP |
| --- | --- | --- |
| **KPlugPistonWaitFix v1.0.0** | テンキー7／Backspaceの切替後、次の操作が受け付けられなくなる待機状態を防止 | [ダウンロード](../../release/windows/PistonTransitionFix/KPlugPistonWaitFix_v1.0.0_KPlugFixes_Install.zip) |
| **KPlugPistonAutoResume v0.2.0** | 切替後、元のGo処理でピストン動作を自動再開 | [ダウンロード](../../release/windows/PistonTransitionFix/KPlugPistonAutoResume_v0.2.0_KPlugFixes_Install.zip) |

**AutoResumeにはWaitFix v1.0.0が必要です。** AutoResumeはWaitFixの代わりにはなりません。

## 修正する不具合と効果

| プラグイン | 発生していた不具合・不便 | 対処内容 | 改善効果 |
| --- | --- | --- | --- |
| **PistonWaitFix** | テンキー7／Backspaceで挿入モーションを切り替えた後、待機状態から抜けず次の操作が効かなくなる | 元の`IE_AskPistonChange`の待機判定だけを補正する | 操作不能を回避し、次のH操作を受け付けられるようにする |
| **PistonAutoResume** | 挿入モーション切替後にピストン動作が停止したままになり、再開に操作が必要になる | `InsertIdle`／`A_InsertIdle`到達後、ゲーム本来のGo処理で再開する | 体位変更後のピストン動作を自動再開する。別のH操作があれば再開予約を取り消す |

## 導入・解除

1. Koikatuを完全に終了し、同名DLLがあればバックアップします。
2. 使用するプラグインのZIPをゲーム本体フォルダーへ展開します。DLLの配置先は`BepInEx/plugins/KPlugFixes/`です。
3. `BepInEx/plugins/`配下の別フォルダーに同名DLLを残して二重ロードしないよう確認します。
4. 起動後、BepInExログで下記を確認してください（それぞれ導入した場合）。

```text
[PistonWaitFix] v1.0.0 active.
[PistonAutoResume] v0.2.0 active.
```

解除するにはゲームを終了し、該当DLLを外して必要に応じてバックアップから復元します。ほかのプラグインは削除しないでください。

## AutoResume設定

`BepInEx/config/local.kplug.pistonautoresume.cfg`（初回起動後に生成）

```ini
[Piston Auto Resume]
Enabled = true
ResumeDelaySeconds = 0.8
```

待機時間は0.0〜3.0秒、既定値は0.8秒です。自動再開を止める場合は`Enabled = false`にします。女性Animatorが`InsertIdle`または`A_InsertIdle`に到達してから待機し、ほかのH操作があれば再開予約を取り消します。

## 対応環境・制限

- 確認環境：Koikatu 5.1、kPlug 3.6.0、BepInEx 5.4.23.5、Unity 5.6.2f1。
- WaitFixは元の`IE_AskPistonChange`の待機判定だけを補正し、元処理の`ForceCoupleIdle(true)`は変更しません。
- AutoResumeは元のGo処理を利用します。両プラグインともkPlug.dllやゲーム本体DLLは改変しません。
- 現行導入ZIP内のDLLは成功済み版と同一ですが、新しいZIPの展開操作自体は実機未検証です。

## ソース・詳細

- [WaitFixソース](../../src/PistonTransitionFix/WaitFix/KPlugPistonWaitFix_v1.0.0.cs) ／ [個別技術説明](KPlugPistonWaitFix_v1.0.0_README_ja.txt)
- [AutoResumeソース](../../src/PistonTransitionFix/AutoResume/KPlugPistonAutoResume_v0.2.0.cs) ／ [個別技術説明](KPlugPistonAutoResume_v0.2.0_README_ja.txt)
- [公開SHA一覧](../../SHA256SUMS.txt)
