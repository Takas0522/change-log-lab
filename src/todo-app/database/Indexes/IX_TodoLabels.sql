-- =====================================
-- TodoLabels Table Indexes
-- =====================================
-- REQ-FUNC-008～012対応: ラベルによるToDo検索最適化

-- ラベルからのToDo検索用
-- REQ-FUNC-013対応: ラベルによる絞込
CREATE NONCLUSTERED INDEX [IX_TodoLabels_LabelId] 
    ON [dbo].[TodoLabels] ([LabelId])
    INCLUDE ([TodoId], [CreatedAt]);
GO

-- ToDoからのラベル検索用（逆方向）
CREATE NONCLUSTERED INDEX [IX_TodoLabels_TodoId] 
    ON [dbo].[TodoLabels] ([TodoId])
    INCLUDE ([LabelId], [CreatedAt]);
GO

-- Extended Properties
EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'ラベルIDからToDoIDを高速検索するためのインデックス。ラベル絞込機能で使用。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'TodoLabels',
    @level2type = N'INDEX',  @level2name = N'IX_TodoLabels_LabelId';
GO
