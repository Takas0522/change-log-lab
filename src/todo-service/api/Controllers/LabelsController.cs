using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.DTOs;
using TodoApi.Models;

namespace TodoApi.Controllers;

[ApiController]
[Route("api/lists/{listId}/labels")]
[Authorize]
public class LabelsController : ControllerBase
{
    private readonly TodoDbContext _context;
    private readonly ILogger<LabelsController> _logger;

    public LabelsController(TodoDbContext context, ILogger<LabelsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("user_id")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID in token");
        }
        return userId;
    }

    private async Task<(List list, string role)> GetListWithUserRoleAsync(Guid listId, Guid userId)
    {
        var list = await _context.Lists
            .Include(l => l.Members)
            .FirstOrDefaultAsync(l => l.Id == listId);

        if (list == null)
        {
            throw new InvalidOperationException("List not found");
        }

        var role = "viewer";
        if (list.OwnerId == userId)
        {
            role = "owner";
        }
        else
        {
            var member = list.Members.FirstOrDefault(m => m.UserId == userId);
            if (member != null)
            {
                role = member.Role;
            }
        }

        return (list, role);
    }

    /// <summary>
    /// Get all labels for a list
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<LabelDto>>> GetLabels(Guid listId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var (list, role) = await GetListWithUserRoleAsync(listId, userId);

            var labels = await _context.Labels
                .Where(l => l.ListId == listId)
                .OrderBy(l => l.Name)
                .Select(l => new LabelDto(
                    l.Id,
                    l.ListId,
                    l.Name,
                    l.Color,
                    l.CreatedAt,
                    l.UpdatedAt
                ))
                .ToListAsync();

            return Ok(labels);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Create a new label
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<LabelDto>> CreateLabel(Guid listId, CreateLabelRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var (list, role) = await GetListWithUserRoleAsync(listId, userId);

            // Only owner and editor can create labels
            if (role == "viewer")
            {
                return Forbid();
            }

            // Validate color format (hex color)
            if (!System.Text.RegularExpressions.Regex.IsMatch(request.Color, "^#[0-9A-Fa-f]{6}$"))
            {
                return BadRequest("Color must be a valid hex color code (e.g., #FF5733)");
            }

            // Check if label with same name already exists in this list
            var existingLabel = await _context.Labels
                .FirstOrDefaultAsync(l => l.ListId == listId && l.Name == request.Name);

            if (existingLabel != null)
            {
                return Conflict("A label with this name already exists in this list");
            }

            var label = new Label
            {
                Id = Guid.NewGuid(),
                ListId = listId,
                Name = request.Name,
                Color = request.Color.ToUpper()
            };

            _context.Labels.Add(label);
            await _context.SaveChangesAsync();

            var labelDto = new LabelDto(
                label.Id,
                label.ListId,
                label.Name,
                label.Color,
                label.CreatedAt,
                label.UpdatedAt
            );

            return CreatedAtAction(nameof(GetLabel), new { listId, labelId = label.Id }, labelDto);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Get a specific label
    /// </summary>
    [HttpGet("{labelId}")]
    public async Task<ActionResult<LabelDto>> GetLabel(Guid listId, Guid labelId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var (list, role) = await GetListWithUserRoleAsync(listId, userId);

            var label = await _context.Labels
                .FirstOrDefaultAsync(l => l.Id == labelId && l.ListId == listId);

            if (label == null)
            {
                return NotFound("Label not found");
            }

            var labelDto = new LabelDto(
                label.Id,
                label.ListId,
                label.Name,
                label.Color,
                label.CreatedAt,
                label.UpdatedAt
            );

            return Ok(labelDto);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Update a label
    /// </summary>
    [HttpPut("{labelId}")]
    public async Task<ActionResult<LabelDto>> UpdateLabel(Guid listId, Guid labelId, UpdateLabelRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var (list, role) = await GetListWithUserRoleAsync(listId, userId);

            // Only owner and editor can update labels
            if (role == "viewer")
            {
                return Forbid();
            }

            var label = await _context.Labels
                .FirstOrDefaultAsync(l => l.Id == labelId && l.ListId == listId);

            if (label == null)
            {
                return NotFound("Label not found");
            }

            if (request.Name != null)
            {
                // Check for duplicate name
                var existingLabel = await _context.Labels
                    .FirstOrDefaultAsync(l => l.ListId == listId && l.Name == request.Name && l.Id != labelId);

                if (existingLabel != null)
                {
                    return Conflict("A label with this name already exists in this list");
                }

                label.Name = request.Name;
            }

            if (request.Color != null)
            {
                // Validate color format
                if (!System.Text.RegularExpressions.Regex.IsMatch(request.Color, "^#[0-9A-Fa-f]{6}$"))
                {
                    return BadRequest("Color must be a valid hex color code (e.g., #FF5733)");
                }

                label.Color = request.Color.ToUpper();
            }

            await _context.SaveChangesAsync();

            var labelDto = new LabelDto(
                label.Id,
                label.ListId,
                label.Name,
                label.Color,
                label.CreatedAt,
                label.UpdatedAt
            );

            return Ok(labelDto);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Delete a label
    /// </summary>
    [HttpDelete("{labelId}")]
    public async Task<ActionResult> DeleteLabel(Guid listId, Guid labelId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var (list, role) = await GetListWithUserRoleAsync(listId, userId);

            // Only owner and editor can delete labels
            if (role == "viewer")
            {
                return Forbid();
            }

            var label = await _context.Labels
                .FirstOrDefaultAsync(l => l.Id == labelId && l.ListId == listId);

            if (label == null)
            {
                return NotFound("Label not found");
            }

            _context.Labels.Remove(label);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Assign a label to a todo
    /// </summary>
    [HttpPost("../todos/{todoId}/labels")]
    public async Task<ActionResult> AssignLabel(Guid listId, Guid todoId, AssignLabelRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var (list, role) = await GetListWithUserRoleAsync(listId, userId);

            // Only owner and editor can assign labels
            if (role == "viewer")
            {
                return Forbid();
            }

            // Verify todo exists and belongs to the list
            var todo = await _context.Todos
                .FirstOrDefaultAsync(t => t.Id == todoId && t.ListId == listId);

            if (todo == null)
            {
                return NotFound("Todo not found");
            }

            // Verify label exists and belongs to the list
            var label = await _context.Labels
                .FirstOrDefaultAsync(l => l.Id == request.LabelId && l.ListId == listId);

            if (label == null)
            {
                return NotFound("Label not found");
            }

            // Check if already assigned
            var existingAssignment = await _context.TodoLabels
                .FirstOrDefaultAsync(tl => tl.TodoId == todoId && tl.LabelId == request.LabelId);

            if (existingAssignment != null)
            {
                return Conflict("Label already assigned to this todo");
            }

            var todoLabel = new TodoLabel
            {
                Id = Guid.NewGuid(),
                TodoId = todoId,
                LabelId = request.LabelId,
                CreatedAt = DateTime.UtcNow
            };

            _context.TodoLabels.Add(todoLabel);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Remove a label from a todo
    /// </summary>
    [HttpDelete("../todos/{todoId}/labels/{labelId}")]
    public async Task<ActionResult> RemoveLabel(Guid listId, Guid todoId, Guid labelId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var (list, role) = await GetListWithUserRoleAsync(listId, userId);

            // Only owner and editor can remove labels
            if (role == "viewer")
            {
                return Forbid();
            }

            var todoLabel = await _context.TodoLabels
                .Include(tl => tl.Todo)
                .FirstOrDefaultAsync(tl => tl.TodoId == todoId && tl.LabelId == labelId && tl.Todo.ListId == listId);

            if (todoLabel == null)
            {
                return NotFound("Label assignment not found");
            }

            _context.TodoLabels.Remove(todoLabel);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
