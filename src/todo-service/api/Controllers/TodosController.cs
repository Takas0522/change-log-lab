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
    public async Task<ActionResult<IEnumerable<TodoResponse>>> GetTodos(
        Guid listId, 
        [FromQuery] string? status = null,
        [FromQuery] Guid? labelId = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? dueDateFrom = null,
        [FromQuery] DateTime? dueDateTo = null)
    {
        var userId = GetUserId();
        var (hasAccess, _) = await CheckListAccess(listId, userId);

        if (!hasAccess)
        {
            return Forbid();
        }

        var query = _context.Todos
            .Include(t => t.TodoLabels)
                .ThenInclude(tl => tl.Label)
            .Where(t => t.ListId == listId);

        // Apply filters
        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(t => t.Status == status);
        }

        if (labelId.HasValue)
        {
            query = query.Where(t => t.TodoLabels.Any(tl => tl.LabelId == labelId.Value));
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(t => t.Title.Contains(search) || (t.Description != null && t.Description.Contains(search)));
        }

        if (dueDateFrom.HasValue)
        {
            query = query.Where(t => t.DueDate >= dueDateFrom.Value);
        }

        if (dueDateTo.HasValue)
        {
            query = query.Where(t => t.DueDate <= dueDateTo.Value);
        }

        var todos = await query
            .OrderBy(t => t.Position)
            .Select(t => new TodoResponse(
                t.Id,
                t.ListId,
                t.Title,
                t.Description,
                t.IsCompleted,
                t.Status,
                t.DueDate,
                t.Position,
                t.CreatedAt,
                t.UpdatedAt,
                t.TodoLabels.Select(tl => new LabelDto(
                    tl.Label.Id,
                    tl.Label.ListId,
                    tl.Label.Name,
                    tl.Label.Color,
                    tl.Label.CreatedAt,
                    tl.Label.UpdatedAt
                )).ToList()
            ))
            .ToListAsync();

        return Ok(todos);
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
            todo.Status,
            todo.DueDate,
            todo.Position,
            todo.CreatedAt,
            todo.UpdatedAt,
            todo.TodoLabels.Select(tl => new LabelDto(
                tl.Label.Id,
                tl.Label.ListId,
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
            Status = request.Status ?? "not_started",
            DueDate = request.DueDate,
            Position = position,
            IsCompleted = false
        };

        _context.Todos.Add(todo);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Todo created: {TodoId} in list {ListId} by user {UserId}", 
            todo.Id, listId, userId);

        var response = new TodoResponse(
            todo.Id,
            todo.ListId,
            todo.Title,
            todo.Description,
            todo.IsCompleted,
            todo.Status,
            todo.DueDate,
            todo.Position,
            todo.CreatedAt,
            todo.UpdatedAt,
            new List<LabelDto>()
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
            .Include(t => t.TodoLabels)
                .ThenInclude(tl => tl.Label)
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

        if (request.Status != null)
        {
            todo.Status = request.Status;
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

        _logger.LogInformation("Todo updated: {TodoId} in list {ListId} by user {UserId}", 
            id, listId, userId);

        var response = new TodoResponse(
            todo.Id,
            todo.ListId,
            todo.Title,
            todo.Description,
            todo.IsCompleted,
            todo.Status,
            todo.DueDate,
            todo.Position,
            todo.CreatedAt,
            todo.UpdatedAt,
            todo.TodoLabels.Select(tl => new LabelDto(
                tl.Label.Id,
                tl.Label.ListId,
                tl.Label.Name,
                tl.Label.Color,
                tl.Label.CreatedAt,
                tl.Label.UpdatedAt
            )).ToList()
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
