# Todo App - 高機能ToDoアプリケーション

OWASP準拠、100万レコード対応の3層アーキテクチャToDoアプリケーション

## プロジェクト構成

```
src/todo-app/
├── database/          # SQL Database Project
│   ├── Tables/        # テーブル定義（Todos, Labels, TodoLabels）
│   ├── Indexes/       # インデックス定義（性能最適化）
│   ├── Triggers/      # トリガー（UpdatedAt自動更新）
│   └── Scripts/       # 全文検索、初期データ
├── api/               # ASP.NET Core Backend
│   ├── TodoApp.API/              # Web API層
│   ├── TodoApp.Application/      # Application/Service層
│   ├── TodoApp.Domain/           # Domain/Entity層
│   └── TodoApp.Infrastructure/   # Infrastructure/Repository層
└── front/             # Angular Frontend
    └── src/
        └── app/
            ├── components/        # UIコンポーネント
            ├── services/          # HTTP通信サービス
            ├── models/            # データモデル
            └── environments/      # 環境設定
```

## 機能一覧

### ToDo管理（REQ-FUNC-001～007）
- ✅ ToDo作成・取得・更新・削除（CRUD）
- ✅ ステータス管理（NotStarted/InProgress/Completed/Abandoned）
- ✅ 論理削除対応
- ✅ 楽観的同時実行制御（RowVersion）

### ラベル管理（REQ-FUNC-008～012）
- ✅ ラベル作成・取得・更新・削除
- ✅ カラーコード付き（HEX形式 #RRGGBB）
- ✅ 1つのToDoに最大10個のラベル
- ✅ 多対多リレーション（中間テーブル）

### 検索・絞込（REQ-FUNC-013～017）
- ✅ ラベルによる絞込
- ✅ 作成日時・更新日時による範囲検索
- ✅ ステータスによる絞込
- ✅ キーワード検索（全文検索、日本語対応）
- ✅ 複合絞込（複数条件の組み合わせ）

### 非機能要件対応
- ✅ **性能**: API応答500ms以内、100万レコード対応（REQ-PERF-001～008）
- ✅ **セキュリティ**: OWASP準拠（SQLインジェクション、XSS、CSRF対策）
- ✅ **ユーザビリティ**: Angular 17+ Signals、レスポンシブデザイン
- ✅ **データ整合性**: トランザクション管理、楽観的同時実行制御

## セットアップ

### 前提条件
- **.NET SDK**: 8.0以上
- **Node.js**: 20.x以上
- **SQL Server**: 2022以上 または Azure SQL Database
- **Angular CLI**: 17以上

### 1. Database セットアップ

```bash
cd src/todo-app/database

# SQL Serverに接続してテーブル作成
sqlcmd -S localhost -d TodoAppDb -i Tables/Todos.sql
sqlcmd -S localhost -d TodoAppDb -i Tables/Labels.sql
sqlcmd -S localhost -d TodoAppDb -i Tables/TodoLabels.sql

# インデックス作成
sqlcmd -S localhost -d TodoAppDb -i Indexes/IX_Todos.sql
sqlcmd -S localhost -d TodoAppDb -i Indexes/IX_Labels.sql
sqlcmd -S localhost -d TodoAppDb -i Indexes/IX_TodoLabels.sql

# トリガー・全文検索
sqlcmd -S localhost -d TodoAppDb -i Triggers/TR_Todos_UpdatedAt.sql
sqlcmd -S localhost -d TodoAppDb -i Scripts/FullTextSearch.sql

# 初期データ（オプション）
sqlcmd -S localhost -d TodoAppDb -i Scripts/SeedData.sql
```

### 2. Backend API セットアップ

```bash
cd src/todo-app/api/TodoApp.API

# パッケージ復元
dotnet restore

# 接続文字列設定（appsettings.json）
# "DefaultConnection": "Server=localhost;Database=TodoAppDb;Trusted_Connection=true;"

# ビルド
dotnet build

# 実行
dotnet run
# → https://localhost:5001/swagger でSwagger UIにアクセス
```

**API エンドポイント:**
- `GET /api/v1/todos` - Todo一覧取得（ページネーション、絞込対応）
- `GET /api/v1/todos/{id}` - Todo詳細取得
- `POST /api/v1/todos` - Todo作成
- `PUT /api/v1/todos/{id}` - Todo更新
- `DELETE /api/v1/todos/{id}` - Todo削除（論理削除）
- `PATCH /api/v1/todos/{id}/status` - ステータス更新
- `GET /api/v1/labels` - ラベル一覧取得
- `POST /api/v1/labels` - ラベル作成

### 3. Frontend セットアップ

```bash
cd src/todo-app/front

# 依存関係インストール
npm install

# 開発サーバー起動
npm start
# → http://localhost:4200 にアクセス

# ビルド（本番用）
npm run build
```

**主要コンポーネント:**
- `TodoListComponent` - Todo一覧表示（Signals使用）
- `TodoService` - Todo API通信
- `LabelService` - ラベル API通信

## OWASP準拠のセキュリティ対策

### Backend（ASP.NET Core）

| OWASP項目 | 対策内容 | 実装箇所 |
|---|---|---|
| A01:2021 Broken Access Control | 認可チェック（将来実装） | Controllers |
| A03:2021 Injection | パラメータ化クエリ、Entity Framework Core | DbContext |
| A04:2021 Insecure Design | 論理削除、トランザクション管理 | Services |
| A05:2021 Security Misconfiguration | セキュリティヘッダー、CORS設定 | Program.cs |
| A07:2021 Auth Failures | JWT認証（将来実装） | Middleware |
| A09:2021 Security Logging | 構造化ログ | Controllers |

### Frontend（Angular）

| OWASP項目 | 対策内容 | 実装箇所 |
|---|---|---|
| XSS対策 | Angular自動エスケープ | すべてのComponent |
| CSRF対策 | Anti-Forgeryトークン | HttpClient Interceptor |
| 入力検証 | Validators、パターンマッチング | ReactiveFormsModule |

### Database（SQL Server）

- ✅ CHECK制約によるデータ検証
- ✅ 外部キー制約
- ✅ トランザクション分離レベル
- ✅ 楽観的同時実行制御（RowVersion）

## パフォーマンス最適化

### インデックス戦略（100万レコード対応）

| インデックス | 用途 | 性能目標 |
|---|---|---|
| IX_Todos_Status_CreatedAt | ステータス絞込+ソート（カバリングインデックス） | 500ms以内 |
| IX_Todos_CreatedAt_Desc | デフォルト一覧表示 | 500ms以内 |
| Full-Text Index | 日本語キーワード検索 | 10万件: 1秒以内、100万件: 2秒以内 |
| UX_Labels_Name | ラベル名重複チェック | 即時 |

### フロントエンド最適化

- ✅ **Signals API**: 細粒度なリアクティブ更新（REQ-PERF-003）
- ✅ **OnPush変更検知**: 不要な再レンダリング防止
- ✅ **LazyLoading**: ルーティングベースのコード分割
- ✅ **TrackBy**: @for ループの最適化

## テスト

### バックエンド単体テスト
```bash
cd src/todo-app/api
dotnet test
```

### フロントエンド単体テスト
```bash
cd src/todo-app/front
npm test
```

### E2Eテスト
```bash
cd src/todo-app/front
npm run e2e
```

## デプロイ

### Azure App Service + Azure SQL Database
```bash
# Backend
az webapp create --name todoapp-api --resource-group rg-todoapp --plan plan-todoapp
az webapp deployment source config-zip --name todoapp-api --resource-group rg-todoapp --src api.zip

# Frontend
ng build --configuration production
az storage blob upload-batch -s dist/front -d $web --account-name todoappstatic
```

## トラブルシューティング

### API接続エラー
- CORSポリシーを確認: `Program.cs`の`AllowFrontend`設定
- HTTPSポート: Backend `5001`、Frontend `4200`

### 全文検索が動作しない
```sql
-- 全文検索サービス確認
SELECT SERVERPROPERTY('IsFullTextInstalled');

-- カタログ再構築
ALTER FULLTEXT INDEX ON Todos START FULL POPULATION;
```

### パフォーマンス低下
```sql
-- インデックスの断片化確認
SELECT 
    OBJECT_NAME(ips.object_id) AS TableName,
    i.name AS IndexName,
    ips.avg_fragmentation_in_percent
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
INNER JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
WHERE ips.avg_fragmentation_in_percent > 10;

-- 再構築
ALTER INDEX ALL ON Todos REBUILD;
```

## ライセンス

MIT License

## 関連ドキュメント

- [要求仕様書（SRS-001）](../../../docs/todo-app/要求定義/SRS-001-todo-app.md)
- [詳細設計書（SDD-001）](../../../docs/todo-app/詳細設計/SDD-001-todo-app.md)
- [結合テストシナリオ（ITS-001）](../../../docs/todo-app/結合テストシナリオ/ITS-001-todo-app.md)
