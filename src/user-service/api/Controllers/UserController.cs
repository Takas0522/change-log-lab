using UserApi.Data;
using UserApi.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UserApi.Models;

namespace UserApi.Controllers;

[ApiController]
[Route("users")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly UserDbContext _dbContext;
    private readonly ILogger<UserController> _logger;

    public UserController(UserDbContext dbContext, ILogger<UserController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID in token");
        }
        
        return userId;
    }

    /// <summary>
    /// Get current user's profile
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileResponse>> GetMyProfile()
    {
        var userId = GetCurrentUserId();
        
        var profile = await _dbContext.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            return NotFound(new { error = "Profile not found" });
        }

        return Ok(new UserProfileResponse
        {
            UserId = profile.UserId,
            Email = profile.Email,
            DisplayName = profile.DisplayName,
            Bio = profile.Bio,
            AvatarUrl = profile.AvatarUrl,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        });
    }

    /// <summary>
    /// Update current user's profile
    /// </summary>
    [HttpPut("me")]
    public async Task<ActionResult<UserProfileResponse>> UpdateMyProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetCurrentUserId();
        
        var profile = await _dbContext.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            return NotFound(new { error = "Profile not found" });
        }

        // Update fields
        if (request.DisplayName != null)
        {
            profile.DisplayName = request.DisplayName;
        }
        if (request.Bio != null)
        {
            profile.Bio = request.Bio;
        }
        if (request.AvatarUrl != null)
        {
            profile.AvatarUrl = request.AvatarUrl;
        }
        
        profile.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Profile updated for user {UserId}", userId);

        return Ok(new UserProfileResponse
        {
            UserId = profile.UserId,
            Email = profile.Email,
            DisplayName = profile.DisplayName,
            Bio = profile.Bio,
            AvatarUrl = profile.AvatarUrl,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        });
    }

    /// <summary>
    /// Get user profile by ID
    /// </summary>
    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<UserProfileResponse>> GetUserProfile(Guid userId)
    {
        var profile = await _dbContext.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            return NotFound(new { error = "Profile not found" });
        }

        return Ok(new UserProfileResponse
        {
            UserId = profile.UserId,
            Email = profile.Email,
            DisplayName = profile.DisplayName,
            Bio = profile.Bio,
            AvatarUrl = profile.AvatarUrl,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        });
    }

    /// <summary>
    /// Search users by email or display name (for invitations)
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<UserSearchResponse>> SearchUsers(
        [FromQuery] string? q,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20)
    {
        if (take > 100)
        {
            take = 100; // Limit max results
        }

        var query = _dbContext.UserProfiles.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var searchTerm = q.Trim().ToLower();
            query = query.Where(p => 
                p.Email.ToLower().Contains(searchTerm) || 
                p.DisplayName.ToLower().Contains(searchTerm));
        }

        var totalCount = await query.CountAsync();
        
        var users = await query
            .OrderBy(p => p.DisplayName)
            .Skip(skip)
            .Take(take)
            .Select(p => new UserSearchResult
            {
                UserId = p.UserId,
                Email = p.Email,
                DisplayName = p.DisplayName,
                AvatarUrl = p.AvatarUrl
            })
            .ToListAsync();

        return Ok(new UserSearchResponse
        {
            Users = users,
            TotalCount = totalCount
        });
    }

    /// <summary>
    /// Create a user profile (internal use, typically called after user registration)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserProfileResponse>> CreateProfile([FromBody] CreateProfileRequest request)
    {
        // Check if profile already exists
        var existingProfile = await _dbContext.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == request.UserId);

        if (existingProfile != null)
        {
            return Conflict(new { error = "Profile already exists" });
        }

        var profile = new UserProfile
        {
            UserId = request.UserId,
            Email = request.Email,
            DisplayName = request.DisplayName,
            Bio = request.Bio,
            AvatarUrl = request.AvatarUrl,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.UserProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Profile created for user {UserId}", request.UserId);

        return CreatedAtAction(
            nameof(GetUserProfile), 
            new { userId = profile.UserId }, 
            new UserProfileResponse
            {
                UserId = profile.UserId,
                Email = profile.Email,
                DisplayName = profile.DisplayName,
                Bio = profile.Bio,
                AvatarUrl = profile.AvatarUrl,
                CreatedAt = profile.CreatedAt,
                UpdatedAt = profile.UpdatedAt
            });
    }
}
