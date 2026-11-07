kPlug Piston Transition Fix｜導入案内

これはWaitFixとAutoResumeの2種類のプラグインを説明する文書です。
それぞれ別のZIPとして配布されています。

1. KPlugPistonWaitFix v1.0.0
   挿入モーションの切替後に操作を受け付けなくなる待機状態を防止します。
   導入ZIP: ../../release/windows/PistonTransitionFix/KPlugPistonWaitFix_v1.0.0_KPlugFixes_Install.zip

2. KPlugPistonAutoResume v0.2.0
   切替後にゲーム本来のGo経路でピストン動作を自動再開します。
   導入ZIP: ../../release/windows/PistonTransitionFix/KPlugPistonAutoResume_v0.2.0_KPlugFixes_Install.zip

AutoResumeの使用にはWaitFix v1.0.0が必要です。

対応環境: Koikatu 5.1 / kPlug 3.6.0 / BepInEx 5.4.23.5

導入:
ゲームを終了し、既存の同名DLLをバックアップしてから、
必要なZIPをゲーム本体フォルダーへ展開します。
現在のDLL配置先:
  BepInEx\plugins\KPlugFixes\KPlugPistonWaitFix.dll
  BepInEx\plugins\KPlugFixes\KPlugPistonAutoResume.dll
同じDLLをほかのプラグインフォルダーに重複配置しないでください。

AutoResumeの設定:
  BepInEx\config\local.kplug.pistonautoresume.cfg
  Enabled = true
  ResumeDelaySeconds = 0.8
無効にする場合はEnabled = falseです。

起動ログ:
  [PistonWaitFix] v1.0.0 active.
  [PistonAutoResume] v0.2.0 active.

解除:
ゲームを終了し、導入したDLLを取り外します。
以前の版があればバックアップから復元します。

詳細: README_ja.md / README.md
