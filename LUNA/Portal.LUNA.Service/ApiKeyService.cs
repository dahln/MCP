using Microsoft.EntityFrameworkCore;
using Portal.LUNA.Database;
using Portal.LUNA.Dto;

namespace Portal.LUNA.Service;

public class ApiKeyService
{
    private readonly ApplicationDbContext _db;

    public ApiKeyService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<Dto.UserApiKey>> GetUserApiKeysAsync(string userId)
    {
        var keys = await _db.UserApiKeys
            .Include(k => k.KeyValuePairs)
            .Where(k => k.UserId == userId)
            .ToListAsync();

        var serverIds = keys.Select(k => k.ServerId).Distinct().ToList();
        var servers = await _db.AvailableMcpServers
            .Where(s => serverIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        var instances = await _db.McpContainerInstances.ToListAsync();

        var settings = await _db.SystemSettings.FirstOrDefaultAsync();
        var rootUrl = settings?.PortalRootUrl?.TrimEnd('/') ?? string.Empty;

        return keys.Select(k =>
        {
            var instance = k.ContainerInstanceId != null
                ? instances.FirstOrDefault(i => i.Id == k.ContainerInstanceId)
                : null;

            string address = string.Empty;
            if (instance != null)
            {
                address = !string.IsNullOrEmpty(rootUrl)
                    ? $"{rootUrl}:{instance.HostPort}/mcp"
                    : $"http://localhost:{instance.HostPort}/mcp";
            }

            return new Dto.UserApiKey
            {
                Id = k.Id,
                UserId = k.UserId,
                ServerId = k.ServerId,
                ServerName = servers.ContainsKey(k.ServerId) ? servers[k.ServerId] : null,
                ApiKey = k.ApiKey,
                ContainerId = k.ContainerInstanceId,
                ContainerName = instance?.ContainerName,
                McpServerAddress = address,
                CreatedAt = k.CreatedAt,
                KeyValuePairs = k.KeyValuePairs.Select(kv => new Dto.ApiKeyKvp
                {
                    Id = kv.Id,
                    ApiKeyId = kv.ApiKeyId,
                    Key = kv.Key,
                    Value = kv.Value
                }).ToList()
            };
        }).ToList();
    }

    public async Task<Dto.UserApiKey?> GetUserApiKeyByIdAsync(string id, string userId)
    {
        var k = await _db.UserApiKeys.Include(k => k.KeyValuePairs).FirstOrDefaultAsync(k => k.Id == id && k.UserId == userId);
        if (k == null) return null;

        var server = await _db.AvailableMcpServers.FindAsync(k.ServerId);
        var instance = k.ContainerInstanceId != null
            ? await _db.McpContainerInstances.FindAsync(k.ContainerInstanceId)
            : null;

        var settings = await _db.SystemSettings.FirstOrDefaultAsync();
        var rootUrl = settings?.PortalRootUrl?.TrimEnd('/') ?? string.Empty;

        string address = string.Empty;
        if (instance != null)
        {
            address = !string.IsNullOrEmpty(rootUrl)
                ? $"{rootUrl}:{instance.HostPort}/mcp"
                : $"http://localhost:{instance.HostPort}/mcp";
        }

        return new Dto.UserApiKey
        {
            Id = k.Id,
            UserId = k.UserId,
            ServerId = k.ServerId,
            ServerName = server?.Name,
            ApiKey = k.ApiKey,
            ContainerId = k.ContainerInstanceId,
            ContainerName = instance?.ContainerName,
            McpServerAddress = address,
            CreatedAt = k.CreatedAt,
            KeyValuePairs = k.KeyValuePairs.Select(kv => new Dto.ApiKeyKvp
            {
                Id = kv.Id,
                ApiKeyId = kv.ApiKeyId,
                Key = kv.Key,
                Value = kv.Value
            }).ToList()
        };
    }

    public async Task<Dto.UserApiKey> GenerateApiKeyAsync(string userId, string serverId, string? containerInstanceId)
    {
        var existing = await _db.UserApiKeys.FirstOrDefaultAsync(k => k.UserId == userId && k.ServerId == serverId);
        if (existing != null)
            throw new Exception("You already have an API key for this server.");

        var key = new Database.UserApiKey
        {
            UserId = userId,
            ServerId = serverId,
            ApiKey = GenerateSecureKey(),
            ContainerInstanceId = containerInstanceId
        };
        _db.UserApiKeys.Add(key);
        await _db.SaveChangesAsync();

        var server = await _db.AvailableMcpServers.FindAsync(serverId);
        return new Dto.UserApiKey
        {
            Id = key.Id,
            UserId = key.UserId,
            ServerId = key.ServerId,
            ServerName = server?.Name,
            ApiKey = key.ApiKey,
            ContainerId = containerInstanceId,
            CreatedAt = key.CreatedAt
        };
    }

    public async Task<bool> DeleteApiKeyAsync(string id, string userId)
    {
        var key = await _db.UserApiKeys.Include(k => k.KeyValuePairs).FirstOrDefaultAsync(k => k.Id == id && k.UserId == userId);
        if (key == null) return false;
        _db.UserApiKeyKvps.RemoveRange(key.KeyValuePairs);
        _db.UserApiKeys.Remove(key);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<Dto.ApiKeyKvp> AddKvpAsync(string apiKeyId, string userId, Dto.ApiKeyKvp model)
    {
        var key = await _db.UserApiKeys.FirstOrDefaultAsync(k => k.Id == apiKeyId && k.UserId == userId);
        if (key == null) throw new Exception("API key not found.");

        var kvp = new Database.UserApiKeyKvp
        {
            ApiKeyId = apiKeyId,
            Key = model.Key,
            Value = model.Value
        };
        _db.UserApiKeyKvps.Add(kvp);
        await _db.SaveChangesAsync();

        model.Id = kvp.Id;
        model.ApiKeyId = kvp.ApiKeyId;
        return model;
    }

    public async Task<bool> UpdateKvpAsync(string kvpId, string apiKeyId, string userId, Dto.ApiKeyKvp model)
    {
        var key = await _db.UserApiKeys.FirstOrDefaultAsync(k => k.Id == apiKeyId && k.UserId == userId);
        if (key == null) return false;

        var kvp = await _db.UserApiKeyKvps.FirstOrDefaultAsync(k => k.Id == kvpId && k.ApiKeyId == apiKeyId);
        if (kvp == null) return false;

        kvp.Key = model.Key;
        kvp.Value = model.Value;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteKvpAsync(string kvpId, string apiKeyId, string userId)
    {
        var key = await _db.UserApiKeys.FirstOrDefaultAsync(k => k.Id == apiKeyId && k.UserId == userId);
        if (key == null) return false;

        var kvp = await _db.UserApiKeyKvps.FirstOrDefaultAsync(k => k.Id == kvpId && k.ApiKeyId == apiKeyId);
        if (kvp == null) return false;

        _db.UserApiKeyKvps.Remove(kvp);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ValidateApiKeyAsync(string apiKey, string serverId)
    {
        return await _db.UserApiKeys.AnyAsync(k => k.ApiKey == apiKey && k.ServerId == serverId);
    }

    public async Task<List<Dto.ApiKeyKvp>> GetKvpsForApiKeyAsync(string apiKey)
    {
        var key = await _db.UserApiKeys.Include(k => k.KeyValuePairs).FirstOrDefaultAsync(k => k.ApiKey == apiKey);
        if (key == null) return new List<Dto.ApiKeyKvp>();

        return key.KeyValuePairs.Select(kv => new Dto.ApiKeyKvp
        {
            Id = kv.Id,
            ApiKeyId = kv.ApiKeyId,
            Key = kv.Key,
            Value = kv.Value
        }).ToList();
    }

    private static string GenerateSecureKey()
    {
        return $"luna_{Guid.NewGuid():N}{Guid.NewGuid():N}";
    }
}
