namespace TodoApp.Application.DTOs;

/// <summary>
/// ToDo DTO
/// API通信用のデータ転送オブジェクト
/// </summary>
public class TodoDto
{
    public long TodoId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<LabelDto> Labels { get; set; } = new();
}

/// <summary>
/// ToDo作成リクエスト
/// REQ-FUNC-001対応
/// </summary>
public class CreateTodoRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string Status { get; set; } = "NotStarted";
    public List<int> LabelIds { get; set; } = new();
}

/// <summary>
/// ToDo更新リクエスト
/// REQ-FUNC-003対応
/// </summary>
public class UpdateTodoRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<int> LabelIds { get; set; } = new();
}

/// <summary>
/// ステータス更新リクエスト
/// REQ-FUNC-007対応
/// </summary>
public class UpdateTodoStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// ToDo検索条件
/// REQ-FUNC-013～017対応
/// </summary>
public class TodoQueryParameters
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Keyword { get; set; }
    public List<string>? Statuses { get; set; }
    public List<int>? LabelIds { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string SortBy { get; set; } = "CreatedAt";
    public string SortOrder { get; set; } = "DESC";
}
