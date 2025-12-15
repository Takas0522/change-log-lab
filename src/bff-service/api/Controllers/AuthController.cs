using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BffApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IHttpClientFactory httpClientFactory, ILogger<AuthController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] JsonElement requestBody)
    {
        return await ProxyRequest("AuthService", "/api/auth/register", HttpMethod.Post, requestBody);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] JsonElement requestBody)
    {
        return await ProxyRequest("AuthService", "/api/auth/login", HttpMethod.Post, requestBody);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        return await ProxyRequest("AuthService", "/api/auth/logout", HttpMethod.Post);
    }

    private async Task<IActionResult> ProxyRequest(string serviceName, string path, HttpMethod method, JsonElement? body = null)
    {
        var client = _httpClientFactory.CreateClient(serviceName);
        var request = new HttpRequestMessage(method, path);

        // Forward Authorization header
        if (Request.Headers.ContainsKey("Authorization"))
        {
            request.Headers.Add("Authorization", Request.Headers["Authorization"].ToString());
        }

        // Add body if provided
        if (body.HasValue)
        {
            var jsonContent = JsonSerializer.Serialize(body.Value);
            request.Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
        }

        try
        {
            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            return StatusCode((int)response.StatusCode, 
                string.IsNullOrEmpty(content) ? null : JsonSerializer.Deserialize<JsonElement>(content));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying request to {ServiceName}", serviceName);
            return StatusCode(500, new { error = "Service unavailable" });
        }
    }
}
