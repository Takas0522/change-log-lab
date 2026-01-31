-- =====================================
-- Todos Table Indexes
-- =====================================
-- REQ-PERF-006～008対応: 100万レコード対応、検索性能最適化

-- ステータス+作成日時による絞込・ソート用（カバリングインデックス）
-- REQ-FUNC-013, REQ-PERF-006対応
CREATE NONCLUSTERED INDEX [IX_Todos_Status_CreatedAt] 
    ON [dbo].[Todos] ([Status], [CreatedAt] DESC)
    INCLUDE ([TodoId], [Title], [IsDeleted])
    WHERE [IsDeleted] = 0;
GO

-- 作成日時ソート用（デフォルトの新しい順）
-- REQ-FUNC-014対応
CREATE NONCLUSTERED INDEX [IX_Todos_CreatedAt_Desc] 
    ON [dbo].[Todos] ([CreatedAt] DESC)
    INCLUDE ([TodoId], [Title], [Status], [IsDeleted])
    WHERE [IsDeleted] = 0;
GO

-- 論理削除フィルター+作成日時用
-- REQ-DATA-002対応
CREATE NONCLUSTERED INDEX [IX_Todos_IsDeleted_CreatedAt] 
    ON [dbo].[Todos] ([IsDeleted], [CreatedAt] DESC)
    INCLUDE ([TodoId], [Title], [Status]);
GO

-- 複合絞込用（ステータス+日時範囲）
-- REQ-FUNC-017対応: 複合絞込
CREATE NONCLUSTERED INDEX [IX_Todos_Status_CreatedAt_Range] 
    ON [dbo].[Todos] ([Status], [CreatedAt])
    INCLUDE ([TodoId], [Title], [Content], [IsDeleted])
    WHERE [IsDeleted] = 0;
GO

-- Extended Properties
EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'ステータス別フィルタリングと作成日時ソート用カバリングインデックス。100万レコードでの高速検索を保証。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Todos',
    @level2type = N'INDEX',  @level2name = N'IX_Todos_Status_CreatedAt';
GO
