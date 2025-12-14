# INIT-001 リポジトリ骨格・基本ルール整備

## 目的
モノレポとして、Angular / ASP.NET / SignalR / Functions / DB を同一リポジトリで開発できる骨格を作る。

## 背景
初期状態で `docs/`, `specs/`, `src/` が空のため、以降のIssueが迷わず着手できる前提を整える。

## 依存関係
- Depends on: なし
- Blocks: INIT-002〜INIT-013

## 作業スコープ
- ディレクトリ方針の確定（例）
  - `src/web/`（Angular）
  - `src/services/api/`（ASP.NET API）
  - `src/services/realtime/`（SignalRサービス）
  - `src/functions/pg-events/`（.NET Functions：常駐LISTEN）
  - `src/packages/contracts/`（DTO/Event schema）
  - `src/db/`（マイグレーション/SQL）
- ルールの明文化（命名、ポート、環境変数キー）

## 受け入れ条件（Acceptance Criteria）
- 上記ディレクトリ方針が `docs/architecture.md` または `docs/repo-layout.md` に記載されている
- ローカル起動時に使う標準ポート・標準環境変数名が決まっている（後続Issueで参照可能）

## メモ
- 技術要件は「latest」を採用するが、実装時のバージョンは `global.json` / `package.json` 等で固定する方針を明記する
