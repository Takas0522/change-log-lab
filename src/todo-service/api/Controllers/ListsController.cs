using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.DTOs;
using TodoApi.Models;

namespace TodoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ListsController : ControllerBase
{
    private readonly TodoDbContext _context;
    private readonly ILogger<ListsController> _logger;

    public ListsController(TodoDbContext context, ILogger<ListsController> logger)
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

    // GET: api/lists
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ListResponse>>> GetLists()
    {
        var userId = GetUserId();

        // Get lists where user is owner or member
        var ownedLists = await _context.Lists
            .Where(l => l.OwnerId == userId)
            .Select(l => new ListResponse(
                l.Id,
                l.Title,
                l.Description,
                l.OwnerId,
                "owner",
                l.CreatedAt,
                l.UpdatedAt
            ))
            .ToListAsync();

        var memberLists = await _context.ListMembers
            .Where(lm => lm.UserId == userId)
            .Include(lm => lm.List)
            .Select(lm => new ListResponse(
                lm.List.Id,
                lm.List.Title,
                lm.List.Description,
                lm.List.OwnerId,
                lm.Role,
                lm.List.CreatedAt,
                lm.List.UpdatedAt
            ))
            .ToListAsync();

        var allLists = ownedLists.Concat(memberLists).DistinctBy(l => l.Id).ToList();

        return Ok(allLists);
    }

    // GET: api/lists/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<ListDetailResponse>> GetList(Guid id)
    {
        var userId = GetUserId();

        var list = await _context.Lists
            .Include(l => l.Todos.OrderBy(t => t.Position))
            .Include(l => l.Members)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (list == null)
        {
            return NotFound(new { message = "List not found" });
        }

        // Check if user has access
        var isOwner = list.OwnerId == userId;
        var member = list.Members.FirstOrDefault(m => m.UserId == userId);
        var userRole = isOwner ? "owner" : member?.Role ?? null;

        if (userRole == null)
        {
            return Forbid();
        }

        var todos = list.Todos.Select(t => new TodoResponse(
            t.Id,
            t.ListId,
            t.Title,
            t.Description,
            t.IsCompleted,
            t.DueDate,
            t.Position,
            t.CreatedAt,
            t.UpdatedAt
        )).ToList();

        var response = new ListDetailResponse(
            list.Id,
            list.Title,
            list.Description,
            list.OwnerId,
            userRole,
            list.CreatedAt,
            list.UpdatedAt,
            todos
        );

        return Ok(response);
    }

    // POST: api/lists
    [HttpPost]
    public async Task<ActionResult<ListResponse>> CreateList(CreateListRequest request)
    {
        var userId = GetUserId();

        var list = new List
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            OwnerId = userId
        };

        _context.Lists.Add(list);
        await _context.SaveChangesAsync();

        _logger.LogInformation("List created: {ListId} by user {UserId}", list.Id, userId);

        var response = new ListResponse(
            list.Id,
            list.Title,
            list.Description,
            list.OwnerId,
            "owner",
            list.CreatedAt,
            list.UpdatedAt
        );

        return CreatedAtAction(nameof(GetList), new { id = list.Id }, response);
    }

    // PUT: api/lists/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<ListResponse>> UpdateList(Guid id, UpdateListRequest request)
    {
        var userId = GetUserId();

        var list = await _context.Lists
            .Include(l => l.Members)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (list == null)
        {
            return NotFound(new { message = "List not found" });
        }

        // Check if user has write permission (owner or editor)
        var isOwner = list.OwnerId == userId;
        var member = list.Members.FirstOrDefault(m => m.UserId == userId);
        var userRole = isOwner ? "owner" : member?.Role ?? null;

        if (userRole == null || userRole == "viewer")
        {
            return Forbid();
        }

        if (request.Title != null)
        {
            list.Title = request.Title;
        }

        if (request.Description != null)
        {
            list.Description = request.Description;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("List updated: {ListId} by user {UserId}", list.Id, userId);

        var response = new ListResponse(
            list.Id,
            list.Title,
            list.Description,
            list.OwnerId,
            userRole,
            list.CreatedAt,
            list.UpdatedAt
        );

        return Ok(response);
    }

    // DELETE: api/lists/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteList(Guid id)
    {
        var userId = GetUserId();

        var list = await _context.Lists.FindAsync(id);

        if (list == null)
        {
            return NotFound(new { message = "List not found" });
        }

        // Only owner can delete
        if (list.OwnerId != userId)
        {
            return Forbid();
        }

        _context.Lists.Remove(list);
        await _context.SaveChangesAsync();

        _logger.LogInformation("List deleted: {ListId} by user {UserId}", id, userId);

        return NoContent();
    }

    // POST: api/lists/{id}/members
    [HttpPost("{id}/members")]
    public async Task<ActionResult<ListMemberResponse>> AddMember(Guid id, AddMemberRequest request)
    {
        var userId = GetUserId();

        var list = await _context.Lists.FindAsync(id);
        if (list == null)
        {
            return NotFound(new { message = "List not found" });
        }

        // Only owner can add members
        if (list.OwnerId != userId)
        {
            return Forbid();
        }

        // Check if member already exists
        var existingMember = await _context.ListMembers
            .FirstOrDefaultAsync(lm => lm.ListId == id && lm.UserId == request.UserId);

        if (existingMember != null)
        {
            return Conflict(new { message = "User is already a member of this list" });
        }

        var member = new ListMember
        {
            Id = Guid.NewGuid(),
            ListId = id,
            UserId = request.UserId,
            Role = request.Role
        };

        _context.ListMembers.Add(member);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Member added to list {ListId}: User {MemberUserId} with role {Role}", 
            id, request.UserId, request.Role);

        var response = new ListMemberResponse(
            member.Id,
            member.ListId,
            member.UserId,
            member.Role,
            member.CreatedAt,
            member.UpdatedAt
        );

        return CreatedAtAction(nameof(GetList), new { id }, response);
    }

    // DELETE: api/lists/{id}/members/{memberId}
    [HttpDelete("{id}/members/{memberId}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid memberId)
    {
        var userId = GetUserId();

        var list = await _context.Lists.FindAsync(id);
        if (list == null)
        {
            return NotFound(new { message = "List not found" });
        }

        // Only owner can remove members
        if (list.OwnerId != userId)
        {
            return Forbid();
        }

        var member = await _context.ListMembers
            .FirstOrDefaultAsync(lm => lm.Id == memberId && lm.ListId == id);

        if (member == null)
        {
            return NotFound(new { message = "Member not found" });
        }

        _context.ListMembers.Remove(member);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Member removed from list {ListId}: {MemberId}", id, memberId);

        return NoContent();
    }

    // DELETE: api/lists/{id}/access/{targetUserId}
    [HttpDelete("{id}/access/{targetUserId}")]
    public async Task<IActionResult> RemoveAccess(Guid id, Guid targetUserId)
    {
        var userId = GetUserId();

        var list = await _context.Lists
            .Include(l => l.Members)
            .FirstOrDefaultAsync(l => l.Id == id);

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

        // Find member by user ID
        var targetMember = list.Members.FirstOrDefault(m => m.UserId == targetUserId);

        if (targetMember == null)
        {
            return NotFound(new { message = "User does not have access to this list" });
        }

        // Only owner can remove other owners or editors
        if ((targetMember.Role == "owner" || targetMember.Role == "editor") && !isOwner)
        {
            return Forbid();
        }

        _context.ListMembers.Remove(targetMember);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Access removed from list {ListId}: User {UserId}", id, targetUserId);

        return NoContent();
    }
}
