# SQLクエリとパフォーマンス分析

このディレクトリには、SQLパフォーマンスレビューに関連するクエリと実行計画が含まれています。

## ファイル一覧

### 元のクエリと実行計画
- **original_query.sql**: レビュー対象の元のSQLクエリ
- **original_execution_plan.txt**: EXPLAIN ANALYZEによる実行計画

### 最適化されたクエリ
- **optimized_monthly_stats.sql**: パフォーマンス最適化されたクエリ（3つのオプション）

## 主な最適化内容

### 1. インデックスの追加
```sql
CREATE INDEX IF NOT EXISTS idx_todos_created_at ON todos(created_at);
```

### 2. クエリの改善
- `created_at` と `updated_at` の使用を統一
- 不要なJOINの削除（外部キー制約により保証されている）
- インデックスを活用した効率的なフィルタリング

### 3. 期待される効果
- データ量1万件で約10倍の高速化
- Sequential Scan → Index Scan への変更
- バッファヒット率の改善

## 使用方法

### 1. マイグレーションの適用
```bash
psql -U <user> -d <database> -f ../migrations/001_add_performance_indexes.sql
```

### 2. 最適化クエリの実行
```bash
psql -U <user> -d <database> -f optimized_monthly_stats.sql
```

### 3. パフォーマンス検証
```sql
EXPLAIN ANALYZE <your_query>;
```

## 詳細レビュー

完全なパフォーマンスレビュー結果は以下を参照してください：
- [docs/sql-performance-review.md](../../../docs/sql-performance-review.md)
