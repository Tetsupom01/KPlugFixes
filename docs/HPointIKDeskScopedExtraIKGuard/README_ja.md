# HPointIK DeskScoped ExtraIK Guard v0.2.0 RC1｜成功版の固定記録

記録日：2026-10-17 JST

**状態：実機検証済みRC1。GitHubへの固定情報・検証記録の登録は完了。ただし、成功済みZIP/DLLの実体は未登録。** 今回の作業環境ではLibraryにある元ZIPのバイト列を取得できなかったため、同じ名前の代替物を生成・登録しない。

## 対象と変更範囲

問題A：机バック愛撫（`kha_f_05`）へ移動した後、腕・手の姿勢が崩れる症状。元挿入モーションの追加IK判定が移動中に残ると、標準机用IK `kha_f_05` 読込後、`HActionBase.SetPlay → kPlug.Core.HActionBase_PostSetPlay → ExtraAnimManager.CheckOnStateChange → SetIkTargets(false)` が発火し、汎用 `kha_f_00` を重ねて読込む経路が確認されている。

RC1は、`HSceneProc.ChangeAnimator` の**移動先が机バック愛撫**であり、旧挿入状態からの遷移・旧追加IKの判定条件がすべて一致する場合に限り、当該 `SetIkTargets(false)` **1回だけ**を抑止する。机以外、判定不一致、取得失敗時は元処理を通す（fail-open）。汎用IKを全体停止するパッチではない。

RC1は成功済みEXP v0.2.0 DLLと**バイト完全同一**。内部の `EXPERIMENT` / `EXP` 表示は同一性を維持するため残している。RC1の再コンパイル、適用条件変更、FlowDiagとの同梱は行わない。

## 成功版の識別情報

| 項目 | ファイル・SHA-256 |
|---|---|
| 正規のOneStep ZIP | `HPointIK_DeskScopedExtraIKGuard_v0.2.0_RC1_Frozen_OneStep.zip` |
| ZIP SHA-256 | `DDAAF270E707FD9B8F7074FB4641565511E2E2D5065E13C2B5566477CFB4F951` |
| 実機成功DLL | `HPointIKStaleExtraIKGuard.dll` |
| DLL SHA-256 | `F167EF2459C695B4A7B1782141E9640E1C013D00B833D1E48D5B0C54FD4ECC54` |
| 元ソース SHA-256 | `B9E155DEF6C166FE58ED36E128425092E11AB4BF180F687F63BEA1A5165A6B47` |
| kPlug.dll基準SHA（再配布しない） | `34C13976108DB0A18A7AD6B7CFDA5517B4A9A83D85C9A909820144264C743855` |
| Assembly-CSharp.dll基準SHA（再配布しない） | `0038281CAF8DF48A7903C55DC389642EEEB3F2A9114BD9D68AC11C8AC0396BC5` |

実機配置先：`BepInEx\\plugins\\KPlugFixes\\HPointIKStaleExtraIKGuard.dll`。元ZIPの実体が取得できた際は、ZIPのSHA・内包DLLのSHAを照合した後、変更なしで `release/windows/` に登録する。それまでは**GitHub上にRC1の配布ZIPがあると表示しない**。

## 実機検証結果

2026-10-17のEXP v0.2.0＋FlowDiag検証：机への移動（09:13:35、09:15:01、09:16:29）で `SUPPRESS_DESK_EXTRA` を計3回確認。抑止後の手Targetの手動観測は左Y=0.6735m、右Y=0.6722m。回収時にEXPとFlowDiagを削除。

続いて同一DLLをRC1としてインストール。FlowDiagなしの09:32:19開始セッションのみを集計すると以下のとおり。

```text
END animatorCalls=26 deskCalls=6 checks=6 suppressed=4 passed=0 errors=0
```

机への対象移動で計4回の抑止、内部エラー0件。非対象2回は元処理維持。ユーザー目視では複数回移動して「ほぼ問題ない」。限定された回数の実機検証であり、全モーション・全キャラクターの網羅確認を意味しない。

## 問題Bは独立案件

机バック愛撫の初期姿勢は正常だが、テンキーEnterを押した瞬間に手が机へ一瞬めり込んでから正常に移行する。**RC1導入以前から存在した症状で、原因は未確定**。RC1を拡張・改変して対処せず、Enter入力からAnimator・IK・CrossFader・Solverの更新順序を、必要時のみ独立した読み取り専用診断で確認する。失敗済み `oriTargs` 強制復元案v0.1.3.3を再採用しない。

## 保存・再現ルール

- 実機成功済みDLLとZIPは、原バイトとSHA-256を維持する。RC1名での再ビルド禁止。
- RC1、既存CrossFaderCompatFix、NullGuards等、`kPlug.dll`、`Assembly-CSharp.dll`を問題Bの推測で変更しない。
- FlowDiagは別の診断資産。RC1本番パッケージに混ぜない。
- 公開GitHubにはPJが作成した成果物だけを格納し、ゲームDLLやkPlug、BepInEx、Harmony等の第三者バイナリを再配布しない。
- 本文のSHA・動作結果は2026-10-17の引き継ぎMD、EXP実機検証MD、RC1回収ログに基づく記録。GitHub登録時にRC1 ZIP実体を独立検算したという意味ではない。
