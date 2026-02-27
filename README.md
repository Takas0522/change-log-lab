# change-log-lab

GitHub Copilot を活用した開発プロセスの実験・学習用モノレポ。  
マイクロサービス構成の ToDo アプリケーションを中核に、CLI オーケストレーター、SRE エージェント、脆弱性スキャン、レガシーアプリなど複数のラボプロジェクトを含む。

---

## リポジトリ構成

| ディレクトリ | 概要 |
|---|---|
| [`src/`](#srcメインアプリケーション) | メインアプリケーション（マイクロサービス群 + Angular フロントエンド） |
| [`specs/`](#specs仕様設計) | 初期実装の Issue マップ・設計仕様 |
| [`docs/`](#docsドキュメント) | レイアウト定義・要求定義・詳細設計 |
| [`cli-orch-lab/`](#cli-orch-labcopilot-cli-orchestrator) | Copilot CLI エージェントによる開発プロセスオーケストレーター |
| [`sre-agent-lab/`](#sre-agent-labsre-エージェントラボ) | SRE エージェント向けスタンドアロン ToDo アプリ |
| [`vulnerability-site/`](#vulnerability-site脆弱性スキャンラボ) | 意図的脆弱性を含む Web アプリ + 自動スキャナー |
| [`legacy-app-demo/`](#legacy-app-demoレガシーアプリデモ) | ASP.NET WebForms (.NET Framework 4.8) レガシー ToDo アプリ |

---

## `src/` — メインアプリケーション

マイクロサービスアーキテクチャの共有 ToDo アプリケーション。JWT 認証、ロールベースアクセス制御、リアルタイム同期を備える。

### サービス一覧

| サービス | ポート | 技術スタック | 説明 |
|---|---|---|---|
| [auth-service](src/auth-service/api/README.md) | `:5000` | .NET 10 / EF Core / PostgreSQL | ユーザー登録・ログイン・ログアウト、JWT 認証（10 分有効期限）、デバイス別セッション管理 |
| [user-service](src/user-service/api/README.md) | `:5002` | .NET 10 / EF Core / PostgreSQL | ユーザープロフィール管理、招待用ユーザー検索 |
| [todo-service](src/todo-service/api/README.md) | `:5001` | .NET 10 / EF Core / PostgreSQL | リスト・ToDo の CRUD、ロールベース共有（owner/editor/viewer）、Outbox パターン |
| [bff-service](src/bff-service/api/README.md) | `:5000` | .NET 10 | BFF — Auth / User / Todo を集約し Angular 向け統合 API を提供 |
| [web](src/web/README.md) | `:4200` | Angular 21 / Tailwind CSS / SignalR | SPA フロントエンド — スタンドアロンコンポーネント、Signals 状態管理、リアルタイム同期 |

### 認証モデル

- JWT（有効期限 10 分、リフレッシュトークンなし）
- デバイスベースのログアウト（`device_sessions` テーブル）
- BCrypt によるパスワードハッシュ

---

## `specs/` — 仕様・設計

初期実装（INIT）の Issue マップと各機能の設計仕様を管理。

- [00-issue-map.md](specs/000-init/00-issue-map.md) — 全体アーキテクチャと Issue 一覧

### 実装フェーズ

| フェーズ | 内容 | Issue |
|---|---|---|
| **1 — マイクロサービス基盤** | Auth / User / Todo サービス | INIT-005, 006, 007 |
| **2 — 共有 & リアルタイム** | 共有・招待、SignalR、Outbox + NOTIFY/LISTEN | INIT-008 〜 011 |
| **3 — BFF & フロントエンド** | BFF 集約、Angular コア、リアルタイム UI | INIT-012 〜 014 |
| **4 — 検証** | ローカル E2E 検証 | INIT-015 |

---

## `docs/` — ドキュメント

| ドキュメント | 説明 |
|---|---|
| [repo-layout.md](docs/repo-layout.md) | ディレクトリ構成・ポート割当・環境変数・命名規約 |
| [要求定義/INDEX.md](docs/要求定義/INDEX.md) | SRS（ソフトウェア要求仕様書）インデックス — ISO/IEC/IEEE 29148 準拠 |
| [詳細設計/INDEX.md](docs/詳細設計/INDEX.md) | SDD（ソフトウェア設計記述）インデックス — IEEE 1016-2009 準拠 |
| [SRE-Agents-Demo/](docs/SRE-Agents-Demo/README.md) | SRE エージェントデモ用ドキュメント群 |

---

## `cli-orch-lab/` — Copilot CLI Orchestrator

GitHub Copilot CLI カスタムエージェントのみで構成された開発プロセスオーケストレーター。

**ワークフロー**: 要件定義 → タスク計画 → サマリ → 開発（TDD: テストシナリオ → テスト実装 → 本実装） → 統合テスト

- エージェント定義: `.github/agents/`
- 成果物出力: `work/output/`
- 状態管理: `work/status.yaml`

詳細: [cli-orch-lab/README.md](cli-orch-lab/README.md)

---

## `sre-agent-lab/` — SRE エージェントラボ

SRE エージェント向けのスタンドアロン ToDo アプリケーション。

- **バックエンド**: .NET 10 + EF Core + PostgreSQL（`:8080`）
- **フロントエンド**: Angular 21 + Tailwind CSS（`:4200`）
- **認証**: 独自 JWT 認証
- **開発環境**: Dev Container ベース

詳細: [sre-agent-lab/README.md](sre-agent-lab/README.md)

---

## `vulnerability-site/` — 脆弱性スキャンラボ

意図的に約 22 種の脆弱性を埋め込んだ Web アプリケーションと、CEH ベースの 7 フェーズ AI 自動スキャナー。

| コンポーネント | 技術 | ポート |
|---|---|---|
| Frontend | React | `:3000` |
| API | ASP.NET | `:5000` |
| Database | PostgreSQL | `:15432` |
| Scanner | OWASP ZAP / Nikto / sqlmap / Nmap / Trivy | — |

詳細: [vulnerability-site/README.md](vulnerability-site/README.md)

---

## `legacy-app-demo/` — レガシーアプリデモ

意図的にレガシー構成で構築された ToDo アプリケーション。

- **技術**: ASP.NET WebForms / .NET Framework 4.8 / SQL Server / Visual Studio 2019
- **機能**: タスク CRUD、フィルタリング、ソート、検索、優先度、期日、通知、コメント

詳細: [legacy-app-demo/docs/init.md](legacy-app-demo/docs/init.md)

---

## 標準ポート割当

| ポート | 用途 |
|---|---|
| `4200` | Angular Dev Server |
| `5000` | BFF / API |
| `5001` | Todo Service |
| `5002` | User Service / SignalR |
| `5432` | PostgreSQL |
| `7071` | Azure Functions |
| `10000–10002` | Azurite |

---

## 環境変数

| 変数名 | 用途 |
|---|---|
| `DB_CONNECTION_STRING` | PostgreSQL 接続文字列 |
| `JWT_SIGNING_KEY` | JWT 署名キー |

詳細は [docs/repo-layout.md](docs/repo-layout.md) を参照。
