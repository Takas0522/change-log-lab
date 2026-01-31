using Microsoft.AspNetCore.Mvc;
using TodoApp.Application.DTOs;
using TodoApp.Application.Common;

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

    public TodosController(ILogger<TodosController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// ToDo一覧を取得する
    /// REQ-FUNC-001対応
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<TodoDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTodos([FromQuery] TodoQueryParameters parameters)
    {
        _logger.LogInformation("GetTodos called with Page={Page}, PageSize={PageSize}", 
            parameters.Page, parameters.PageSize);
        
        // TODO: Service実装後に差し替え
        var result = new PagedResult<TodoDto>
        {
            Items = new List<TodoDto>(),
            TotalCount = 0,
            Page = parameters.Page,
            PageSize = parameters.PageSize
        };
        
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
        _logger.LogInformation("GetTodoById called with ID={Id}", id);
        
        // TODO: Service実装後に差し替え
        return NotFound(new ApiResponse<object>
        {
            Success = false,
            Errors = new List<ApiError>
            {
                new ApiError { Code = "NOT_FOUND", Message = $"Todo with ID {id} not found" }
            },
            Meta = new ResponseMeta
            {
                Timestamp = DateTime.UtcNow,
                RequestId = HttpContext.TraceIdentifier
            }
        });
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
        _logger.LogInformation("CreateTodo called with Title={Title}", request.Title);
        
        // TODO: Service実装後に差し替え
        var todo = new TodoDto
        {
            TodoId = 1,
            Title = request.Title,
            Content = request.Content,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Labels = new List<LabelDto>()
        };
        
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
        _logger.LogInformation("UpdateTodo called with ID={Id}", id);
        
        // TODO: Service実装後に差し替え
        return NotFound();
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
        _logger.LogInformation("DeleteTodo called with ID={Id}", id);
        
        // TODO: Service実装後に差し替え
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
        _logger.LogInformation("UpdateTodoStatus called with ID={Id}, Status={Status}", 
            id, request.Status);
        
        // TODO: Service実装後に差し替え
        return NotFound();
    }
}
