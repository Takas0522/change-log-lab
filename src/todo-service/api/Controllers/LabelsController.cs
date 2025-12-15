using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;
using TodoApi.Data;
using TodoApi.DTOs;
using TodoApi.Models;

namespace TodoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LabelsController : ControllerBase
{
    private readonly TodoDbContext _context;
    private readonly ILogger<LabelsController> _logger;
    private static readonly Regex HexColorRegex = new(@"^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

    public LabelsController(TodoDbContext context, ILogger<LabelsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? throw new UnauthorizedAccessException("User ID not found in token");
        return Guid.Parse(userIdClaim);
    }

    /// <summary>
    /// Get all labels for the current user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LabelResponse>>> GetLabels()
    {
        try
        {
            var userId = GetUserId();

            var labels = await _context.Labels
                .Where(l => l.UserId == userId)
                .OrderBy(l => l.CreatedAt)
                .ToListAsync();

            var response = labels.Select(l => new LabelResponse(
                l.Id,
                l.UserId,
                l.Name,
                l.Color,
                l.CreatedAt,
                l.UpdatedAt
            ));

            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting labels");
            return StatusCode(500, "An error occurred while retrieving labels");
        }
    }

    /// <summary>
    /// Get a specific label by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<LabelResponse>> GetLabel(Guid id)
    {
        try
        {
            var userId = GetUserId();

            var label = await _context.Labels
                .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);

            if (label == null)
            {
                return NotFound($"Label with ID {id} not found");
            }

            var response = new LabelResponse(
                label.Id,
                label.UserId,
                label.Name,
                label.Color,
                label.CreatedAt,
                label.UpdatedAt
            );

            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting label {LabelId}", id);
            return StatusCode(500, "An error occurred while retrieving the label");
        }
    }

    /// <summary>
    /// Create a new label
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<LabelResponse>> CreateLabel([FromBody] CreateLabelRequest request)
    {
        try
        {
            var userId = GetUserId();

            // Validate input
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest("Label name is required");
            }

            if (request.Name.Length > 100)
            {
                return BadRequest("Label name must not exceed 100 characters");
            }

            if (!HexColorRegex.IsMatch(request.Color))
            {
                return BadRequest("Color must be in HEX format (#RRGGBB)");
            }

            var label = new Label
            {
                UserId = userId,
                Name = request.Name.Trim(),
                Color = request.Color.ToUpperInvariant()
            };

            _context.Labels.Add(label);
            await _context.SaveChangesAsync();

            // Create outbox event
            var outboxEvent = new OutboxEvent
            {
                EventId = Guid.NewGuid(),
                EventType = "label_created",
                AggregateId = label.Id,
                AggregateType = "label",
                Payload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    labelId = label.Id,
                    userId = label.UserId,
                    name = label.Name,
                    color = label.Color
                })
            };

            _context.OutboxEvents.Add(outboxEvent);
            await _context.SaveChangesAsync();

            var response = new LabelResponse(
                label.Id,
                label.UserId,
                label.Name,
                label.Color,
                label.CreatedAt,
                label.UpdatedAt
            );

            return CreatedAtAction(nameof(GetLabel), new { id = label.Id }, response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating label");
            return StatusCode(500, "An error occurred while creating the label");
        }
    }

    /// <summary>
    /// Update an existing label
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<LabelResponse>> UpdateLabel(Guid id, [FromBody] UpdateLabelRequest request)
    {
        try
        {
            var userId = GetUserId();

            var label = await _context.Labels
                .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);

            if (label == null)
            {
                return NotFound($"Label with ID {id} not found");
            }

            // Validate and update fields
            if (request.Name != null)
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest("Label name cannot be empty");
                }

                if (request.Name.Length > 100)
                {
                    return BadRequest("Label name must not exceed 100 characters");
                }

                label.Name = request.Name.Trim();
            }

            if (request.Color != null)
            {
                if (!HexColorRegex.IsMatch(request.Color))
                {
                    return BadRequest("Color must be in HEX format (#RRGGBB)");
                }

                label.Color = request.Color.ToUpperInvariant();
            }

            await _context.SaveChangesAsync();

            // Create outbox event
            var outboxEvent = new OutboxEvent
            {
                EventId = Guid.NewGuid(),
                EventType = "label_updated",
                AggregateId = label.Id,
                AggregateType = "label",
                Payload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    labelId = label.Id,
                    userId = label.UserId,
                    name = label.Name,
                    color = label.Color
                })
            };

            _context.OutboxEvents.Add(outboxEvent);
            await _context.SaveChangesAsync();

            var response = new LabelResponse(
                label.Id,
                label.UserId,
                label.Name,
                label.Color,
                label.CreatedAt,
                label.UpdatedAt
            );

            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating label {LabelId}", id);
            return StatusCode(500, "An error occurred while updating the label");
        }
    }

    /// <summary>
    /// Delete a label (will automatically remove from all todos)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteLabel(Guid id)
    {
        try
        {
            var userId = GetUserId();

            var label = await _context.Labels
                .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);

            if (label == null)
            {
                return NotFound($"Label with ID {id} not found");
            }

            _context.Labels.Remove(label);

            // Create outbox event
            var outboxEvent = new OutboxEvent
            {
                EventId = Guid.NewGuid(),
                EventType = "label_deleted",
                AggregateId = label.Id,
                AggregateType = "label",
                Payload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    labelId = label.Id,
                    userId = label.UserId
                })
            };

            _context.OutboxEvents.Add(outboxEvent);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting label {LabelId}", id);
            return StatusCode(500, "An error occurred while deleting the label");
        }
    }
}
