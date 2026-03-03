using Dev.MCP.LUNA.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Dev.MCP.LUNA.Tools;

[McpServerToolType]
public class SandboxTools
{
    private readonly SandboxService _sandboxService;
    private readonly IConfiguration _configuration;

    public SandboxTools(SandboxService sandboxService, IConfiguration configuration)
    {
        _sandboxService = sandboxService;
        _configuration = configuration;
    }

    [McpServerTool, Description("Creates a new development sandbox container. Returns the sandbox ID which must be passed to all subsequent tool calls. Call this at the beginning of each work session.")]
    public async Task<string> CreateSandbox()
    {
        var githubUser = _configuration["GitHubUser"] ?? string.Empty;
        var githubPat = _configuration["GitHubPat"] ?? string.Empty;

        if (string.IsNullOrEmpty(githubUser) || string.IsNullOrEmpty(githubPat))
            return "Error: GitHubUser and GitHubPat must be configured. Set them via the Portal key-value pairs.";

        try
        {
            var sandboxId = await _sandboxService.CreateSandboxAsync(githubUser, githubPat);
            return $"Sandbox created successfully. Sandbox ID: {sandboxId}\nUse this ID in all subsequent tool calls.";
        }
        catch (Exception ex)
        {
            return $"Error creating sandbox: {ex.Message}";
        }
    }

    [McpServerTool, Description("Destroys a development sandbox container. Call this at the end of each work session to clean up resources.")]
    public async Task<string> DestroySandbox(
        [Description("The sandbox ID returned by CreateSandbox")] string sandboxId)
    {
        try
        {
            await _sandboxService.DestroySandboxAsync(sandboxId);
            return "Sandbox destroyed successfully.";
        }
        catch (Exception ex)
        {
            return $"Error destroying sandbox: {ex.Message}";
        }
    }

    [McpServerTool, Description("Executes a shell command in the development sandbox. Returns the command output.")]
    public async Task<string> RunCommand(
        [Description("The sandbox ID returned by CreateSandbox")] string sandboxId,
        [Description("The shell command to execute (bash)")] string command,
        [Description("Timeout in seconds (default: 120, max: 300)")] int timeoutSeconds = 120)
    {
        if (timeoutSeconds > 300) timeoutSeconds = 300;

        if (!await _sandboxService.SandboxExistsAsync(sandboxId))
            return "Error: Sandbox not found or not running. Create a new sandbox first.";

        try
        {
            return await _sandboxService.ExecuteCommandAsync(sandboxId, command, timeoutSeconds);
        }
        catch (Exception ex)
        {
            return $"Error executing command: {ex.Message}";
        }
    }
}
