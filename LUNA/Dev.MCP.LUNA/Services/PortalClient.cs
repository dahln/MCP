using System.Text;
using System.Text.Json;
using Portal.LUNA.Dto;

namespace Dev.MCP.LUNA;

public class PortalClient : IPortalClient
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions _opts = new(JsonSerializerDefaults.Web);

    public PortalClient(IHttpClientFactory factory)
    {
        _client = factory.CreateClient("portal");
    }

    public async Task<CreateSandboxResponseDto> CreateSandboxAsync(string apiKey)
    {
        var body = JsonSerializer.Serialize(new CreateSandboxRequestDto { ApiKey = apiKey });
        var response = await _client.PostAsync("/api/sandboxes",
            new StringContent(body, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<CreateSandboxResponseDto>(content, _opts)!;
    }

    public async Task DestroySandboxAsync(string sandboxId, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/sandboxes/{sandboxId}");
        request.Headers.Add("X-API-Key", apiKey);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task<UserApiKeyDto?> ValidateApiKeyAsync(string apiKey)
    {
        var response = await _client.GetAsync($"/api/api-keys/validate?apiKey={Uri.EscapeDataString(apiKey)}");
        if (!response.IsSuccessStatusCode) return null;
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<UserApiKeyDto>(content, _opts);
    }
}
