using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TodoApp.Domain.Enums;

namespace TodoApp.Domain.Entities;

/// <summary>
/// ToDoエンティティ
/// REQ-FUNC-001～007, REQ-DATA-001～004対応
/// </summary>
[Table("Todos")]
public class Todo
{
    /// <summary>
    /// ToDoアイテムID（主キー）
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long TodoId { get; set; }
    
    /// <summary>
    /// タイトル（必須、最大200文字）
    /// REQ-FUNC-001対応
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// 本文（任意、最大5000文字）
    /// REQ-FUNC-002対応
    /// </summary>
    [MaxLength(5000)]
    public string? Content { get; set; }
    
    /// <summary>
    /// ステータス
    /// REQ-FUNC-006対応
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = nameof(TodoStatus.NotStarted);
    
    /// <summary>
    /// 論理削除フラグ
    /// REQ-DATA-002対応
    /// </summary>
    [Required]
    public bool IsDeleted { get; set; } = false;
    
    /// <summary>
    /// 削除日時（UTC）
    /// REQ-DATA-002対応
    /// </summary>
    public DateTime? DeletedAt { get; set; }
    
    /// <summary>
    /// 作成日時（UTC）
    /// REQ-DATA-001対応
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 更新日時（UTC）
    /// REQ-DATA-001対応
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 楽観的同時実行制御用バージョン
    /// REQ-DATA-004対応
    /// </summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; }
    
    /// <summary>
    /// 関連ラベル（多対多リレーション）
    /// REQ-FUNC-008～012対応
    /// </summary>
    public virtual ICollection<TodoLabel> TodoLabels { get; set; } = new List<TodoLabel>();
    
    /// <summary>
    /// ステータスを列挙型で取得
    /// </summary>
    [NotMapped]
    public TodoStatus StatusEnum
    {
        get => Enum.Parse<TodoStatus>(Status);
        set => Status = value.ToString();
    }
}
