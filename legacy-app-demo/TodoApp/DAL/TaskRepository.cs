using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using TodoApp.Models;

namespace TodoApp.DAL
{
    /// <summary>タスクのデータアクセスロジックを提供するリポジトリクラス。</summary>
    public class TaskRepository
    {
        // -------------------------------------------------------
        // 照会
        // -------------------------------------------------------

        /// <summary>
        /// 指定した検索条件・フィルタ・ソート順でタスク一覧を取得する。
        /// </summary>
        /// <param name="searchTerm">タイトルまたは説明に対するキーワード検索（null / 空文字で全件）。</param>
        /// <param name="statusFilter">完了状態フィルタ（0:未完了, 1:完了, null:全件）。</param>
        /// <param name="priorityFilter">優先度フィルタ（0-2, null:全件）。</param>
        /// <param name="sortBy">ソートカラム名（CreatedAt / DueDate / Priority / Title）。</param>
        /// <param name="sortDescending">true のとき降順ソート。</param>
        /// <returns>条件を満たすタスクのリスト。</returns>
        public IList<TodoTask> GetAll(
            string searchTerm     = null,
            int?   statusFilter   = null,
            int?   priorityFilter = null,
            string sortBy         = "CreatedAt",
            bool   sortDescending = false)
        {
            // ホワイトリストで ORDER BY カラムを制限（SQL インジェクション対策）
            string orderColumn;
            switch (sortBy)
            {
                case "Title":    orderColumn = "Title";    break;
                case "Priority": orderColumn = "Priority"; break;
                case "DueDate":  orderColumn = "DueDate";  break;
                default:         orderColumn = "CreatedAt"; break;
            }
            string orderDir = sortDescending ? "DESC" : "ASC";

            string whereClause = BuildWhereClause(searchTerm, statusFilter, priorityFilter);
            string sql = string.Format(
                "SELECT Id,Title,Description,IsCompleted,Priority,DueDate,NotificationDate,CreatedAt,UpdatedAt " +
                "FROM Tasks {0} ORDER BY {1} {2}",
                whereClause, orderColumn, orderDir);

            var tasks = new List<TodoTask>();
            using (SQLiteConnection conn = DatabaseHelper.CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                {
                    AddFilterParameters(cmd, searchTerm, statusFilter, priorityFilter);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            tasks.Add(MapTask(reader));
                    }
                }
            }
            return tasks;
        }

        /// <summary>指定した ID のタスクを取得する。存在しない場合は null を返す。</summary>
        /// <param name="id">タスク ID。</param>
        public TodoTask GetById(int id)
        {
            const string sql =
                "SELECT Id,Title,Description,IsCompleted,Priority,DueDate,NotificationDate,CreatedAt,UpdatedAt " +
                "FROM Tasks WHERE Id = @Id";

            using (SQLiteConnection conn = DatabaseHelper.CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            return MapTask(reader);
                    }
                }
            }
            return null;
        }

        // -------------------------------------------------------
        // 登録
        // -------------------------------------------------------

        /// <summary>新しいタスクをデータベースに登録し、採番された ID を返す。</summary>
        /// <param name="task">登録するタスク情報。Id は無視される。</param>
        /// <returns>新規採番された ID。</returns>
        public int Insert(TodoTask task)
        {
            const string sql =
                "INSERT INTO Tasks (Title,Description,IsCompleted,Priority,DueDate,NotificationDate,CreatedAt,UpdatedAt) " +
                "VALUES (@Title,@Description,@IsCompleted,@Priority,@DueDate,@NotificationDate,@Now,@Now); " +
                "SELECT last_insert_rowid();";

            using (SQLiteConnection conn = DatabaseHelper.CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                {
                    SetTaskParameters(cmd, task);
                    cmd.Parameters.AddWithValue("@Now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        // -------------------------------------------------------
        // 更新
        // -------------------------------------------------------

        /// <summary>既存タスクの情報を更新する。</summary>
        /// <param name="task">更新するタスク情報。Id に基づいてレコードを特定する。</param>
        public void Update(TodoTask task)
        {
            const string sql =
                "UPDATE Tasks " +
                "SET Title=@Title,Description=@Description,IsCompleted=@IsCompleted," +
                "    Priority=@Priority,DueDate=@DueDate,NotificationDate=@NotificationDate,UpdatedAt=@Now " +
                "WHERE Id=@Id";

            using (SQLiteConnection conn = DatabaseHelper.CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                {
                    SetTaskParameters(cmd, task);
                    cmd.Parameters.AddWithValue("@Now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@Id",  task.Id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>指定した ID のタスクの完了状態を反転する。</summary>
        /// <param name="id">タスク ID。</param>
        public void ToggleComplete(int id)
        {
            const string sql =
                "UPDATE Tasks " +
                "SET IsCompleted = (1 - IsCompleted), UpdatedAt = @Now " +
                "WHERE Id = @Id";

            using (SQLiteConnection conn = DatabaseHelper.CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@Id",  id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // -------------------------------------------------------
        // 削除
        // -------------------------------------------------------

        /// <summary>指定した ID のタスクを削除する（関連コメントも CASCADE 削除される）。</summary>
        /// <param name="id">タスク ID。</param>
        public void Delete(int id)
        {
            const string sql = "DELETE FROM Tasks WHERE Id = @Id";

            using (SQLiteConnection conn = DatabaseHelper.CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // -------------------------------------------------------
        // プライベートヘルパー
        // -------------------------------------------------------

        private static string BuildWhereClause(string searchTerm, int? statusFilter, int? priorityFilter)
        {
            string clause = "WHERE 1=1";
            if (!string.IsNullOrEmpty(searchTerm))
                clause += " AND (Title LIKE @SearchTerm OR Description LIKE @SearchTerm)";
            if (statusFilter.HasValue)
                clause += " AND IsCompleted = @StatusFilter";
            if (priorityFilter.HasValue)
                clause += " AND Priority = @PriorityFilter";
            return clause;
        }

        private static void AddFilterParameters(SQLiteCommand cmd, string searchTerm, int? statusFilter, int? priorityFilter)
        {
            if (!string.IsNullOrEmpty(searchTerm))
                cmd.Parameters.AddWithValue("@SearchTerm", "%" + searchTerm + "%");
            if (statusFilter.HasValue)
                cmd.Parameters.AddWithValue("@StatusFilter", statusFilter.Value);
            if (priorityFilter.HasValue)
                cmd.Parameters.AddWithValue("@PriorityFilter", priorityFilter.Value);
        }

        private static void SetTaskParameters(SQLiteCommand cmd, TodoTask task)
        {
            cmd.Parameters.AddWithValue("@Title",            task.Title);
            cmd.Parameters.AddWithValue("@Description",      (object)task.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsCompleted",      task.IsCompleted ? 1 : 0);
            cmd.Parameters.AddWithValue("@Priority",         (int)task.Priority);
            cmd.Parameters.AddWithValue("@DueDate",          task.DueDate.HasValue          ? (object)task.DueDate.Value.ToString("yyyy-MM-dd HH:mm:ss")          : DBNull.Value);
            cmd.Parameters.AddWithValue("@NotificationDate", task.NotificationDate.HasValue ? (object)task.NotificationDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : DBNull.Value);
        }

        private static TodoTask MapTask(IDataReader reader)
        {
            return new TodoTask
            {
                Id               = Convert.ToInt32(reader["Id"]),
                Title            = reader["Title"].ToString(),
                Description      = reader["Description"] == DBNull.Value ? null : reader["Description"].ToString(),
                IsCompleted      = Convert.ToInt32(reader["IsCompleted"]) != 0,
                Priority         = (TaskPriority)Convert.ToInt32(reader["Priority"]),
                DueDate          = reader["DueDate"]          == DBNull.Value ? (DateTime?)null : DateTime.Parse(reader["DueDate"].ToString()),
                NotificationDate = reader["NotificationDate"] == DBNull.Value ? (DateTime?)null : DateTime.Parse(reader["NotificationDate"].ToString()),
                CreatedAt        = DateTime.Parse(reader["CreatedAt"].ToString()),
                UpdatedAt        = DateTime.Parse(reader["UpdatedAt"].ToString())
            };
        }
    }
}
