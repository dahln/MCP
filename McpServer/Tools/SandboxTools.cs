using McpServer.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace McpServer.Tools;

[McpServerToolType]
public class SandboxTools
{
    private readonly DockerService _docker;

    public SandboxTools(DockerService docker)
    {
        _docker = docker;
    }

    [McpServerTool(Name = "create_sandbox", Title = "Create Sandbox")]
    [Description("Creates a new isolated Docker sandbox container. Returns the sandbox ID which must be passed to all subsequent operations.")]
    public async Task<string> CreateSandboxAsync(CancellationToken cancellationToken)
    {
        var sandboxId = await _docker.CreateSandboxAsync(cancellationToken);
        return $"Sandbox created. ID: {sandboxId}";
    }

    [McpServerTool(Name = "destroy_sandbox", Title = "Destroy Sandbox")]
    [Description("Destroys a sandbox container and releases all associated resources. Call this when work in the sandbox is complete.")]
    public async Task<string> DestroySandboxAsync(
        [Description("The ID of the sandbox container to destroy.")] string sandboxId,
        CancellationToken cancellationToken)
    {
        await _docker.DestroySandboxAsync(sandboxId, cancellationToken);
        return $"Sandbox {sandboxId} destroyed.";
    }

    [McpServerTool(Name = "execute_command", Title = "Execute Command in Sandbox")]
    [Description("Executes an arbitrary shell command inside the sandbox container.")]
    public async Task<string> ExecuteCommandAsync(
        [Description("The ID of the sandbox container.")] string sandboxId,
        [Description("The shell command to execute.")] string command,
        [Description("Optional working directory inside the sandbox.")] string? workingDirectory,
        CancellationToken cancellationToken)
    {
        var (exitCode, stdout, stderr) = await _docker.ExecAsync(
            sandboxId, command, workingDirectory, null, cancellationToken);

        return FormatResult(exitCode, stdout, stderr);
    }

    internal static string FormatResult(int exitCode, string stdout, string stderr)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(stdout)) parts.Add($"STDOUT:\n{stdout.Trim()}");
        if (!string.IsNullOrWhiteSpace(stderr)) parts.Add($"STDERR:\n{stderr.Trim()}");
        parts.Add($"Exit code: {exitCode}");
        return string.Join("\n\n", parts);
    }
}
