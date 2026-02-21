using System;
using System.Data.SQLite;
using System.IO;
using System.Web;

namespace TodoApp.DAL
{
    /// <summary>アプリケーション起動時に SQLite データベースとスキーマを初期化するクラス。</summary>
    public static class DatabaseInitializer
    {
        /// <summary>
        /// App_Data フォルダに SQLite データベースファイルを作成し、
        /// 必要なテーブルが存在しない場合は作成する。
        /// サンプルデータが存在しない場合は初期データを投入する。
        /// </summary>
        public static void Initialize()
        {
            // App_Data ディレクトリを確実に作成
            string appDataPath = HttpContext.Current != null
                ? HttpContext.Current.Server.MapPath("~/App_Data")
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data");

            if (!Directory.Exists(appDataPath))
                Directory.CreateDirectory(appDataPath);

            using (SQLiteConnection conn = DatabaseHelper.CreateConnection())
            {
                conn.Open();

                // 外部キー制約を有効化
                using (SQLiteCommand pragma = new SQLiteCommand("PRAGMA foreign_keys = ON;", conn))
                    pragma.ExecuteNonQuery();

                CreateSchema(conn);
                SeedData(conn);
            }
        }

        private static void CreateSchema(SQLiteConnection conn)
        {
            const string createTasks = @"
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
                CREATE INDEX IF NOT EXISTS IX_Tasks_DueDate     ON Tasks (DueDate);";

            const string createComments = @"
                CREATE TABLE IF NOT EXISTS TaskComments (
                    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                    TaskId    INTEGER NOT NULL,
                    Content   TEXT    NOT NULL,
                    CreatedAt TEXT    NOT NULL DEFAULT (datetime('now', 'localtime')),
                    FOREIGN KEY (TaskId) REFERENCES Tasks(Id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS IX_TaskComments_TaskId ON TaskComments (TaskId);";

            using (SQLiteCommand cmd = new SQLiteCommand(createTasks,    conn)) cmd.ExecuteNonQuery();
            using (SQLiteCommand cmd = new SQLiteCommand(createComments, conn)) cmd.ExecuteNonQuery();
        }

        private static void SeedData(SQLiteConnection conn)
        {
            // タスクが存在しない場合のみサンプルデータを投入
            using (SQLiteCommand check = new SQLiteCommand("SELECT COUNT(*) FROM Tasks;", conn))
            {
                long count = (long)check.ExecuteScalar();
                if (count > 0)
                    return;
            }

            string now  = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string plus3 = DateTime.Now.AddDays(3).ToString("yyyy-MM-dd");
            string plus7 = DateTime.Now.AddDays(7).ToString("yyyy-MM-dd");
            string minus1 = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

            const string insertSql =
                "INSERT INTO Tasks (Title,Description,IsCompleted,Priority,DueDate,CreatedAt,UpdatedAt) " +
                "VALUES (@Title,@Desc,@Done,@Pri,@Due,@Now,@Now)";

            object[][] seeds =
            {
                new object[] { "サンプルタスク（低優先度）", "これは低優先度のサンプルタスクです。", 0, 0, DBNull.Value },
                new object[] { "サンプルタスク（中優先度）", "これは中優先度のサンプルタスクです。", 0, 1, plus7 },
                new object[] { "サンプルタスク（高優先度）", "これは高優先度の緊急タスクです。",    0, 2, plus3 },
                new object[] { "完了済みサンプルタスク",     "このタスクはすでに完了しています。",   1, 1, minus1 }
            };

            foreach (object[] row in seeds)
            {
                using (SQLiteCommand cmd = new SQLiteCommand(insertSql, conn))
                {
                    cmd.Parameters.AddWithValue("@Title", row[0]);
                    cmd.Parameters.AddWithValue("@Desc",  row[1]);
                    cmd.Parameters.AddWithValue("@Done",  row[2]);
                    cmd.Parameters.AddWithValue("@Pri",   row[3]);
                    cmd.Parameters.AddWithValue("@Due",   row[4]);
                    cmd.Parameters.AddWithValue("@Now",   now);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
