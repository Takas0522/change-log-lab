# SRE Agent Lab - ToDoアプリケーション

## 概要

簡易的なToDo管理アプリケーションです。独自の認証基盤を持ち、ユーザーごとにToDoを管理できます。

### 主な機能

- **ユーザー認証**: JWT認証によるユーザー登録・ログイン機能
- **ToDo管理**: 
  - タイトル、本文、ステータス（未着手/着手中/完了）の管理
  - 完了予定日時の設定
  - 作成日時・完了日の自動記録
- **ToDo操作**:
  - 一覧表示とフィルタリング
  - ステータスの簡易変更（一覧画面から）
  - 詳細編集・削除（詳細画面から）

## 技術スタック

### バックエンド
- **.NET 10.0** - Web API
- **Entity Framework Core** - ORM
- **PostgreSQL** - データベース
- **JWT** - 認証

### フロントエンド
- **Angular 21** - SPAフレームワーク
- **Tailwind CSS 4.x** - CSSフレームワーク
- **RxJS** - リアクティブプログラミング

## 前提条件

- **Docker** および **Docker Compose** がインストールされていること
- **Visual Studio Code** がインストールされていること
- **Dev Containers** 拡張機能がインストールされていること

## 開発環境のセットアップ

### 1. Dev Containerでの起動

このプロジェクトはDev Containerを使用した開発を前提としています。

1. Visual Studio Codeでプロジェクトを開く
2. コマンドパレット（`Ctrl+Shift+P` または `Cmd+Shift+P`）を開く
3. `Dev Containers: Reopen in Container` を選択
4. コンテナのビルドと起動が完了するまで待つ

### 2. データベースのセットアップ

Dev Container起動時に自動的にPostgreSQLがセットアップされますが、手動でスキーマとシードデータを投入する場合は以下を実行します。

```bash
# スキーマの作成
psql -U postgres -d sre_agent_lab_db -f src/api/db/schema.sql

# シードデータの投入
psql -U postgres -d sre_agent_lab_db -f src/api/db/seed.sql
```

**データベース接続情報:**
- Host: `localhost`
- Port: `5432`
- Database: `sre_agent_lab_db`
- Username: `postgres`
- Password: `postgres`

### 3. バックエンドAPIの起動

```bash
# src/apiディレクトリに移動
cd src/api

# 依存パッケージの復元
dotnet restore

# アプリケーションの起動（開発モード）
dotnet run
```

APIサーバーは `http://localhost:8080` で起動します。

**開発用設定:**
- JWT Secret: 開発用の秘密鍵が `appsettings.Development.json` に設定済み
- トークン有効期限: 60分

### 4. フロントエンドの起動

```bash
# src/webディレクトリに移動
cd src/web

# 依存パッケージのインストール
npm install

# 開発サーバーの起動
npm start
```

フロントエンドアプリケーションは `http://localhost:4200` で起動します。

> **Note:** 開発サーバーにはプロキシ設定（`proxy.conf.json`）が含まれており、`/api` へのリクエストは自動的にバックエンドAPI（`http://localhost:8080`）に転送されます。

## アプリケーションの使い方

### 1. ユーザー登録

1. ブラウザで `http://localhost:4200` にアクセス
2. 「Register」ボタンをクリック
3. メールアドレス、パスワード、表示名を入力して登録

### 2. ログイン

1. 登録したメールアドレスとパスワードでログイン
2. 自動的にToDo一覧画面に遷移

### 3. ToDoの作成

1. 「新規作成」ボタンをクリック
2. タイトル、本文、完了予定日を入力
3. 「作成」ボタンで保存

### 4. ToDoの管理

- **一覧画面**: ステータスの変更、フィルタリング、詳細表示
- **詳細画面**: 全項目の編集、削除

## API エンドポイント

### 認証 (`/api/auth`)

| メソッド | エンドポイント | 説明 | 認証 |
|---------|--------------|------|-----|
| POST | `/api/auth/register` | ユーザー登録 | 不要 |
| POST | `/api/auth/login` | ログイン | 不要 |
| GET | `/api/auth/me` | 認証ユーザー情報取得 | 必要 |

### ToDo (`/api/todos`)

| メソッド | エンドポイント | 説明 | 認証 |
|---------|--------------|------|-----|
| GET | `/api/todos` | ToDo一覧取得（フィルタ可） | 必要 |
| GET | `/api/todos/{id}` | ToDo詳細取得 | 必要 |
| POST | `/api/todos` | ToDo新規作成 | 必要 |
| PUT | `/api/todos/{id}` | ToDo更新 | 必要 |
| PATCH | `/api/todos/{id}/status` | ステータス変更 | 必要 |
| DELETE | `/api/todos/{id}` | ToDo削除 | 必要 |

## プロジェクト構造

```
.
├── docs/                          # ドキュメント
│   ├── api-spec.md               # API仕様
│   ├── auth-spec.md              # 認証仕様
│   ├── database-spec.md          # データベース仕様
│   ├── screens-spec.md           # 画面仕様
│   ├── init.md                   # プロジェクト初期化ドキュメント
│   └── plans/
│       └── development-plan.md   # 開発計画
├── src/
│   ├── api/                      # バックエンドAPI (.NET)
│   │   ├── Controllers/          # APIコントローラー
│   │   ├── Data/                 # DbContext
│   │   ├── DTOs/                 # データ転送オブジェクト
│   │   ├── Models/               # エンティティモデル
│   │   ├── Services/             # ビジネスロジック
│   │   ├── db/                   # SQLスクリプト
│   │   ├── appsettings.json      # 本番設定
│   │   └── appsettings.Development.json  # 開発設定
│   └── web/                      # フロントエンド (Angular)
│       └── src/
│           └── app/
│               ├── components/   # UIコンポーネント
│               ├── services/     # サービス層
│               ├── interceptors/ # HTTPインターセプター
│               └── models/       # 型定義
└── README.md                     # このファイル
```

## 開発のヒント

### バックエンド

- **Entity Framework Coreマイグレーション**: 現在はCode-FirstではなくDatabase-Firstで運用しています
- **エラーハンドリング**: コントローラー内で適切な例外処理を実装してください
- **ロギング**: `ILogger`を使用してログを出力できます

### フロントエンド

- **Signals**: Angular 21のSignals APIを活用して状態管理を行っています
- **ルートガード**: 認証状態に基づいて`authGuard`と`guestGuard`でルーティングを制御しています
- **HTTPインターセプター**: JWTトークンを自動的にリクエストヘッダーに付与します

## トラブルシューティング

### データベース接続エラー

```bash
# PostgreSQLが起動しているか確認
docker ps

# PostgreSQLに直接接続して確認
psql -U postgres -d sre_agent_lab_db
```

### ビルドエラー

```bash
# バックエンド
cd src/api
dotnet clean
dotnet restore
dotnet build

# フロントエンド
cd src/web
rm -rf node_modules
npm install
```

### ポートの競合

デフォルトポート（API: 5000, Web: 4200）が既に使用されている場合:

- **API**: 環境変数 `ASPNETCORE_URLS=http://+:5000` などでポート指定
- **Web**: `npm start -- --port 4201` のようにポート指定

## ライセンス

このプロジェクトはデモ用アプリケーションです。

## セキュリティに関する注意

**重要**: このアプリケーションはデモ用途です。本番環境で使用する場合は以下を必ず変更してください:

- JWT Secretキーを環境変数から注入する
- データベースの認証情報を適切に管理する
- HTTPSを有効にする
- CORS設定を適切に制限する
- パスワードポリシーを強化する
