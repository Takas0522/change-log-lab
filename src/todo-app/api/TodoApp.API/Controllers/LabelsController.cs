using Microsoft.AspNetCore.Mvc;
using TodoApp.Application.DTOs;
using TodoApp.Application.Common;
using TodoApp.Application.Services.Interfaces;

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
    private readonly ILabelService _labelService;

    public LabelsController(
        ILogger<LabelsController> logger,
        ILabelService labelService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _labelService = labelService ?? throw new ArgumentNullException(nameof(labelService));
    }

    /// <summary>
    /// ラベル一覧を取得する
    /// REQ-FUNC-009対応
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<LabelDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLabels()
    {
        _logger.LogInformation("Getting all labels");
        
        var labels = await _labelService.GetAllLabelsAsync();
        
        var response = new ApiResponse<List<LabelDto>>
        {
            Success = true,
            Data = labels,
            Meta = new ResponseMeta
            {
                Timestamp = DateTime.UtcNow,
                RequestId = HttpContext.TraceIdentifier,
                Total = labels.Count
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
        _logger.LogInformation("Creating new label with name: {Name}", request.Name);
        
        var label = await _labelService.CreateLabelAsync(request);
        
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
        _logger.LogInformation("Getting label by id: {LabelId}", id);
        
        var label = await _labelService.GetLabelByIdAsync(id);
        if (label == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Errors = new List<ApiError>
                {
                    new ApiError { Code = "NOT_FOUND", Message = $"Label with id {id} not found" }
                },
                Meta = new ResponseMeta
                {
                    Timestamp = DateTime.UtcNow,
                    RequestId = HttpContext.TraceIdentifier
                }
            });
        }
        
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
        
        return Ok(response);
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
        _logger.LogInformation("Updating label: {LabelId}", id);
        
        var label = await _labelService.UpdateLabelAsync(id, request);
        
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
        
        return Ok(response);
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
        _logger.LogInformation("Deleting label: {LabelId}", id);
        
        await _labelService.DeleteLabelAsync(id);
        
        return NoContent();
    }
}
