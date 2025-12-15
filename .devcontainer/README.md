# DevContainer Database Setup

このDevContainerは起動時に自動的にデータベースをセットアップします。

## 自動セットアップ

DevContainerが初回作成されると、`setup-databases.sh`スクリプトが自動的に実行され、以下の処理が行われます：

1. PostgreSQLの起動を待機
2. `auth_db`データベースの作成
3. テーブルスキーマの適用
4. シードデータの投入

**注意**: このスクリプトはコンテナの初回作成時のみ実行されます（`postCreateCommand`）。コンテナの再起動では実行されません。

## データベース構成

### PostgreSQL
- **Host**: localhost
- **Port**: 5432
- **User**: postgres
- **Password**: postgres

### Databases

#### auth_db
認証サービス用のデータベース

**テーブル:**
- `users` - ユーザー情報
- `device_sessions` - 端末別セッション管理

**シードデータ:**
- `admin@example.com` (password: `password123`)
- `user@example.com` (password: `password123`)
- `demo@example.com` (password: `password123`)

## 手動セットアップ

DevContainerの自動セットアップが実行されなかった場合、以下のコマンドで手動実行できます：

```bash
bash /workspaces/change-log-lab/.devcontainer/setup-databases.sh
```

## データベースのリセット

データベースをリセットする場合：

```bash
# データベースを削除
psql -U postgres -h localhost -c "DROP DATABASE auth_db;"

# 再セットアップ
bash /workspaces/change-log-lab/.devcontainer/setup-databases.sh
```

## 追加データベースの設定

新しいサービスのデータベースを追加する場合、`setup-databases.sh`に以下を追加してください：

```bash
echo "📦 Setting up your_db..."
psql -U postgres -h localhost -c "CREATE DATABASE your_db;" 2>/dev/null || echo "ℹ️  Database your_db already exists"

echo "📝 Applying your_db schema..."
psql -U postgres -h localhost -d your_db -f /workspaces/change-log-lab/src/your-service/db/schema.sql

echo "🌱 Loading your_db seed data..."
psql -U postgres -h localhost -d your_db -f /workspaces/change-log-lab/src/your-service/db/seed.sql
```

## トラブルシューティング

### PostgreSQLに接続できない

```bash
# PostgreSQLのステータス確認
psql -U postgres -h localhost -c '\l'
```

### セットアップスクリプトの再実行

```bash
# スクリプトに実行権限があることを確認
chmod +x /workspaces/change-log-lab/.devcontainer/setup-databases.sh

# 手動実行
bash /workspaces/change-log-lab/.devcontainer/setup-databases.sh
```

### DevContainerの完全再構築

VS Codeのコマンドパレット（Ctrl/Cmd + Shift + P）から：
- `Dev Containers: Rebuild Container` を実行
