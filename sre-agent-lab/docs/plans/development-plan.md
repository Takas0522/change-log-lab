# 開発計画

## 概要

本計画はToDoアプリケーションの開発をタスク単位で分割したものである。
各タスクは依存関係を明記し、開発者が順序通りに進められるようにする。

---

## タスク一覧

| # | タスク名 | 依存 | 成果物 |
|---|---|---|---|
| 1 | DevContainer設定 | なし | .devcontainer/ |
| 2 | バックエンド プロジェクト雛形 | なし | src/api/ (csproj, Program.cs, appsettings) |
| 3 | データベースモデル・コンテキスト | Task 2 | Models/, Data/ |
| 4 | 認証機能 | Task 3 | Services/JwtService, Controllers/Auth, DTOs/Auth |
| 5 | ToDo機能 | Task 4 | Controllers/Todos, DTOs/Todo |
| 6 | SQLファイル | Task 3 | db/schema.sql, db/seed.sql |
| 7 | フロントエンド プロジェクト雛形 | なし | src/web/ |
| 8 | フロントエンド 認証機能 | Task 7 | services/auth, interceptor, login, register |
| 9 | フロントエンド ToDo機能 | Task 8 | services/todo, todo-list, todo-detail, todo-create |
| 10 | CI/CDパイプライン | Task 5, 9 | .github/workflows/deploy.yml |

---

## タスク詳細

### Task 1: DevContainer設定

**目的:** ローカル開発環境をDockerコンテナで統一する。

**作成ファイル:**
- `.devcontainer/devcontainer.json` - 開発コンテナ定義
- `.devcontainer/docker-compose.yml` - アプリ + PostgreSQL
- `.devcontainer/Dockerfile` - .NET 10.0 + Node.js + PostgreSQL client
- `.devcontainer/setup-database.sh` - DB初期化スクリプト

**受入条件:**
- DevContainer起動でPostgreSQLが利用可能になること
- setup-database.shでスキーマとシードデータが投入されること

---

### Task 2: バックエンド プロジェクト雛形

**目的:** .NET Web APIプロジェクトの基盤を構築する。

**作成ファイル:**
- `src/api/api.csproj` - プロジェクト定義 (.NET 10.0)
- `src/api/Program.cs` - エントリーポイント (DI, JWT認証, CORS, OpenAPI)
- `src/api/appsettings.json` - 本番設定
- `src/api/appsettings.Development.json` - 開発設定

**依存パッケージ:**
- BCrypt.Net-Next 4.0.3
- Microsoft.AspNetCore.Authentication.JwtBearer
- Microsoft.EntityFrameworkCore
- Npgsql.EntityFrameworkCore.PostgreSQL

**受入条件:**
- `dotnet build` が成功すること
- `dotnet run` でAPIサーバーが起動すること

---

### Task 3: データベースモデル・コンテキスト

**目的:** EF CoreのモデルとDbContextを定義する。

**作成ファイル:**
- `src/api/Models/User.cs` - ユーザーエンティティ
- `src/api/Models/TodoItem.cs` - ToDoエンティティ
- `src/api/Models/TodoStatus.cs` - ステータス定数
- `src/api/Data/AppDbContext.cs` - DbContext (Fluent API, snake_case, 自動タイムスタンプ)

**設計方針:**
- Fluent APIで列名をsnake_caseにマッピング
- SaveChanges オーバーライドで created_at, updated_at を自動管理
- ステータス変更時の completed_at 自動設定ロジックをDbContextに実装

**受入条件:**
- ビルドが成功すること
- モデル定義がdatabase-spec.mdと一致すること

---

### Task 4: 認証機能

**目的:** JWT認証によるユーザー登録・ログイン・認証情報取得を実装する。

**作成ファイル:**
- `src/api/Services/JwtService.cs` - JWT生成・検証
- `src/api/DTOs/AuthDtos.cs` - 認証用DTO
- `src/api/Controllers/AuthController.cs` - 認証コントローラー

**エンドポイント:**
- `POST /api/auth/register` - ユーザー登録
- `POST /api/auth/login` - ログイン
- `GET /api/auth/me` - 認証ユーザー情報

**受入条件:**
- ユーザー登録が正常に動作すること
- ログインでJWTが返却されること
- JWTを使って /api/auth/me にアクセスできること

---

### Task 5: ToDo機能

**目的:** ToDo CRUD操作とフィルタリングを実装する。

**作成ファイル:**
- `src/api/DTOs/TodoDtos.cs` - ToDo用DTO
- `src/api/Controllers/TodosController.cs` - ToDoコントローラー

**エンドポイント:**
- `GET /api/todos` - 一覧取得 (フィルタ対応)
- `GET /api/todos/{id}` - 詳細取得
- `POST /api/todos` - 新規作成
- `PUT /api/todos/{id}` - 更新
- `PATCH /api/todos/{id}/status` - ステータス変更
- `DELETE /api/todos/{id}` - 削除

**受入条件:**
- 全CRUDエンドポイントが正常に動作すること
- フィルタリングが動作すること
- ユーザーは自分のToDoのみアクセスできること

---

### Task 6: SQLファイル

**目的:** データベーススキーマとシードデータを定義する。

**作成ファイル:**
- `src/api/db/schema.sql` - テーブル定義
- `src/api/db/seed.sql` - テストデータ

**受入条件:**
- schema.sqlの実行でテーブルが作成されること
- seed.sqlの実行でテストデータが投入されること
- ON CONFLICT によりべき等に実行可能であること

---

### Task 7: フロントエンド プロジェクト雛形

**目的:** Angular 21プロジェクトをTailwind CSS付きで構築する。

**作成ファイル:**
- `src/web/` 配下のAngularプロジェクト一式
- Tailwind CSS 4.x設定

**受入条件:**
- `npm start` で開発サーバーが起動すること
- Tailwind CSSのユーティリティクラスが適用されること

---

### Task 8: フロントエンド 認証機能

**目的:** 認証サービス、ルーティング、ログイン・登録画面を実装する。

**作成ファイル:**
- `src/web/src/app/models/index.ts` - 型定義
- `src/web/src/app/services/auth.service.ts` - 認証サービス (signals使用)
- `src/web/src/app/interceptors/auth.interceptor.ts` - JWT付与インターセプター
- `src/web/src/app/components/login/login.component.ts` - ログイン画面
- `src/web/src/app/components/register/register.component.ts` - 登録画面
- `src/web/src/app/app.routes.ts` - ルーティング定義 (authGuard, guestGuard)

**受入条件:**
- ログイン画面が表示されること
- ログイン/登録操作が正常に動作すること
- 未認証時にToDo画面へのアクセスがログイン画面にリダイレクトされること

---

### Task 9: フロントエンド ToDo機能

**目的:** ToDo一覧・詳細・新規作成の各画面を実装する。

**作成ファイル:**
- `src/web/src/app/services/todo.service.ts` - ToDoサービス
- `src/web/src/app/components/header/header.component.ts` - 共通ヘッダー
- `src/web/src/app/components/todo-list/todo-list.component.ts` - 一覧画面
- `src/web/src/app/components/todo-detail/todo-detail.component.ts` - 詳細/編集画面
- `src/web/src/app/components/todo-create/todo-create.component.ts` - 新規作成画面

**受入条件:**
- ToDo一覧が表示されること
- フィルタリングが動作すること
- 一覧画面からステータス変更が可能なこと
- 詳細画面から編集・削除が可能なこと
- 新規作成が可能なこと

---

### Task 10: CI/CDパイプライン

**目的:** GitHub Actionsでビルド・デプロイ・シードデータ投入を自動化する。

**作成ファイル:**
- `.github/workflows/deploy.yml`

**ジョブ構成:**
1. `build-api` - .NET APIのビルド・パブリッシュ
2. `build-web` - Angularアプリのビルド
3. `deploy` - Azure App Serviceへのデプロイ
4. `seed-data` - データベーススキーマ適用・シードデータ投入

**受入条件:**
- ワークフローYAMLの構文が正しいこと
- ビルドジョブが並列実行される構成であること
- シードデータがべき等に投入される構成であること
