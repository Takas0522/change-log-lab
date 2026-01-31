using Microsoft.AspNetCore.Mvc;
using TodoApp.Application.DTOs;
using TodoApp.Application.Common;
using TodoApp.Application.Services.Interfaces;

namespace TodoApp.API.Controllers;

/// <summary>
/// ToDo管理API
/// REQ-FUNC-001～007対応
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class TodosController : ControllerBase
{
    private readonly ILogger<TodosController> _logger;
    private readonly ITodoService _todoService;

    public TodosController(
        ILogger<TodosController> logger,
        ITodoService todoService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _todoService = todoService ?? throw new ArgumentNullException(nameof(todoService));
    }

    /// <summary>
    /// ToDo一覧を取得する
    /// REQ-FUNC-001対応
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<TodoDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTodos([FromQuery] TodoQueryParameters parameters)
    {
        _logger.LogInformation("Getting todos with parameters: Page={Page}, PageSize={PageSize}", 
            parameters.Page, parameters.PageSize);
        
        var result = await _todoService.GetTodosAsync(parameters);
        
        var response = new ApiResponse<PagedResult<TodoDto>>
        {
            Success = true,
            Data = result,
            Meta = new ResponseMeta
            {
                Timestamp = DateTime.UtcNow,
                RequestId = HttpContext.TraceIdentifier,
                Total = result.TotalCount,
                Page = parameters.Page,
                PageSize = parameters.PageSize
            }
        };
        
        return Ok(response);
    }

    /// <summary>
    /// ToDoの詳細を取得する
    /// REQ-FUNC-002対応
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TodoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTodoById(long id)
    {
        _logger.LogInformation("Getting todo by id: {TodoId}", id);
        
        var todo = await _todoService.GetTodoByIdAsync(id);
        if (todo == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Errors = new List<ApiError>
                {
                    new ApiError { Code = "NOT_FOUND", Message = $"Todo with id {id} not found" }
                },
                Meta = new ResponseMeta
                {
                    Timestamp = DateTime.UtcNow,
                    RequestId = HttpContext.TraceIdentifier
                }
            });
        }
        
        var response = new ApiResponse<TodoDto>
        {
            Success = true,
            Data = todo,
            Meta = new ResponseMeta
            {
                Timestamp = DateTime.UtcNow,
                RequestId = HttpContext.TraceIdentifier
            }
        };
        
        return Ok(response);
    }

    /// <summary>
    /// 新しいToDoを作成する
    /// REQ-FUNC-001対応
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TodoDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTodo([FromBody] CreateTodoRequest request)
    {
        _logger.LogInformation("Creating new todo with title: {Title}", request.Title);
        
        var todo = await _todoService.CreateTodoAsync(request);
        
        var response = new ApiResponse<TodoDto>
        {
            Success = true,
            Data = todo,
            Meta = new ResponseMeta
            {
                Timestamp = DateTime.UtcNow,
                RequestId = HttpContext.TraceIdentifier
            }
        };
        
        return CreatedAtAction(nameof(GetTodoById), new { id = todo.TodoId }, response);
    }

    /// <summary>
    /// ToDoを更新する
    /// REQ-FUNC-003対応
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TodoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTodo(long id, [FromBody] UpdateTodoRequest request)
    {
        _logger.LogInformation("Updating todo: {TodoId}", id);
        
        var todo = await _todoService.UpdateTodoAsync(id, request);
        
        var response = new ApiResponse<TodoDto>
        {
            Success = true,
            Data = todo,
            Meta = new ResponseMeta
            {
                Timestamp = DateTime.UtcNow,
                RequestId = HttpContext.TraceIdentifier
            }
        };
        
        return Ok(response);
    }

    /// <summary>
    /// ToDoを削除する（論理削除）
    /// REQ-FUNC-004, REQ-DATA-002対応
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTodo(long id)
    {
        _logger.LogInformation("Deleting todo: {TodoId}", id);
        
        await _todoService.DeleteTodoAsync(id);
        
        return NoContent();
    }

    /// <summary>
    /// ToDoのステータスを更新する
    /// REQ-FUNC-007対応
    /// </summary>
    [HttpPatch("{id}/status")]
    [ProducesResponseType(typeof(ApiResponse<TodoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTodoStatus(long id, [FromBody] UpdateTodoStatusRequest request)
    {
        _logger.LogInformation("Updating todo status: {TodoId} to {Status}", id, request.Status);
        
        var todo = await _todoService.UpdateStatusAsync(id, request);
        
        var response = new ApiResponse<TodoDto>
        {
            Success = true,
            Data = todo,
            Meta = new ResponseMeta
            {
                Timestamp = DateTime.UtcNow,
                RequestId = HttpContext.TraceIdentifier
            }
        };
        
        return Ok(response);
    }
}
