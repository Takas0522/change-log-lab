# ユーザー間リスト共有機能 — アーキテクチャ設計書

| 項目 | 内容 |
|------|------|
| 文書番号 | FEAT-SHARE-ARCH-001 |
| バージョン | 0.1.0 |
| 作成日 | 2026-03-05 |
| 作成者 | — |
| ステータス | Draft |

---

## 改訂履歴

| バージョン | 日付 | 変更内容 | 作成者 |
|-----------|------|---------|--------|
| 0.1.0 | 2026-03-05 | 初版作成 | — |

---

## 目次

1. [はじめに](#1-はじめに)
2. [機能概要](#2-機能概要)
3. [アーキテクチャ概要](#3-アーキテクチャ概要)
4. [サービス間連携](#4-サービス間連携)
5. [招待ワークフロー](#5-招待ワークフロー)
6. [API 設計](#6-api-設計)
7. [権限モデル](#7-権限モデル)
8. [イベント駆動基盤への統合](#8-イベント駆動基盤への統合)
9. [フロントエンド変更点](#9-フロントエンド変更点)
10. [変更影響範囲](#10-変更影響範囲)

---

## 1. はじめに

### 1.1 目的

本文書は、ToDo アプリケーションにおける**ユーザー間リスト共有機能**のアーキテクチャ設計を定義する。現行システムはリスト作成者（owner）が直接メンバーを追加する方式であるが、本機能追加により招待 → 受諾/辞退のワークフローを導入し、安全かつ明示的な共有体験を提供する。

### 1.2 スコープ

| 区分 | 内容 |
|------|------|
| **対象範囲** | リスト単位の招待ワークフロー（招待送信・受諾・辞退・取消）、ロール付き共有（owner / editor / viewer）、招待一覧表示 |
| **対象外** | リンク共有（URL による不特定多数への公開）、グループ招待、外部ユーザー招待（未登録ユーザー） |

### 1.3 用語定義

| 用語 | 定義 |
|------|------|
| Inviter | 招待を送信するユーザー（owner または editor） |
| Invitee | 招待を受け取るユーザー（既存登録ユーザー） |
| Invitation | リストへの参加招待レコード。ステータス（pending / accepted / declined / cancelled）を持つ |
| List Member | 招待受諾後にリストへのアクセス権を付与された参加者 |

### 1.4 参照文書

| 文書 | 説明 |
|------|------|
| [specs/000-init/00-issue-map.md](../../specs/000-init/00-issue-map.md) | 初期実装の Issue マップ・全体アーキテクチャ |
| [specs/000-init/INIT-007-sharing-invites.md](../../specs/000-init/INIT-007-sharing-invites.md) | 共有（招待 viewer）API 初期仕様 |
| [docs/repo-layout.md](../repo-layout.md) | リポジトリレイアウト・ポート割当・環境変数 |
| [docs/new-feature/data-model.md](data-model.md) | 本機能のデータモデル設計書 |

---

## 2. 機能概要

### 2.1 現状の課題

現行の `POST /api/lists/{id}/members` エンドポイントは、owner が直接メンバーを追加する方式である。以下の課題がある。

| # | 課題 | 影響 |
|---|------|------|
| 1 | 被招待者の同意なくリストに追加される | ユーザー体験としてリスト一覧に覚えのないリストが突然表示される |
| 2 | 招待の履歴が残らない | 誰がいつ招待したか追跡できない |
| 3 | 辞退の手段がない | 不要なリストへの参加を拒否できず、自分で退出する必要がある |
| 4 | editor からの招待ができない | owner のみがメンバー追加可能であり、チーム運用で不便 |

### 2.2 解決方針

招待ワークフローを導入し、以下のフローを実現する。

```
Inviter                              Invitee
   │                                    │
   ├── POST /invites ──────────────────>│  招待送信（role 指定）
   │                                    │
   │   [pending 状態で保存]              │
   │                                    │
   │                                    ├── GET /invites/received
   │                                    │   (受信招待一覧)
   │                                    │
   │                    accept ────────>├── POST /invites/{id}/accept
   │                      or            │   → list_members へ追加
   │                    decline ───────>├── POST /invites/{id}/decline
   │                                    │
   ├── DELETE /invites/{id} ───────────>│  招待取消（pending のみ）
   │                                    │
```

### 2.3 ユースケース

| UC-ID | タイトル | アクター | 概要 |
|-------|---------|---------|------|
| UC-SHARE-01 | リスト共有の招待送信 | owner / editor | 既存ユーザーをユーザー検索で選択し、ロール（editor / viewer）を指定して招待する |
| UC-SHARE-02 | 招待の受諾 | invitee | 受信した招待を確認し、Accept することでリストのメンバーとして参加する |
| UC-SHARE-03 | 招待の辞退 | invitee | 不要な招待を Decline する。リストへのアクセス権は付与されない |
| UC-SHARE-04 | 招待の取消 | inviter | 送信済みの pending 招待をキャンセルする |
| UC-SHARE-05 | 受信招待一覧の確認 | invitee | 自分宛ての pending 招待をリストで確認する |
| UC-SHARE-06 | メンバー除外 | owner | 既存メンバーをリストから除外する |
| UC-SHARE-07 | メンバーロール変更 | owner | 既存メンバーのロール（editor ↔ viewer）を変更する |
| UC-SHARE-08 | リストからの退出 | editor / viewer | 自分自身がリストから離脱する |

---

## 3. アーキテクチャ概要

### 3.1 関連サービス構成

本機能は既存のマイクロサービス構成上に構築する。新規サービスの追加は行わない。

```
┌─────────────┐      ┌──────────────┐      ┌──────────────┐
│   Angular    │─────>│  BFF Service │─────>│ Todo Service │
│     Web      │      │  (:5000)     │      │  (:5001)     │
│  (:4200)     │      └──────┬───────┘      └──────┬───────┘
└─────────────┘              │                      │
                             │                      ├── list_invites (新規)
                             │                      ├── list_members (既存)
                             │                      └── outbox_events (既存)
                             │
                      ┌──────┴───────┐
                      │ User Service │
                      │  (:5002)     │
                      └──────────────┘
                             │
                             └── user_profiles (既存 / ユーザー検索用)
```

### 3.2 各サービスの責務

| サービス | 責務 |
|----------|------|
| **todo-service** | 招待の CRUD・ステータス遷移、`list_members` への追加・削除、Outbox イベント生成 |
| **user-service** | ユーザー検索 API（招待対象の選択に使用）。変更なし |
| **bff-service** | Angular 向けプロキシ。招待関連のルーティング追加 |
| **web (Angular)** | 招待送信 UI、受信招待一覧、メンバー管理 UI |

### 3.3 設計原則

| 原則 | 適用 |
|------|------|
| マイクロサービス境界の維持 | 招待・アクセス管理は todo-service 内に閉じる。user-service への書き込みは行わない |
| 既存権限モデルの踏襲 | owner / editor / viewer ロールを変更せず再利用する |
| Outbox パターンの活用 | 招待ステータス変更時に Outbox イベントを生成し、将来のリアルタイム通知基盤と統合可能にする |
| 冪等性の確保 | 同一ユーザーへの重複招待は `409 Conflict` で防止する |

---

## 4. サービス間連携

### 4.1 招待送信フロー

```
Angular ──> BFF ──> Todo Service ──> todo-db
                         │
                         ├── list_invites INSERT (status: pending)
                         └── outbox_events INSERT (invite_created)
```

1. Angular が `POST /api/lists/{listId}/invites` を BFF 経由で呼び出す
2. Todo Service が以下を検証する:
   - リストが存在すること
   - 呼び出し元ユーザーが owner または editor であること
   - 招待対象ユーザーが既にメンバーでないこと
   - 同一ユーザーへの pending 招待が存在しないこと
3. `list_invites` テーブルに `status: pending` で INSERT する
4. 同一トランザクション内で `outbox_events` に `invite_created` イベントを INSERT する

### 4.2 招待受諾フロー

```
Angular ──> BFF ──> Todo Service ──> todo-db
                         │
                         ├── list_invites UPDATE (status: accepted)
                         ├── list_members INSERT (role from invite)
                         └── outbox_events INSERT (invite_accepted)
```

1. Angular が `POST /api/invites/{inviteId}/accept` を BFF 経由で呼び出す
2. Todo Service が以下を検証する:
   - 招待が存在し、`status: pending` であること
   - 呼び出し元ユーザーが invitee であること
3. 同一トランザクション内で:
   - `list_invites` の status を `accepted` に更新する
   - `list_members` に新規レコードを INSERT する（role は招待時に指定された値）
   - `outbox_events` に `invite_accepted` イベントを INSERT する

### 4.3 ユーザー検索（招待対象選択）

```
Angular ──> BFF ──> User Service ──> user-db
```

招待対象ユーザーの選択には、既存の `GET /users/search?q={searchTerm}` を使用する。User Service への追加変更は不要。

---

## 5. 招待ワークフロー

### 5.1 ステータス遷移図

```
                 ┌─────────┐
      create ───>│ pending │
                 └────┬────┘
                      │
            ┌─────────┼─────────┐
            │         │         │
            v         v         v
      ┌──────────┐ ┌─────────┐ ┌───────────┐
      │ accepted │ │declined │ │ cancelled │
      └──────────┘ └─────────┘ └───────────┘
```

### 5.2 ステータス定義

| ステータス | 説明 | 遷移元 | 操作者 |
|-----------|------|--------|--------|
| `pending` | 招待送信済み・未応答 | — (初期状態) | inviter |
| `accepted` | 招待受諾。`list_members` にレコードが追加される | `pending` | invitee |
| `declined` | 招待辞退。アクセス権は付与されない | `pending` | invitee |
| `cancelled` | 招待取消。inviter による無効化 | `pending` | inviter |

### 5.3 ビジネスルール

| # | ルール | 説明 |
|---|--------|------|
| BR-01 | 招待対象は既存ユーザーのみ | `invitee_user_id` は user-service に存在するユーザーの UUID を指定する |
| BR-02 | 重複招待の禁止 | 同一リスト・同一ユーザーに対して `pending` 状態の招待が既に存在する場合、新規招待は `409 Conflict` |
| BR-03 | 既存メンバーへの招待禁止 | 対象ユーザーが既に `list_members` に存在する場合、招待は `409 Conflict` |
| BR-04 | 自己招待の禁止 | inviter と invitee が同一ユーザーの場合、`400 Bad Request` |
| BR-05 | owner への招待禁止 | リスト owner を招待対象にすることはできない。`400 Bad Request` |
| BR-06 | 終了状態からの遷移禁止 | `accepted` / `declined` / `cancelled` から他のステータスへの遷移は不可 |
| BR-07 | owner ロールの招待禁止 | 招待時に指定できるロールは `editor` または `viewer` のみ。`owner` は指定不可 |

---

## 6. API 設計

### 6.1 招待 API（Todo Service）

| メソッド | パス | 権限 | 説明 |
|---------|------|------|------|
| POST | `/api/lists/{listId}/invites` | owner / editor | 招待送信 |
| GET | `/api/lists/{listId}/invites` | owner / editor | リストの招待一覧取得（全ステータス） |
| DELETE | `/api/lists/{listId}/invites/{inviteId}` | inviter / owner | 招待取消（pending のみ） |
| GET | `/api/invites/received` | 認証済みユーザー | 自分宛ての pending 招待一覧 |
| POST | `/api/invites/{inviteId}/accept` | invitee | 招待受諾 |
| POST | `/api/invites/{inviteId}/decline` | invitee | 招待辞退 |

### 6.2 メンバー管理 API（Todo Service — 既存拡張）

| メソッド | パス | 権限 | 説明 | 変更種別 |
|---------|------|------|------|---------|
| GET | `/api/lists/{listId}/members` | owner / editor / viewer | メンバー一覧取得 | **新規** |
| PUT | `/api/lists/{listId}/members/{memberId}` | owner | メンバーロール変更 | **新規** |
| DELETE | `/api/lists/{listId}/members/{memberId}` | owner | メンバー除外 | 既存 |
| POST | `/api/lists/{listId}/members/leave` | editor / viewer | リストからの退出 | **新規** |

### 6.3 リクエスト / レスポンス定義

#### POST /api/lists/{listId}/invites

リクエスト (`CreateInviteRequest`):
```json
{
  "inviteeUserId": "uuid",
  "role": "editor"
}
```

バリデーション:
- `inviteeUserId`: 必須、UUID 形式
- `role`: 必須、`"editor"` | `"viewer"` のいずれか

レスポンス (`InviteResponse`, 201 Created):
```json
{
  "id": "uuid",
  "listId": "uuid",
  "listTitle": "プロジェクト A",
  "inviterUserId": "uuid",
  "inviteeUserId": "uuid",
  "role": "editor",
  "status": "pending",
  "createdAt": "2026-03-05T00:00:00Z",
  "respondedAt": null
}
```

#### GET /api/invites/received

レスポンス (`InviteResponse[]`, 200 OK):
```json
[
  {
    "id": "uuid",
    "listId": "uuid",
    "listTitle": "プロジェクト A",
    "inviterUserId": "uuid",
    "inviteeUserId": "uuid",
    "role": "editor",
    "status": "pending",
    "createdAt": "2026-03-05T00:00:00Z",
    "respondedAt": null
  }
]
```

#### POST /api/invites/{inviteId}/accept

レスポンス (`InviteResponse`, 200 OK):
```json
{
  "id": "uuid",
  "listId": "uuid",
  "listTitle": "プロジェクト A",
  "inviterUserId": "uuid",
  "inviteeUserId": "uuid",
  "role": "editor",
  "status": "accepted",
  "createdAt": "2026-03-05T00:00:00Z",
  "respondedAt": "2026-03-05T01:00:00Z"
}
```

#### POST /api/invites/{inviteId}/decline

レスポンス (`InviteResponse`, 200 OK): 同上（`status: "declined"`）

#### GET /api/lists/{listId}/members

レスポンス (`ListMemberDetailResponse[]`, 200 OK):
```json
[
  {
    "id": "uuid",
    "listId": "uuid",
    "userId": "uuid",
    "role": "editor",
    "createdAt": "2026-03-01T00:00:00Z",
    "updatedAt": "2026-03-01T00:00:00Z"
  }
]
```

#### PUT /api/lists/{listId}/members/{memberId}

リクエスト (`UpdateMemberRoleRequest`):
```json
{
  "role": "viewer"
}
```

バリデーション:
- `role`: 必須、`"editor"` | `"viewer"` のいずれか（`"owner"` への変更は不可）

レスポンス (`ListMemberDetailResponse`, 200 OK)

#### POST /api/lists/{listId}/members/leave

リクエスト: なし（認証トークンから `userId` を取得）

レスポンス: 204 No Content

### 6.4 エラーレスポンス

| HTTP ステータス | 条件 |
|----------------|------|
| 400 Bad Request | 自己招待、owner ロール指定、owner への招待、バリデーションエラー |
| 401 Unauthorized | 未認証 |
| 403 Forbidden | 権限不足（viewer が招待送信、invitee 以外が受諾等） |
| 404 Not Found | リスト・招待が見つからない |
| 409 Conflict | 重複招待、既存メンバーへの招待 |

### 6.5 BFF プロキシルーティング

| HTTP メソッド | BFF パス | プロキシ先 (Todo Service) |
|-------------|---------|--------------------------|
| POST | `/api/lists/{listId}/invites` | `/api/lists/{listId}/invites` |
| GET | `/api/lists/{listId}/invites` | `/api/lists/{listId}/invites` |
| DELETE | `/api/lists/{listId}/invites/{inviteId}` | `/api/lists/{listId}/invites/{inviteId}` |
| GET | `/api/invites/received` | `/api/invites/received` |
| POST | `/api/invites/{inviteId}/accept` | `/api/invites/{inviteId}/accept` |
| POST | `/api/invites/{inviteId}/decline` | `/api/invites/{inviteId}/decline` |
| GET | `/api/lists/{listId}/members` | `/api/lists/{listId}/members` |
| PUT | `/api/lists/{listId}/members/{memberId}` | `/api/lists/{listId}/members/{memberId}` |
| POST | `/api/lists/{listId}/members/leave` | `/api/lists/{listId}/members/leave` |

---

## 7. 権限モデル

### 7.1 招待操作の権限マトリクス

| 操作 | owner | editor | viewer | invitee | 未参加者 |
|------|:-----:|:------:|:------:|:-------:|:--------:|
| 招待送信 | OK | OK | 拒否（403） | — | 拒否（403） |
| リスト招待一覧取得 | OK | OK | 拒否（403） | — | 拒否（403） |
| 招待取消 | OK（全招待） | OK（自身の招待のみ） | 拒否（403） | — | 拒否（403） |
| 受信招待一覧 | — | — | — | OK | — |
| 招待受諾 | — | — | — | OK | 拒否（403） |
| 招待辞退 | — | — | — | OK | 拒否（403） |

### 7.2 メンバー管理の権限マトリクス

| 操作 | owner | editor | viewer | 未参加者 |
|------|:-----:|:------:|:------:|:--------:|
| メンバー一覧取得 | OK | OK | OK | 拒否（403） |
| メンバーロール変更 | OK | 拒否（403） | 拒否（403） | 拒否（403） |
| メンバー除外 | OK | 拒否（403） | 拒否（403） | 拒否（403） |
| リストからの退出 | 拒否（400）※ | OK | OK | 拒否（403） |

※ owner はリストから退出できない。owner を移譲する機能は本スコープ外。

### 7.3 editor の招待制約

editor が送信する招待は、自身と同等以下のロール（editor / viewer）のみ指定可能とする。将来的に owner がこの権限を制限するオプション（INIT-007 後続フェーズ）を追加可能な設計とする。

---

## 8. イベント駆動基盤への統合

### 8.1 Outbox イベント定義

既存の `outbox_events` テーブル・NOTIFY/LISTEN 基盤を活用し、以下のイベントタイプを追加する。

| イベントタイプ | 発火タイミング | payload 概要 |
|---------------|--------------|-------------|
| `invite_created` | 招待作成時 | `{ inviteId, listId, inviterUserId, inviteeUserId, role }` |
| `invite_accepted` | 招待受諾時 | `{ inviteId, listId, inviteeUserId, role }` |
| `invite_declined` | 招待辞退時 | `{ inviteId, listId, inviteeUserId }` |
| `invite_cancelled` | 招待取消時 | `{ inviteId, listId, inviteeUserId }` |
| `member_removed` | メンバー除外時 | `{ listId, userId }` |
| `member_role_changed` | ロール変更時 | `{ listId, userId, oldRole, newRole }` |
| `member_left` | リスト退出時 | `{ listId, userId }` |

### 8.2 リアルタイム通知（将来対応）

Outbox イベントは SignalR 経由でリアルタイム配信可能な設計とする。初期実装では Outbox テーブルへの記録のみとし、SignalR 配信は既存の INIT-009 〜 INIT-011 基盤の拡張として対応する。

想定される通知先:
- `invite_created` → invitee のデバイスへプッシュ
- `invite_accepted` → リストの既存メンバー全員へブロードキャスト
- `member_removed` → 除外されたユーザーのデバイスへプッシュ

---

## 9. フロントエンド変更点

### 9.1 新規コンポーネント

| コンポーネント | パス | 説明 |
|--------------|------|------|
| InviteDialogComponent | `src/app/components/invite-dialog/` | ユーザー検索 + ロール選択 + 招待送信ダイアログ |
| InvitesReceivedComponent | `src/app/components/invites-received/` | 受信招待一覧（Accept / Decline ボタン付き） |
| MemberListComponent | `src/app/components/member-list/` | リストメンバー一覧（ロール変更・除外操作） |

### 9.2 新規サービス

| サービス | パス | 説明 |
|---------|------|------|
| InviteService | `src/app/services/invite.service.ts` | 招待 API 呼び出し。Angular Signals パターンで状態管理 |

### 9.3 既存コンポーネント変更

| コンポーネント | 変更内容 |
|--------------|---------|
| ListDetailComponent | メンバー管理セクション追加、「招待」ボタン追加 |
| SidebarComponent / HeaderComponent | 受信招待バッジ（pending 件数）表示 |

### 9.4 ルーティング追加

| パス | コンポーネント | 説明 |
|------|--------------|------|
| `/invites` | InvitesReceivedComponent | 受信招待一覧ページ |

---

## 10. 変更影響範囲

### 10.1 新規ファイル一覧

| # | ファイルパス | 内容 |
|---|------------|------|
| 1 | `src/todo-service/api/Models/ListInvite.cs` | 招待エンティティ |
| 2 | `src/todo-service/api/Controllers/InvitesController.cs` | 招待 CRUD + 受諾/辞退 API |
| 3 | `src/todo-service/api/DTOs/InviteDtos.cs` | 招待関連 DTO |
| 4 | `src/todo-service/db/migrations/migration_add_invites.sql` | 招待テーブル作成マイグレーション |
| 5 | `src/bff-service/api/Controllers/InvitesController.cs` | BFF プロキシコントローラー |
| 6 | `src/web/src/app/services/invite.service.ts` | 招待 API サービス |
| 7 | `src/web/src/app/components/invite-dialog/` | 招待ダイアログコンポーネント |
| 8 | `src/web/src/app/components/invites-received/` | 受信招待一覧コンポーネント |
| 9 | `src/web/src/app/components/member-list/` | メンバー管理コンポーネント |

### 10.2 変更ファイル一覧

| # | ファイルパス | 変更内容 |
|---|------------|---------|
| 1 | `src/todo-service/api/Data/TodoDbContext.cs` | `DbSet<ListInvite>` 追加、`OnModelCreating` 拡張 |
| 2 | `src/todo-service/api/Models/List.cs` | `Invites` ナビゲーションプロパティ追加 |
| 3 | `src/todo-service/api/Controllers/ListsController.cs` | メンバー一覧・ロール変更・退出エンドポイント追加 |
| 4 | `src/todo-service/api/DTOs/TodoDtos.cs` | `ListMemberDetailResponse`, `UpdateMemberRoleRequest` 追加 |
| 5 | `src/bff-service/api/Controllers/ListsController.cs` (相当) | メンバー管理プロキシルート追加 |
| 6 | `src/web/src/app/models/index.ts` | Invite 関連モデル追加 |
| 7 | `src/web/src/app/components/list-detail/` | メンバー管理 UI・招待ボタン追加 |
| 8 | `src/web/src/app/app.routes.ts` | `/invites` ルート追加 |
