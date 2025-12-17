namespace TodoApi.Models;

public class Todo
{
    public Guid Id { get; set; }
    public Guid ListId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? DueDate { get; set; }
    public int Position { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public List List { get; set; } = null!;
    public ICollection<TodoLabel> TodoLabels { get; set; } = new List<TodoLabel>();
}
