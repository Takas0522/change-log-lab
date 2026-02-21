---
applyTo: "src/**/db/**"
---

# PostgreSQL データベース設計ガイドライン

## データベース構成

マイクロサービスごとにデータベースを分離（Database-per-Service パターン）：

| サービス | データベース | 主なテーブル |
|----------|------------|-------------|
| auth-service | auth-db | `users`, `device_sessions` |
| user-service | user-db | `user_profiles` |
| todo-service | todo-db | `lists`, `todos`, `list_members`, `outbox_events` |

## スキーマ規約

### 命名規則
- テーブル名: **snake_case** / 複数形（`device_sessions`, `list_members`）
- カラム名: **snake_case**（`user_id`, `created_at`, `is_completed`）
- インデックス名: `idx_{table}_{columns}`
- 制約名: `pk_{table}`, `fk_{table}_{ref_table}`, `uq_{table}_{columns}`

### データ型
| 用途 | 型 | 備考 |
|------|-----|------|
| 主キー | `UUID` | `gen_random_uuid()` でデフォルト生成 |
| 日時 | `TIMESTAMP WITH TIME ZONE` | タイムゾーン付き必須 |
| 文字列 | `VARCHAR(n)` | 長さ制約を明示 |
| 真偽値 | `BOOLEAN` | `DEFAULT FALSE` を推奨 |
| JSON データ | `JSONB` | イベントペイロード等に使用 |

### テーブル定義テンプレート
```sql
CREATE TABLE IF NOT EXISTS {table_name} (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    -- ビジネスカラム
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- 外部キーにはインデックスを必ず作成
CREATE INDEX IF NOT EXISTS idx_{table}_{fk_column}
    ON {table_name}({fk_column});
```

## Outbox イベントパターン

```sql
-- Outbox テーブル
CREATE TABLE IF NOT EXISTS outbox_events (
    id          BIGSERIAL PRIMARY KEY,
    event_id    UUID NOT NULL UNIQUE,       -- 冪等性キー
    event_type  VARCHAR(100) NOT NULL,
    aggregate_id UUID NOT NULL,
    payload     JSONB NOT NULL,
    created_at  TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    processed_at TIMESTAMP WITH TIME ZONE   -- NULL = 未処理
);

-- pg_notify トリガー
CREATE OR REPLACE FUNCTION notify_outbox_event()
RETURNS TRIGGER AS $$
BEGIN
    PERFORM pg_notify('outbox_events', NEW.event_id::TEXT);
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;
```

## セキュリティ
- 常にパラメータ化クエリを使用（EF Core 経由）
- 動的 SQL の文字列連結は禁止
- 権限は最小限の原則に従う
- シークレットはコードに埋め込まない

## パフォーマンス
- 検索パターンに応じたインデックスを作成
- `SELECT *` は禁止（必要なカラムのみ取得）
- 全文検索には GIN インデックスを使用
- 実行計画 (`EXPLAIN ANALYZE`) でボトルネックを確認
