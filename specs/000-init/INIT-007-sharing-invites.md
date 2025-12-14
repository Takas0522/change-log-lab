# INIT-007 共有（招待viewer）API（招待/受諾/アクセス管理）

## 目的
リストを既存ユーザーへ招待して、viewer（参照のみ）で共有できるようにする。

## 依存関係
- Depends on: INIT-003, INIT-005, INIT-006
- Blocks: INIT-009, INIT-011, INIT-012

## 仕様（確定事項）
- 共有はリスト単位
- 招待対象は既存ユーザーのみ（invitee_user_id指定）
- 共有権限は viewer（参照のみ）

## データモデル（最小）
- `share.list_access(list_id, user_id, role)`
- `share.list_invites(id, list_id, inviter_user_id, invitee_user_id, status)`

## エンドポイント（最小）
- `POST /lists/{listId}/invites`（viewer招待）
- `POST /invites/{inviteId}/accept`（招待受諾）
- `DELETE /invites/{inviteId}`（取り消し）
- `DELETE /lists/{listId}/access/{userId}`（viewer剥奪）

## 受け入れ条件（Acceptance Criteria）
- owner/editorがviewer招待できる
- viewerはリスト/ToDoを参照できるが、作成/更新/削除は403になる
- 招待/受諾/剥奪がOutboxイベント生成と接続できる設計になっている（実装はINIT-009/010）
