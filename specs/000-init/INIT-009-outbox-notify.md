# INIT-009 Outbox + NOTIFY（event_idのみ）実装

## 目的
DB変更をトリガーにサービス間連携・リアルタイム配信を確実に起動できるよう、OutboxとNOTIFYを実装する。

## 依存関係
- Depends on: INIT-003, INIT-004, INIT-006, INIT-007
- Blocks: INIT-010, INIT-012, INIT-013

## 仕様（確定事項）
- NOTIFYチャネルは単一
- NOTIFY payload は `event_id` のみ
- 取りこぼし回復のためOutboxを正とする

## 作業スコープ
- `outbox.outbox_events`
  - `event_id`（UUID等、一意）
  - `event_type`（例: `ListChanged`）
  - `list_id`（対象list）
  - `payload`（必要最小、サイズに注意）
  - `created_at`
  - `processed_at` / `status` / `attempt_count` 等
- 書き込み操作（Todo/Share関連）でOutbox行を追加
- 同一トランザクション内で `NOTIFY <channel>, '<event_id>'`

## 受け入れ条件（Acceptance Criteria）
- Todoの作成/更新/削除や招待受諾などでOutboxにイベントが積まれる
- NOTIFYが発火し、LISTEN側（Functions）がevent_idを受け取れる
- NOTIFY未受信でも、Outboxの未処理イベントを回収可能な状態になっている（回収はINIT-010で実装）

## 注意点
- NOTIFY payloadは大きなJSONを載せない（上限の都合）。必須はevent_idのみ。
- 重複配信は起き得る前提で event_id による冪等性を担保する。
