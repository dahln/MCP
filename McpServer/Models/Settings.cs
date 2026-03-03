namespace McpServer.Models;

public class GitHubSettings
{
    public string PersonalAccessToken { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string ProductHeaderValue { get; set; } = "MCP-Server";
}

public class DockerSettings
{
    public string SandboxImage { get; set; } = "ghcr.io/dahln/lunasandbox:latest";
}

public class AuthSettings
{
    public string ApiKey { get; set; } = string.Empty;
}
