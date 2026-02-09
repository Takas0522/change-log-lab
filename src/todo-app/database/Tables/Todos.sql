-- =====================================
-- Todo Table Definition
-- =====================================
-- REQ-FUNC-001～005, REQ-DATA-001～004対応
-- 論理削除、日時管理、トランザクション管理

CREATE TABLE [dbo].[Todos]
(
    -- Primary Key
    [TodoId] BIGINT IDENTITY(1,1) NOT NULL,
    
    -- Core Fields
    [Title] NVARCHAR(200) NOT NULL,
    [Content] NVARCHAR(MAX) NULL,
    [Status] NVARCHAR(20) NOT NULL DEFAULT 'NotStarted',
    
    -- Soft Delete (REQ-DATA-002対応: 論理削除)
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [DeletedAt] DATETIME2(7) NULL,
    
    -- Timestamps (REQ-DATA-001対応: UTC日時)
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    
    -- Optimistic Concurrency Control (REQ-DATA-004対応)
    [RowVersion] ROWVERSION NOT NULL,
    
    -- Constraints
    CONSTRAINT [PK_Todos] PRIMARY KEY CLUSTERED ([TodoId]),
    
    -- Status Constraint (REQ-FUNC-006対応: NotStarted/InProgress/Completed/Abandoned)
    CONSTRAINT [CK_Todos_Status] CHECK ([Status] IN ('NotStarted', 'InProgress', 'Completed', 'Abandoned')),
    
    -- Content Length Constraint (REQ-FUNC-002対応: 本文5000文字以内)
    CONSTRAINT [CK_Todos_Content_Length] CHECK (LEN([Content]) <= 5000),
    
    -- Logical Delete Constraint
    CONSTRAINT [CK_Todos_DeletedAt] CHECK ([IsDeleted] = 0 OR [DeletedAt] IS NOT NULL)
);
GO

-- Extended Properties (Documentation)
EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'ToDoアイテムを管理するメインテーブル。論理削除、楽観的同時実行制御対応。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Todos';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'ToDoアイテムの一意識別子',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Todos',
    @level2type = N'COLUMN', @level2name = N'TodoId';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'ToDoのタイトル（最大200文字）',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Todos',
    @level2type = N'COLUMN', @level2name = N'Title';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'ToDoの詳細内容（最大5000文字）',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Todos',
    @level2type = N'COLUMN', @level2name = N'Content';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'ステータス: NotStarted/InProgress/Completed/Abandoned',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Todos',
    @level2type = N'COLUMN', @level2name = N'Status';
GO
