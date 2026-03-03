using Docker.DotNet;
using Docker.DotNet.Models;

namespace Dev.MCP.LUNA.Services;

public class SandboxService
{
    private readonly DockerClient _docker;
    private const string SandboxImage = "ghcr.io/dahln/lunasandbox:latest";

    public SandboxService()
    {
        _docker = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient();
    }

    public async Task<string> CreateSandboxAsync(string githubUser, string githubPat, Dictionary<string, string>? extraEnv = null)
    {
        var envVars = new List<string>
        {
            $"GITHUB_USER={githubUser}",
            $"GITHUB_PAT={githubPat}"
        };

        if (extraEnv != null)
            envVars.AddRange(extraEnv.Select(kv => $"{kv.Key}={kv.Value}"));

        var sandboxName = $"luna-sandbox-{Guid.NewGuid():N[..8]}";

        var createParams = new CreateContainerParameters
        {
            Image = SandboxImage,
            Name = sandboxName,
            Env = envVars,
            Cmd = new List<string> { "/bin/bash", "-c", "while true; do sleep 30; done" },
            HostConfig = new HostConfig
            {
                RestartPolicy = new RestartPolicy { Name = RestartPolicyKind.No }
            }
        };

        var response = await _docker.Containers.CreateContainerAsync(createParams);
        await _docker.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());

        return response.ID;
    }

    public async Task<string> ExecuteCommandAsync(string sandboxId, string command, int timeoutSeconds = 120)
    {
        var execParams = new ContainerExecCreateParameters
        {
            Cmd = new[] { "/bin/bash", "-c", command },
            AttachStdout = true,
            AttachStderr = true,
            Tty = false
        };

        var exec = await _docker.Exec.ExecCreateContainerAsync(sandboxId, execParams);
        using var stream = await _docker.Exec.StartAndAttachContainerExecAsync(exec.ID, false);

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        var (stdout, stderr) = await stream.ReadOutputToEndAsync(cts.Token);

        string output = string.Empty;
        if (!string.IsNullOrEmpty(stdout)) output += stdout;
        if (!string.IsNullOrEmpty(stderr)) output += $"\n[stderr]\n{stderr}";

        return output.Trim();
    }

    public async Task DestroySandboxAsync(string sandboxId)
    {
        try
        {
            await _docker.Containers.StopContainerAsync(sandboxId, new ContainerStopParameters { WaitBeforeKillSeconds = 5 });
        }
        catch { /* ignore if already stopped */ }

        await _docker.Containers.RemoveContainerAsync(sandboxId, new ContainerRemoveParameters { Force = true });
    }

    public async Task<bool> SandboxExistsAsync(string sandboxId)
    {
        try
        {
            var inspect = await _docker.Containers.InspectContainerAsync(sandboxId);
            return inspect.State.Running;
        }
        catch { return false; }
    }
}
