# WPF Native UI E2E Harness (WinAppCli-first)

このディレクトリは、WPFネイティブUI向けE2Eの**実行契約**を管理します。

## 方針
- 第一優先: WinAppCli
- 本環境でWinAppCli未導入でも、スイート定義と実行コマンド契約を先に固定
- `ops-settings` は現時点 placeholder として契約化し、実装後に自動化昇格

## 実行
```powershell
pwsh -File .\tests\wpf-native-e2e\scripts\run-winappcli-suite.ps1 -Suite smoke
pwsh -File .\tests\wpf-native-e2e\scripts\run-winappcli-suite.ps1 -Suite regression
```

## 契約
- スイート定義: `tests/wpf-native-e2e/winappcli/suites/*.json`
- 期待アーティファクト: `artifacts/e2e/{suite}/`
