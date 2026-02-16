using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TodoApp.Domain.Entities;

/// <summary>
/// ラベルエンティティ
/// REQ-FUNC-008～012対応
/// </summary>
[Table("Labels")]
public class Label
{
    /// <summary>
    /// ラベルID（主キー）
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int LabelId { get; set; }
    
    /// <summary>
    /// ラベル名（必須、ユニーク、最大50文字）
    /// REQ-FUNC-009対応
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// カラーコード（HEX形式 #RRGGBB）
    /// REQ-FUNC-009対応
    /// </summary>
    [Required]
    [MaxLength(7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$")]
    public string Color { get; set; } = "#000000";
    
    /// <summary>
    /// 作成日時（UTC）
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 論理削除フラグ
    /// </summary>
    [Required]
    public bool IsDeleted { get; set; } = false;
    
    /// <summary>
    /// 関連ToDo（多対多リレーション）
    /// </summary>
    public virtual ICollection<TodoLabel> TodoLabels { get; set; } = new List<TodoLabel>();
}
