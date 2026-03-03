using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Dev.MCP.LUNA;

[McpServerToolType]
public class NodeTools
{
    [McpServerTool, Description("Run a Node.js script in a sandbox container.")]
    public static Task<string> NodeRun(
        [Description("Docker container ID")] string containerId,
        [Description("Node.js script path inside the container")] string scriptPath,
        [Description("Arguments to pass to the script")] string args = "",
        [Description("Working directory")] string workDir = "/workspace")
        => DockerExecHelper.RunAsync(containerId, $"node {scriptPath} {args}".TrimEnd(), workDir);

    [McpServerTool, Description("Build an Angular project in a sandbox container.")]
    public static Task<string> AngularBuild(
        [Description("Docker container ID")] string containerId,
        [Description("Project path inside the container")] string projectPath = ".",
        [Description("Working directory")] string workDir = "/workspace")
        => DockerExecHelper.RunAsync(containerId, "npx ng build", Path.Combine(workDir, projectPath));

    [McpServerTool, Description("Build a React project in a sandbox container.")]
    public static Task<string> ReactBuild(
        [Description("Docker container ID")] string containerId,
        [Description("Project path inside the container")] string projectPath = ".",
        [Description("Working directory")] string workDir = "/workspace")
        => DockerExecHelper.RunAsync(containerId, "npm run build", Path.Combine(workDir, projectPath));
}
