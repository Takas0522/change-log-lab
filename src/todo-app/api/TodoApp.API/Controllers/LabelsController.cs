using Microsoft.AspNetCore.Mvc;
using TodoApp.Application.DTOs;
using TodoApp.Application.Common;

namespace TodoApp.API.Controllers;

/// <summary>
/// ラベル管理API
/// REQ-FUNC-008～012対応
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class LabelsController : ControllerBase
{
    private readonly ILogger<LabelsController> _logger;

    public LabelsController(ILogger<LabelsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// ラベル一覧を取得する
    /// REQ-FUNC-009対応
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<LabelDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLabels()
    {
        _logger.LogInformation("GetLabels called");
        
        // TODO: Service実装後に差し替え
        var response = new ApiResponse<List<LabelDto>>
        {
            Success = true,
            Data = new List<LabelDto>(),
            Meta = new ResponseMeta
            {
                Timestamp = DateTime.UtcNow,
                RequestId = HttpContext.TraceIdentifier
            }
        };
        
        return Ok(response);
    }

    /// <summary>
    /// ラベルを作成する
    /// REQ-FUNC-010対応
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<LabelDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateLabel([FromBody] CreateLabelRequest request)
    {
        _logger.LogInformation("CreateLabel called with Name={Name}", request.Name);
        
        // TODO: Service実装後に差し替え
        var label = new LabelDto
        {
            LabelId = 1,
            Name = request.Name,
            Color = request.Color,
            CreatedAt = DateTime.UtcNow
        };
        
        var response = new ApiResponse<LabelDto>
        {
            Success = true,
            Data = label,
            Meta = new ResponseMeta
            {
                Timestamp = DateTime.UtcNow,
                RequestId = HttpContext.TraceIdentifier
            }
        };
        
        return CreatedAtAction(nameof(GetLabelById), new { id = label.LabelId }, response);
    }

    /// <summary>
    /// ラベルの詳細を取得する
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<LabelDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLabelById(int id)
    {
        _logger.LogInformation("GetLabelById called with ID={Id}", id);
        
        // TODO: Service実装後に差し替え
        return NotFound();
    }

    /// <summary>
    /// ラベルを更新する
    /// REQ-FUNC-011対応
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<LabelDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLabel(int id, [FromBody] UpdateLabelRequest request)
    {
        _logger.LogInformation("UpdateLabel called with ID={Id}", id);
        
        // TODO: Service実装後に差し替え
        return NotFound();
    }

    /// <summary>
    /// ラベルを削除する（論理削除）
    /// REQ-FUNC-012対応
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLabel(int id)
    {
        _logger.LogInformation("DeleteLabel called with ID={Id}", id);
        
        // TODO: Service実装後に差し替え
        return NoContent();
    }
}
