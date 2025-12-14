# 000-init: Issue Map（初期実装）

このディレクトリ配下のMarkdownは、そのままGitHub Issue本文として貼り付けできる粒度で分割しています。

## 前提（確定事項）
- Frontend: Angular（latest）
- Backend: ASP.NET（latest）
- Database: PostgreSQL
- Functions: .NET（常駐、LISTEN）
- 認証: 自前ユーザーDB + JWT（有効期限10分）
- Refresh Token: なし
- ログアウト: 端末（device）単位で失効
- device_id: WebのlocalStorageに保存
- ToDo: リスト単位（List）
- 共有: 既存ユーザー招待方式 / viewer（参照のみ）
- 複数デバイス同期: 専用SignalRサービスへ配信して反映
- 連携: PostgreSQL変更をトリガー（NOTIFY/LISTEN）にFunctionsで実行
- NOTIFY: 単一チャネル、payloadは event_id のみ
- 冪等性: event_idで重複排除可能にする
- ローカル検証: DevContainer + docker-compose

## Issue一覧と依存関係
- INIT-001 リポジトリ骨格・基本ルール整備
  - Depends on: なし
- INIT-002 DevContainer + docker-compose（postgres/azurite）基盤
  - Depends on: INIT-001
- INIT-003 PostgreSQLスキーマ（schema分離）/マイグレーション基盤
  - Depends on: INIT-001, INIT-002
- INIT-004 共有契約（DTO/Event schema）パッケージ
  - Depends on: INIT-001
- INIT-005 Auth API（ユーザー登録/ログイン/JWT/端末別ログアウト）
  - Depends on: INIT-003, INIT-004
- INIT-006 Todo API（List/Todo CRUD + 権限基盤）
  - Depends on: INIT-003, INIT-004, INIT-005
- INIT-007 共有（招待viewer）API（招待/受諾/アクセス管理）
  - Depends on: INIT-003, INIT-005, INIT-006
- INIT-008 Realtime（SignalR）サービス
  - Depends on: INIT-004, INIT-005
- INIT-009 Outbox + NOTIFY（event_idのみ）実装
  - Depends on: INIT-003, INIT-004, INIT-006, INIT-007
- INIT-010 .NET Functions（常駐LISTEN + outbox回収 + SignalR publish）
  - Depends on: INIT-008, INIT-009
- INIT-011 Angular Web（ログイン + List/Todo管理 + 招待UI）
  - Depends on: INIT-005, INIT-006, INIT-007
- INIT-012 Angular Web（SignalR購読 + リアルタイム反映）
  - Depends on: INIT-008, INIT-010, INIT-011
- INIT-013 ローカルE2E検証チェック（手順/観点）
  - Depends on: INIT-002, INIT-005, INIT-006, INIT-007, INIT-010, INIT-011, INIT-012

## 目標（Definition of Done）
- DevContainer上で `docker compose up` と開発コマンドの起動ができる
- ユーザー登録→ログイン→List/Todo管理ができる
- viewer招待（既存ユーザー）→viewerは参照のみで閲覧できる
- 別ブラウザ（別device_id）での変更がSignalR経由でリアルタイム反映される
- DB変更通知は NOTIFY/LISTEN + Outbox で取りこぼしを回復できる
