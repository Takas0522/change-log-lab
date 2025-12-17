namespace TodoApi.Models;

public class TodoLabel
{
    public Guid Id { get; set; }
    public Guid TodoId { get; set; }
    public Guid LabelId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Todo Todo { get; set; } = null!;
    public Label Label { get; set; } = null!;
}
