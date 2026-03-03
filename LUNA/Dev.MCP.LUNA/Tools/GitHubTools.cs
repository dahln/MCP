using Dev.MCP.LUNA.Services;
using ModelContextProtocol.Server;
using Octokit;
using System.ComponentModel;

namespace Dev.MCP.LUNA.Tools;

[McpServerToolType]
public class GitHubTools
{
    private readonly IConfiguration _configuration;
    private readonly SandboxService _sandboxService;

    public GitHubTools(IConfiguration configuration, SandboxService sandboxService)
    {
        _configuration = configuration;
        _sandboxService = sandboxService;
    }

    private GitHubClient CreateGitHubClient()
    {
        var pat = _configuration["GitHubPat"]
            ?? throw new Exception("GitHubPat not configured. Set it via the Portal key-value pairs.");
        var client = new GitHubClient(new ProductHeaderValue("Dev.MCP.LUNA"));
        client.Credentials = new Credentials(pat);
        return client;
    }

    [McpServerTool, Description("Creates a new GitHub repository for the authenticated user.")]
    public async Task<string> CreateRepository(
        [Description("The sandbox ID returned by CreateSandbox")] string sandboxId,
        [Description("Repository name")] string name,
        [Description("Repository description")] string description = "",
        [Description("Whether the repository should be private")] bool isPrivate = false)
    {
        try
        {
            var client = CreateGitHubClient();
            var repo = await client.Repository.Create(new NewRepository(name)
            {
                Description = description,
                Private = isPrivate,
                AutoInit = true
            });

            // Clone in the sandbox
            var githubUser = _configuration["GitHubUser"] ?? "";
            var gitUrl = $"https://{githubUser}:{_configuration["GitHubPat"]}@github.com/{repo.FullName}.git";
            await _sandboxService.ExecuteCommandAsync(sandboxId,
                $"cd /workspace && git clone {gitUrl} {name} 2>&1");

            return $"Repository created: {repo.HtmlUrl}\nCloned to /workspace/{name} in sandbox.";
        }
        catch (Exception ex)
        {
            return $"Error creating repository: {ex.Message}";
        }
    }

    [McpServerTool, Description("Commits and pushes changes in the sandbox working directory to the remote repository.")]
    public async Task<string> CommitAndPush(
        [Description("The sandbox ID returned by CreateSandbox")] string sandboxId,
        [Description("Path to the repository in the sandbox (e.g. /workspace/myrepo)")] string repoPath,
        [Description("Commit message")] string commitMessage,
        [Description("Branch name (default: main)")] string branch = "main")
    {
        var githubUser = _configuration["GitHubUser"] ?? "luna-bot";
        var commands = $@"
cd {repoPath} && \
git config user.email 'luna@dev.local' && \
git config user.name '{githubUser}' && \
git add -A && \
git commit -m '{commitMessage.Replace("'", "\\'")}' && \
git push origin {branch} 2>&1";

        if (!await _sandboxService.SandboxExistsAsync(sandboxId))
            return "Error: Sandbox not found.";

        return await _sandboxService.ExecuteCommandAsync(sandboxId, commands);
    }

    [McpServerTool, Description("Creates a pull request on GitHub.")]
    public async Task<string> CreatePullRequest(
        [Description("The sandbox ID returned by CreateSandbox")] string sandboxId,
        [Description("Repository full name (owner/repo)")] string repoFullName,
        [Description("PR title")] string title,
        [Description("PR body/description")] string body,
        [Description("Source branch (head)")] string headBranch,
        [Description("Target branch (base, default: main)")] string baseBranch = "main")
    {
        try
        {
            var client = CreateGitHubClient();
            var parts = repoFullName.Split('/');
            if (parts.Length != 2)
                return "Error: repoFullName must be in format 'owner/repo'";

            var pr = await client.PullRequest.Create(parts[0], parts[1], new NewPullRequest(title, headBranch, baseBranch)
            {
                Body = body
            });

            return $"Pull request created: {pr.HtmlUrl}";
        }
        catch (Exception ex)
        {
            return $"Error creating pull request: {ex.Message}";
        }
    }

    [McpServerTool, Description("Adds a collaborator to a GitHub repository.")]
    public async Task<string> AddCollaborator(
        [Description("The sandbox ID returned by CreateSandbox")] string sandboxId,
        [Description("Repository full name (owner/repo)")] string repoFullName,
        [Description("GitHub username of the collaborator to add")] string collaboratorUsername,
        [Description("Permission level: pull, push, admin (default: push)")] string permission = "push")
    {
        try
        {
            var client = CreateGitHubClient();
            var parts = repoFullName.Split('/');
            if (parts.Length != 2)
                return "Error: repoFullName must be in format 'owner/repo'";

            var permissionLevel = permission.ToLower() switch
            {
                "pull" => CollaboratorPermission.Pull,
                "admin" => CollaboratorPermission.Admin,
                _ => CollaboratorPermission.Push
            };

            await client.Repository.Collaborator.Add(parts[0], parts[1], collaboratorUsername,
                new CollaboratorRequest(permissionLevel.ToString().ToLower()));

            return $"Collaborator '{collaboratorUsername}' added to {repoFullName} with '{permission}' permission.";
        }
        catch (Exception ex)
        {
            return $"Error adding collaborator: {ex.Message}";
        }
    }

    [McpServerTool, Description("Pulls the latest changes from the remote repository into the sandbox.")]
    public async Task<string> Pull(
        [Description("The sandbox ID returned by CreateSandbox")] string sandboxId,
        [Description("Path to the repository in the sandbox")] string repoPath,
        [Description("Branch name (default: main)")] string branch = "main")
    {
        if (!await _sandboxService.SandboxExistsAsync(sandboxId))
            return "Error: Sandbox not found.";

        return await _sandboxService.ExecuteCommandAsync(sandboxId,
            $"cd {repoPath} && git pull origin {branch} 2>&1");
    }

    [McpServerTool, Description("Lists repositories for the authenticated GitHub user.")]
    public async Task<string> ListRepositories()
    {
        try
        {
            var client = CreateGitHubClient();
            var repos = await client.Repository.GetAllForCurrent();
            return string.Join("\n", repos.Take(50).Select(r => $"- {r.FullName} ({(r.Private ? "private" : "public")})"));
        }
        catch (Exception ex)
        {
            return $"Error listing repositories: {ex.Message}";
        }
    }
}
