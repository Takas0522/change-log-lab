using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using TodoApp.Models;

namespace TodoApp.DAL
{
    /// <summary>タスクコメントのデータアクセスロジックを提供するリポジトリクラス。</summary>
    public class CommentRepository
    {
        /// <summary>指定したタスク ID に紐付くコメント一覧を作成日時の降順で取得する。</summary>
        /// <param name="taskId">タスク ID。</param>
        /// <returns>コメントのリスト。</returns>
        public IList<TaskComment> GetByTaskId(int taskId)
        {
            const string sql =
                "SELECT Id,TaskId,Content,CreatedAt " +
                "FROM TaskComments WHERE TaskId = @TaskId ORDER BY CreatedAt DESC";

            var comments = new List<TaskComment>();
            using (SQLiteConnection conn = DatabaseHelper.CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@TaskId", taskId);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            comments.Add(MapComment(reader));
                    }
                }
            }
            return comments;
        }

        /// <summary>新しいコメントを登録する。</summary>
        /// <param name="comment">登録するコメント情報。Id は無視される。</param>
        public void Add(TaskComment comment)
        {
            const string sql =
                "INSERT INTO TaskComments (TaskId,Content,CreatedAt) VALUES (@TaskId,@Content,@Now)";

            using (SQLiteConnection conn = DatabaseHelper.CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@TaskId",  comment.TaskId);
                    cmd.Parameters.AddWithValue("@Content", comment.Content);
                    cmd.Parameters.AddWithValue("@Now",     DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>指定した ID のコメントを削除する。</summary>
        /// <param name="id">コメント ID。</param>
        public void Delete(int id)
        {
            const string sql = "DELETE FROM TaskComments WHERE Id = @Id";

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

        private static TaskComment MapComment(IDataReader reader)
        {
            return new TaskComment
            {
                Id        = Convert.ToInt32(reader["Id"]),
                TaskId    = Convert.ToInt32(reader["TaskId"]),
                Content   = reader["Content"].ToString(),
                CreatedAt = DateTime.Parse(reader["CreatedAt"].ToString())
            };
        }
    }
}
