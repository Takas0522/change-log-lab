using System;

namespace TodoApp.Models
{
    /// <summary>タスクの優先度を表す列挙型。</summary>
    public enum TaskPriority
    {
        /// <summary>低優先度</summary>
        Low = 0,
        /// <summary>中優先度</summary>
        Medium = 1,
        /// <summary>高優先度</summary>
        High = 2
    }

    /// <summary>ToDoタスクを表すモデルクラス。</summary>
    public class TodoTask
    {
        /// <summary>タスクの一意識別子。</summary>
        public int Id { get; set; }

        /// <summary>タスクのタイトル。</summary>
        public string Title { get; set; }

        /// <summary>タスクの説明。</summary>
        public string Description { get; set; }

        /// <summary>タスクが完了しているかどうか。</summary>
        public bool IsCompleted { get; set; }

        /// <summary>タスクの優先度。</summary>
        public TaskPriority Priority { get; set; }

        /// <summary>タスクの期限。null の場合は期限なし。</summary>
        public DateTime? DueDate { get; set; }

        /// <summary>通知日時。null の場合は通知なし。</summary>
        public DateTime? NotificationDate { get; set; }

        /// <summary>タスクの作成日時。</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>タスクの最終更新日時。</summary>
        public DateTime UpdatedAt { get; set; }
    }
}
