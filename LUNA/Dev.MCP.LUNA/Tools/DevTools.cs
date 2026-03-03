using Dev.MCP.LUNA.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Dev.MCP.LUNA.Tools;

[McpServerToolType]
public class DevTools
{
    private readonly SandboxService _sandboxService;

    public DevTools(SandboxService sandboxService)
    {
        _sandboxService = sandboxService;
    }

    [McpServerTool, Description("Restores NuGet packages for a .NET project or solution in the sandbox.")]
    public async Task<string> DotnetRestore(
        [Description("The sandbox ID returned by CreateSandbox")] string sandboxId,
        [Description("Path to the .NET project or solution file in the sandbox")] string projectPath)
    {
        if (!await _sandboxService.SandboxExistsAsync(sandboxId))
            return "Error: Sandbox not found.";

        return await _sandboxService.ExecuteCommandAsync(sandboxId,
            $"cd {System.IO.Path.GetDirectoryName(projectPath)} && dotnet restore \"{projectPath}\" 2>&1", 180);
    }

    [McpServerTool, Description("Builds a .NET project or solution in the sandbox.")]
    public async Task<string> DotnetBuild(
        [Description("The sandbox ID returned by CreateSandbox")] string sandboxId,
        [Description("Path to the .NET project or solution file in the sandbox")] string projectPath,
        [Description("Build configuration: Debug or Release (default: Debug)")] string configuration = "Debug")
    {
        if (!await _sandboxService.SandboxExistsAsync(sandboxId))
            return "Error: Sandbox not found.";

        return await _sandboxService.ExecuteCommandAsync(sandboxId,
            $"cd {System.IO.Path.GetDirectoryName(projectPath)} && dotnet build \"{projectPath}\" -c {configuration} 2>&1", 300);
    }

    [McpServerTool, Description("Runs a .NET project in the sandbox.")]
    public async Task<string> DotnetRun(
        [Description("The sandbox ID returned by CreateSandbox")] string sandboxId,
        [Description("Path to the .NET project in the sandbox")] string projectPath,
        [Description("Additional arguments to pass to the application")] string args = "",
        [Description("Timeout in seconds (default: 60)")] int timeoutSeconds = 60)
    {
        if (!await _sandboxService.SandboxExistsAsync(sandboxId))
            return "Error: Sandbox not found.";

        return await _sandboxService.ExecuteCommandAsync(sandboxId,
            $"cd {System.IO.Path.GetDirectoryName(projectPath)} && timeout {timeoutSeconds} dotnet run --project \"{projectPath}\" {args} 2>&1",
            timeoutSeconds + 10);
    }

    [McpServerTool, Description("Runs .NET tests for a project in the sandbox.")]
    public async Task<string> DotnetTest(
        [Description("The sandbox ID returned by CreateSandbox")] string sandboxId,
        [Description("Path to the .NET test project in the sandbox")] string projectPath)
    {
        if (!await _sandboxService.SandboxExistsAsync(sandboxId))
            return "Error: Sandbox not found.";

        return await _sandboxService.ExecuteCommandAsync(sandboxId,
            $"cd {System.IO.Path.GetDirectoryName(projectPath)} && dotnet test \"{projectPath}\" 2>&1", 300);
    }

    [McpServerTool, Description("Installs npm packages for a Node.js project in the sandbox.")]
    public async Task<string> NpmInstall(
        [Description("The sandbox ID returned by CreateSandbox")] string sandboxId,
        [Description("Path to the Node.js project directory in the sandbox")] string projectPath)
    {
        if (!await _sandboxService.SandboxExistsAsync(sandboxId))
            return "Error: Sandbox not found.";

        return await _sandboxService.ExecuteCommandAsync(sandboxId,
            $"cd {projectPath} && npm install 2>&1", 300);
    }

    [McpServerTool, Description("Builds a Node.js/Angular/React project using npm in the sandbox.")]
    public async Task<string> NpmBuild(
        [Description("The sandbox ID returned by CreateSandbox")] string sandboxId,
        [Description("Path to the project directory in the sandbox")] string projectPath,
        [Description("Build script name (default: build)")] string script = "build")
    {
        if (!await _sandboxService.SandboxExistsAsync(sandboxId))
            return "Error: Sandbox not found.";

        return await _sandboxService.ExecuteCommandAsync(sandboxId,
            $"cd {projectPath} && npm run {script} 2>&1", 300);
    }

    [McpServerTool, Description("Installs Python packages using pip in the sandbox.")]
    public async Task<string> PipInstall(
        [Description("The sandbox ID returned by CreateSandbox")] string sandboxId,
        [Description("Package name(s) to install (space separated)")] string packages,
        [Description("Path to requirements.txt file (optional)")] string requirementsFile = "")
    {
        if (!await _sandboxService.SandboxExistsAsync(sandboxId))
            return "Error: Sandbox not found.";

        string command = string.IsNullOrEmpty(requirementsFile)
            ? $"pip install {packages} 2>&1"
            : $"pip install -r {requirementsFile} 2>&1";

        return await _sandboxService.ExecuteCommandAsync(sandboxId, command, 180);
    }

    [McpServerTool, Description("Runs a Python script in the sandbox.")]
    public async Task<string> PythonRun(
        [Description("The sandbox ID returned by CreateSandbox")] string sandboxId,
        [Description("Path to the Python script in the sandbox")] string scriptPath,
        [Description("Additional arguments to pass to the script")] string args = "",
        [Description("Timeout in seconds (default: 60)")] int timeoutSeconds = 60)
    {
        if (!await _sandboxService.SandboxExistsAsync(sandboxId))
            return "Error: Sandbox not found.";

        return await _sandboxService.ExecuteCommandAsync(sandboxId,
            $"cd {System.IO.Path.GetDirectoryName(scriptPath)} && timeout {timeoutSeconds} python3 \"{scriptPath}\" {args} 2>&1",
            timeoutSeconds + 10);
    }

    [McpServerTool, Description("Lists the contents of a directory in the sandbox.")]
    public async Task<string> ListFiles(
        [Description("The sandbox ID returned by CreateSandbox")] string sandboxId,
        [Description("Directory path to list (default: /workspace)")] string path = "/workspace")
    {
        if (!await _sandboxService.SandboxExistsAsync(sandboxId))
            return "Error: Sandbox not found.";

        return await _sandboxService.ExecuteCommandAsync(sandboxId, $"ls -la {path} 2>&1");
    }

    [McpServerTool, Description("Reads the content of a file in the sandbox.")]
    public async Task<string> ReadFile(
        [Description("The sandbox ID returned by CreateSandbox")] string sandboxId,
        [Description("Path to the file in the sandbox")] string filePath)
    {
        if (!await _sandboxService.SandboxExistsAsync(sandboxId))
            return "Error: Sandbox not found.";

        return await _sandboxService.ExecuteCommandAsync(sandboxId, $"cat \"{filePath}\" 2>&1");
    }

    [McpServerTool, Description("Writes content to a file in the sandbox.")]
    public async Task<string> WriteFile(
        [Description("The sandbox ID returned by CreateSandbox")] string sandboxId,
        [Description("Path to write the file in the sandbox")] string filePath,
        [Description("Content to write to the file")] string content)
    {
        if (!await _sandboxService.SandboxExistsAsync(sandboxId))
            return "Error: Sandbox not found.";

        var escapedContent = content.Replace("'", "'\"'\"'");
        string command = $"mkdir -p \"{System.IO.Path.GetDirectoryName(filePath)}\" && cat > \"{filePath}\" << 'LUNA_FILE_EOF'\n{content}\nLUNA_FILE_EOF";
        return await _sandboxService.ExecuteCommandAsync(sandboxId, command);
    }
}
