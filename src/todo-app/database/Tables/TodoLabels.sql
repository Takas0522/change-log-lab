-- =====================================
-- TodoLabel Junction Table Definition
-- =====================================
-- REQ-FUNC-008～012対応: ToDo-Label多対多リレーション

CREATE TABLE [dbo].[TodoLabels]
(
    -- Composite Primary Key
    [TodoId] BIGINT NOT NULL,
    [LabelId] INT NOT NULL,
    
    -- Timestamp
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    
    -- Constraints
    CONSTRAINT [PK_TodoLabels] PRIMARY KEY CLUSTERED ([TodoId], [LabelId]),
    
    -- Foreign Key to Todos (CASCADE DELETE対応)
    CONSTRAINT [FK_TodoLabels_Todos] 
        FOREIGN KEY ([TodoId]) 
        REFERENCES [dbo].[Todos]([TodoId]) 
        ON DELETE CASCADE,
    
    -- Foreign Key to Labels (CASCADE DELETE対応)
    CONSTRAINT [FK_TodoLabels_Labels] 
        FOREIGN KEY ([LabelId]) 
        REFERENCES [dbo].[Labels]([LabelId]) 
        ON DELETE CASCADE
);
GO

-- Extended Properties
EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'ToDoとLabelの多対多リレーションを管理する中間テーブル。1つのToDoに最大10個のラベルを付与可能。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'TodoLabels';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'ToDoアイテムのID',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'TodoLabels',
    @level2type = N'COLUMN', @level2name = N'TodoId';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'ラベルのID',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'TodoLabels',
    @level2type = N'COLUMN', @level2name = N'LabelId';
GO
