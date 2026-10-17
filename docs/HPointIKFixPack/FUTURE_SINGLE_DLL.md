# Future design: physical single-DLL integration (deferred)

今回のFixPack v1.0.0は2 DLLを1パッケージで管理する採用構成。次の変更を今回へ混ぜない。

将来1 DLL化する場合、新しい別Version/別Harmony ID/別生成DLLとして、凍結RC1と問題B成功DLLを比較基準に保持する。両PrefixのHook衝突／priority／元のCoroutineと状態遷移／Restore・Resetの適用条件／例外時のfail-open／診断との独立性を照合。まず同一操作（机→壁→寝のHポイント移動、机バック愛撫→Enter、その後の挿入、ReturnとSwap）で機能A・Bの退行がないことを実機比較する。Rollbackは凍結2 DLL版を原本としてワンステップで復元できるようにする。既存DLLを上書き・再ビルドしない。
