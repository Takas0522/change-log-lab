# ユーザー間リスト共有機能 — データモデル設計書

| 項目 | 内容 |
|------|------|
| 文書番号 | FEAT-SHARE-DATA-001 |
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
2. [ER図](#2-er図)
3. [テーブル定義](#3-テーブル定義)
4. [マイグレーション SQL](#4-マイグレーション-sql)
5. [バックエンド Model クラス](#5-バックエンド-model-クラス)
6. [DTO 定義](#6-dto-定義)
7. [DbContext 変更](#7-dbcontext-変更)
8. [データ整合性ルール](#8-データ整合性ルール)
9. [シードデータ](#9-シードデータ)

---

## 1. はじめに

### 1.1 目的

本文書は、ユーザー間リスト共有機能で追加・変更されるデータモデルを定義する。対象データベースは todo-service 管轄の PostgreSQL（todo-db）である。

### 1.2 スコープ

| 区分 | 内容 |
|------|------|
| 新規テーブル | `list_invites`（招待ワークフロー管理） |
| 既存テーブル変更 | なし（`list_members`, `lists` のスキーマ変更は不要） |
| インデックス追加 | `list_invites` に対する検索最適化インデックス |

### 1.3 参照文書

| 文書 | 説明 |
|------|------|
| [architecture.md](architecture.md) | 本機能のアーキテクチャ設計書 |
| [src/todo-service/db/schema.sql](../../src/todo-service/db/schema.sql) | 既存の todo-db スキーマ定義 |

---

## 2. ER図

### 2.1 全体 ER図

```
┌──────────────────────┐
│       lists           │
│──────────────────────│
│ id          UUID  PK  │
│ title       VARCHAR   │
│ description TEXT       │
│ owner_id    UUID      │
│ created_at  TIMESTAMPTZ│
│ updated_at  TIMESTAMPTZ│
└──────────┬───────────┘
           │
           │ 1
           │
     ┌─────┴─────┐
     │            │
     │ N          │ N
     v            v
┌────────────┐  ┌────────────────────────┐
│ list_members│  │    list_invites (新規)  │
│────────────│  │────────────────────────│
│ id      PK │  │ id               PK    │
│ list_id FK │  │ list_id          FK    │
│ user_id    │  │ inviter_user_id        │
│ role       │  │ invitee_user_id        │
│ created_at │  │ role                   │
│ updated_at │  │ status                 │
└────────────┘  │ created_at             │
                │ responded_at           │
     ┌──────────┘
     │ 1
     │
     │ N
     v
┌────────────┐
│   todos     │
│────────────│
│ id      PK │
│ list_id FK │
│ title      │
│ ...        │
└────────────┘
```

### 2.2 招待とメンバーの関係

```
list_invites (status: pending)
        │
        │  accept
        v
list_members (role = invite.role)
```

- 招待受諾時: `list_invites.status` → `accepted`, 同時に `list_members` へ INSERT
- 招待辞退時: `list_invites.status` → `declined`, `list_members` への影響なし
- 招待取消時: `list_invites.status` → `cancelled`, `list_members` への影響なし

---

## 3. テーブル定義

### 3.1 list_invites テーブル（新規）

| カラム名 | 型 | 制約 | 説明 |
|----------|------|------|------|
| `id` | `UUID` | `PK`, `DEFAULT gen_random_uuid()` | 招待レコード一意識別子 |
| `list_id` | `UUID` | `FK → lists(id) ON DELETE CASCADE`, `NOT NULL` | 招待対象リスト ID |
| `inviter_user_id` | `UUID` | `NOT NULL` | 招待送信者のユーザー ID（user-service 参照） |
| `invitee_user_id` | `UUID` | `NOT NULL` | 招待受信者のユーザー ID（user-service 参照） |
| `role` | `VARCHAR(50)` | `NOT NULL`, `CHECK (role IN ('editor', 'viewer'))` | 招待時に指定されたロール |
| `status` | `VARCHAR(50)` | `NOT NULL`, `DEFAULT 'pending'`, `CHECK (status IN ('pending', 'accepted', 'declined', 'cancelled'))` | 招待ステータス |
| `created_at` | `TIMESTAMP WITH TIME ZONE` | `NOT NULL`, `DEFAULT NOW()` | 招待作成日時 |
| `responded_at` | `TIMESTAMP WITH TIME ZONE` | `NULL` | 受諾・辞退・取消日時 |

#### ユニーク制約

```sql
UNIQUE(list_id, invitee_user_id) WHERE status = 'pending'
```

> 部分一意インデックス（Partial Unique Index）を使用。同一リスト・同一ユーザーに対して `pending` 状態の招待は 1 件のみ許可する。過去に `declined` / `cancelled` された招待は再送可能。

#### インデックス

| インデックス名 | カラム | 条件 | 用途 |
|---------------|--------|------|------|
| `idx_list_invites_list` | `list_id` | — | リスト別招待一覧取得 |
| `idx_list_invites_invitee_pending` | `invitee_user_id` | `WHERE status = 'pending'` | 受信招待一覧取得（pending のみ） |
| `idx_list_invites_inviter` | `inviter_user_id` | — | 送信招待一覧取得 |
| `uq_list_invites_pending` | `(list_id, invitee_user_id)` | `WHERE status = 'pending'` | 重複 pending 招待の防止 |

### 3.2 既存テーブルとの関係

#### lists テーブル（変更なし）

`list_invites.list_id` が `lists.id` を外部キーとして参照する。`ON DELETE CASCADE` により、リスト削除時に関連する招待レコードも自動削除される。

#### list_members テーブル（変更なし）

スキーマの変更は不要。招待受諾時に `list_members` へビジネスロジックで INSERT する。既存の `role` カラム（`owner` / `editor` / `viewer`）をそのまま活用する。

#### outbox_events テーブル（変更なし）

スキーマの変更は不要。招待関連イベントを既存の `event_type` カラムで区分する（`invite_created`, `invite_accepted` 等）。

---

## 4. マイグレーション SQL

### 4.1 migration_add_invites.sql

ファイル配置先: `src/todo-service/db/migrations/migration_add_invites.sql`

```sql
BEGIN;

-- =============================================================
-- list_invites: 招待ワークフロー管理テーブル
-- =============================================================
CREATE TABLE IF NOT EXISTS list_invites (
    id                UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    list_id           UUID        NOT NULL REFERENCES lists(id) ON DELETE CASCADE,
    inviter_user_id   UUID        NOT NULL,
    invitee_user_id   UUID        NOT NULL,
    role              VARCHAR(50) NOT NULL CHECK (role IN ('editor', 'viewer')),
    status            VARCHAR(50) NOT NULL DEFAULT 'pending'
                                  CHECK (status IN ('pending', 'accepted', 'declined', 'cancelled')),
    created_at        TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    responded_at      TIMESTAMP WITH TIME ZONE
);

-- リスト別招待一覧取得
CREATE INDEX IF NOT EXISTS idx_list_invites_list
    ON list_invites(list_id);

-- 受信招待一覧取得（pending のみ）
CREATE INDEX IF NOT EXISTS idx_list_invites_invitee_pending
    ON list_invites(invitee_user_id)
    WHERE status = 'pending';

-- 送信招待一覧取得
CREATE INDEX IF NOT EXISTS idx_list_invites_inviter
    ON list_invites(inviter_user_id);

-- 重複 pending 招待の防止（部分一意インデックス）
CREATE UNIQUE INDEX IF NOT EXISTS uq_list_invites_pending
    ON list_invites(list_id, invitee_user_id)
    WHERE status = 'pending';

COMMIT;
```

### 4.2 ロールバック SQL

```sql
BEGIN;

DROP INDEX IF EXISTS uq_list_invites_pending;
DROP INDEX IF EXISTS idx_list_invites_inviter;
DROP INDEX IF EXISTS idx_list_invites_invitee_pending;
DROP INDEX IF EXISTS idx_list_invites_list;
DROP TABLE IF EXISTS list_invites;

COMMIT;
```

### 4.3 マイグレーション適用手順

```bash
# todo-db に接続してマイグレーションを実行
psql -U postgres -d todo_db -f src/todo-service/db/migrations/migration_add_invites.sql
```

> マイグレーションは既存テーブルのスキーマを変更しないため、既存データへの影響はない。

---

## 5. バックエンド Model クラス

### 5.1 新規: ListInvite.cs

ファイル配置先: `src/todo-service/api/Models/ListInvite.cs`

```csharp
namespace TodoApi.Models;

public class ListInvite
{
    public Guid Id { get; set; }
    public Guid ListId { get; set; }
    public Guid InviterUserId { get; set; }
    public Guid InviteeUserId { get; set; }
    public string Role { get; set; } = "viewer";  // editor, viewer
    public string Status { get; set; } = "pending";  // pending, accepted, declined, cancelled
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }

    // Navigation properties
    public List List { get; set; } = null!;
}
```

### 5.2 変更: List.cs

`Invites` ナビゲーションプロパティを追加する。

```csharp
namespace TodoApi.Models;

public class List
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Todo> Todos { get; set; } = new List<Todo>();
    public ICollection<ListMember> Members { get; set; } = new List<ListMember>();
    public ICollection<ListInvite> Invites { get; set; } = new List<ListInvite>();  // 追加
}
```

---

## 6. DTO 定義

### 6.1 新規 DTO: InviteDtos.cs

ファイル配置先: `src/todo-service/api/DTOs/InviteDtos.cs`

```csharp
namespace TodoApi.DTOs;

// 招待作成リクエスト
public record CreateInviteRequest(
    Guid InviteeUserId,
    string Role  // editor, viewer
);

// 招待レスポンス
public record InviteResponse(
    Guid Id,
    Guid ListId,
    string ListTitle,
    Guid InviterUserId,
    Guid InviteeUserId,
    string Role,
    string Status,
    DateTime CreatedAt,
    DateTime? RespondedAt
);
```

### 6.2 既存 DTO 追加: TodoDtos.cs

以下の DTO を `TodoDtos.cs` に追加する。

```csharp
// メンバー詳細レスポンス（既存 ListMemberResponse の拡張版）
public record ListMemberDetailResponse(
    Guid Id,
    Guid ListId,
    Guid UserId,
    string Role,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
```

> `UpdateMemberRoleRequest` は既存定義済み。

---

## 7. DbContext 変更

### 7.1 DbSet 追加

```csharp
public DbSet<ListInvite> ListInvites { get; set; } = null!;
```

### 7.2 OnModelCreating 拡張

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // ... 既存設定 ...

    // ListInvite エンティティ設定
    modelBuilder.Entity<ListInvite>(entity =>
    {
        entity.ToTable("list_invites");

        entity.HasKey(e => e.Id);

        entity.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        entity.Property(e => e.ListId)
            .HasColumnName("list_id")
            .IsRequired();

        entity.Property(e => e.InviterUserId)
            .HasColumnName("inviter_user_id")
            .IsRequired();

        entity.Property(e => e.InviteeUserId)
            .HasColumnName("invitee_user_id")
            .IsRequired();

        entity.Property(e => e.Role)
            .HasColumnName("role")
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(e => e.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
            .HasDefaultValue("pending")
            .IsRequired();

        entity.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        entity.Property(e => e.RespondedAt)
            .HasColumnName("responded_at");

        // Relationships
        entity.HasOne(e => e.List)
            .WithMany(l => l.Invites)
            .HasForeignKey(e => e.ListId)
            .OnDelete(DeleteBehavior.Cascade);

        // Partial unique index (pending invites only)
        entity.HasIndex(e => new { e.ListId, e.InviteeUserId })
            .HasFilter("status = 'pending'")
            .IsUnique()
            .HasDatabaseName("uq_list_invites_pending");
    });
}
```

---

## 8. データ整合性ルール

### 8.1 参照整合性

| 制約 | 定義 | 説明 |
|------|------|------|
| `list_invites.list_id → lists.id` | `ON DELETE CASCADE` | リスト削除時に関連招待を自動削除 |
| `list_invites.role` | `CHECK (role IN ('editor', 'viewer'))` | owner ロールでの招待を DB レベルで防止 |
| `list_invites.status` | `CHECK (status IN ('pending', 'accepted', 'declined', 'cancelled'))` | 不正なステータス値を DB レベルで防止 |

### 8.2 ビジネス整合性（アプリケーションレイヤー）

| # | ルール | 検証箇所 |
|---|--------|---------|
| 1 | `inviter_user_id` はリストの owner または editor であること | `InvitesController.CreateInvite()` |
| 2 | `invitee_user_id` はリストの owner でないこと | `InvitesController.CreateInvite()` |
| 3 | `invitee_user_id` は `list_members` に存在しないこと | `InvitesController.CreateInvite()` |
| 4 | `inviter_user_id ≠ invitee_user_id` であること | `InvitesController.CreateInvite()` |
| 5 | ステータス遷移は `pending` → `accepted` / `declined` / `cancelled` のみ | `InvitesController.Accept()` / `Decline()` / `Cancel()` |
| 6 | 招待受諾時の `list_members` INSERT は同一トランザクション内で実行すること | `InvitesController.Accept()` |

### 8.3 トランザクション境界

#### 招待受諾トランザクション

```
BEGIN TRANSACTION
  1. list_invites.status = 'accepted', responded_at = NOW()
  2. list_members INSERT (list_id, user_id, role)
  3. outbox_events INSERT (invite_accepted)
COMMIT
```

3 つの操作はアトミックに実行される。いずれかが失敗した場合はロールバックする。

#### 招待作成トランザクション

```
BEGIN TRANSACTION
  1. list_invites INSERT (status: pending)
  2. outbox_events INSERT (invite_created)
COMMIT
```

---

## 9. シードデータ

### 9.1 テスト用シードデータ

ファイル配置先: `src/todo-service/db/seed.sql`（既存ファイルに追記）

```sql
-- =============================================================
-- list_invites: テスト用招待データ
-- =============================================================

-- 前提: 以下の seed データが既に存在すること
--   users: alice (owner), bob (editor), charlie (viewer), dave (未参加)
--   lists: sample_list_1 (owner: alice)
--   list_members: bob -> editor, charlie -> viewer

-- dave への pending 招待（alice が送信、viewer ロール）
INSERT INTO list_invites (id, list_id, inviter_user_id, invitee_user_id, role, status)
VALUES (
    'a0000000-0000-0000-0000-000000000001',
    '(sample_list_1_id)',
    '(alice_user_id)',
    '(dave_user_id)',
    'viewer',
    'pending'
) ON CONFLICT DO NOTHING;

-- 過去に declined された招待（再招待テスト用）
INSERT INTO list_invites (id, list_id, inviter_user_id, invitee_user_id, role, status, responded_at)
VALUES (
    'a0000000-0000-0000-0000-000000000002',
    '(sample_list_1_id)',
    '(alice_user_id)',
    '(eve_user_id)',
    'editor',
    'declined',
    NOW() - INTERVAL '7 days'
) ON CONFLICT DO NOTHING;
```

> 実際の UUID は既存シードデータに合わせて置換すること。

### 9.2 シードデータ検証クエリ

```sql
-- pending 招待の確認
SELECT li.id, l.title, li.invitee_user_id, li.role, li.status
FROM list_invites li
JOIN lists l ON l.id = li.list_id
WHERE li.status = 'pending';

-- 全招待の確認
SELECT li.*, l.title AS list_title
FROM list_invites li
JOIN lists l ON l.id = li.list_id
ORDER BY li.created_at DESC;
```
