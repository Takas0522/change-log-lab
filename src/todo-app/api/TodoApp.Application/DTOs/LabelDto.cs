namespace TodoApp.Application.DTOs;

/// <summary>
/// ラベル DTO
/// </summary>
public class LabelDto
{
    public int LabelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// ラベル作成リクエスト
/// REQ-FUNC-010対応
/// </summary>
public class CreateLabelRequest
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#000000";
}

/// <summary>
/// ラベル更新リクエスト
/// REQ-FUNC-011対応
/// </summary>
public class UpdateLabelRequest
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}
