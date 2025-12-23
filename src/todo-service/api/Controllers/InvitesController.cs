using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.DTOs;
using TodoApi.Models;

namespace TodoApi.Controllers;

/// <summary>
/// Controller for managing list invitations and access control
/// </summary>
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

    /// <summary>
    /// Create an invitation to share a list with a user as viewer
    /// </summary>
    /// <param name="listId">The list to share</param>
    /// <param name="request">Invitation details</param>
    /// <returns>The created invitation</returns>
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

        // Check if user has permission to invite (owner or editor)
        var isOwner = list.OwnerId == userId;
        var member = list.Members.FirstOrDefault(m => m.UserId == userId);
        var userRole = isOwner ? "owner" : member?.Role ?? null;

        if (userRole == null || userRole == "viewer")
        {
            return Forbid();
        }

        // Check if user is trying to invite themselves
        if (request.InviteeUserId == userId)
        {
            return BadRequest(new { message = "Cannot invite yourself" });
        }

        // Check if invitee is already owner
        if (list.OwnerId == request.InviteeUserId)
        {
            return BadRequest(new { message = "User is already the owner of this list" });
        }

        // Check if invitee is already a member
        var existingMember = list.Members.FirstOrDefault(m => m.UserId == request.InviteeUserId);
        if (existingMember != null)
        {
            return Conflict(new { message = "User is already a member of this list" });
        }

        // Check if there's already a pending invite
        var existingInvite = await _context.ListInvites
            .FirstOrDefaultAsync(i => i.ListId == listId && i.InviteeUserId == request.InviteeUserId && i.Status == "pending");

        if (existingInvite != null)
        {
            return Conflict(new { message = "There is already a pending invitation for this user" });
        }

        // Create the invitation
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

        _logger.LogInformation("Invitation created: {InviteId} for list {ListId} by user {UserId} to user {InviteeUserId}",
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

    /// <summary>
    /// Get a specific invitation
    /// </summary>
    /// <param name="inviteId">The invitation ID</param>
    /// <returns>The invitation details</returns>
    [HttpGet("invites/{inviteId}")]
    public async Task<ActionResult<InviteResponse>> GetInvite(Guid inviteId)
    {
        var userId = GetUserId();

        var invite = await _context.ListInvites.FindAsync(inviteId);

        if (invite == null)
        {
            return NotFound(new { message = "Invitation not found" });
        }

        // Only the inviter or invitee can view the invitation
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

    /// <summary>
    /// Accept an invitation to access a list
    /// </summary>
    /// <param name="inviteId">The invitation ID</param>
    /// <returns>No content on success</returns>
    [HttpPost("invites/{inviteId}/accept")]
    public async Task<IActionResult> AcceptInvite(Guid inviteId)
    {
        var userId = GetUserId();

        var invite = await _context.ListInvites
            .Include(i => i.List)
            .FirstOrDefaultAsync(i => i.Id == inviteId);

        if (invite == null)
        {
            return NotFound(new { message = "Invitation not found" });
        }

        // Only the invitee can accept the invitation
        if (invite.InviteeUserId != userId)
        {
            return Forbid();
        }

        // Check if invitation is still pending
        if (invite.Status != "pending")
        {
            return BadRequest(new { message = $"Cannot accept invitation with status: {invite.Status}" });
        }

        // Check if user is already a member (shouldn't happen but check for safety)
        var existingMember = await _context.ListMembers
            .FirstOrDefaultAsync(m => m.ListId == invite.ListId && m.UserId == userId);

        if (existingMember != null)
        {
            return Conflict(new { message = "User is already a member of this list" });
        }

        // Create member with viewer role
        var member = new ListMember
        {
            Id = Guid.NewGuid(),
            ListId = invite.ListId,
            UserId = userId,
            Role = "viewer"
        };

        _context.ListMembers.Add(member);

        // Update invitation status
        invite.Status = "accepted";

        await _context.SaveChangesAsync();

        _logger.LogInformation("Invitation accepted: {InviteId} by user {UserId}, granted viewer access to list {ListId}",
            inviteId, userId, invite.ListId);

        return NoContent();
    }

    /// <summary>
    /// Cancel or reject an invitation
    /// </summary>
    /// <param name="inviteId">The invitation ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("invites/{inviteId}")]
    public async Task<IActionResult> DeleteInvite(Guid inviteId)
    {
        var userId = GetUserId();

        var invite = await _context.ListInvites.FindAsync(inviteId);

        if (invite == null)
        {
            return NotFound(new { message = "Invitation not found" });
        }

        // Inviter can cancel, invitee can reject
        var isInviter = invite.InviterUserId == userId;
        var isInvitee = invite.InviteeUserId == userId;

        if (!isInviter && !isInvitee)
        {
            return Forbid();
        }

        // Check if invitation is still pending
        if (invite.Status != "pending")
        {
            return BadRequest(new { message = $"Cannot delete invitation with status: {invite.Status}" });
        }

        // Update status based on who is deleting
        invite.Status = isInviter ? "cancelled" : "rejected";

        await _context.SaveChangesAsync();

        _logger.LogInformation("Invitation {InviteId} {Action} by user {UserId}",
            inviteId, invite.Status, userId);

        return NoContent();
    }

    /// <summary>
    /// Remove a user's access to a list
    /// </summary>
    /// <param name="listId">The list ID</param>
    /// <param name="userId">The user ID to remove</param>
    /// <returns>No content on success</returns>
    [HttpDelete("lists/{listId}/access/{userId}")]
    public async Task<IActionResult> RemoveAccess(Guid listId, Guid userId)
    {
        var currentUserId = GetUserId();

        var list = await _context.Lists
            .Include(l => l.Members)
            .FirstOrDefaultAsync(l => l.Id == listId);

        if (list == null)
        {
            return NotFound(new { message = "List not found" });
        }

        // Only owner or editor can remove access
        var isOwner = list.OwnerId == currentUserId;
        var currentUserMember = list.Members.FirstOrDefault(m => m.UserId == currentUserId);
        var currentUserRole = isOwner ? "owner" : currentUserMember?.Role ?? null;

        if (currentUserRole == null || currentUserRole == "viewer")
        {
            return Forbid();
        }

        // Cannot remove the owner's access
        if (list.OwnerId == userId)
        {
            return BadRequest(new { message = "Cannot remove the owner's access" });
        }

        // Find the member to remove
        var memberToRemove = list.Members.FirstOrDefault(m => m.UserId == userId);

        if (memberToRemove == null)
        {
            return NotFound(new { message = "User does not have access to this list" });
        }

        _context.ListMembers.Remove(memberToRemove);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Access removed: User {UserId} removed from list {ListId} by user {CurrentUserId}",
            userId, listId, currentUserId);

        return NoContent();
    }
}
