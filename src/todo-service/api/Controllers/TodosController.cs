using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.DTOs;
using TodoApi.Models;

namespace TodoApi.Controllers;

[ApiController]
[Route("api/lists/{listId}/[controller]")]
[Authorize]
public class TodosController : ControllerBase
{
    private readonly TodoDbContext _context;
    private readonly ILogger<TodosController> _logger;

    public TodosController(TodoDbContext context, ILogger<TodosController> logger)
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

    private async Task<(bool hasAccess, string? role)> CheckListAccess(Guid listId, Guid userId)
    {
        var list = await _context.Lists
            .Include(l => l.Members)
            .FirstOrDefaultAsync(l => l.Id == listId);

        if (list == null)
        {
            return (false, null);
        }

        if (list.OwnerId == userId)
        {
            return (true, "owner");
        }

        var member = list.Members.FirstOrDefault(m => m.UserId == userId);
        if (member != null)
        {
            return (true, member.Role);
        }

        return (false, null);
    }

    // GET: api/lists/{listId}/todos
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TodoResponse>>> GetTodos(Guid listId, [FromQuery] string? labelIds = null)
    {
        var userId = GetUserId();
        var (hasAccess, _) = await CheckListAccess(listId, userId);

        if (!hasAccess)
        {
            return Forbid();
        }

        var query = _context.Todos
            .Where(t => t.ListId == listId)
            .Include(t => t.TodoLabels)
            .ThenInclude(tl => tl.Label)
            .AsQueryable();

        // Filter by label IDs if provided
        if (!string.IsNullOrWhiteSpace(labelIds))
        {
            var labelIdList = labelIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => Guid.TryParse(id.Trim(), out var guid) ? guid : Guid.Empty)
                .Where(id => id != Guid.Empty)
                .ToList();

            if (labelIdList.Any())
            {
                query = query.Where(t => t.TodoLabels.Any(tl => labelIdList.Contains(tl.LabelId)));
            }
        }

        var todos = await query
            .OrderBy(t => t.Position)
            .ToListAsync();

        var response = todos.Select(t => new TodoResponse(
            t.Id,
            t.ListId,
            t.Title,
            t.Description,
            t.IsCompleted,
            t.DueDate,
            t.Position,
            t.CreatedAt,
            t.UpdatedAt,
            t.TodoLabels.Select(tl => new LabelResponse(
                tl.Label.Id,
                tl.Label.UserId,
                tl.Label.Name,
                tl.Label.Color,
                tl.Label.CreatedAt,
                tl.Label.UpdatedAt
            )).ToList()
        ));

        return Ok(response);
    }

    // GET: api/lists/{listId}/todos/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<TodoResponse>> GetTodo(Guid listId, Guid id)
    {
        var userId = GetUserId();
        var (hasAccess, _) = await CheckListAccess(listId, userId);

        if (!hasAccess)
        {
            return Forbid();
        }

        var todo = await _context.Todos
            .Include(t => t.TodoLabels)
            .ThenInclude(tl => tl.Label)
            .FirstOrDefaultAsync(t => t.Id == id && t.ListId == listId);

        if (todo == null)
        {
            return NotFound(new { message = "Todo not found" });
        }

        var response = new TodoResponse(
            todo.Id,
            todo.ListId,
            todo.Title,
            todo.Description,
            todo.IsCompleted,
            todo.DueDate,
            todo.Position,
            todo.CreatedAt,
            todo.UpdatedAt,
            todo.TodoLabels.Select(tl => new LabelResponse(
                tl.Label.Id,
                tl.Label.UserId,
                tl.Label.Name,
                tl.Label.Color,
                tl.Label.CreatedAt,
                tl.Label.UpdatedAt
            )).ToList()
        );

        return Ok(response);
    }

    // POST: api/lists/{listId}/todos
    [HttpPost]
    public async Task<ActionResult<TodoResponse>> CreateTodo(Guid listId, CreateTodoRequest request)
    {
        var userId = GetUserId();
        var (hasAccess, role) = await CheckListAccess(listId, userId);

        if (!hasAccess || role == "viewer")
        {
            return Forbid();
        }

        // Get the next position if not specified
        int position = request.Position ?? 0;
        if (request.Position == null)
        {
            var maxPosition = await _context.Todos
                .Where(t => t.ListId == listId)
                .MaxAsync(t => (int?)t.Position);
            position = (maxPosition ?? -1) + 1;
        }

        var todo = new Todo
        {
            Id = Guid.NewGuid(),
            ListId = listId,
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            Position = position,
            IsCompleted = false
        };

        _context.Todos.Add(todo);
        await _context.SaveChangesAsync();

        // Handle label assignments (max 10)
        if (request.LabelIds != null && request.LabelIds.Any())
        {
            if (request.LabelIds.Count > 10)
            {
                return BadRequest("Maximum 10 labels can be assigned to a todo");
            }

            // Verify labels belong to the user
            var userLabels = await _context.Labels
                .Where(l => l.UserId == userId && request.LabelIds.Contains(l.Id))
                .Select(l => l.Id)
                .ToListAsync();

            foreach (var labelId in userLabels)
            {
                var todoLabel = new TodoLabel
                {
                    TodoId = todo.Id,
                    LabelId = labelId
                };
                _context.TodoLabels.Add(todoLabel);
            }

            await _context.SaveChangesAsync();
        }

        // Load labels for response
        var todoWithLabels = await _context.Todos
            .Include(t => t.TodoLabels)
            .ThenInclude(tl => tl.Label)
            .FirstOrDefaultAsync(t => t.Id == todo.Id);

        _logger.LogInformation("Todo created: {TodoId} in list {ListId} by user {UserId}", 
            todo.Id, listId, userId);

        var response = new TodoResponse(
            todo.Id,
            todo.ListId,
            todo.Title,
            todo.Description,
            todo.IsCompleted,
            todo.DueDate,
            todo.Position,
            todo.CreatedAt,
            todo.UpdatedAt,
            todoWithLabels?.TodoLabels.Select(tl => new LabelResponse(
                tl.Label.Id,
                tl.Label.UserId,
                tl.Label.Name,
                tl.Label.Color,
                tl.Label.CreatedAt,
                tl.Label.UpdatedAt
            )).ToList() ?? new List<LabelResponse>()
        );

        return CreatedAtAction(nameof(GetTodo), new { listId, id = todo.Id }, response);
    }

    // PUT: api/lists/{listId}/todos/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<TodoResponse>> UpdateTodo(Guid listId, Guid id, UpdateTodoRequest request)
    {
        var userId = GetUserId();
        var (hasAccess, role) = await CheckListAccess(listId, userId);

        if (!hasAccess || role == "viewer")
        {
            return Forbid();
        }

        var todo = await _context.Todos
            .FirstOrDefaultAsync(t => t.Id == id && t.ListId == listId);

        if (todo == null)
        {
            return NotFound(new { message = "Todo not found" });
        }

        if (request.Title != null)
        {
            todo.Title = request.Title;
        }

        if (request.Description != null)
        {
            todo.Description = request.Description;
        }

        if (request.IsCompleted.HasValue)
        {
            todo.IsCompleted = request.IsCompleted.Value;
        }

        if (request.DueDate.HasValue)
        {
            todo.DueDate = request.DueDate.Value;
        }

        if (request.Position.HasValue)
        {
            todo.Position = request.Position.Value;
        }

        await _context.SaveChangesAsync();

        // Handle label updates
        if (request.LabelIds != null)
        {
            if (request.LabelIds.Count > 10)
            {
                return BadRequest("Maximum 10 labels can be assigned to a todo");
            }

            // Verify labels belong to the user
            var userLabels = await _context.Labels
                .Where(l => l.UserId == userId && request.LabelIds.Contains(l.Id))
                .Select(l => l.Id)
                .ToListAsync();

            // Remove existing labels
            var existingLabels = await _context.TodoLabels
                .Where(tl => tl.TodoId == id)
                .ToListAsync();
            _context.TodoLabels.RemoveRange(existingLabels);

            // Add new labels
            foreach (var labelId in userLabels)
            {
                var todoLabel = new TodoLabel
                {
                    TodoId = todo.Id,
                    LabelId = labelId
                };
                _context.TodoLabels.Add(todoLabel);
            }

            await _context.SaveChangesAsync();
        }

        // Load labels for response
        var todoWithLabels = await _context.Todos
            .Include(t => t.TodoLabels)
            .ThenInclude(tl => tl.Label)
            .FirstOrDefaultAsync(t => t.Id == id);

        _logger.LogInformation("Todo updated: {TodoId} in list {ListId} by user {UserId}", 
            id, listId, userId);

        var response = new TodoResponse(
            todo.Id,
            todo.ListId,
            todo.Title,
            todo.Description,
            todo.IsCompleted,
            todo.DueDate,
            todo.Position,
            todo.CreatedAt,
            todo.UpdatedAt,
            todoWithLabels?.TodoLabels.Select(tl => new LabelResponse(
                tl.Label.Id,
                tl.Label.UserId,
                tl.Label.Name,
                tl.Label.Color,
                tl.Label.CreatedAt,
                tl.Label.UpdatedAt
            )).ToList() ?? new List<LabelResponse>()
        );

        return Ok(response);
    }

    // DELETE: api/lists/{listId}/todos/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTodo(Guid listId, Guid id)
    {
        var userId = GetUserId();
        var (hasAccess, role) = await CheckListAccess(listId, userId);

        if (!hasAccess || role == "viewer")
        {
            return Forbid();
        }

        var todo = await _context.Todos
            .FirstOrDefaultAsync(t => t.Id == id && t.ListId == listId);

        if (todo == null)
        {
            return NotFound(new { message = "Todo not found" });
        }

        _context.Todos.Remove(todo);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Todo deleted: {TodoId} in list {ListId} by user {UserId}", 
            id, listId, userId);

        return NoContent();
    }
}
