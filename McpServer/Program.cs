using McpServer.Middleware;
using McpServer.Models;
using McpServer.Services;
using McpServer.Tools;

var builder = WebApplication.CreateBuilder(args);

// Bind configuration sections
builder.Services.Configure<GitHubSettings>(builder.Configuration.GetSection("GitHub"));
builder.Services.Configure<DockerSettings>(builder.Configuration.GetSection("Docker"));
builder.Services.Configure<AuthSettings>(builder.Configuration.GetSection("Auth"));

// Register services
builder.Services.AddSingleton<DockerService>();

// Register MCP server with all tool types
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<SandboxTools>()
    .WithTools<GitHubTools>()
    .WithTools<DotNetTools>();

var app = builder.Build();

// API key authentication middleware – applied to all requests
app.UseMiddleware<ApiKeyMiddleware>();

app.MapMcp("/mcp");

app.Run();
