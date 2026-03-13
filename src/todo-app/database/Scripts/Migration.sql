-- =====================================
-- Database Migration Script
-- =====================================
-- 初回セットアップ用の統合スクリプト

-- 実行順序:
-- 1. Tables/Todos.sql
-- 2. Tables/Labels.sql
-- 3. Tables/TodoLabels.sql
-- 4. Indexes/IX_Todos.sql
-- 5. Indexes/IX_Labels.sql
-- 6. Indexes/IX_TodoLabels.sql
-- 7. Triggers/TR_Todos_UpdatedAt.sql
-- 8. Scripts/FullTextSearch.sql
-- 9. Scripts/SeedData.sql (Optional)

PRINT 'Starting Todo App Database Migration...';
GO

-- スキーマバージョン管理テーブル
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SchemaVersions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SchemaVersions]
    (
        [VersionId] INT IDENTITY(1,1) NOT NULL,
        [Version] NVARCHAR(20) NOT NULL,
        [Description] NVARCHAR(500) NOT NULL,
        [AppliedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_SchemaVersions] PRIMARY KEY CLUSTERED ([VersionId])
    );
    
    PRINT 'SchemaVersions table created.';
END
GO

-- バージョン1.0.0の記録
IF NOT EXISTS (SELECT * FROM [dbo].[SchemaVersions] WHERE [Version] = '1.0.0')
BEGIN
    INSERT INTO [dbo].[SchemaVersions] ([Version], [Description])
    VALUES ('1.0.0', 'Initial schema: Todos, Labels, TodoLabels with full-text search');
    
    PRINT 'Version 1.0.0 applied.';
END
GO

PRINT 'Database Migration completed successfully.';
GO
