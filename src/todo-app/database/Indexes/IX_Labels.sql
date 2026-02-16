-- =====================================
-- Labels Table Indexes
-- =====================================
-- REQ-FUNC-008～012対応: ラベル検索最適化

-- ラベル名ユニーク制約（インデックスとして実装）
-- REQ-FUNC-008対応: 重複ラベル名禁止
CREATE UNIQUE NONCLUSTERED INDEX [UX_Labels_Name] 
    ON [dbo].[Labels] ([Name])
    WHERE [IsDeleted] = 0;
GO

-- 作成日時順ソート用
CREATE NONCLUSTERED INDEX [IX_Labels_CreatedAt] 
    ON [dbo].[Labels] ([CreatedAt] DESC)
    INCLUDE ([LabelId], [Name], [Color], [IsDeleted]);
GO

-- Extended Properties
EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'ラベル名のユニーク制約。論理削除されていないラベルのみが対象。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Labels',
    @level2type = N'INDEX',  @level2name = N'UX_Labels_Name';
GO
