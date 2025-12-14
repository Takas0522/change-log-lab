# INIT-010 .NET Functions（常駐LISTEN + outbox回収 + SignalR publish）

## 目的
PostgreSQLのNOTIFYを常駐でLISTENし、Outboxイベントを取得してRealtime（SignalR）へpublishする。

## 依存関係
- Depends on: INIT-008, INIT-009
- Blocks: INIT-012, INIT-013

## 方針
- Functionsは常駐（スケール/再起動を考慮しつつも、ローカルでは常時稼働前提）
- LISTEN専用コネクションと、Outbox取得用コネクションは分ける
- NOTIFY取りこぼしに備え、起動時/定期でOutboxをポーリングしてcatch-upする
- at-least-once配信を前提に event_id で冪等化する

## 作業スコープ
- `LISTEN <channel>` 受信
- 受信event_idでOutbox取得
- Realtimeサービスの内部publish APIへ送信
- 再接続（指数バックオフ）
- 未処理Outboxの回収（例: 一定件数ずつ）

## 受け入れ条件（Acceptance Criteria）
- NOTIFYを受けたら、対応するOutboxイベントを取得してpublishできる
- DB接続断後に自動復旧できる（少なくともプロセス継続で再接続）
- NOTIFYが欠落した場合でもOutbox回収で配信される

## ローカル要件
- Functions実行に必要な `AzureWebJobsStorage` は azurite を使用する（INIT-002）
