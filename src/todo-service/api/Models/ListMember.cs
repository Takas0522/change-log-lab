namespace TodoApi.Models;

public class ListMember
{
    public Guid Id { get; set; }
    public Guid ListId { get; set; }
    public Guid UserId { get; set; }  // Reference to User Service user
    public string Role { get; set; } = "viewer";  // owner, editor, viewer
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public List List { get; set; } = null!;
}
