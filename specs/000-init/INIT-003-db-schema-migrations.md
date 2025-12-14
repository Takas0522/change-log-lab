# INIT-003 PostgreSQLスキーマ（schema分離）/マイグレーション基盤

## 目的
単一PostgreSQLの中で、サービス間の責務をスキーマ分離しつつ、テーブル/制約を初期定義する。

## 依存関係
- Depends on: INIT-001, INIT-002
- Blocks: INIT-005〜INIT-010

## 方針
- DBは単一インスタンス
- スキーマ分離（例）
  - `auth`（users, user_device_sessions など）
  - `todo`（lists, todos など）
  - `share`（list_access, list_invites など）
  - `outbox`（outbox_events など）

## 作業スコープ
- `specs/data-model.md` に論理モデル（テーブル/カラム/制約）を記載
- `src/db/migrations/` に初期DDL（方式はFlyway/Liquibase/DbUp/EF Core migrations等、別Issueで実装してもよいが最低限SQLで開始できる形）
- インデックス/一意制約
  - `(list_id, user_id)` の一意
  - `event_id` の一意

## 受け入れ条件（Acceptance Criteria）
- users / device sessions / lists / todos / access / invites / outbox が作成できる
- 参照整合性と最小限の一意制約が入っている
- ローカルpostgresへ適用する手順が `docs/local-dev.md` に追記されている
