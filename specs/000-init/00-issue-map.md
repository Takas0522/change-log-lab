# 000-init: Issue Map（初期実装）

このディレクトリ配下のMarkdownは、そのままGitHub Issue本文として貼り付けできる粒度で分割しています。

## 前提（確定事項）

### アーキテクチャ概要

マイクロサービス構成で、各サービスが独立したデータベースを持つ：

- **Auth Service**: ユーザー認証・認可を担当
  - Backend: ASP.NET（latest）
  - Database: PostgreSQL（auth-db）
  
- **Todo Service**: ToDoのCRUD・権限管理を担当
  - Backend: ASP.NET（latest）
  - Database: PostgreSQL（todo-db）
  
- **User Service**: ユーザープロフィール・招待管理を担当
  - Backend: ASP.NET（latest）
  - Database: PostgreSQL（user-db）

- **BFF Service**: フロントエンド向けの統合APIを提供
  - Backend: ASP.NET（latest）
  - 各マイクロサービスのAPIを集約し、フロントエンドに最適化されたAPIを提供
  
- **Realtime Service**: リアルタイム通信を担当
  - SignalR: .NET（latest）
  - 複数デバイス間の同期を実現

- **Functions**: DBイベント処理を担当
  - .NET（常駐、LISTEN）
  - PostgreSQL NOTIFY/LISTENでイベントを取得

### 認証・セキュリティ

- 認証: 自前ユーザーDB + JWT（有効期限10分）
- Refresh Token: なし
- ログアウト: 端末（device）単位で失効
- device_id: WebのlocalStorageに保存

### 機能仕様

- ToDo: リスト単位（List）で管理
- 共有: 既存ユーザー招待方式 / viewer（参照のみ）
- 複数デバイス同期: 専用SignalRサービスへ配信して反映
- 連携: PostgreSQL変更をトリガー（NOTIFY/LISTEN）にFunctionsで実行
- NOTIFY: 単一チャネル、payloadは event_id のみ
- 冪等性: event_idで重複排除可能にする
- ローカル検証: DevContainer + docker-compose

### フロントエンド

- Framework: Angular（latest）
- BFF経由で各サービスにアクセス

## Issue一覧と依存関係

### Phase 1: マイクロサービス基盤構築

- **INIT-005 Auth Service + DB構築（ユーザー登録/ログイン/JWT/端末別ログアウト）**
  - auth-serviceディレクトリ構成
  - PostgreSQL（auth-db）のスキーマ設計・マイグレーション
  - ユーザー登録・ログインAPI
  - JWT発行・検証
  - デバイス管理・ログアウトAPI
  - Depends on: なし

- **INIT-006 User Service + DB構築（ユーザープロフィール・招待管理）**
  - user-serviceディレクトリ構成
  - PostgreSQL（user-db）のスキーマ設計・マイグレーション
  - プロフィール管理API
  - ユーザー検索API（招待用）
  - Depends on: INIT-005（JWT検証）

- **INIT-007 Todo Service + DB構築（List/Todo CRUD + 権限基盤）**
  - todo-serviceディレクトリ構成
  - PostgreSQL（todo-db）のスキーマ設計・マイグレーション
  - List CRUD API
  - Todo CRUD API
  - 権限管理（Owner/Viewer）
  - Depends on: INIT-005（JWT検証）

### Phase 2: 共有・リアルタイム機能

- **INIT-008 共有（招待・受諾）機能実装**
  - User ServiceとTodo Serviceの連携
  - 招待API（Todo Service）
  - 受諾API（Todo Service + User Service連携）
  - アクセス管理（viewer権限）
  - Depends on: INIT-006, INIT-007

- **INIT-009 Realtime（SignalR）サービス構築**
  - realtime-serviceディレクトリ構成
  - SignalR Hubの実装
  - 認証統合（JWT検証）
  - チャネル管理（List単位）
  - Depends on: INIT-005

- **INIT-010 Outbox + NOTIFY（event_idのみ）実装**
  - Todo ServiceのOutboxテーブル設計
  - トランザクション内でのOutbox書き込み
  - NOTIFY発火実装
  - event_idによる冪等性保証
  - Depends on: INIT-007, INIT-008

- **INIT-011 Functions（常駐LISTEN + outbox回収 + SignalR publish）**
  - functionsディレクトリ構成
  - PostgreSQL LISTENの実装
  - Outbox polling/回収
  - SignalRへのpublish
  - エラーハンドリング・リトライ
  - Depends on: INIT-009, INIT-010

### Phase 3: BFF・フロントエンド

- **INIT-012 BFF Service構築（API集約・最適化）**
  - bff-serviceディレクトリ構成
  - Auth Service連携（認証・ログイン）
  - User Service連携（プロフィール・検索）
  - Todo Service連携（List/Todo CRUD・共有）
  - Realtime Service連携（WebSocket接続情報）
  - GraphQL or REST APIの提供
  - Depends on: INIT-005, INIT-006, INIT-007, INIT-008, INIT-009

- **INIT-013 Angular Web Core（認証・List/Todo管理・招待UI）**
  - webディレクトリ構成（既存）の拡張
  - ログイン・ユーザー登録画面
  - List一覧・作成・編集画面
  - Todo CRUD画面
  - 招待・共有管理画面
  - BFF経由でのAPI呼び出し
  - Depends on: INIT-012

- **INIT-014 Angular Web Realtime（SignalR購読 + リアルタイム反映）**
  - SignalRクライアント統合
  - リアルタイム更新の反映
  - 接続管理・再接続ロジック
  - デバイス間同期の動作確認
  - Depends on: INIT-009, INIT-011, INIT-013

### Phase 4: 検証

- **INIT-015 ローカルE2E検証チェック（手順/観点）**
  - docker-compose.ymlの完成
  - 全サービス起動手順の文書化
  - E2Eテストシナリオ作成
  - 手動検証チェックリスト
  - Depends on: INIT-005, INIT-006, INIT-007, INIT-008, INIT-011, INIT-013, INIT-014

## 目標（Definition of Done）

- マイクロサービスごとに独立したディレクトリ・DBが存在する
- BFFが各サービスを集約し、フロントエンドに統一されたAPIを提供する
- DevContainer上で `docker compose up` で全サービスが起動する
- ユーザー登録→ログイン→List/Todo管理ができる
- viewer招待（既存ユーザー）→viewerは参照のみで閲覧できる
- 別ブラウザ（別device_id）での変更がSignalR経由でリアルタイム反映される
- DB変更通知は NOTIFY/LISTEN + Outbox で取りこぼしを回復できる
- すべてのサービスが独立してスケーラブルな構成になっている
