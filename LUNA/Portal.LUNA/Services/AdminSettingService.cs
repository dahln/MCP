using Microsoft.EntityFrameworkCore;
using Portal.LUNA.Data;

namespace Portal.LUNA.Services;

public interface IAdminSettingService
{
    Task<string?> GetValueAsync(string key);
    Task SetValueAsync(string key, string value);
    Task<List<(string Key, string Value)>> GetAllAsync();
}

public class AdminSettingService : IAdminSettingService
{
    private readonly ApplicationDbContext _db;
    public AdminSettingService(ApplicationDbContext db) => _db = db;

    public async Task<string?> GetValueAsync(string key)
    {
        var setting = await _db.AdminSettings.FirstOrDefaultAsync(s => s.Key == key);
        return setting?.Value;
    }

    public async Task SetValueAsync(string key, string value)
    {
        var setting = await _db.AdminSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting == null)
            _db.AdminSettings.Add(new AdminSetting { Id = Guid.NewGuid().ToString(), Key = key, Value = value });
        else
            setting.Value = value;
        await _db.SaveChangesAsync();
    }

    public async Task<List<(string Key, string Value)>> GetAllAsync() =>
        await _db.AdminSettings.Select(s => ValueTuple.Create(s.Key, s.Value)).ToListAsync();
}
