using Microsoft.EntityFrameworkCore;
using Portal.LUNA.Data;
using Portal.LUNA.Models;

namespace Portal.LUNA.Services;

public interface IMcpServerService
{
    Task<List<AvailableMcpServerDto>> GetAllAsync();
    Task<List<AvailableMcpServerDto>> GetEnabledAsync();
    Task<AvailableMcpServerDto?> GetByIdAsync(string id);
    Task<AvailableMcpServerDto> CreateAsync(AvailableMcpServerDto dto);
    Task<AvailableMcpServerDto?> UpdateAsync(string id, AvailableMcpServerDto dto);
    Task<bool> DeleteAsync(string id);
}

public class McpServerService : IMcpServerService
{
    private readonly ApplicationDbContext _db;
    public McpServerService(ApplicationDbContext db) => _db = db;

    public async Task<List<AvailableMcpServerDto>> GetAllAsync() =>
        await _db.AvailableMcpServers.Select(s => MapToDto(s)).ToListAsync();

    public async Task<List<AvailableMcpServerDto>> GetEnabledAsync() =>
        await _db.AvailableMcpServers.Where(s => s.IsEnabled).Select(s => MapToDto(s)).ToListAsync();

    public async Task<AvailableMcpServerDto?> GetByIdAsync(string id)
    {
        var entity = await _db.AvailableMcpServers.FindAsync(id);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<AvailableMcpServerDto> CreateAsync(AvailableMcpServerDto dto)
    {
        var entity = new AvailableMcpServer
        {
            Id = Guid.NewGuid().ToString(),
            Name = dto.Name,
            DockerImage = dto.DockerImage,
            Description = dto.Description,
            IsEnabled = dto.IsEnabled
        };
        _db.AvailableMcpServers.Add(entity);
        await _db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<AvailableMcpServerDto?> UpdateAsync(string id, AvailableMcpServerDto dto)
    {
        var entity = await _db.AvailableMcpServers.FindAsync(id);
        if (entity == null) return null;
        entity.Name = dto.Name;
        entity.DockerImage = dto.DockerImage;
        entity.Description = dto.Description;
        entity.IsEnabled = dto.IsEnabled;
        await _db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var entity = await _db.AvailableMcpServers.FindAsync(id);
        if (entity == null) return false;
        _db.AvailableMcpServers.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }

    private static AvailableMcpServerDto MapToDto(AvailableMcpServer e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        DockerImage = e.DockerImage,
        Description = e.Description,
        IsEnabled = e.IsEnabled
    };
}
