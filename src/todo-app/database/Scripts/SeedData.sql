-- =====================================
-- Initial Seed Data
-- =====================================
-- 開発・テスト用の初期データ

SET IDENTITY_INSERT [dbo].[Labels] ON;
GO

-- サンプルラベルデータ
INSERT INTO [dbo].[Labels] ([LabelId], [Name], [Color], [CreatedAt], [IsDeleted])
VALUES
    (1, N'重要', '#FF0000', GETUTCDATE(), 0),
    (2, N'仕事', '#0000FF', GETUTCDATE(), 0),
    (3, N'個人', '#00FF00', GETUTCDATE(), 0),
    (4, N'緊急', '#FFA500', GETUTCDATE(), 0),
    (5, N'学習', '#800080', GETUTCDATE(), 0);
GO

SET IDENTITY_INSERT [dbo].[Labels] OFF;
GO

-- サンプルToDoデータ
INSERT INTO [dbo].[Todos] ([Title], [Content], [Status], [CreatedAt], [UpdatedAt], [IsDeleted])
VALUES
    (N'プロジェクト計画書の作成', N'新規プロジェクトの要求定義と設計書を作成する', 'NotStarted', GETUTCDATE(), GETUTCDATE(), 0),
    (N'コードレビュー実施', N'チームメンバーのプルリクエストをレビューする', 'InProgress', GETUTCDATE(), GETUTCDATE(), 0),
    (N'週次報告書提出', N'今週の進捗を報告書にまとめて提出する', 'Completed', GETUTCDATE(), GETUTCDATE(), 0),
    (N'Angular 18へのアップグレード', N'既存プロジェクトをAngular 18にアップグレードする', 'NotStarted', GETUTCDATE(), GETUTCDATE(), 0),
    (N'データベース設計レビュー', N'テーブル定義とインデックス戦略の妥当性を確認', 'InProgress', GETUTCDATE(), GETUTCDATE(), 0);
GO

-- ToDoとLabelの関連付け
DECLARE @TodoId1 BIGINT, @TodoId2 BIGINT, @TodoId3 BIGINT, @TodoId4 BIGINT, @TodoId5 BIGINT;

SELECT @TodoId1 = [TodoId] FROM [dbo].[Todos] WHERE [Title] = N'プロジェクト計画書の作成';
SELECT @TodoId2 = [TodoId] FROM [dbo].[Todos] WHERE [Title] = N'コードレビュー実施';
SELECT @TodoId3 = [TodoId] FROM [dbo].[Todos] WHERE [Title] = N'週次報告書提出';
SELECT @TodoId4 = [TodoId] FROM [dbo].[Todos] WHERE [Title] = N'Angular 18へのアップグレード';
SELECT @TodoId5 = [TodoId] FROM [dbo].[Todos] WHERE [Title] = N'データベース設計レビュー';

INSERT INTO [dbo].[TodoLabels] ([TodoId], [LabelId], [CreatedAt])
VALUES
    (@TodoId1, 1, GETUTCDATE()), -- 重要
    (@TodoId1, 2, GETUTCDATE()), -- 仕事
    (@TodoId2, 2, GETUTCDATE()), -- 仕事
    (@TodoId2, 4, GETUTCDATE()), -- 緊急
    (@TodoId3, 2, GETUTCDATE()), -- 仕事
    (@TodoId4, 5, GETUTCDATE()), -- 学習
    (@TodoId5, 1, GETUTCDATE()), -- 重要
    (@TodoId5, 2, GETUTCDATE()); -- 仕事
GO

PRINT 'Initial seed data inserted successfully.';
GO
