# INIT-006 Todo API（List/Todo CRUD + 権限基盤）

## 目的
ユーザーがリスト単位でToDo管理できるAPIを提供する。

## 依存関係
- Depends on: INIT-003, INIT-004, INIT-005
- Blocks: INIT-007, INIT-009, INIT-011, INIT-012

## スコープ
- Lists
  - 作成/一覧/取得/更新/削除
- Todos
  - 作成/一覧/取得/更新/削除
- 権限
  - owner/editor: 書き込み可
  - viewer: 読み取りのみ（共有Issueで追加）

## 受け入れ条件（Acceptance Criteria）
- 認証必須でCRUDできる
- Listごとに所有者（owner）が設定され、最低限のアクセス制御がある
- 書き込み操作が `event_id` を持つOutboxイベント生成と接続できる設計になっている（実装はINIT-009/010）
