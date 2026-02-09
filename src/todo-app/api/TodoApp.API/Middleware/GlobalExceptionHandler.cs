using System.Net;
using System.Text.Json;
using TodoApp.Application.Common;

namespace TodoApp.API.Middleware;

/// <summary>
/// グローバル例外ハンドラー
/// REQ-SEC-005対応: 一元的なエラーハンドリング
/// </summary>
public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(
        RequestDelegate next,
        ILogger<GlobalExceptionHandler> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse
        {
            TraceId = context.TraceIdentifier
        };

        switch (exception)
        {
            case NotFoundException notFoundEx:
                _logger.LogWarning(notFoundEx, "Resource not found: {Message}", notFoundEx.Message);
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response.Error = "NotFound";
                response.Message = notFoundEx.Message;
                break;

            case ValidationException validationEx:
                _logger.LogWarning(validationEx, "Validation error: {Message}", validationEx.Message);
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Error = "ValidationError";
                response.Message = validationEx.Message;
                break;

            case ConcurrencyException concurrencyEx:
                _logger.LogWarning(concurrencyEx, "Concurrency error: {Message}", concurrencyEx.Message);
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                response.Error = "ConcurrencyError";
                response.Message = concurrencyEx.Message;
                break;

            case InvalidOperationException invalidOpEx:
                _logger.LogWarning(invalidOpEx, "Invalid operation: {Message}", invalidOpEx.Message);
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Error = "InvalidOperation";
                response.Message = invalidOpEx.Message;
                break;

            default:
                _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Error = "InternalServerError";
                
                // 本番環境では詳細なエラーメッセージを隠す
                response.Message = _env.IsDevelopment()
                    ? exception.Message
                    : "An error occurred while processing your request.";

                if (_env.IsDevelopment())
                {
                    response.Details = exception.ToString();
                }
                break;
        }

        var result = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(result);
    }

    private class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string TraceId { get; set; } = string.Empty;
        public string? Details { get; set; }
    }
}
