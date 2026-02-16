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
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "ラベル名は必須です")]
    [System.ComponentModel.DataAnnotations.MaxLength(50, ErrorMessage = "ラベル名は50文字以内です")]
    public string Name { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "カラーコードはHEX形式(#RRGGBB)で指定してください")]
    public string Color { get; set; } = "#000000";
}

/// <summary>
/// ラベル更新リクエスト
/// REQ-FUNC-011対応
/// </summary>
public class UpdateLabelRequest
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "ラベル名は必須です")]
    [System.ComponentModel.DataAnnotations.MaxLength(50, ErrorMessage = "ラベル名は50文字以内です")]
    public string Name { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "カラーコードはHEX形式(#RRGGBB)で指定してください")]
    public string Color { get; set; } = string.Empty;
}
