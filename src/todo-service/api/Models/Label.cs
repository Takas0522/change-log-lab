namespace TodoApi.Models;

public class Label
{
    public Guid Id { get; set; }
    public Guid ListId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public List List { get; set; } = null!;
    public ICollection<TodoLabel> TodoLabels { get; set; } = new List<TodoLabel>();
}
