using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Dev.MCP.LUNA;

[McpServerToolType]
public class PythonTools
{
    [McpServerTool, Description("Run a Python script in a sandbox container.")]
    public static Task<string> PythonRun(
        [Description("Docker container ID")] string containerId,
        [Description("Python script path inside the container")] string scriptPath,
        [Description("Arguments to pass to the script")] string args = "",
        [Description("Working directory")] string workDir = "/workspace")
        => DockerExecHelper.RunAsync(containerId, $"python3 {scriptPath} {args}".TrimEnd(), workDir);
}
