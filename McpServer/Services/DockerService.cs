using McpServer.Models;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace McpServer.Services;

public class DockerService
{
    private readonly string _sandboxImage;
    private readonly ILogger<DockerService> _logger;

    public DockerService(IOptions<DockerSettings> dockerOptions, ILogger<DockerService> logger)
    {
        _sandboxImage = dockerOptions.Value.SandboxImage;
        _logger = logger;
    }

    /// <summary>Creates a new sandbox container and returns its ID.</summary>
    public async Task<string> CreateSandboxAsync(CancellationToken cancellationToken = default)
    {
        var (exitCode, stdout, stderr) = await RunDockerAsync(
            $"run -d --rm {_sandboxImage} tail -f /dev/null",
            cancellationToken);

        if (exitCode != 0)
            throw new InvalidOperationException($"Failed to create sandbox: {stderr}");

        return stdout.Trim();
    }

    /// <summary>Destroys a sandbox container by its ID.</summary>
    public async Task DestroySandboxAsync(string sandboxId, CancellationToken cancellationToken = default)
    {
        var (exitCode, _, stderr) = await RunDockerAsync($"rm -f {sandboxId}", cancellationToken);
        if (exitCode != 0)
            _logger.LogWarning("Failed to destroy sandbox {SandboxId}: {Error}", sandboxId, stderr);
    }

    /// <summary>Executes a command inside the sandbox container.</summary>
    public async Task<(int ExitCode, string Stdout, string Stderr)> ExecAsync(
        string sandboxId,
        string command,
        string? workDir = null,
        IDictionary<string, string>? env = null,
        CancellationToken cancellationToken = default)
    {
        var envArgs = env is not null
            ? string.Join(" ", env.Select(kv => $"-e {kv.Key}={ShellEscape(kv.Value)}"))
            : string.Empty;

        var workDirArg = workDir is not null ? $"-w {workDir}" : string.Empty;

        var dockerArgs = $"exec {envArgs} {workDirArg} {sandboxId} sh -c {ShellEscape(command)}";
        return await RunDockerAsync(dockerArgs, cancellationToken);
    }

    /// <summary>Copies a file or directory from the host into the sandbox.</summary>
    public async Task CopyToSandboxAsync(
        string sandboxId,
        string sourcePath,
        string destPath,
        CancellationToken cancellationToken = default)
    {
        var (exitCode, _, stderr) = await RunDockerAsync($"cp {sourcePath} {sandboxId}:{destPath}", cancellationToken);
        if (exitCode != 0)
            throw new InvalidOperationException($"Failed to copy to sandbox: {stderr}");
    }

    private static string ShellEscape(string value)
    {
        return "'" + value.Replace("'", "'\\''") + "'";
    }

    private async Task<(int ExitCode, string Stdout, string Stderr)> RunDockerAsync(
        string arguments,
        CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        _logger.LogDebug("Running: docker {Arguments}", arguments);
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return (process.ExitCode, stdout, stderr);
    }
}
