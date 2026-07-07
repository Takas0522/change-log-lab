# FlaUI E2E PoC

FlaUI UIA3 で .NET 10 WPF アプリを起動し、ログインと発注管理の主要導線を確認するPoCです。

## 実行

```powershell
pwsh -File .\tests\OrderClientApp.FlaUIE2E\run-flaui-smoke.ps1 -Configuration Release
```

## テスト内容

1. `AdminLogin_ReachesDashboard`
   - `OrderClientApp.Wpf.exe` を FlaUI から起動
   - `UsernameTextBox` / `PasswordBox` に `admin.user` / `Admin#2026` を `ValuePattern` で設定
   - パスワード欄から Enter でログイン
   - `HeaderTextBlock` に `admin.user` と `管理者` が表示されることを検証
   - `AdminButton` が表示されることを検証
2. `AdminCanCreateOrder_FromOrderManagement`
   - 管理者ログイン後に `OrderManagementButton` から発注一覧を開く
   - `CreateOrderButton` から発注詳細を開く
   - `SupplierTextBox` と `LineItemsDataGrid` に発注データを入力
   - Ctrl+S で保存し、発注一覧へ戻ることを検証

成功時・失敗時は `artifacts\e2e\flaui\` にスクリーンショットを保存します。

## .NET 10 互換性メモ

FlaUI 5.0.0 は `net6.0-windows7.0` / `net8.0-windows7.0` の assets を持ちます。PoC プロジェクトは `net10.0-windows7.0` で作成し、.NET の上位互換としてビルド・実行できるかを検証します。

## 環境変数

| 変数 | 既定値 | 用途 |
|---|---|---|
| `ORDER_CLIENT_APP_EXE` | 自動検出 | テスト対象 exe の明示指定 |
| `ORDER_CLIENT_APP_CONFIGURATION` | `Release` → `Debug` の順で自動検出 | ビルド構成 |
