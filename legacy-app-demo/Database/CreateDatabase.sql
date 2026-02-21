-- ============================================================
-- ToDoアプリケーション データベーススキーマ（SQLite 用）
-- このスクリプトは参照用です。
-- 実際のテーブル作成は DatabaseInitializer.cs で行われます。
-- ============================================================

-- タスクテーブル
CREATE TABLE IF NOT EXISTS Tasks (
    Id               INTEGER PRIMARY KEY AUTOINCREMENT,
    Title            TEXT    NOT NULL,
    Description      TEXT,
    IsCompleted      INTEGER NOT NULL DEFAULT 0,
    Priority         INTEGER NOT NULL DEFAULT 1,
    DueDate          TEXT,
    NotificationDate TEXT,
    CreatedAt        TEXT    NOT NULL DEFAULT (datetime('now', 'localtime')),
    UpdatedAt        TEXT    NOT NULL DEFAULT (datetime('now', 'localtime'))
);

CREATE INDEX IF NOT EXISTS IX_Tasks_IsCompleted ON Tasks (IsCompleted);
CREATE INDEX IF NOT EXISTS IX_Tasks_Priority    ON Tasks (Priority);
CREATE INDEX IF NOT EXISTS IX_Tasks_DueDate     ON Tasks (DueDate);

-- タスクコメントテーブル
CREATE TABLE IF NOT EXISTS TaskComments (
    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
    TaskId    INTEGER NOT NULL,
    Content   TEXT    NOT NULL,
    CreatedAt TEXT    NOT NULL DEFAULT (datetime('now', 'localtime')),
    FOREIGN KEY (TaskId) REFERENCES Tasks(Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS IX_TaskComments_TaskId ON TaskComments (TaskId);

-- サンプルデータ（初回のみ手動投入する場合）
-- INSERT INTO Tasks (Title, Description, IsCompleted, Priority, DueDate) VALUES
--     ('サンプルタスク（低優先度）', 'これは低優先度のサンプルタスクです。', 0, 0, NULL),
--     ('サンプルタスク（中優先度）', 'これは中優先度のサンプルタスクです。', 0, 1, date('now', '+7 days')),
--     ('サンプルタスク（高優先度）', 'これは高優先度の緊急タスクです。',    0, 2, date('now', '+3 days')),
--     ('完了済みサンプルタスク',     'このタスクはすでに完了しています。',   1, 1, date('now', '-1 days'));
