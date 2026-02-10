namespace SreAgentLab.Models;

public class TodoItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Title { get; set; }
    public string? Body { get; set; }
    public string Status { get; set; } = TodoStatus.NotStarted;
    public DateTime CreatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}
