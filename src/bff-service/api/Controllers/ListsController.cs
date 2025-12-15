using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BffApi.Controllers;

[ApiController]
[Route("api/lists")]
[Authorize]
public class ListsController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ListsController> _logger;

    public ListsController(IHttpClientFactory httpClientFactory, ILogger<ListsController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetLists()
    {
        return await ProxyRequest("TodoService", "/api/lists", HttpMethod.Get);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetList(Guid id)
    {
        return await ProxyRequest("TodoService", $"/api/lists/{id}", HttpMethod.Get);
    }

    [HttpPost]
    public async Task<IActionResult> CreateList([FromBody] JsonElement requestBody)
    {
        return await ProxyRequest("TodoService", "/api/lists", HttpMethod.Post, requestBody);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateList(Guid id, [FromBody] JsonElement requestBody)
    {
        return await ProxyRequest("TodoService", $"/api/lists/{id}", HttpMethod.Put, requestBody);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteList(Guid id)
    {
        return await ProxyRequest("TodoService", $"/api/lists/{id}", HttpMethod.Delete);
    }

    [HttpPost("{id}/invite")]
    public async Task<IActionResult> InviteToList(Guid id, [FromBody] JsonElement requestBody)
    {
        return await ProxyRequest("TodoService", $"/api/lists/{id}/invite", HttpMethod.Post, requestBody);
    }

    [HttpPost("{id}/accept")]
    public async Task<IActionResult> AcceptInvite(Guid id)
    {
        return await ProxyRequest("TodoService", $"/api/lists/{id}/accept", HttpMethod.Post);
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
