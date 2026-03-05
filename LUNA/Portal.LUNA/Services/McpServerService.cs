using Docker.DotNet;
using Docker.DotNet.Models;
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
    Task<McpServerInstanceDto?> StartAsync(string serverId);
    Task<bool> StopAsync(string instanceId);
}

public class McpServerService : IMcpServerService
{
    private readonly ApplicationDbContext _db;
    private readonly IDockerClient _docker;
    private const int PortRangeStart = 9000;

    public McpServerService(ApplicationDbContext db, IDockerClient docker)
    {
        _db = db;
        _docker = docker;
    }

    public async Task<List<AvailableMcpServerDto>> GetAllAsync() =>
        await _db.AvailableMcpServers
            .Include(s => s.Instances.Where(i => i.StoppedAt == null))
            .Select(s => MapToDto(s))
            .ToListAsync();

    public async Task<List<AvailableMcpServerDto>> GetEnabledAsync() =>
        await _db.AvailableMcpServers
            .Where(s => s.IsEnabled)
            .Include(s => s.Instances.Where(i => i.StoppedAt == null))
            .Select(s => MapToDto(s))
            .ToListAsync();

    public async Task<AvailableMcpServerDto?> GetByIdAsync(string id)
    {
        var entity = await _db.AvailableMcpServers
            .Include(s => s.Instances.Where(i => i.StoppedAt == null))
            .FirstOrDefaultAsync(s => s.Id == id);
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

    public async Task<McpServerInstanceDto?> StartAsync(string serverId)
    {
        var server = await _db.AvailableMcpServers.FindAsync(serverId);
        if (server == null) return null;

        // Find next available port
        var usedPorts = await _db.McpServerInstances
            .Where(i => i.StoppedAt == null)
            .Select(i => i.Port)
            .ToListAsync();
        int port = PortRangeStart;
        while (usedPorts.Contains(port)) port++;

        try
        {
            Console.WriteLine($"[StartAsync] Attempting to pull image: {server.DockerImage}");

            // Pull image first
            var pullProgress = new Progress<JSONMessage>(msg =>
            {
                if (!string.IsNullOrEmpty(msg.ErrorMessage))
                {
                    Console.WriteLine($"[Docker Pull Error] {msg.ErrorMessage}");
                }
                else if (!string.IsNullOrEmpty(msg.Status))
                {
                    Console.WriteLine($"[Docker Pull] {msg.Status}");
                }
            });

            await _docker.Images.CreateImageAsync(
                new ImagesCreateParameters { FromImage = server.DockerImage },
                null,
                pullProgress
            );

            Console.WriteLine($"[StartAsync] Image pulled successfully, creating container...");

            // Create container
            var response = await _docker.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Image = server.DockerImage,
                Env = new List<string>
                {
                    "PortalUrl=http://host.docker.internal:5000",
                    "ASPNETCORE_ENVIRONMENT=Production"
                },
                HostConfig = new HostConfig
                {
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                        { "8080/tcp", new[] { new PortBinding { HostPort = port.ToString() } } }
                    }
                }
            });

            Console.WriteLine($"[StartAsync] Container created: {response.ID}, starting...");

            // Start container
            await _docker.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());

            Console.WriteLine($"[StartAsync] Container started successfully on port {port}");

            // Record instance
            var instance = new McpServerInstance
            {
                Id = Guid.NewGuid().ToString(),
                AvailableMcpServerId = serverId,
                ContainerId = response.ID,
                Port = port,
                StartedAt = DateTime.UtcNow
            };
            _db.McpServerInstances.Add(instance);
            await _db.SaveChangesAsync();

            return MapInstanceToDto(instance);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error starting MCP server] {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[Stack Trace] {ex.StackTrace}");
            return null;
        }
    }

    public async Task<bool> StopAsync(string instanceId)
    {
        var instance = await _db.McpServerInstances.FindAsync(instanceId);
        if (instance == null) return false;

        try
        {
            await _docker.Containers.StopContainerAsync(instance.ContainerId, new ContainerStopParameters());
            await _docker.Containers.RemoveContainerAsync(instance.ContainerId, new ContainerRemoveParameters { Force = true });
        }
        catch
        {
            // Container may already be gone
        }

        instance.StoppedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    private static AvailableMcpServerDto MapToDto(AvailableMcpServer e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        DockerImage = e.DockerImage,
        Description = e.Description,
        IsEnabled = e.IsEnabled,
        RunningInstances = e.Instances.Select(MapInstanceToDto).ToList()
    };

    private static McpServerInstanceDto MapInstanceToDto(McpServerInstance i) => new()
    {
        Id = i.Id,
        AvailableMcpServerId = i.AvailableMcpServerId,
        ContainerId = i.ContainerId,
        Port = i.Port,
        StartedAt = i.StartedAt,
        StoppedAt = i.StoppedAt
    };
}
