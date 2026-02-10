# GitHub Actions 環境変数・シークレット一覧

本ドキュメントは、CI/CD ワークフロー（`.github/workflows/deploy.yml`）で必要となる **GitHub Actions の環境変数（Variables）** および **シークレット（Secrets）** の一覧です。

---

## Secrets（Repository secrets / Environment secrets）

GitHub リポジトリの **Settings → Secrets and variables → Actions** で設定してください。  
デプロイジョブは `environment: production` を使用するため、Environment secrets として `production` 環境に登録することを推奨します。

| 名前 | 用途 | 使用ジョブ | 例 |
|------|------|------------|-----|
| `AZURE_CLIENT_ID` | Microsoft Entra アプリケーション（サービスプリンシパル）のクライアント ID。OpenID Connect 認証に使用 | `deploy-api`, `deploy-web` | `00000000-0000-0000-0000-000000000000` |
| `AZURE_TENANT_ID` | Microsoft Entra ID のテナント（ディレクトリ）ID | `deploy-api`, `deploy-web` | `00000000-0000-0000-0000-000000000000` |
| `AZURE_SUBSCRIPTION_ID` | Azure サブスクリプション ID | `deploy-api`, `deploy-web` | `00000000-0000-0000-0000-000000000000` |
| `APPINSIGHTS_CONNECTION_STRING` | Azure Application Insights の接続文字列。ビルド時に Web フロントエンドへ注入される | `build-web` | `InstrumentationKey=xxx;IngestionEndpoint=https://...` |
| `DATABASE_URL` | PostgreSQL データベースへの接続文字列（`psql` 形式） | `seed_database` | `postgresql://user:password@host:5432/dbname?sslmode=require` |
| `SEED_PASSWORD_HASH` | シードデータに埋め込むユーザーパスワードの BCrypt ハッシュ | `seed_database` | `$2a$11$...` |

---

## Variables（Repository variables / Environment variables）

GitHub リポジトリの **Settings → Secrets and variables → Actions → Variables** タブで設定してください。

| 名前 | 用途 | 使用ジョブ | 例 |
|------|------|------------|-----|
| `AZURE_API_APP_NAME` | デプロイ先の Azure App Service 名（API） | `deploy-api` | `my-todo-api` |
| `AZURE_WEB_APP_NAME` | デプロイ先の Azure App Service 名（Web） | `deploy-web` | `my-todo-web` |

---

## ワークフローレベルの定数（env）

以下はワークフロー YAML 内にハードコードされている環境変数です。変更する場合は `deploy.yml` を直接編集してください。

| 名前 | 値 | 用途 |
|------|-----|------|
| `DOTNET_VERSION` | `10.0.x` | API ビルドに使用する .NET SDK バージョン |
| `NODE_VERSION` | `22` | Web ビルドに使用する Node.js バージョン |

---

## 設定手順の概要

1. **Azure リソースを作成** — App Service（API）、App Service（Web）、PostgreSQL データベース、Application Insights
2. **Microsoft Entra ID でアプリ登録・フェデレーション資格情報を構成**
   - `az ad app create --display-name myApp` でアプリ登録
   - `az ad sp create --id <appId>` でサービスプリンシパルを作成
   - 各 App Service に対して `Website Contributor` ロールを割り当て
   - フェデレーション資格情報を作成（subject: `repo:<org>/<repo>:environment:production`）
   - 参考: [GitHub Actions で Azure に接続する (OpenID Connect)](https://learn.microsoft.com/ja-jp/azure/developer/github/connect-from-azure)
3. **GitHub に `production` 環境を作成** — リポジトリの Settings → Environments → New environment
4. **Secrets を登録** — 上記テーブルの 6 つのシークレットを `production` 環境に追加
5. **Variables を登録** — `AZURE_API_APP_NAME` と `AZURE_WEB_APP_NAME` をリポジトリ変数または `production` 環境変数として追加
6. **ワークフローを実行** — `main` ブランチへの push、または Actions タブから手動実行（workflow_dispatch）
