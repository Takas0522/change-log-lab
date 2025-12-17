namespace TodoApi.Models;

public class ListInvite
{
    public Guid Id { get; set; }
    public Guid ListId { get; set; }
    public Guid InviterUserId { get; set; }  // User who sent the invite
    public Guid InviteeUserId { get; set; }  // User being invited
    public string Status { get; set; } = "pending";  // pending, accepted, rejected, cancelled
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public List List { get; set; } = null!;
}
