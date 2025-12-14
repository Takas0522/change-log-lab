# リポジトリレイアウト

このドキュメントは、モノレポ全体の**ディレクトリ構成**、**標準ポート**、**環境変数キー**、および**バージョン固定方針**を定義します。  
後続の INIT 仕様（INIT-002〜INIT-013）はこのドキュメントを参照してください。

---

## 1. ディレクトリ構成

```
change-log-lab/
├── docs/               # ドキュメント（アーキテクチャ、運用）
├── specs/              # 仕様（INIT-xxx, FEAT-xxx）
└── src/
    ├── web/            # Angular アプリケーション
    ├── services/
    │   ├── api/        # ASP.NET API（認証・CRUD・Outbox）
    │   └── realtime/   # SignalR サービス（リアルタイム通知）
    ├── functions/
    │   └── pg-events/  # .NET Functions（PostgreSQL LISTEN 常駐）
    ├── packages/
    │   └── contracts/  # 共通DTO・Event schema（.NET / TypeScript）
    └── db/             # マイグレーション・SQL スクリプト
```

### 各ディレクトリの役割

| パス | 責務 |
|------|------|
| `src/web/` | Angular フロントエンド（認証・Todo CRUD・リアルタイム表示） |
| `src/services/api/` | ASP.NET API（JWT 発行、Todo CRUD、Outbox へ書き込み） |
| `src/services/realtime/` | SignalR サービス（外部から publish を受けてクライアントへ通知） |
| `src/functions/pg-events/` | .NET Functions（PostgreSQL の NOTIFY をリッスンして SignalR へ publish） |
| `src/packages/contracts/` | DTO・Event schema（複数サービスで共有） |
| `src/db/` | DB スキーマ定義・マイグレーションスクリプト |

---

## 2. 標準ポート一覧

ローカル開発時に使用する標準ポート（一般的デフォルト採用）：

| サービス / ツール | ポート | 備考 |
|------------------|--------|------|
| Angular dev server | `4200` | `ng serve` デフォルト |
| ASP.NET API | `5000` | HTTP のみ（開発時） |
| SignalR realtime service | `5002` | HTTP のみ（開発時） |
| Azure Functions Core Tools | `7071` | `func start` デフォルト |
| PostgreSQL | `5432` | 標準ポート |
| Azurite (Blob) | `10000` | Azure Storage Emulator |
| Azurite (Queue) | `10001` | Azure Storage Emulator |
| Azurite (Table) | `10002` | Azure Storage Emulator |

**Note**: DevContainer / Docker Compose で起動する場合も、ホスト側からアクセスする際は上記ポートにマップする想定です。

---

## 3. 標準環境変数キー一覧

後続仕様で `.env.example` や `appsettings.json` に記載する際に使用する環境変数キー：

| キー | 用途 | 例（ローカル開発時） |
|------|------|---------------------|
| `DB_CONNECTION_STRING` | PostgreSQL 接続文字列（API / Realtime / Functions 共通） | `Host=localhost;Port=5432;Database=changelog;Username=postgres;Password=postgres` |
| `JWT_SIGNING_KEY` | JWT 署名鍵（API で発行、Realtime で検証） | `your-secret-key-here-change-in-production` |
| `AzureWebJobsStorage` | Functions ローカル実行用 Azurite 接続文字列 | `UseDevelopmentStorage=true` |
| `REALTIME_PUBLISH_URL` | Functions → Realtime への内部 publish エンドポイント | `http://localhost:5002/api/publish` |
| `REALTIME_PUBLISH_SECRET` | Functions → Realtime の内部認証用シークレット | `internal-publish-secret` |

**拡張方針**:  
- `JWT_ISSUER` / `JWT_AUDIENCE` など追加が必要になった場合、後続仕様で最小限追加してください。  
- `.env.example` の作成は INIT-002 で行います（上記キーを列挙）。

---

## 4. バージョン固定方針

### 方針
- **技術要件は "latest" を採用**するが、実装時のバージョンは以下で明示的に固定します：
  - .NET: `global.json`（SDK バージョン）
  - Angular: `package.json`（`@angular/cli` / `@angular/core` 等）
  - Node.js: `.nvmrc` または DevContainer の `FROM` イメージ
  - PostgreSQL: Docker Compose の `image` タグ

### 各言語・ツールの固定場所

| 対象 | 固定方法 | 配置ファイル |
|------|---------|-------------|
| .NET SDK | `global.json` | `src/` または各サービスルート |
| Angular CLI / Core | `package.json` | `src/web/` |
| Node.js | `.nvmrc` または DevContainer | `src/web/` またはルート |
| PostgreSQL | Docker Compose `image` | `.devcontainer/compose.yml` |
| Azurite | Docker Compose `image` | `.devcontainer/compose.yml` |

**Note**: .NET の **ソリューション構成**（単一 `.sln` / サービス別 / 共通 props）は、INIT-005（Auth API）、INIT-008（Realtime）、INIT-010（Functions）で実装時に確定します（本仕様ではスコープ外）。

---

## 5. 命名規約

### プロジェクト名
- .NET: `ChangeLogLab.<Service>`（例: `ChangeLogLab.Api`, `ChangeLogLab.Realtime`, `ChangeLogLab.Functions.PgEvents`）
- Angular: `change-log-lab-web`（`package.json` の `name`）

### Git ブランチ
- 実装時は `feat/INIT-xxx` または `feat/FEAT-xxx` 形式を推奨

### DB オブジェクト
- スキーマ定義と命名規約は INIT-003 で確定します

---

## 参照先

- このドキュメントは **INIT-001: リポジトリ骨格・基本ルール整備** の成果物です
- `.env.example` の実体は **INIT-002: DevContainer / Compose 環境構築** で作成されます
- DB スキーマは **INIT-003: DB スキーマ・マイグレーション** で定義されます
