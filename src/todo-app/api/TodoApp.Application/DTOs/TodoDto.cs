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
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "タイトルは必須です")]
    [System.ComponentModel.DataAnnotations.MaxLength(200, ErrorMessage = "タイトルは200文字以内です")]
    public string Title { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.MaxLength(4000, ErrorMessage = "内容は4000文字以内です")]
    public string? Content { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    public string Status { get; set; } = "NotStarted";

    [System.ComponentModel.DataAnnotations.MaxLength(10, ErrorMessage = "ラベルは最大10個までです")]
    public List<int> LabelIds { get; set; } = new();
}

/// <summary>
/// ToDo更新リクエスト
/// REQ-FUNC-003対応
/// </summary>
public class UpdateTodoRequest
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "タイトルは必須です")]
    [System.ComponentModel.DataAnnotations.MaxLength(200, ErrorMessage = "タイトルは200文字以内です")]
    public string Title { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.MaxLength(4000, ErrorMessage = "内容は4000文字以内です")]
    public string? Content { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    public string Status { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.MaxLength(10, ErrorMessage = "ラベルは最大10個までです")]
    public List<int> LabelIds { get; set; } = new();
}

/// <summary>
/// ステータス更新リクエスト
/// REQ-FUNC-007対応
/// </summary>
public class UpdateTodoStatusRequest
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "ステータスは必須です")]
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
