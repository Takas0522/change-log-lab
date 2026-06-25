# WPF 配布手順（ベースライン）

## 1. 発行

```powershell
dotnet publish .\src\OrderClientApp.Wpf\OrderClientApp.Wpf.csproj `
  -c Release `
  -p:PublishProfile=FolderProfile
```

出力先: `artifacts/publish/OrderClientApp/`

## 2. 手動インストール

1. 出力フォルダ一式を配布先 PC へコピー
2. `OrderClientApp.Wpf.exe` を実行
3. 初回起動時に `%LOCALAPPDATA%\OrderClientApp\order-client.db` が生成される

## 3. 手動アップデート

1. アプリを終了
2. 配布先の実行ファイル群を新しい発行物で上書き
3. DB は `%LOCALAPPDATA%` 側にあるため通常維持される

## 4. バックアップ運用

1. アプリ管理者でログイン
2. `設定` 画面で `DBバックアップ` 実行
3. 任意フォルダへ `order-client-backup-yyyyMMdd-HHmmss.db` を作成

## 5. ロールバック

1. アプリ終了
2. `%LOCALAPPDATA%\OrderClientApp\order-client.db` を退避
3. バックアップファイルを同名 `order-client.db` で配置
