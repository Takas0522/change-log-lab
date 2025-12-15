# Auth Service API

認証サービス - ユーザー登録、ログイン、JWT認証、端末別ログアウト機能を提供します。

## 機能

- ✅ ユーザー登録 (`POST /auth/register`)
- ✅ ログイン (`POST /auth/login`)
- ✅ ログアウト (`POST /auth/logout`) - 端末別
- ✅ ユーザー情報取得 (`GET /auth/me`)
- ✅ JWT認証 (有効期限: 10分)
- ✅ セッションバージョン管理による端末別ログアウト
- ✅ BCryptによる安全なパスワードハッシュ化

## 技術スタック

- .NET 10.0
- Entity Framework Core 10.0
- PostgreSQL (Npgsql)
- JWT Bearer Authentication
- BCrypt.Net

## データベース

### テーブル構造

#### users
- `id` (UUID, PK)
- `email` (VARCHAR, UNIQUE)
- `password_hash` (VARCHAR)
- `display_name` (VARCHAR)
- `created_at` (TIMESTAMP)
- `updated_at` (TIMESTAMP)

#### device_sessions
- `id` (UUID, PK)
- `user_id` (UUID, FK)
- `device_id` (VARCHAR)
- `session_version` (INT)
- `last_login_at` (TIMESTAMP)
- `created_at` (TIMESTAMP)
- `updated_at` (TIMESTAMP)
- UNIQUE(`user_id`, `device_id`)

## セットアップ

### 1. データベースの準備

PostgreSQLデータベースを作成し、スキーマを適用します:

```bash
# PostgreSQLに接続
psql -U postgres

# データベース作成
CREATE DATABASE auth_db;

# データベースに接続
\c auth_db

# スキーマ適用
\i ../db/schema.sql
```

### 2. 設定ファイル

`appsettings.Development.json`で以下を設定:

- データベース接続文字列
- JWT Secret (32文字以上推奨)

### 3. ビルドと実行

```bash
dotnet build
dotnet run
```

APIは `http://localhost:5000` で起動します。

## API エンドポイント

### POST /auth/register
ユーザー登録

**リクエスト:**
```json
{
  "email": "user@example.com",
  "password": "password123",
  "displayName": "John Doe"
}
```

**レスポンス:**
```json
{
  "userId": "uuid",
  "email": "user@example.com",
  "displayName": "John Doe",
  "message": "User registered successfully. Please login."
}
```

### POST /auth/login
ログイン

**リクエスト:**
```json
{
  "email": "user@example.com",
  "password": "password123",
  "deviceId": "web-browser-device-123"
}
```

**レスポンス:**
```json
{
  "token": "jwt-token-here",
  "email": "user@example.com",
  "userId": "uuid",
  "displayName": "John Doe"
}
```

### GET /auth/me
現在のユーザー情報取得（認証必須）

**ヘッダー:**
```
Authorization: Bearer <jwt-token>
```

**レスポンス:**
```json
{
  "userId": "uuid",
  "email": "user@example.com",
  "displayName": "John Doe"
}
```

### POST /auth/logout
ログアウト（認証必須）

**ヘッダー:**
```
Authorization: Bearer <jwt-token>
```

**リクエスト:**
```json
{
  "deviceId": "web-browser-device-123"
}
```

**レスポンス:**
```json
{
  "message": "Logged out successfully"
}
```

## セキュリティ機能

### 端末別セッション管理

- 各ユーザーは複数の端末でログイン可能
- 各端末には固有の `device_id` を割り当て
- ログアウトすると該当端末のみ無効化

### セッションバージョン検証

1. ログイン時、JWT に `session_version` を含める
2. リクエストごとにミドルウェアでDBの `session_version` と照合
3. 不一致の場合は401エラーを返す
4. ログアウト時は `session_version` をインクリメント

### パスワード保護

- BCryptを使用した安全なハッシュ化
- プレーンテキストのパスワードは保存しない

## テスト

`api.http` ファイルを使用してエンドポイントをテストできます。

1. ユーザー登録
2. ログイン (tokenを取得)
3. `/auth/me` で認証確認
4. ログアウト
5. 再度 `/auth/me` を試行 (401エラーになるはず)

## 受け入れ条件

- ✅ 登録→ログインでJWTが取得できる
- ✅ `logout` 後、同じJWT（exp内）でもAPIが401になる（DB照合で拒否）
- ✅ パスワードは安全なハッシュ（BCrypt）で保存
