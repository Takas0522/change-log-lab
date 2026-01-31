# Todo App Database

OWASP準拠、100万レコード対応のSQL Databaseプロジェクト

## 概要

高機能ToDoアプリケーションのデータベース層実装。論理削除、楽観的同時実行制御、日本語全文検索をサポート。

## テーブル構成

### Todos（ToDoアイテム）
- **PK**: TodoId (BIGINT IDENTITY)
- **機能**: タイトル、本文、ステータス管理
- **特徴**: 論理削除、楽観的同時実行制御（RowVersion）、自動更新日時
- **制約**: 
  - ステータス: NotStarted/InProgress/Completed/Abandoned
  - 本文: 最大5000文字

### Labels（ラベル）
- **PK**: LabelId (INT IDENTITY)
- **機能**: カラー付きラベル管理
- **特徴**: ラベル名ユニーク制約（論理削除除外）
- **制約**: 
  - カラー: HEX形式 #RRGGBB
  - 名前: 1～50文字

### TodoLabels（多対多リレーション）
- **PK**: (TodoId, LabelId)
- **機能**: ToDo-Label関連付け
- **特徴**: CASCADE DELETE

## インデックス戦略

### パフォーマンス最適化（100万レコード対応）

| インデックス | 用途 | 対応要件 |
|---|---|---|
| IX_Todos_Status_CreatedAt | ステータス絞込+ソート（カバリングインデックス） | REQ-PERF-006 |
| IX_Todos_CreatedAt_Desc | デフォルト一覧表示 | REQ-FUNC-014 |
| IX_Todos_IsDeleted_CreatedAt | 論理削除フィルタ | REQ-DATA-002 |
| UX_Labels_Name | ラベル名重複防止 | REQ-FUNC-008 |
| IX_TodoLabels_LabelId | ラベル別ToDo検索 | REQ-FUNC-013 |

### 全文検索
- **カタログ**: TodoFullTextCatalog
- **対象カラム**: Title, Content
- **言語**: 日本語（LCID: 1041）
- **性能目標**: 
  - 10万件: 1秒以内
  - 100万件: 2秒以内

## セットアップ

### 1. データベース作成
```sql
CREATE DATABASE TodoAppDB;
GO
USE TodoAppDB;
GO
```

### 2. スキーマ適用
```bash
# テーブル作成
sqlcmd -S localhost -d TodoAppDB -i Tables/Todos.sql
sqlcmd -S localhost -d TodoAppDB -i Tables/Labels.sql
sqlcmd -S localhost -d TodoAppDB -i Tables/TodoLabels.sql

# インデックス作成
sqlcmd -S localhost -d TodoAppDB -i Indexes/IX_Todos.sql
sqlcmd -S localhost -d TodoAppDB -i Indexes/IX_Labels.sql
sqlcmd -S localhost -d TodoAppDB -i Indexes/IX_TodoLabels.sql

# トリガー作成
sqlcmd -S localhost -d TodoAppDB -i Triggers/TR_Todos_UpdatedAt.sql

# 全文検索
sqlcmd -S localhost -d TodoAppDB -i Scripts/FullTextSearch.sql

# 初期データ（オプション）
sqlcmd -S localhost -d TodoAppDB -i Scripts/SeedData.sql
```

### 3. マイグレーション管理
```sql
-- スキーマバージョン確認
SELECT * FROM SchemaVersions;
```

## セキュリティ対策（OWASP準拠）

### SQLインジェクション対策
- ✅ Entity Framework Core使用（パラメータ化クエリ）
- ✅ ストアドプロシージャ不使用（動的SQL回避）
- ✅ CHECK制約による入力検証

### データ保護
- ✅ 論理削除（物理削除禁止）
- ✅ トランザクション管理
- ✅ 楽観的同時実行制御

### 監査ログ
- ✅ CreatedAt（作成日時）
- ✅ UpdatedAt（更新日時）
- ✅ DeletedAt（削除日時）

## パフォーマンステスト

### 100万レコード性能検証

```sql
-- テストデータ生成（100万件）
DECLARE @i INT = 1;
WHILE @i <= 1000000
BEGIN
    INSERT INTO Todos (Title, Content, Status)
    VALUES (
        CONCAT('Test Todo ', @i),
        CONCAT('Content for todo ', @i),
        CASE @i % 4
            WHEN 0 THEN 'NotStarted'
            WHEN 1 THEN 'InProgress'
            WHEN 2 THEN 'Completed'
            ELSE 'Abandoned'
        END
    );
    SET @i = @i + 1;
END

-- 性能測定
SET STATISTICS TIME ON;
SET STATISTICS IO ON;

-- ステータス絞込+ソート（目標: 500ms以内）
SELECT TOP 50 TodoId, Title, Status, CreatedAt
FROM Todos
WHERE Status = 'InProgress' AND IsDeleted = 0
ORDER BY CreatedAt DESC;

-- 全文検索（目標: 2秒以内）
SELECT TodoId, Title
FROM Todos
WHERE CONTAINS((Title, Content), N'プロジェクト')
  AND IsDeleted = 0;
```

## トラブルシューティング

### 全文検索が動作しない
```sql
-- 全文検索サービス確認
SELECT SERVERPROPERTY('IsFullTextInstalled');

-- カタログの状態確認
SELECT * FROM sys.fulltext_catalogs;

-- インデックスの再構築
ALTER FULLTEXT INDEX ON Todos START FULL POPULATION;
```

### インデックスの最適化
```sql
-- インデックスの断片化確認
SELECT 
    OBJECT_NAME(ips.object_id) AS TableName,
    i.name AS IndexName,
    ips.avg_fragmentation_in_percent
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
INNER JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
WHERE ips.avg_fragmentation_in_percent > 10
ORDER BY ips.avg_fragmentation_in_percent DESC;

-- インデックス再構築
ALTER INDEX ALL ON Todos REBUILD;
```

## 要件対応

| 要件ID | 説明 | 実装内容 |
|---|---|---|
| REQ-FUNC-001～005 | ToDo CRUD | Todosテーブル |
| REQ-FUNC-006～007 | ステータス管理 | Status列、CHECK制約 |
| REQ-FUNC-008～012 | ラベル管理 | Labels、TodoLabelsテーブル |
| REQ-FUNC-013～017 | 絞込・検索 | インデックス、全文検索 |
| REQ-DATA-001 | UTC日時 | DATETIME2、GETUTCDATE() |
| REQ-DATA-002 | 論理削除 | IsDeleted、DeletedAt |
| REQ-DATA-003 | 監査ログ | CreatedAt、UpdatedAt、トリガー |
| REQ-DATA-004 | トランザクション | RowVersion（楽観的同時実行制御） |
| REQ-PERF-006～008 | 100万レコード対応 | カバリングインデックス、全文検索 |
| REQ-SEC-001～003 | SQLインジェクション対策 | パラメータ化クエリ、CHECK制約 |

## ライセンス

MIT License
