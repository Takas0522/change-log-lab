-- =====================================
-- Full-Text Search Configuration
-- =====================================
-- REQ-FUNC-016, REQ-PERF-007～008対応: 日本語全文検索

-- 全文検索カタログ作成
CREATE FULLTEXT CATALOG [TodoFullTextCatalog] AS DEFAULT;
GO

-- 全文検索インデックス作成（日本語ワードブレーカー使用）
CREATE FULLTEXT INDEX ON [dbo].[Todos](
    [Title] LANGUAGE 1041,    -- Japanese Word Breaker (LCID: 1041)
    [Content] LANGUAGE 1041   -- Japanese Word Breaker
)
KEY INDEX [PK_Todos]
WITH (
    CHANGE_TRACKING = AUTO,   -- 自動更新追跡
    STOPLIST = SYSTEM         -- システムストップワード使用
);
GO

-- Extended Properties
EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'日本語全文検索用カタログ。TitleとContentフィールドに対する形態素解析ベースの検索をサポート。',
    @level0type = N'FULLTEXT CATALOG', 
    @level0name = N'TodoFullTextCatalog';
GO
