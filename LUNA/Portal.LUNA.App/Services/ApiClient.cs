using System.Net.Http.Json;
using System.Text.Json;
using Blazored.LocalStorage;
using Portal.LUNA.Dto;

namespace Portal.LUNA.App.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;
    private static readonly JsonSerializerOptions _opts = new(JsonSerializerDefaults.Web);

    public ApiClient(HttpClient http, ILocalStorageService localStorage)
    {
        _http = http;
        _localStorage = localStorage;
    }

    private async Task SetAuthHeaderAsync()
    {
        var token = await _localStorage.GetItemAsStringAsync("auth_token");
        if (!string.IsNullOrEmpty(token))
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<(bool Success, string? Token, bool IsAdmin, string? Email, string? UserId)> LoginAsync(string email, string password)
    {
        var response = await _http.PostAsJsonAsync("/api/auth/login", new { email, password });
        if (!response.IsSuccessStatusCode) return (false, null, false, null, null);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = data.GetProperty("token").GetString();
        var isAdmin = data.GetProperty("isAdmin").GetBoolean();
        var userEmail = data.GetProperty("email").GetString();
        var userId = data.GetProperty("userId").GetString();
        if (token != null)
        {
            await _localStorage.SetItemAsStringAsync("auth_token", token);
            await _localStorage.SetItemAsync("is_admin", isAdmin);
            await _localStorage.SetItemAsStringAsync("user_email", userEmail ?? "");
            await _localStorage.SetItemAsStringAsync("user_id", userId ?? "");
        }
        return (true, token, isAdmin, userEmail, userId);
    }

    public async Task<bool> RegisterAsync(string email, string password)
    {
        var response = await _http.PostAsJsonAsync("/api/auth/register", new { email, password });
        return response.IsSuccessStatusCode;
    }

    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync("auth_token");
        await _localStorage.RemoveItemAsync("is_admin");
        await _localStorage.RemoveItemAsync("user_email");
        await _localStorage.RemoveItemAsync("user_id");
        _http.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<bool> IsLoggedInAsync()
    {
        var token = await _localStorage.GetItemAsStringAsync("auth_token");
        return !string.IsNullOrEmpty(token);
    }

    public async Task<bool> IsAdminAsync()
    {
        return await _localStorage.GetItemAsync<bool>("is_admin");
    }

    public async Task<string?> GetEmailAsync() =>
        await _localStorage.GetItemAsStringAsync("user_email");

    // MCP Servers
    public async Task<List<AvailableMcpServerDto>> GetAllMcpServersAsync()
    {
        await SetAuthHeaderAsync();
        return await _http.GetFromJsonAsync<List<AvailableMcpServerDto>>("/api/mcp-servers") ?? new();
    }

    public async Task<List<AvailableMcpServerDto>> GetEnabledMcpServersAsync()
    {
        await SetAuthHeaderAsync();
        return await _http.GetFromJsonAsync<List<AvailableMcpServerDto>>("/api/mcp-servers/enabled") ?? new();
    }

    public async Task<AvailableMcpServerDto?> CreateMcpServerAsync(AvailableMcpServerDto dto)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync("/api/mcp-servers", dto);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AvailableMcpServerDto>();
    }

    public async Task<AvailableMcpServerDto?> UpdateMcpServerAsync(string id, AvailableMcpServerDto dto)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PutAsJsonAsync($"/api/mcp-servers/{id}", dto);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AvailableMcpServerDto>();
    }

    public async Task<bool> DeleteMcpServerAsync(string id)
    {
        await SetAuthHeaderAsync();
        var response = await _http.DeleteAsync($"/api/mcp-servers/{id}");
        return response.IsSuccessStatusCode;
    }

    // API Keys
    public async Task<List<UserApiKeyDto>> GetMyApiKeysAsync()
    {
        await SetAuthHeaderAsync();
        return await _http.GetFromJsonAsync<List<UserApiKeyDto>>("/api/api-keys") ?? new();
    }

    public async Task<UserApiKeyDto?> GenerateApiKeyAsync(string mcpServerId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsync($"/api/api-keys/generate/{mcpServerId}", null);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<UserApiKeyDto>();
    }

    public async Task<bool> RevokeApiKeyAsync(string id)
    {
        await SetAuthHeaderAsync();
        var response = await _http.DeleteAsync($"/api/api-keys/{id}/revoke");
        return response.IsSuccessStatusCode;
    }

    public async Task<UserApiKeySettingDto?> UpsertSettingAsync(string apiKeyId, string key, string value)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PutAsJsonAsync($"/api/api-keys/{apiKeyId}/settings",
            new UserApiKeySettingDto { Key = key, Value = value });
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<UserApiKeySettingDto>();
    }

    public async Task<bool> DeleteSettingAsync(string settingId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.DeleteAsync($"/api/api-keys/settings/{settingId}");
        return response.IsSuccessStatusCode;
    }

    // Admin Settings
    public async Task<List<AdminSettingDto>> GetAdminSettingsAsync()
    {
        await SetAuthHeaderAsync();
        return await _http.GetFromJsonAsync<List<AdminSettingDto>>("/api/admin/settings") ?? new();
    }

    public async Task<bool> SetAdminSettingAsync(string key, string value)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PutAsJsonAsync("/api/admin/settings",
            new AdminSettingDto { Key = key, Value = value });
        return response.IsSuccessStatusCode;
    }

    // Containers
    public async Task<List<ContainerInfoDto>> GetContainersAsync()
    {
        await SetAuthHeaderAsync();
        return await _http.GetFromJsonAsync<List<ContainerInfoDto>>("/api/containers") ?? new();
    }

    public async Task<bool> StartContainerAsync(string containerId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsync($"/api/containers/{containerId}/start", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> StopContainerAsync(string containerId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsync($"/api/containers/{containerId}/stop", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteContainerAsync(string containerId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.DeleteAsync($"/api/containers/{containerId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<string?> GetRootDomainAsync()
    {
        var settings = await GetAdminSettingsAsync();
        return settings.FirstOrDefault(s => s.Key == "RootDomain")?.Value;
    }
}
