using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace Dev.MCP.LUNA;

[McpServerToolType]
public class SandboxTools
{
    private readonly IPortalClient _portal;

    public SandboxTools(IPortalClient portal)
    {
        _portal = portal;
    }

    [McpServerTool, Description("Create a new sandbox container. Returns a sandboxId to use in subsequent operations.")]
    public async Task<string> CreateSandbox(
        [Description("The API key for authentication")] string apiKey)
    {
        var result = await _portal.CreateSandboxAsync(apiKey);
        return JsonSerializer.Serialize(new { sandboxId = result.SandboxId });
    }

    [McpServerTool, Description("Destroy a sandbox container when work is complete.")]
    public async Task<string> DestroySandbox(
        [Description("The sandbox ID to destroy")] string sandboxId,
        [Description("The API key for authentication")] string apiKey)
    {
        await _portal.DestroySandboxAsync(sandboxId, apiKey);
        return JsonSerializer.Serialize(new { success = true, message = $"Sandbox {sandboxId} destroyed." });
    }
}
