-- =====================================
-- Label Table Definition
-- =====================================
-- REQ-FUNC-008～012対応: ラベル機能

CREATE TABLE [dbo].[Labels]
(
    -- Primary Key
    [LabelId] INT IDENTITY(1,1) NOT NULL,
    
    -- Core Fields
    [Name] NVARCHAR(50) NOT NULL,
    [Color] NVARCHAR(7) NOT NULL,
    
    -- Timestamps
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    
    -- Soft Delete
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    
    -- Constraints
    CONSTRAINT [PK_Labels] PRIMARY KEY CLUSTERED ([LabelId]),
    
    -- Color Format Constraint (REQ-FUNC-009対応: HEX形式 #RRGGBB)
    CONSTRAINT [CK_Labels_Color] CHECK ([Color] LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]'),
    
    -- Name Length Constraint
    CONSTRAINT [CK_Labels_Name_Length] CHECK (LEN([Name]) >= 1 AND LEN([Name]) <= 50)
);
GO

-- Extended Properties
EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'ToDoに付与可能なラベル（カテゴリ、タグ）を管理するテーブル',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Labels';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'ラベルの一意識別子',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Labels',
    @level2type = N'COLUMN', @level2name = N'LabelId';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'ラベル名（最大50文字、ユニーク）',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Labels',
    @level2type = N'COLUMN', @level2name = N'Name';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'ラベルの色（HEX形式: #RRGGBB）',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Labels',
    @level2type = N'COLUMN', @level2name = N'Color';
GO
