# INIT-008 Realtime（SignalR）サービス

## 目的
クライアントへリアルタイム更新を配信する専用サービスを用意する。

## 依存関係
- Depends on: INIT-004, INIT-005
- Blocks: INIT-010, INIT-012

## 方針
- SignalRは別サービスとして分離
- クライアント接続はJWTで認証
- 配信は Functions から内部API経由でpublish（またはHubの管理API）

## スコープ
- Hub
  - 接続/再接続
  - user_id/list_id単位の購読（グループ）
- 内部publish API
  - Functionsから `event_id`/`list_id` 等を渡して配信できる

## 受け入れ条件（Acceptance Criteria）
- JWTが有効なユーザーが接続できる
- listにアクセス権があるユーザーにのみ、当該listのイベントが届く設計になっている
- publish APIは外部から叩けない前提（最低限: internal network/secret）を明記する
