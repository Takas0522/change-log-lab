namespace TodoApp.Domain.Enums;

/// <summary>
/// ToDoステータス
/// REQ-FUNC-006対応: NotStarted/InProgress/Completed/Abandoned
/// </summary>
public enum TodoStatus
{
    /// <summary>未着手</summary>
    NotStarted = 0,
    
    /// <summary>進行中</summary>
    InProgress = 1,
    
    /// <summary>完了</summary>
    Completed = 2,
    
    /// <summary>中断</summary>
    Abandoned = 3
}
