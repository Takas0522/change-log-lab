---
description: PostgreSQLのスキーマ変更（テーブル追加・カラム追加・インデックス作成）を生成するプロンプト
mode: agent
tools:
  - read
  - edit
  - search
  - todo
---

# DBスキーマ変更

PostgreSQLのスキーマ変更を設計・実装してください。

## 変更依頼

変更内容: ${{schema_change_description}}

## データベース構成

| サービス | DB | スキーマファイル |
|----------|-----|-----------------|
| auth-service | auth-db | `src/auth-service/db/schema.sql` |
| user-service | user-db | `src/user-service/db/schema.sql` |
| todo-service | todo-db | `src/todo-service/db/schema.sql` |

## スキーマ規約

### 命名規則
- テーブル名: `snake_case` / 複数形（例: `list_members`）
- カラム名: `snake_case`（例: `created_at`）
- インデックス: `idx_{table}_{columns}`
- FK: `fk_{table}_{ref_table}`

### データ型
| 用途 | 型 |
|------|-----|
| 主キー | `UUID DEFAULT gen_random_uuid()` |
| 日時 | `TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()` |
| 文字列 | `VARCHAR(n)` |
| 真偽値 | `BOOLEAN DEFAULT FALSE` |
| JSON | `JSONB` |

## 実装チェックリスト

- [ ] `schema.sql` にテーブル定義を追加
- [ ] 外部キーにインデックスを作成
- [ ] `seed.sql` に開発用テストデータを追加
- [ ] EF Core の Model を更新
- [ ] DbContext の `OnModelCreating` にマッピングを追加
- [ ] DTO（レコード型）を作成・更新
- [ ] `dotnet build` で確認
