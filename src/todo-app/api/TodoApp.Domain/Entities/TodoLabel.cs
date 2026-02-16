using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TodoApp.Domain.Entities;

/// <summary>
/// ToDo-Label中間テーブル
/// REQ-FUNC-008～012対応: 多対多リレーション
/// </summary>
[Table("TodoLabels")]
public class TodoLabel
{
    /// <summary>
    /// ToDoアイテムID（複合主キー）
    /// </summary>
    [Required]
    public long TodoId { get; set; }
    
    /// <summary>
    /// ラベルID（複合主キー）
    /// </summary>
    [Required]
    public int LabelId { get; set; }
    
    /// <summary>
    /// 作成日時（UTC）
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 関連ToDo（ナビゲーションプロパティ）
    /// </summary>
    [ForeignKey(nameof(TodoId))]
    public virtual Todo Todo { get; set; } = null!;
    
    /// <summary>
    /// 関連ラベル（ナビゲーションプロパティ）
    /// </summary>
    [ForeignKey(nameof(LabelId))]
    public virtual Label Label { get; set; } = null!;
}
