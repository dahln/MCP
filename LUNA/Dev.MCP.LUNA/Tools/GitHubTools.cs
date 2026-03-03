using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Octokit;

namespace Dev.MCP.LUNA;

[McpServerToolType]
public class GitHubTools
{
    private readonly IPortalClient _portal;

    public GitHubTools(IPortalClient portal)
    {
        _portal = portal;
    }

    private async Task<GitHubClient> GetClientAsync(string apiKey)
    {
        var info = await _portal.ValidateApiKeyAsync(apiKey);
        if (info == null) throw new UnauthorizedAccessException("Invalid API key.");
        var pat = info.Settings.FirstOrDefault(s => s.Key == "GithubPAT")?.Value
            ?? throw new InvalidOperationException("GithubPAT not configured.");
        var client = new GitHubClient(new ProductHeaderValue("Dev.MCP.LUNA"));
        client.Credentials = new Credentials(pat);
        return client;
    }

    [McpServerTool, Description("Create a new GitHub repository.")]
    public async Task<string> CreateRepository(
        [Description("API key")] string apiKey,
        [Description("Repository name")] string repoName,
        [Description("Whether the repository is private")] bool isPrivate = true,
        [Description("Repository description")] string? description = null)
    {
        var client = await GetClientAsync(apiKey);
        var repo = await client.Repository.Create(new NewRepository(repoName)
        {
            Private = isPrivate,
            Description = description
        });
        return JsonSerializer.Serialize(new { success = true, url = repo.HtmlUrl, name = repo.FullName });
    }

    [McpServerTool, Description("Create a pull request on GitHub.")]
    public async Task<string> CreatePullRequest(
        [Description("API key")] string apiKey,
        [Description("Repository name (owner/repo)")] string repository,
        [Description("PR title")] string title,
        [Description("Source branch (head)")] string head,
        [Description("Target branch (base)")] string baseBranch,
        [Description("PR body/description")] string? body = null)
    {
        var client = await GetClientAsync(apiKey);
        var parts = repository.Split('/');
        var pr = await client.PullRequest.Create(parts[0], parts[1], new NewPullRequest(title, head, baseBranch)
        {
            Body = body
        });
        return JsonSerializer.Serialize(new { success = true, url = pr.HtmlUrl, number = pr.Number });
    }

    [McpServerTool, Description("Add a collaborator to a GitHub repository.")]
    public async Task<string> AddCollaborator(
        [Description("API key")] string apiKey,
        [Description("Repository name (owner/repo)")] string repository,
        [Description("GitHub username to add")] string collaboratorUsername,
        [Description("Permission level: pull, push, admin")] string permission = "push")
    {
        var client = await GetClientAsync(apiKey);
        var parts = repository.Split('/');
        var perm = permission.ToLower() switch
        {
            "pull" => "pull",
            "admin" => "admin",
            _ => "push"
        };
        await client.Repository.Collaborator.Add(parts[0], parts[1], collaboratorUsername,
            new CollaboratorRequest(perm));
        return JsonSerializer.Serialize(new { success = true, message = $"Added {collaboratorUsername} to {repository}." });
    }

    [McpServerTool, Description("Run a git command inside a sandbox container using docker exec.")]
    public static Task<string> RunGitCommand(
        [Description("Sandbox container ID to run in")] string containerId,
        [Description("Git command arguments (e.g. 'clone https://...')")] string gitArgs,
        [Description("Working directory inside container")] string workDir = "/workspace")
        => DockerExecHelper.RunAsync(containerId, $"git {gitArgs}", workDir);
}
