using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.EntityFrameworkCore;
using Portal.LUNA.Database;
using Portal.LUNA.Dto;

namespace Portal.LUNA.Service;

public class McpServerService
{
    private readonly ApplicationDbContext _db;
    private readonly DockerClient _docker;

    public McpServerService(ApplicationDbContext db)
    {
        _db = db;
        _docker = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient();
    }

    // --- Available MCP Server catalog (Admin) ---

    public async Task<List<Dto.AvailableMcpServer>> GetAvailableServersAsync()
    {
        return await _db.AvailableMcpServers
            .Select(s => new Dto.AvailableMcpServer
            {
                Id = s.Id,
                Name = s.Name,
                DockerImage = s.DockerImage,
                Description = s.Description,
                ExternalPort = s.ExternalPort
            })
            .ToListAsync();
    }

    public async Task<Dto.AvailableMcpServer?> GetAvailableServerByIdAsync(string id)
    {
        var s = await _db.AvailableMcpServers.FindAsync(id);
        if (s == null) return null;
        return new Dto.AvailableMcpServer
        {
            Id = s.Id,
            Name = s.Name,
            DockerImage = s.DockerImage,
            Description = s.Description,
            ExternalPort = s.ExternalPort
        };
    }

    public async Task<Dto.AvailableMcpServer> CreateAvailableServerAsync(Dto.AvailableMcpServer model)
    {
        var entity = new Database.AvailableMcpServer
        {
            Name = model.Name,
            DockerImage = model.DockerImage,
            Description = model.Description,
            ExternalPort = model.ExternalPort
        };
        _db.AvailableMcpServers.Add(entity);
        await _db.SaveChangesAsync();
        model.Id = entity.Id;
        return model;
    }

    public async Task<bool> UpdateAvailableServerAsync(Dto.AvailableMcpServer model)
    {
        var entity = await _db.AvailableMcpServers.FindAsync(model.Id);
        if (entity == null) return false;
        entity.Name = model.Name;
        entity.DockerImage = model.DockerImage;
        entity.Description = model.Description;
        entity.ExternalPort = model.ExternalPort;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAvailableServerAsync(string id)
    {
        var entity = await _db.AvailableMcpServers.FindAsync(id);
        if (entity == null) return false;
        _db.AvailableMcpServers.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }

    // --- Container management (Admin) ---

    public async Task<List<Dto.McpContainer>> ListAllContainersAsync()
    {
        var lunaContainers = await _db.McpContainerInstances.ToListAsync();
        var lunaContainerIds = lunaContainers.Select(c => c.ContainerId).ToHashSet();
        var serverMap = await _db.AvailableMcpServers.ToDictionaryAsync(s => s.Id, s => s.Name);

        var result = new List<Dto.McpContainer>();

        try
        {
            var dockerContainers = await _docker.Containers.ListContainersAsync(
                new ContainersListParameters { All = true });

            foreach (var dc in dockerContainers)
            {
                var isLuna = lunaContainerIds.Contains(dc.ID);
                var lunaRecord = lunaContainers.FirstOrDefault(l => l.ContainerId == dc.ID);
                var hostPort = dc.Ports?.FirstOrDefault()?.PublicPort ?? 0;

                result.Add(new Dto.McpContainer
                {
                    Id = lunaRecord?.Id ?? dc.ID,
                    ContainerId = dc.ID,
                    ContainerName = dc.Names?.FirstOrDefault()?.TrimStart('/') ?? dc.ID,
                    ServerId = lunaRecord?.ServerId ?? string.Empty,
                    ServerName = lunaRecord != null && serverMap.ContainsKey(lunaRecord.ServerId)
                        ? serverMap[lunaRecord.ServerId] : null,
                    Status = dc.State,
                    CreatedAt = lunaRecord?.CreatedAt ?? DateTime.UtcNow,
                    ManagedByLuna = isLuna,
                    HostPort = hostPort > 0 ? hostPort : lunaRecord?.HostPort
                });
            }
        }
        catch
        {
            // Docker may not be available, return DB records only
            foreach (var lc in lunaContainers)
            {
                result.Add(new Dto.McpContainer
                {
                    Id = lc.Id,
                    ContainerId = lc.ContainerId,
                    ContainerName = lc.ContainerName,
                    ServerId = lc.ServerId,
                    ServerName = serverMap.ContainsKey(lc.ServerId) ? serverMap[lc.ServerId] : null,
                    Status = "unknown",
                    CreatedAt = lc.CreatedAt,
                    ManagedByLuna = true,
                    HostPort = lc.HostPort
                });
            }
        }

        return result;
    }

    public async Task<Dto.McpContainer> StartContainerAsync(Dto.CreateContainerRequest request)
    {
        var server = await _db.AvailableMcpServers.FindAsync(request.ServerId)
            ?? throw new Exception("MCP server definition not found.");

        // Pull image if needed, then create container
        int hostPort = await FindAvailablePortAsync(9000);

        var createParams = new CreateContainerParameters
        {
            Image = server.DockerImage,
            Name = request.ContainerName,
            ExposedPorts = new Dictionary<string, EmptyStruct>
            {
                { $"{server.ExternalPort}/tcp", new EmptyStruct() }
            },
            HostConfig = new HostConfig
            {
                PortBindings = new Dictionary<string, IList<PortBinding>>
                {
                    {
                        $"{server.ExternalPort}/tcp",
                        new List<PortBinding> { new PortBinding { HostPort = hostPort.ToString() } }
                    }
                },
                RestartPolicy = new RestartPolicy { Name = RestartPolicyKind.UnlessStopped }
            }
        };

        var createResponse = await _docker.Containers.CreateContainerAsync(createParams);
        await _docker.Containers.StartContainerAsync(createResponse.ID, new ContainerStartParameters());

        var instance = new McpContainerInstance
        {
            ContainerId = createResponse.ID,
            ContainerName = request.ContainerName,
            ServerId = request.ServerId,
            HostPort = hostPort
        };
        _db.McpContainerInstances.Add(instance);
        await _db.SaveChangesAsync();

        return new Dto.McpContainer
        {
            Id = instance.Id,
            ContainerId = createResponse.ID,
            ContainerName = request.ContainerName,
            ServerId = request.ServerId,
            ServerName = server.Name,
            Status = "running",
            CreatedAt = instance.CreatedAt,
            ManagedByLuna = true,
            HostPort = hostPort
        };
    }

    public async Task<bool> StopContainerAsync(string instanceId)
    {
        var instance = await _db.McpContainerInstances.FindAsync(instanceId);
        if (instance == null) return false;

        await _docker.Containers.StopContainerAsync(instance.ContainerId, new ContainerStopParameters());
        return true;
    }

    public async Task<bool> DeleteContainerAsync(string instanceId)
    {
        var instance = await _db.McpContainerInstances.FindAsync(instanceId);
        if (instance == null) return false;

        try
        {
            await _docker.Containers.StopContainerAsync(instance.ContainerId, new ContainerStopParameters());
        }
        catch { /* ignore if already stopped */ }

        await _docker.Containers.RemoveContainerAsync(instance.ContainerId,
            new ContainerRemoveParameters { Force = true });

        _db.McpContainerInstances.Remove(instance);
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<int> FindAvailablePortAsync(int startPort)
    {
        var usedPorts = await _db.McpContainerInstances.Select(c => c.HostPort).ToListAsync();
        int port = startPort;
        while (usedPorts.Contains(port)) port++;
        return port;
    }

    // --- Running server list for users ---

    public async Task<List<Dto.AvailableMcpServer>> GetRunningServersForUsersAsync()
    {
        var runningInstanceServerIds = await _db.McpContainerInstances
            .Select(c => c.ServerId)
            .Distinct()
            .ToListAsync();

        return await _db.AvailableMcpServers
            .Where(s => runningInstanceServerIds.Contains(s.Id))
            .Select(s => new Dto.AvailableMcpServer
            {
                Id = s.Id,
                Name = s.Name,
                DockerImage = s.DockerImage,
                Description = s.Description
            })
            .ToListAsync();
    }

    public async Task<string> GetMcpServerAddressAsync(string apiKeyId)
    {
        var apiKey = await _db.UserApiKeys.FindAsync(apiKeyId);
        if (apiKey == null) return string.Empty;

        var settings = await _db.SystemSettings.FirstOrDefaultAsync();
        var rootUrl = settings?.PortalRootUrl?.TrimEnd('/') ?? string.Empty;

        if (string.IsNullOrEmpty(apiKey.ContainerInstanceId)) return string.Empty;

        var instance = await _db.McpContainerInstances.FindAsync(apiKey.ContainerInstanceId);
        if (instance == null) return string.Empty;

        if (!string.IsNullOrEmpty(rootUrl))
            return $"{rootUrl}:{instance.HostPort}/mcp";

        return $"http://localhost:{instance.HostPort}/mcp";
    }
}
