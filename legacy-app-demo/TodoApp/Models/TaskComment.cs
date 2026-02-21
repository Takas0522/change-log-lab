using System;

namespace TodoApp.Models
{
    /// <summary>タスクに付与されるコメントを表すモデルクラス。</summary>
    public class TaskComment
    {
        /// <summary>コメントの一意識別子。</summary>
        public int Id { get; set; }

        /// <summary>このコメントが紐付くタスクの識別子。</summary>
        public int TaskId { get; set; }

        /// <summary>コメントの本文。</summary>
        public string Content { get; set; }

        /// <summary>コメントの作成日時。</summary>
        public DateTime CreatedAt { get; set; }
    }
}
