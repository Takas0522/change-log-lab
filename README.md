# Change Log Lab

マイクロサービスアーキテクチャを採用した、コラボレーション型Todoアプリケーション。

## アーキテクチャ概要

- **Auth Service** (Port 5001): ユーザー認証・JWT発行
- **User Service** (Port 5002): ユーザープロフィール管理
- **Todo Service** (Port 5003): Todo/List CRUD・権限管理
- **BFF Service** (Port 5000): フロントエンド向け統合API
- **Realtime Service** (Port 5004): SignalRによるリアルタイム通知
- **Web** (Port 4200): Angular フロントエンド

各サービスは独立したPostgreSQLデータベースを持ちます。

## 前提条件

- .NET SDK (latest)
- Node.js & npm
- PostgreSQL
- Angular CLI

## セットアップ

### 1. データベースのセットアップ

```bash
# Auth Database
cd src/auth-service/db
PGPASSWORD=postgres psql -h localhost -U postgres -c "DROP DATABASE IF EXISTS auth_db;"
PGPASSWORD=postgres psql -h localhost -U postgres -c "CREATE DATABASE auth_db;"
PGPASSWORD=postgres psql -h localhost -U postgres -d auth_db -f schema.sql
PGPASSWORD=postgres psql -h localhost -U postgres -d auth_db -f seed.sql

# User Database
cd ../../../src/user-service/db
PGPASSWORD=postgres psql -h localhost -U postgres -c "DROP DATABASE IF EXISTS user_db;"
PGPASSWORD=postgres psql -h localhost -U postgres -c "CREATE DATABASE user_db;"
PGPASSWORD=postgres psql -h localhost -U postgres -d user_db -f schema.sql
PGPASSWORD=postgres psql -h localhost -U postgres -d user_db -f seed.sql

# Todo Database
cd ../../../src/todo-service/db
PGPASSWORD=postgres psql -h localhost -U postgres -c "DROP DATABASE IF EXISTS todo_db;"
PGPASSWORD=postgres psql -h localhost -U postgres -c "CREATE DATABASE todo_db;"
PGPASSWORD=postgres psql -h localhost -U postgres -d todo_db -f schema.sql
PGPASSWORD=postgres psql -h localhost -U postgres -d todo_db -f seed.sql
```

### テストユーザー（開発環境のみ）

シードデータで3つのテストユーザーが作成されます：

| メールアドレス | パスワード | 表示名 |
|---|---|---|
| `admin@example.com` | `password123` | Admin User |
| `user@example.com` | `password123` | Regular User |
| `demo@example.com` | `password123` | Demo User |

**注意**: これらは開発環境専用です。本番環境では絶対に使用しないでください。

### 2. バックエンドサービスの起動

各サービスを別々のターミナルで起動します。

#### Auth Service (Port 5001)

```bash
cd src/auth-service/api && dotnet restore && dotnet run
```

#### User Service (Port 5002)

```bash
cd src/user-service/api && dotnet restore && dotnet run
```

#### Todo Service (Port 5003)

```bash
cd src/todo-service/api && dotnet restore && dotnet run
```

#### BFF Service (Port 5000)

```bash
cd src/bff-service/api && dotnet restore && dotnet run
```

### 3. フロントエンドの起動

```bash
cd src/web && npm ci && npm start
```

アプリケーションは http://localhost:4200 で起動します。

## デバッグコマンド

### データベース接続確認

```bash
# Auth DB
PGPASSWORD=postgres psql -h localhost -U postgres -d auth_db -c "\dt"

# User DB
PGPASSWORD=postgres psql -h localhost -U postgres -d user_db -c "\dt"

# Todo DB
PGPASSWORD=postgres psql -h localhost -U postgres -d todo_db -c "\dt"
```

### データベース内容確認

```bash
# ユーザー一覧
PGPASSWORD=postgres psql -h localhost -U postgres -d auth_db -c "SELECT * FROM users;"

# プロフィール一覧
PGPASSWORD=postgres psql -h localhost -U postgres -d user_db -c "SELECT * FROM user_profiles;"

# リスト一覧
PGPASSWORD=postgres psql -h localhost -U postgres -d todo_db -c "SELECT * FROM lists;"

# Todo一覧
PGPASSWORD=postgres psql -h localhost -U postgres -d todo_db -c "SELECT * FROM todos;"

# リストメンバー（招待）
PGPASSWORD=postgres psql -h localhost -U postgres -d todo_db -c "SELECT * FROM list_members;"
```

### サービスヘルスチェック

```bash
# Auth Service
curl http://localhost:5001/api/auth/health || echo "Auth Service: Not Running"

# User Service
curl http://localhost:5002/api/users/health || echo "User Service: Not Running"

# Todo Service
curl http://localhost:5003/api/lists || echo "Todo Service: Not Running (要認証)"

# BFF Service
curl http://localhost:5000/api/auth/health || echo "BFF Service: Not Running"

# Web
curl http://localhost:4200 || echo "Web: Not Running"
```

### ログの確認

各サービスの標準出力でログを確認できます。

### データベースのリセット

```bash
# 全データベースをリセット
cd src/auth-service/db
PGPASSWORD=postgres psql -h localhost -U postgres -d auth_db -c "TRUNCATE users, device_sessions CASCADE;"

cd ../../user-service/db
PGPASSWORD=postgres psql -h localhost -U postgres -d user_db -c "TRUNCATE user_profiles CASCADE;"

cd ../../todo-service/db
PGPASSWORD=postgres psql -h localhost -U postgres -d todo_db -c "TRUNCATE lists, todos, list_members, outbox_events CASCADE;"
```

### APIテスト（.http ファイル）

各サービスの `api` ディレクトリに `api.http` ファイルがあります。VS Codeの REST Client拡張機能で実行できます。

```bash
# Auth Service
src/auth-service/api/api.http

# User Service
src/user-service/api/api.http

# Todo Service
src/todo-service/api/api.http
```

## トラブルシューティング

### ポートが使用中の場合

```bash
# プロセスの確認
lsof -i :5000  # BFF
lsof -i :5001  # Auth
lsof -i :5002  # User
lsof -i :5003  # Todo
lsof -i :4200  # Web

# プロセスの終了
kill -9 <PID>
```

### データベース接続エラー

```bash
# PostgreSQLの起動確認
sudo service postgresql status

# PostgreSQLの起動
sudo service postgresql start
```

### JWT Secret の設定

各サービスの `appsettings.json` で同じJWT Secretを設定してください：

```json
{
  "Jwt": {
    "Secret": "your-secret-key-min-32-chars-long-for-hs256",
    "Issuer": "auth-service",
    "Audience": "auth-service"
  }
}
```

## 開発ワークフロー

1. **データベース作成**: 上記セットアップ手順に従う
2. **バックエンド起動**: Auth → User → Todo → BFF の順で起動
3. **フロントエンド起動**: `npm start` でWebアプリを起動
4. **動作確認**: http://localhost:4200 にアクセス
5. **ユーザー登録**: 新規ユーザーを作成
6. **リスト作成**: Todoリストを作成
7. **招待テスト**: 別ユーザーを招待して共有を確認

## プロジェクト構造

```
.
├── docs/              # ドキュメント
├── specs/             # 仕様書
│   └── 000-init/      # 初期実装仕様
└── src/
    ├── auth-service/  # 認証サービス
    │   ├── api/       # ASP.NET WebAPI
    │   └── db/        # PostgreSQL スキーマ
    ├── user-service/  # ユーザーサービス
    │   ├── api/
    │   └── db/
    ├── todo-service/  # Todoサービス
    │   ├── api/
    │   └── db/
    ├── bff-service/   # BFF (Backend for Frontend)
    │   └── api/
    └── web/           # Angular フロントエンド
        └── src/
```

## 主要機能

- ✅ ユーザー登録・ログイン（device_id管理）
- ✅ JWT認証（10分有効期限）
- ✅ Todoリスト CRUD
- ✅ Todo項目 CRUD
- ✅ ユーザー招待・共有（owner/viewer権限）
- ✅ リアルタイム同期（SignalR） ※要実装
- ✅ Outbox Pattern + NOTIFY/LISTEN ※要実装

## ライセンス

MIT
