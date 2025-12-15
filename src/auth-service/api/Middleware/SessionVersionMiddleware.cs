using System.Security.Claims;
using AuthApi.Data;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Middleware;

public class SessionVersionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionVersionMiddleware> _logger;

    public SessionVersionMiddleware(RequestDelegate next, ILogger<SessionVersionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, AuthDbContext dbContext)
    {
        // Skip validation for auth endpoints
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (path.Contains("/auth/register") || 
            path.Contains("/auth/login") ||
            path.Contains("/swagger") ||
            path.Contains("/health"))
        {
            await _next(context);
            return;
        }

        // Check if user is authenticated
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var deviceIdClaim = context.User.FindFirst("device_id")?.Value;
            var sessionVersionClaim = context.User.FindFirst("sv")?.Value;

            if (!string.IsNullOrEmpty(userIdClaim) && 
                !string.IsNullOrEmpty(deviceIdClaim) && 
                !string.IsNullOrEmpty(sessionVersionClaim) &&
                Guid.TryParse(userIdClaim, out var userId) &&
                int.TryParse(sessionVersionClaim, out var tokenSessionVersion))
            {
                // Validate session version against database
                var deviceSession = await dbContext.DeviceSessions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ds => ds.UserId == userId && ds.DeviceId == deviceIdClaim);

                if (deviceSession == null || deviceSession.SessionVersion != tokenSessionVersion)
                {
                    _logger.LogWarning(
                        "Session version mismatch for user {UserId}, device {DeviceId}. Token: {TokenVersion}, DB: {DbVersion}",
                        userId, deviceIdClaim, tokenSessionVersion, deviceSession?.SessionVersion);
                    
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { error = "Session expired or invalid" });
                    return;
                }
            }
        }

        await _next(context);
    }
}
