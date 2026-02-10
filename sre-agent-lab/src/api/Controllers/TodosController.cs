using SreAgentLab.Data;
using SreAgentLab.DTOs;
using SreAgentLab.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace SreAgentLab.Controllers;

[ApiController]
[Route("api/todos")]
[Authorize]
public class TodosController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public TodosController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private static TodoResponse ToResponse(TodoItem todo) => new(
        todo.Id,
        todo.Title,
        todo.Body,
        todo.Status,
        todo.CreatedAt,
        todo.DueDate,
        todo.CompletedAt,
        todo.UpdatedAt
    );

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TodoResponse>>> GetTodos(
        [FromQuery] string? status,
        [FromQuery] string? title,
        [FromQuery] DateTime? dueDateFrom,
        [FromQuery] DateTime? dueDateTo)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized(new { message = "認証情報が無効です。" });

        var query = _dbContext.Todos
            .AsNoTracking()
            .Where(t => t.UserId == userId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(t => t.Status == status);

        if (!string.IsNullOrWhiteSpace(title))
            query = query.Where(t => t.Title.Contains(title));

        if (dueDateFrom.HasValue)
            query = query.Where(t => t.DueDate >= dueDateFrom.Value);

        if (dueDateTo.HasValue)
            query = query.Where(t => t.DueDate <= dueDateTo.Value);

        var todos = await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return Ok(todos.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TodoResponse>> GetTodo(Guid id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized(new { message = "認証情報が無効です。" });

        var todo = await _dbContext.Todos
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (todo == null) return NotFound(new { message = "ToDoが見つかりません。" });
        if (todo.UserId != userId.Value) return Forbid();

        return Ok(ToResponse(todo));
    }

    [HttpPost]
    public async Task<ActionResult<TodoResponse>> CreateTodo([FromBody] CreateTodoRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized(new { message = "認証情報が無効です。" });

        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { message = "タイトルは必須です。" });

        if (request.Title.Length > 255)
            return BadRequest(new { message = "タイトルは255文字以内で入力してください。" });

        var todo = new TodoItem
        {
            Id = Guid.NewGuid(),
            UserId = userId.Value,
            Title = request.Title,
            Body = request.Body,
            Status = TodoStatus.NotStarted,
            DueDate = request.DueDate
        };

        _dbContext.Todos.Add(todo);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTodo), new { id = todo.Id }, ToResponse(todo));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TodoResponse>> UpdateTodo(Guid id, [FromBody] UpdateTodoRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized(new { message = "認証情報が無効です。" });

        var todo = await _dbContext.Todos.FirstOrDefaultAsync(t => t.Id == id);
        if (todo == null) return NotFound(new { message = "ToDoが見つかりません。" });
        if (todo.UserId != userId.Value) return Forbid();

        if (request.Title != null)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                return BadRequest(new { message = "タイトルは必須です。" });
            if (request.Title.Length > 255)
                return BadRequest(new { message = "タイトルは255文字以内で入力してください。" });
            todo.Title = request.Title;
        }

        if (request.Body != null)
            todo.Body = request.Body;

        if (request.Status != null)
        {
            if (!TodoStatus.IsValid(request.Status))
                return BadRequest(new { message = "ステータスが不正です。未着手/着手中/完了 のいずれかを指定してください。" });
            todo.Status = request.Status;
        }

        if (request.DueDate.HasValue)
            todo.DueDate = request.DueDate;

        await _dbContext.SaveChangesAsync();

        return Ok(ToResponse(todo));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<TodoResponse>> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized(new { message = "認証情報が無効です。" });

        if (!TodoStatus.IsValid(request.Status))
            return BadRequest(new { message = "ステータスが不正です。未着手/着手中/完了 のいずれかを指定してください。" });

        var todo = await _dbContext.Todos.FirstOrDefaultAsync(t => t.Id == id);
        if (todo == null) return NotFound(new { message = "ToDoが見つかりません。" });
        if (todo.UserId != userId.Value) return Forbid();

        todo.Status = request.Status;
        await _dbContext.SaveChangesAsync();

        return Ok(ToResponse(todo));
    }

    [HttpPost("bomb")]
    public async Task<IActionResult> Bomb()
    {
        // 意図的に存在しないテーブルにアクセスしてエラーを発生させる
        await _dbContext.Database.ExecuteSqlRawAsync("SELECT * FROM non_existent_table");
        return Ok();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTodo(Guid id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized(new { message = "認証情報が無効です。" });

        var todo = await _dbContext.Todos.FirstOrDefaultAsync(t => t.Id == id);
        if (todo == null) return NotFound(new { message = "ToDoが見つかりません。" });
        if (todo.UserId != userId.Value) return Forbid();

        _dbContext.Todos.Remove(todo);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
