using AuthApi.Data;
using AuthApi.DTOs;
using AuthApi.Models;
using AuthApi.Services;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AuthApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthDbContext _dbContext;
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        AuthDbContext dbContext, 
        JwtService jwtService,
        ILogger<AuthController> logger)
    {
        _dbContext = dbContext;
        _jwtService = jwtService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        // Check if user already exists
        var existingUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (existingUser != null)
        {
            return BadRequest(new { error = "User with this email already exists" });
        }

        // Hash password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // Create user
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = passwordHash,
            DisplayName = request.DisplayName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User registered: {Email}", request.Email);

        // Return user info (no token yet - user needs to login)
        return Ok(new
        {
            userId = user.Id,
            email = user.Email,
            displayName = user.DisplayName,
            message = "User registered successfully. Please login."
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        // Find user
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
        {
            return Unauthorized(new { error = "Invalid email or password" });
        }

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { error = "Invalid email or password" });
        }

        // Get or create device session
        var deviceSession = await _dbContext.DeviceSessions
            .FirstOrDefaultAsync(ds => ds.UserId == user.Id && ds.DeviceId == request.DeviceId);

        if (deviceSession == null)
        {
            // Create new device session
            deviceSession = new DeviceSession
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                DeviceId = request.DeviceId,
                SessionVersion = 1,
                LastLoginAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.DeviceSessions.Add(deviceSession);
        }
        else
        {
            // Update existing session
            deviceSession.LastLoginAt = DateTime.UtcNow;
            deviceSession.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();

        // Generate JWT
        var token = _jwtService.GenerateToken(
            user.Id, 
            user.Email, 
            request.DeviceId, 
            deviceSession.SessionVersion);

        _logger.LogInformation("User logged in: {Email}, Device: {DeviceId}", request.Email, request.DeviceId);

        return Ok(new AuthResponse
        {
            Token = token,
            Email = user.Email,
            UserId = user.Id,
            DisplayName = user.DisplayName
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { error = "Invalid user" });
        }

        // Find and invalidate the device session
        var deviceSession = await _dbContext.DeviceSessions
            .FirstOrDefaultAsync(ds => ds.UserId == userId && ds.DeviceId == request.DeviceId);

        if (deviceSession != null)
        {
            // Increment session version to invalidate all tokens for this device
            deviceSession.SessionVersion++;
            deviceSession.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("User logged out: {UserId}, Device: {DeviceId}", userId, request.DeviceId);
        }

        return Ok(new { message = "Logged out successfully" });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserInfoResponse>> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { error = "Invalid user" });
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        return Ok(new UserInfoResponse
        {
            UserId = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName
        });
    }
}
