using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BffApi.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UserController> _logger;

    public UserController(IHttpClientFactory httpClientFactory, ILogger<UserController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetProfile()
    {
        return await ProxyRequest("UserService", "/api/users/me", HttpMethod.Get);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] JsonElement requestBody)
    {
        return await ProxyRequest("UserService", "/api/users/me", HttpMethod.Put, requestBody);
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchUsers([FromQuery] string? q)
    {
        var path = string.IsNullOrEmpty(q) ? "/api/users/search" : $"/api/users/search?q={q}";
        return await ProxyRequest("UserService", path, HttpMethod.Get);
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
