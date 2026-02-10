# GitHub Actions 環境変数・シークレット一覧

本ドキュメントは、CI/CD ワークフロー（`.github/workflows/deploy.yml`）で必要となる **GitHub Actions の環境変数（Variables）** および **シークレット（Secrets）** の一覧です。

---

## Secrets（Repository secrets / Environment secrets）

GitHub リポジトリの **Settings → Secrets and variables → Actions** で設定してください。  
デプロイジョブは `environment: production` を使用するため、Environment secrets として `production` 環境に登録することを推奨します。

| 名前 | 用途 | 使用ジョブ | 例 |
|------|------|------------|-----|
| `APPINSIGHTS_CONNECTION_STRING` | Azure Application Insights の接続文字列。ビルド時に Web フロントエンドへ注入される | `build-web` | `InstrumentationKey=xxx;IngestionEndpoint=https://...` |
| `AZURE_API_PUBLISH_PROFILE` | Azure App Service (API) のパブリッシュプロファイル XML | `deploy-api` | Azure Portal の App Service → 発行プロファイルのダウンロードで取得 |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | Azure Static Web Apps のデプロイトークン | `deploy-web` | Azure Portal の Static Web Apps → デプロイトークンで取得 |
| `DATABASE_URL` | PostgreSQL データベースへの接続文字列（`psql` 形式） | `seed-database` | `postgresql://user:password@host:5432/dbname?sslmode=require` |
| `SEED_PASSWORD_HASH` | シードデータに埋め込むユーザーパスワードの BCrypt ハッシュ | `seed-database` | `$2a$11$...` |

---

## Variables（Repository variables / Environment variables）

GitHub リポジトリの **Settings → Secrets and variables → Actions → Variables** タブで設定してください。

| 名前 | 用途 | 使用ジョブ | 例 |
|------|------|------------|-----|
| `AZURE_API_APP_NAME` | デプロイ先の Azure App Service 名 | `deploy-api` | `my-todo-api` |

---

## ワークフローレベルの定数（env）

以下はワークフロー YAML 内にハードコードされている環境変数です。変更する場合は `deploy.yml` を直接編集してください。

| 名前 | 値 | 用途 |
|------|-----|------|
| `DOTNET_VERSION` | `10.0.x` | API ビルドに使用する .NET SDK バージョン |
| `NODE_VERSION` | `22` | Web ビルドに使用する Node.js バージョン |

---

## 設定手順の概要

1. **Azure リソースを作成** — App Service（API）、Static Web Apps（Web）、PostgreSQL データベース、Application Insights
2. **GitHub に `production` 環境を作成** — リポジトリの Settings → Environments → New environment
3. **Secrets を登録** — 上記テーブルの 5 つのシークレットを `production` 環境に追加
4. **Variables を登録** — `AZURE_API_APP_NAME` をリポジトリ変数または `production` 環境変数として追加
5. **ワークフローを実行** — `main` ブランチへの push、または Actions タブから手動実行（workflow_dispatch）
