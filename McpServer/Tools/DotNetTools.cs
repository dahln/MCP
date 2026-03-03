using McpServer.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace McpServer.Tools;

[McpServerToolType]
public class DotNetTools
{
    private readonly DockerService _docker;

    public DotNetTools(DockerService docker)
    {
        _docker = docker;
    }

    [McpServerTool(Name = "dotnet_restore", Title = "Restore .NET Dependencies")]
    [Description("Runs 'dotnet restore' inside the sandbox to restore NuGet packages for a project or solution.")]
    public async Task<string> RestoreAsync(
        [Description("The ID of the sandbox container.")] string sandboxId,
        [Description("Path to the project file (.csproj), solution file (.sln), or directory inside the sandbox.")] string projectPath,
        CancellationToken cancellationToken = default)
    {
        return await RunDotNetAsync(sandboxId, $"restore {projectPath}", cancellationToken);
    }

    [McpServerTool(Name = "dotnet_build", Title = "Build .NET Project")]
    [Description("Runs 'dotnet build' inside the sandbox to compile a .NET project or solution.")]
    public async Task<string> BuildAsync(
        [Description("The ID of the sandbox container.")] string sandboxId,
        [Description("Path to the project file, solution file, or directory inside the sandbox.")] string projectPath,
        [Description("Build configuration (Debug or Release, default: Debug).")] string configuration = "Debug",
        CancellationToken cancellationToken = default)
    {
        return await RunDotNetAsync(sandboxId, $"build {projectPath} -c {configuration}", cancellationToken);
    }

    [McpServerTool(Name = "dotnet_run", Title = "Run .NET Project")]
    [Description("Runs 'dotnet run' inside the sandbox to execute a .NET project.")]
    public async Task<string> RunAsync(
        [Description("The ID of the sandbox container.")] string sandboxId,
        [Description("Path to the project file or directory inside the sandbox.")] string projectPath,
        [Description("Optional arguments to pass to the application.")] string? args,
        [Description("Build configuration (Debug or Release, default: Debug).")] string configuration = "Debug",
        CancellationToken cancellationToken = default)
    {
        var argsStr = args is not null ? $" -- {args}" : string.Empty;
        return await RunDotNetAsync(sandboxId, $"run --project {projectPath} -c {configuration}{argsStr}", cancellationToken);
    }

    [McpServerTool(Name = "dotnet_test", Title = "Run .NET Tests")]
    [Description("Runs 'dotnet test' inside the sandbox to execute the test suite.")]
    public async Task<string> TestAsync(
        [Description("The ID of the sandbox container.")] string sandboxId,
        [Description("Path to the test project file or directory inside the sandbox.")] string projectPath,
        [Description("Build configuration (Debug or Release, default: Debug).")] string configuration = "Debug",
        CancellationToken cancellationToken = default)
    {
        return await RunDotNetAsync(sandboxId, $"test {projectPath} -c {configuration}", cancellationToken);
    }

    [McpServerTool(Name = "dotnet_publish", Title = "Publish .NET Project")]
    [Description("Runs 'dotnet publish' inside the sandbox to publish a .NET project.")]
    public async Task<string> PublishAsync(
        [Description("The ID of the sandbox container.")] string sandboxId,
        [Description("Path to the project file or directory inside the sandbox.")] string projectPath,
        [Description("Optional output directory inside the sandbox.")] string? outputPath,
        [Description("Build configuration (Debug or Release, default: Release).")] string configuration = "Release",
        CancellationToken cancellationToken = default)
    {
        var outputArg = outputPath is not null ? $" -o {outputPath}" : string.Empty;
        return await RunDotNetAsync(sandboxId, $"publish {projectPath} -c {configuration}{outputArg}", cancellationToken);
    }

    [McpServerTool(Name = "dotnet_format", Title = "Format .NET Code")]
    [Description("Runs 'dotnet format' inside the sandbox to format source code.")]
    public async Task<string> FormatAsync(
        [Description("The ID of the sandbox container.")] string sandboxId,
        [Description("Path to the project file, solution file, or directory inside the sandbox.")] string projectPath,
        CancellationToken cancellationToken = default)
    {
        return await RunDotNetAsync(sandboxId, $"format {projectPath}", cancellationToken);
    }

    [McpServerTool(Name = "dotnet_add_package", Title = "Add NuGet Package")]
    [Description("Runs 'dotnet add package' inside the sandbox to add a NuGet package to a project.")]
    public async Task<string> AddPackageAsync(
        [Description("The ID of the sandbox container.")] string sandboxId,
        [Description("Path to the project file (.csproj) inside the sandbox.")] string projectPath,
        [Description("Name of the NuGet package to add.")] string packageName,
        [Description("Optional version of the package (e.g., 1.2.3).")] string? version,
        CancellationToken cancellationToken = default)
    {
        var versionArg = version is not null ? $" --version {version}" : string.Empty;
        return await RunDotNetAsync(sandboxId, $"add {projectPath} package {packageName}{versionArg}", cancellationToken);
    }

    [McpServerTool(Name = "dotnet_new", Title = "Create New .NET Project")]
    [Description("Runs 'dotnet new' inside the sandbox to create a new .NET project from a template.")]
    public async Task<string> NewProjectAsync(
        [Description("The ID of the sandbox container.")] string sandboxId,
        [Description("Template name (e.g., console, webapi, classlib, xunit).")] string template,
        [Description("Name for the new project.")] string projectName,
        [Description("Directory inside the sandbox where the project should be created.")] string outputPath,
        [Description("Target framework (e.g., net10.0). Leave empty to use the default.")] string? framework,
        CancellationToken cancellationToken = default)
    {
        var frameworkArg = framework is not null ? $" -f {framework}" : string.Empty;
        return await RunDotNetAsync(sandboxId, $"new {template} -n {projectName} -o {outputPath}{frameworkArg}", cancellationToken);
    }

    [McpServerTool(Name = "dotnet_info", Title = "Show .NET SDK Info")]
    [Description("Shows .NET SDK and runtime information inside the sandbox.")]
    public async Task<string> InfoAsync(
        [Description("The ID of the sandbox container.")] string sandboxId,
        CancellationToken cancellationToken = default)
    {
        return await RunDotNetAsync(sandboxId, "--info", cancellationToken);
    }

    [McpServerTool(Name = "dotnet_list_projects", Title = "List Projects in Solution")]
    [Description("Lists all projects in a .NET solution file inside the sandbox.")]
    public async Task<string> ListProjectsAsync(
        [Description("The ID of the sandbox container.")] string sandboxId,
        [Description("Path to the solution file (.sln) inside the sandbox.")] string solutionPath,
        CancellationToken cancellationToken = default)
    {
        return await RunDotNetAsync(sandboxId, $"sln {solutionPath} list", cancellationToken);
    }

    private async Task<string> RunDotNetAsync(
        string sandboxId,
        string arguments,
        CancellationToken cancellationToken)
    {
        var (exitCode, stdout, stderr) = await _docker.ExecAsync(
            sandboxId, $"dotnet {arguments}", cancellationToken: cancellationToken);

        return SandboxTools.FormatResult(exitCode, stdout, stderr);
    }
}
