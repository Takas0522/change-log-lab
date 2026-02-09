-- =====================================
-- Todos UpdatedAt Auto-Update Trigger
-- =====================================
-- REQ-DATA-001対応: 更新日時の自動設定

CREATE TRIGGER [dbo].[TR_Todos_UpdatedAt]
ON [dbo].[Todos]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- RowVersionの更新を除外（無限ループ防止）
    IF NOT UPDATE([RowVersion])
    BEGIN
        UPDATE [dbo].[Todos]
        SET [UpdatedAt] = GETUTCDATE()
        FROM [dbo].[Todos] t
        INNER JOIN inserted i ON t.[TodoId] = i.[TodoId]
        WHERE t.[UpdatedAt] = i.[UpdatedAt]; -- 既に更新されていない場合のみ
    END
END;
GO

-- Extended Properties
EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Todos更新時にUpdatedAtを自動的にUTC現在時刻で更新するトリガー。楽観的同時実行制御との競合を回避。',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Todos',
    @level2type = N'TRIGGER', @level2name = N'TR_Todos_UpdatedAt';
GO
