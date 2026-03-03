using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.EntityFrameworkCore;
using Portal.LUNA.Database;
using Portal.LUNA.Database.Entities;
using Portal.LUNA.Dto;

namespace Portal.LUNA.Service;

public interface ISandboxService
{
    Task<CreateSandboxResponseDto> CreateAsync(string apiKey);
    Task<bool> DestroyAsync(string sandboxId, string apiKey);
    Task<SandboxDto?> GetAsync(string sandboxId, string apiKey);
}

public class SandboxService : ISandboxService
{
    private readonly ApplicationDbContext _db;
    private readonly IDockerClient _docker;
    private const string SandboxImage = "ghcr.io/dahln/lunasandbox:latest";

    public SandboxService(ApplicationDbContext db, IDockerClient docker)
    {
        _db = db;
        _docker = docker;
    }

    public async Task<CreateSandboxResponseDto> CreateAsync(string apiKey)
    {
        var userApiKey = await _db.UserApiKeys
            .FirstOrDefaultAsync(k => k.ApiKey == apiKey && k.RevokedAt == null);
        if (userApiKey == null) throw new UnauthorizedAccessException("Invalid API key.");

        var response = await _docker.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Image = SandboxImage,
            HostConfig = new HostConfig { AutoRemove = false }
        });

        await _docker.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());

        var sandbox = new Sandbox
        {
            Id = Guid.NewGuid().ToString(),
            UserApiKeyId = userApiKey.Id,
            ContainerId = response.ID,
            CreatedAt = DateTime.UtcNow
        };
        _db.Sandboxes.Add(sandbox);
        await _db.SaveChangesAsync();

        return new CreateSandboxResponseDto { SandboxId = sandbox.Id };
    }

    public async Task<bool> DestroyAsync(string sandboxId, string apiKey)
    {
        var userApiKey = await _db.UserApiKeys
            .FirstOrDefaultAsync(k => k.ApiKey == apiKey && k.RevokedAt == null);
        if (userApiKey == null) return false;

        var sandbox = await _db.Sandboxes
            .FirstOrDefaultAsync(s => s.Id == sandboxId && s.UserApiKeyId == userApiKey.Id && s.DestroyedAt == null);
        if (sandbox == null) return false;

        try
        {
            await _docker.Containers.StopContainerAsync(sandbox.ContainerId, new ContainerStopParameters());
            await _docker.Containers.RemoveContainerAsync(sandbox.ContainerId, new ContainerRemoveParameters { Force = true });
        }
        catch { /* container may already be gone */ }

        sandbox.DestroyedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<SandboxDto?> GetAsync(string sandboxId, string apiKey)
    {
        var userApiKey = await _db.UserApiKeys
            .FirstOrDefaultAsync(k => k.ApiKey == apiKey && k.RevokedAt == null);
        if (userApiKey == null) return null;

        var sandbox = await _db.Sandboxes
            .FirstOrDefaultAsync(s => s.Id == sandboxId && s.UserApiKeyId == userApiKey.Id);
        if (sandbox == null) return null;

        return new SandboxDto
        {
            Id = sandbox.Id,
            UserApiKeyId = sandbox.UserApiKeyId,
            ContainerId = sandbox.ContainerId,
            CreatedAt = sandbox.CreatedAt,
            DestroyedAt = sandbox.DestroyedAt
        };
    }
}
