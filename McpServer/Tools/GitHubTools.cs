using McpServer.Models;
using McpServer.Services;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using Octokit;
using System.ComponentModel;

namespace McpServer.Tools;

[McpServerToolType]
public class GitHubTools
{
    private readonly GitHubClient _github;
    private readonly GitHubSettings _settings;
    private readonly DockerService _docker;

    public GitHubTools(IOptions<GitHubSettings> gitHubOptions, DockerService docker)
    {
        _settings = gitHubOptions.Value;
        _docker = docker;
        _github = new GitHubClient(new ProductHeaderValue(_settings.ProductHeaderValue))
        {
            Credentials = new Credentials(_settings.PersonalAccessToken)
        };
    }

    // -------------------------------------------------------------------------
    // Repository
    // -------------------------------------------------------------------------

    [McpServerTool(Name = "github_create_repo", Title = "Create GitHub Repository")]
    [Description("Creates a new GitHub repository for the authenticated user.")]
    public async Task<string> CreateRepositoryAsync(
        [Description("Name of the new repository.")] string name,
        [Description("Optional description for the repository.")] string? description,
        [Description("Whether the repository should be private (default: false).")] bool isPrivate = false,
        CancellationToken cancellationToken = default)
    {
        var newRepo = new NewRepository(name)
        {
            Description = description,
            Private = isPrivate,
            AutoInit = true
        };

        var repo = await _github.Repository.Create(newRepo);
        return $"Repository created: {repo.HtmlUrl}";
    }

    [McpServerTool(Name = "github_get_repo", Title = "Get GitHub Repository")]
    [Description("Gets information about a GitHub repository.")]
    public async Task<string> GetRepositoryAsync(
        [Description("Owner of the repository (user or org).")] string owner,
        [Description("Name of the repository.")] string repo,
        CancellationToken cancellationToken = default)
    {
        var repository = await _github.Repository.Get(owner, repo);
        return $"Name: {repository.FullName}\nURL: {repository.HtmlUrl}\nDefault branch: {repository.DefaultBranch}\nPrivate: {repository.Private}\nDescription: {repository.Description}";
    }

    // -------------------------------------------------------------------------
    // Collaborators
    // -------------------------------------------------------------------------

    [McpServerTool(Name = "github_add_collaborator", Title = "Add Repository Collaborator")]
    [Description("Adds a collaborator to a GitHub repository.")]
    public async Task<string> AddCollaboratorAsync(
        [Description("Owner of the repository.")] string owner,
        [Description("Name of the repository.")] string repo,
        [Description("GitHub username of the collaborator to add.")] string collaboratorUsername,
        [Description("Permission level: pull, triage, push, maintain, or admin (default: push).")] string permission = "push",
        CancellationToken cancellationToken = default)
    {
        await _github.Repository.Collaborator.Add(owner, repo, collaboratorUsername,
            new CollaboratorRequest(permission.ToLowerInvariant()));

        return $"Collaborator '{collaboratorUsername}' added to {owner}/{repo} with '{permission}' permission.";
    }

    // -------------------------------------------------------------------------
    // Pull Requests
    // -------------------------------------------------------------------------

    [McpServerTool(Name = "github_create_pr", Title = "Create Pull Request")]
    [Description("Creates a new pull request on GitHub.")]
    public async Task<string> CreatePullRequestAsync(
        [Description("Owner of the repository.")] string owner,
        [Description("Name of the repository.")] string repo,
        [Description("Title of the pull request.")] string title,
        [Description("The branch where changes are (head branch).")] string headBranch,
        [Description("The branch to merge into (base branch, e.g. main).")] string baseBranch,
        [Description("Optional body/description of the pull request.")] string? body,
        [Description("Whether the PR is a draft (default: false).")] bool draft = false,
        CancellationToken cancellationToken = default)
    {
        var newPr = new NewPullRequest(title, headBranch, baseBranch)
        {
            Body = body,
            Draft = draft
        };

        var pr = await _github.PullRequest.Create(owner, repo, newPr);
        return $"Pull request created: {pr.HtmlUrl}\nNumber: #{pr.Number}\nTitle: {pr.Title}";
    }

    [McpServerTool(Name = "github_list_prs", Title = "List Pull Requests")]
    [Description("Lists open pull requests for a repository.")]
    public async Task<string> ListPullRequestsAsync(
        [Description("Owner of the repository.")] string owner,
        [Description("Name of the repository.")] string repo,
        CancellationToken cancellationToken = default)
    {
        var prs = await _github.PullRequest.GetAllForRepository(owner, repo);
        if (prs.Count == 0) return "No open pull requests.";

        var lines = prs.Select(pr => $"#{pr.Number}: {pr.Title} ({pr.User.Login}) - {pr.HtmlUrl}");
        return string.Join("\n", lines);
    }

    // -------------------------------------------------------------------------
    // Git operations (via sandbox using git CLI with PAT)
    // -------------------------------------------------------------------------

    [McpServerTool(Name = "github_clone_repo", Title = "Clone Repository into Sandbox")]
    [Description("Clones a GitHub repository into the sandbox container using the stored PAT for authentication.")]
    public async Task<string> CloneRepositoryAsync(
        [Description("The ID of the sandbox container.")] string sandboxId,
        [Description("Owner of the repository.")] string owner,
        [Description("Name of the repository.")] string repo,
        [Description("Local directory path inside the sandbox to clone into (default: /workspace/<repo>).")] string? localPath,
        CancellationToken cancellationToken = default)
    {
        var destination = localPath ?? $"/workspace/{repo}";

        // Configure git identity and credential helper in sandbox
        var configCmd = string.Join(" && ",
            $"git config --global user.email '{_settings.Username}@users.noreply.github.com'",
            $"git config --global user.name '{_settings.Username}'",
            "git config --global credential.helper '!f() { echo \"username=${GIT_USER}\"; echo \"password=${GIT_TOKEN}\"; }; f'");

        await _docker.ExecAsync(sandboxId, configCmd, cancellationToken: cancellationToken);

        var env = new Dictionary<string, string>
        {
            ["GIT_USER"] = _settings.Username,
            ["GIT_TOKEN"] = _settings.PersonalAccessToken,
            ["GIT_TERMINAL_PROMPT"] = "0"
        };

        var cloneUrl = $"https://github.com/{owner}/{repo}.git";
        var (exitCode, stdout, stderr) = await _docker.ExecAsync(
            sandboxId,
            $"git clone {cloneUrl} {destination}",
            env: env,
            cancellationToken: cancellationToken);

        return SandboxTools.FormatResult(exitCode, stdout, stderr);
    }

    [McpServerTool(Name = "github_commit", Title = "Commit Changes in Sandbox")]
    [Description("Stages all changes and creates a commit inside the sandbox container.")]
    public async Task<string> CommitAsync(
        [Description("The ID of the sandbox container.")] string sandboxId,
        [Description("Path to the repository directory inside the sandbox.")] string repoPath,
        [Description("Commit message.")] string message,
        CancellationToken cancellationToken = default)
    {
        // Write the commit message to a temp file to avoid shell injection
        var tmpFile = $"/tmp/commit_msg_{Guid.NewGuid():N}.txt";
        var writeMsg = $"cat > {tmpFile} << 'ENDOFMSG'\n{message}\nENDOFMSG";
        await _docker.ExecAsync(sandboxId, writeMsg, repoPath, cancellationToken: cancellationToken);

        var combined = $"git add -A && git commit -F {tmpFile} && rm -f {tmpFile}";

        var (exitCode, stdout, stderr) = await _docker.ExecAsync(
            sandboxId, combined, repoPath, cancellationToken: cancellationToken);

        return SandboxTools.FormatResult(exitCode, stdout, stderr);
    }

    [McpServerTool(Name = "github_push", Title = "Push Changes in Sandbox")]
    [Description("Pushes committed changes from the sandbox repository to GitHub.")]
    public async Task<string> PushAsync(
        [Description("The ID of the sandbox container.")] string sandboxId,
        [Description("Path to the repository directory inside the sandbox.")] string repoPath,
        [Description("Remote name (default: origin).")] string remote = "origin",
        [Description("Branch name to push (default: current HEAD branch).")] string? branch = null,
        CancellationToken cancellationToken = default)
    {
        // Default to pushing the current branch to the specified remote
        var refSpec = branch is not null ? $"{remote} {branch}" : $"{remote} HEAD";
        var pushCmd = $"git push {refSpec}";

        var env = new Dictionary<string, string>
        {
            ["GIT_USER"] = _settings.Username,
            ["GIT_TOKEN"] = _settings.PersonalAccessToken,
            ["GIT_TERMINAL_PROMPT"] = "0"
        };

        var (exitCode, stdout, stderr) = await _docker.ExecAsync(
            sandboxId, pushCmd, repoPath, env, cancellationToken);

        return SandboxTools.FormatResult(exitCode, stdout, stderr);
    }

    [McpServerTool(Name = "github_pull", Title = "Pull Changes in Sandbox")]
    [Description("Pulls the latest changes from GitHub into the sandbox repository.")]
    public async Task<string> PullAsync(
        [Description("The ID of the sandbox container.")] string sandboxId,
        [Description("Path to the repository directory inside the sandbox.")] string repoPath,
        [Description("Remote name (default: origin).")] string remote = "origin",
        [Description("Branch name to pull (default: current branch).")] string? branch = null,
        CancellationToken cancellationToken = default)
    {
        var branchArg = branch is not null ? $" {remote} {branch}" : string.Empty;
        var pullCmd = $"git pull{branchArg}";

        var env = new Dictionary<string, string>
        {
            ["GIT_USER"] = _settings.Username,
            ["GIT_TOKEN"] = _settings.PersonalAccessToken,
            ["GIT_TERMINAL_PROMPT"] = "0"
        };

        var (exitCode, stdout, stderr) = await _docker.ExecAsync(
            sandboxId, pullCmd, repoPath, env, cancellationToken);

        return SandboxTools.FormatResult(exitCode, stdout, stderr);
    }

    [McpServerTool(Name = "github_create_branch", Title = "Create Git Branch in Sandbox")]
    [Description("Creates and checks out a new branch inside the sandbox repository.")]
    public async Task<string> CreateBranchAsync(
        [Description("The ID of the sandbox container.")] string sandboxId,
        [Description("Path to the repository directory inside the sandbox.")] string repoPath,
        [Description("Name of the new branch.")] string branchName,
        CancellationToken cancellationToken = default)
    {
        var (exitCode, stdout, stderr) = await _docker.ExecAsync(
            sandboxId, $"git checkout -b {branchName}", repoPath, cancellationToken: cancellationToken);

        return SandboxTools.FormatResult(exitCode, stdout, stderr);
    }
}
