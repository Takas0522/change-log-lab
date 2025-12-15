using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BffApi.Controllers;

[ApiController]
[Route("api/lists/{listId}/todos")]
[Authorize]
public class TodosController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TodosController> _logger;

    public TodosController(IHttpClientFactory httpClientFactory, ILogger<TodosController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetTodos(Guid listId)
    {
        return await ProxyRequest("TodoService", $"/api/lists/{listId}/todos", HttpMethod.Get);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTodo(Guid listId, Guid id)
    {
        return await ProxyRequest("TodoService", $"/api/lists/{listId}/todos/{id}", HttpMethod.Get);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTodo(Guid listId, [FromBody] JsonElement requestBody)
    {
        return await ProxyRequest("TodoService", $"/api/lists/{listId}/todos", HttpMethod.Post, requestBody);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTodo(Guid listId, Guid id, [FromBody] JsonElement requestBody)
    {
        return await ProxyRequest("TodoService", $"/api/lists/{listId}/todos/{id}", HttpMethod.Put, requestBody);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTodo(Guid listId, Guid id)
    {
        return await ProxyRequest("TodoService", $"/api/lists/{listId}/todos/{id}", HttpMethod.Delete);
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
