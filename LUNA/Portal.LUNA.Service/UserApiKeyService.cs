using Microsoft.EntityFrameworkCore;
using Portal.LUNA.Database;
using Portal.LUNA.Database.Entities;
using Portal.LUNA.Dto;

namespace Portal.LUNA.Service;

public interface IUserApiKeyService
{
    Task<List<UserApiKeyDto>> GetByUserAsync(string userId);
    Task<UserApiKeyDto?> GetByIdAsync(string id, string userId);
    Task<UserApiKeyDto?> GetByApiKeyAsync(string apiKey);
    Task<UserApiKeyDto> GenerateAsync(string userId, string mcpServerId);
    Task<bool> RevokeAsync(string id, string userId);
    Task<UserApiKeySettingDto> UpsertSettingAsync(string userApiKeyId, string userId, string key, string value);
    Task<bool> DeleteSettingAsync(string settingId, string userId);
}

public class UserApiKeyService : IUserApiKeyService
{
    private readonly ApplicationDbContext _db;

    public UserApiKeyService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<UserApiKeyDto>> GetByUserAsync(string userId)
    {
        return await _db.UserApiKeys
            .Include(k => k.McpServer)
            .Include(k => k.Settings)
            .Where(k => k.UserId == userId)
            .Select(k => MapToDto(k))
            .ToListAsync();
    }

    public async Task<UserApiKeyDto?> GetByIdAsync(string id, string userId)
    {
        var entity = await _db.UserApiKeys
            .Include(k => k.McpServer)
            .Include(k => k.Settings)
            .FirstOrDefaultAsync(k => k.Id == id && k.UserId == userId);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<UserApiKeyDto?> GetByApiKeyAsync(string apiKey)
    {
        var entity = await _db.UserApiKeys
            .Include(k => k.McpServer)
            .Include(k => k.Settings)
            .FirstOrDefaultAsync(k => k.ApiKey == apiKey && k.RevokedAt == null);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<UserApiKeyDto> GenerateAsync(string userId, string mcpServerId)
    {
        var entity = new UserApiKey
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            McpServerId = mcpServerId,
            ApiKey = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow
        };
        _db.UserApiKeys.Add(entity);
        await _db.SaveChangesAsync();
        
        var result = await _db.UserApiKeys
            .Include(k => k.McpServer)
            .Include(k => k.Settings)
            .FirstAsync(k => k.Id == entity.Id);
        return MapToDto(result);
    }

    public async Task<bool> RevokeAsync(string id, string userId)
    {
        var entity = await _db.UserApiKeys.FirstOrDefaultAsync(k => k.Id == id && k.UserId == userId);
        if (entity == null) return false;
        entity.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<UserApiKeySettingDto> UpsertSettingAsync(string userApiKeyId, string userId, string key, string value)
    {
        var apiKey = await _db.UserApiKeys.FirstOrDefaultAsync(k => k.Id == userApiKeyId && k.UserId == userId);
        if (apiKey == null) throw new UnauthorizedAccessException("API key not found.");

        var existing = await _db.UserApiKeySettings
            .FirstOrDefaultAsync(s => s.UserApiKeyId == userApiKeyId && s.Key == key);
        if (existing != null)
        {
            existing.Value = value;
            await _db.SaveChangesAsync();
            return MapSettingToDto(existing);
        }

        var entity = new UserApiKeySetting
        {
            Id = Guid.NewGuid().ToString(),
            UserApiKeyId = userApiKeyId,
            Key = key,
            Value = value,
            CreatedAt = DateTime.UtcNow
        };
        _db.UserApiKeySettings.Add(entity);
        await _db.SaveChangesAsync();
        return MapSettingToDto(entity);
    }

    public async Task<bool> DeleteSettingAsync(string settingId, string userId)
    {
        var setting = await _db.UserApiKeySettings
            .Include(s => s.UserApiKey)
            .FirstOrDefaultAsync(s => s.Id == settingId && s.UserApiKey.UserId == userId);
        if (setting == null) return false;
        _db.UserApiKeySettings.Remove(setting);
        await _db.SaveChangesAsync();
        return true;
    }

    private static UserApiKeyDto MapToDto(UserApiKey k) => new()
    {
        Id = k.Id,
        UserId = k.UserId,
        McpServerId = k.McpServerId,
        McpServerName = k.McpServer?.Name ?? string.Empty,
        ApiKey = k.ApiKey,
        CreatedAt = k.CreatedAt,
        RevokedAt = k.RevokedAt,
        Settings = k.Settings?.Select(MapSettingToDto).ToList() ?? new()
    };

    private static UserApiKeySettingDto MapSettingToDto(UserApiKeySetting s) => new()
    {
        Id = s.Id,
        UserApiKeyId = s.UserApiKeyId,
        Key = s.Key,
        Value = s.Value,
        CreatedAt = s.CreatedAt
    };
}
