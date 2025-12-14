# INIT-013 ローカルE2E検証チェック（手順/観点）

## 目的
ローカル環境で、要件（認証/共有/リアルタイム/DBトリガ）を一通り動作確認できる手順と観点を提供する。

## 依存関係
- Depends on: INIT-002, INIT-005, INIT-006, INIT-007, INIT-010, INIT-011, INIT-012
- Blocks: なし

## 作業スコープ
- 起動手順
  - DevContainer起動
  - `docker compose up`（postgres/azurite）
  - API/Realtime/Functions/Web の起動方法（compose or watch）
- シナリオ
  1. ユーザーA登録/ログイン
  2. List作成、Todo作成
  3. 既存ユーザーBをviewer招待→B受諾
  4. Bが参照できる/書き込みできない
  5. AがTodo更新→Bにリアルタイム反映
  6. Aがログアウト（この端末）→同JWTでAPIアクセスが401になる
  7. NOTIFY取りこぼし想定：Functions再起動→未処理Outboxが回収される

## 受け入れ条件（Acceptance Criteria）
- 上記シナリオがローカルで完走できる
- 失敗時に確認するログ/観測ポイント（event_id, request-id 等）が文書化されている
