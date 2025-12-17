using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.DTOs;
using TodoApi.Models;

namespace TodoApi.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class InvitesController : ControllerBase
{
    private readonly TodoDbContext _context;
    private readonly ILogger<InvitesController> _logger;

    public InvitesController(TodoDbContext context, ILogger<InvitesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID in token");
        }
        return userId;
    }

    // POST: api/lists/{listId}/invites
    [HttpPost("lists/{listId}/invites")]
    public async Task<ActionResult<InviteResponse>> CreateInvite(Guid listId, CreateInviteRequest request)
    {
        var userId = GetUserId();

        var list = await _context.Lists
            .Include(l => l.Members)
            .FirstOrDefaultAsync(l => l.Id == listId);

        if (list == null)
        {
            return NotFound(new { message = "List not found" });
        }

        // Check if user has permission (owner or editor)
        var isOwner = list.OwnerId == userId;
        var member = list.Members.FirstOrDefault(m => m.UserId == userId);
        var userRole = isOwner ? "owner" : member?.Role ?? null;

        if (userRole == null || userRole == "viewer")
        {
            return Forbid();
        }

        // Check if invitee already has access
        var hasAccess = list.OwnerId == request.InviteeUserId ||
                       list.Members.Any(m => m.UserId == request.InviteeUserId);

        if (hasAccess)
        {
            return Conflict(new { message = "User already has access to this list" });
        }

        // Check if there's already a pending invite
        var existingInvite = await _context.ListInvites
            .FirstOrDefaultAsync(i => i.ListId == listId && 
                                    i.InviteeUserId == request.InviteeUserId && 
                                    i.Status == "pending");

        if (existingInvite != null)
        {
            return Conflict(new { message = "Pending invite already exists" });
        }

        var invite = new ListInvite
        {
            Id = Guid.NewGuid(),
            ListId = listId,
            InviterUserId = userId,
            InviteeUserId = request.InviteeUserId,
            Status = "pending"
        };

        _context.ListInvites.Add(invite);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Invite created: {InviteId} for list {ListId} by user {UserId} to user {InviteeUserId}", 
            invite.Id, listId, userId, request.InviteeUserId);

        var response = new InviteResponse(
            invite.Id,
            invite.ListId,
            invite.InviterUserId,
            invite.InviteeUserId,
            invite.Status,
            invite.CreatedAt,
            invite.UpdatedAt
        );

        return CreatedAtAction(nameof(GetInvite), new { inviteId = invite.Id }, response);
    }

    // GET: api/invites/{inviteId}
    [HttpGet("invites/{inviteId}")]
    public async Task<ActionResult<InviteResponse>> GetInvite(Guid inviteId)
    {
        var userId = GetUserId();

        var invite = await _context.ListInvites.FindAsync(inviteId);

        if (invite == null)
        {
            return NotFound(new { message = "Invite not found" });
        }

        // Only inviter or invitee can view the invite
        if (invite.InviterUserId != userId && invite.InviteeUserId != userId)
        {
            return Forbid();
        }

        var response = new InviteResponse(
            invite.Id,
            invite.ListId,
            invite.InviterUserId,
            invite.InviteeUserId,
            invite.Status,
            invite.CreatedAt,
            invite.UpdatedAt
        );

        return Ok(response);
    }

    // POST: api/invites/{inviteId}/accept
    [HttpPost("invites/{inviteId}/accept")]
    public async Task<ActionResult<ListMemberResponse>> AcceptInvite(Guid inviteId)
    {
        var userId = GetUserId();

        var invite = await _context.ListInvites
            .Include(i => i.List)
            .FirstOrDefaultAsync(i => i.Id == inviteId);

        if (invite == null)
        {
            return NotFound(new { message = "Invite not found" });
        }

        // Only invitee can accept
        if (invite.InviteeUserId != userId)
        {
            return Forbid();
        }

        // Check if invite is still pending
        if (invite.Status != "pending")
        {
            return BadRequest(new { message = $"Invite is already {invite.Status}" });
        }

        // Check if user already has access
        var existingMember = await _context.ListMembers
            .FirstOrDefaultAsync(m => m.ListId == invite.ListId && m.UserId == userId);

        if (existingMember != null)
        {
            return Conflict(new { message = "User already has access to this list" });
        }

        // Update invite status
        invite.Status = "accepted";

        // Create list member with viewer role
        var member = new ListMember
        {
            Id = Guid.NewGuid(),
            ListId = invite.ListId,
            UserId = userId,
            Role = "viewer"
        };

        _context.ListMembers.Add(member);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Invite accepted: {InviteId} by user {UserId}, member created with role viewer", 
            inviteId, userId);

        var response = new ListMemberResponse(
            member.Id,
            member.ListId,
            member.UserId,
            member.Role,
            member.CreatedAt,
            member.UpdatedAt
        );

        return Ok(response);
    }

    // DELETE: api/invites/{inviteId}
    [HttpDelete("invites/{inviteId}")]
    public async Task<IActionResult> DeleteInvite(Guid inviteId)
    {
        var userId = GetUserId();

        var invite = await _context.ListInvites.FindAsync(inviteId);

        if (invite == null)
        {
            return NotFound(new { message = "Invite not found" });
        }

        // Only inviter can cancel or invitee can reject
        if (invite.InviterUserId != userId && invite.InviteeUserId != userId)
        {
            return Forbid();
        }

        // Check if invite is still pending
        if (invite.Status != "pending")
        {
            return BadRequest(new { message = $"Cannot delete invite with status {invite.Status}" });
        }

        // Update status instead of deleting (for audit trail)
        invite.Status = invite.InviterUserId == userId ? "cancelled" : "rejected";
        await _context.SaveChangesAsync();

        _logger.LogInformation("Invite {InviteId} {Status} by user {UserId}", 
            inviteId, invite.Status, userId);

        return NoContent();
    }
}
