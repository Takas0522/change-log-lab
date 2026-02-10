using SreAgentLab.Data;
using SreAgentLab.DTOs;
using SreAgentLab.Models;
using SreAgentLab.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace SreAgentLab.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        AppDbContext dbContext,
        JwtService jwtService,
        ILogger<AuthController> logger)
    {
        _dbContext = dbContext;
        _jwtService = jwtService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "メールアドレスは必須です。" });

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            return BadRequest(new { message = "パスワードは8文字以上で入力してください。" });

        var existingUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (existingUser != null)
            return BadRequest(new { message = "このメールアドレスは既に登録されています。" });

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 11),
            DisplayName = request.DisplayName
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User registered: {Email}", request.Email);

        return Ok(new
        {
            userId = user.Id,
            email = user.Email,
            displayName = user.DisplayName,
            message = "登録が完了しました。"
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
            return Unauthorized(new { message = "メールアドレスまたはパスワードが正しくありません。" });

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "メールアドレスまたはパスワードが正しくありません。" });

        var token = _jwtService.GenerateToken(user.Id, user.Email);

        _logger.LogInformation("User logged in: {Email}", request.Email);

        return Ok(new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName
        });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserInfoResponse>> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "認証情報が無効です。" });

        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return NotFound(new { message = "ユーザーが見つかりません。" });

        return Ok(new UserInfoResponse
        {
            UserId = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName
        });
    }
}
