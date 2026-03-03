using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.EntityFrameworkCore;
using Portal.LUNA.Database;
using Portal.LUNA.Dto;

namespace Portal.LUNA.Service;

public interface IContainerService
{
    Task<List<ContainerInfoDto>> GetAllContainersAsync();
    Task<bool> StartContainerAsync(string containerId);
    Task<bool> StopContainerAsync(string containerId);
    Task<bool> DeleteContainerAsync(string containerId);
}

public class ContainerService : IContainerService
{
    private readonly ApplicationDbContext _db;
    private readonly IDockerClient _docker;

    public ContainerService(ApplicationDbContext db, IDockerClient docker)
    {
        _db = db;
        _docker = docker;
    }

    public async Task<List<ContainerInfoDto>> GetAllContainersAsync()
    {
        var containers = await _docker.Containers.ListContainersAsync(new ContainersListParameters { All = true });
        var sandboxes = await _db.Sandboxes.Include(s => s.UserApiKey).ThenInclude(k => k.McpServer).ToListAsync();
        var sandboxMap = sandboxes.ToDictionary(s => s.ContainerId, s => s);

        return containers.Select(c =>
        {
            sandboxMap.TryGetValue(c.ID, out var sandbox);
            return new ContainerInfoDto
            {
                ContainerId = c.ID,
                Image = c.Image,
                Status = c.Status,
                McpServerName = sandbox?.UserApiKey?.McpServer?.Name,
                SandboxId = sandbox?.Id,
                ManagedByPortal = sandbox != null
            };
        }).ToList();
    }

    public async Task<bool> StartContainerAsync(string containerId)
    {
        return await _docker.Containers.StartContainerAsync(containerId, new ContainerStartParameters());
    }

    public async Task<bool> StopContainerAsync(string containerId)
    {
        return await _docker.Containers.StopContainerAsync(containerId, new ContainerStopParameters());
    }

    public async Task<bool> DeleteContainerAsync(string containerId)
    {
        await _docker.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters { Force = true });
        return true;
    }
}
