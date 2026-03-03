using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Dev.MCP.LUNA;

[McpServerToolType]
public class DotnetTools
{
    [McpServerTool, Description("Run 'dotnet restore' in a sandbox container.")]
    public static Task<string> DotnetRestore(
        [Description("Docker container ID")] string containerId,
        [Description("Project path inside the container")] string projectPath = ".",
        [Description("Working directory")] string workDir = "/workspace")
        => DockerExecHelper.RunAsync(containerId, $"dotnet restore {projectPath}", workDir);

    [McpServerTool, Description("Run 'dotnet build' in a sandbox container.")]
    public static Task<string> DotnetBuild(
        [Description("Docker container ID")] string containerId,
        [Description("Project path inside the container")] string projectPath = ".",
        [Description("Working directory")] string workDir = "/workspace")
        => DockerExecHelper.RunAsync(containerId, $"dotnet build {projectPath}", workDir);

    [McpServerTool, Description("Run 'dotnet run' in a sandbox container.")]
    public static Task<string> DotnetRun(
        [Description("Docker container ID")] string containerId,
        [Description("Project path inside the container")] string projectPath = ".",
        [Description("Working directory")] string workDir = "/workspace")
        => DockerExecHelper.RunAsync(containerId, $"dotnet run --project {projectPath}", workDir);

    [McpServerTool, Description("Run a dotnet application with diagnostic/debug info.")]
    public static Task<string> DotnetDebug(
        [Description("Docker container ID")] string containerId,
        [Description("Project path inside the container")] string projectPath = ".",
        [Description("Working directory")] string workDir = "/workspace")
        => DockerExecHelper.RunAsync(containerId, $"dotnet run --project {projectPath} --verbosity detailed", workDir);
}
