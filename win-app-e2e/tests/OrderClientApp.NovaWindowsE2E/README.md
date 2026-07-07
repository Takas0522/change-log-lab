# NovaWindows Driver E2E PoC

`appium-novawindows-driver` で .NET 10 WPF アプリを操作し、ログインと発注管理の主要導線を確認するPoCです。

## 前提

- Node.js / npm
- Appium CLI
- Appium NovaWindows Driver

```powershell
npm install -g appium
appium driver install --source=npm appium-novawindows-driver
```

## 実行

```powershell
pwsh -File .\tests\OrderClientApp.NovaWindowsE2E\run-novawindows-smoke.ps1 -Configuration Release
```

NovaWindows の直接起動セッションが不安定な場合は、WPF アプリを先に起動して `appTopLevelWindow` で接続します。

```powershell
pwsh -File .\tests\OrderClientApp.NovaWindowsE2E\run-novawindows-smoke.ps1 -Configuration Release -AttachPrelaunchedApp
```

現在の検証済み構成では `-AttachPrelaunchedApp` を推奨します。このモードでは各テストごとにWPFアプリを起動し、`appTopLevelWindow` で接続します。

NovaWindows Driver もまとめて導入する場合:

```powershell
pwsh -File .\tests\OrderClientApp.NovaWindowsE2E\run-novawindows-smoke.ps1 -Configuration Release -InstallNovaWindowsDriver
```

## テスト内容

1. `AdminLogin_ReachesDashboard`
   - `UsernameTextBox` / `PasswordBox` に `admin.user` / `Admin#2026` を `windows: setValue` で設定
   - パスワード欄から Enter でログイン
   - `HeaderTextBlock` に `admin.user` と `管理者` が表示されることを検証
   - `AdminButton` が表示されることを検証
2. `AdminCanCreateOrder_FromOrderManagement`
   - 管理者ログイン後に `OrderManagementButton` から発注一覧を開く
   - `CreateOrderButton` から発注詳細を開く
   - `SupplierTextBox` と `LineItemsDataGrid` に発注データを入力
   - Ctrl+S で保存し、発注一覧へ戻ることを検証

成功時・失敗時は `artifacts\e2e\novawindows\` にスクリーンショットを保存します。

## 環境変数

| 変数 | 既定値 | 用途 |
|---|---|---|
| `NOVA_APPIUM_SERVER_URL` | `http://127.0.0.1:4723/` | 接続先 Appium サーバー |
| `ORDER_CLIENT_APP_EXE` | 自動検出 | テスト対象 exe の明示指定 |
| `ORDER_CLIENT_APP_CONFIGURATION` | `Release` → `Debug` の順で自動検出 | ビルド構成 |
| `ORDER_CLIENT_APP_TOP_LEVEL_WINDOW` | なし | 既存ウィンドウへ `appTopLevelWindow` で接続する場合のハンドル |
| `ORDER_CLIENT_APP_PROCESS_ID` | なし | プリローンチ接続時のWPFプロセスID |
