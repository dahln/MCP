using Dev.MCP.LUNA.Services;
using Dev.MCP.LUNA.Tools;
using ModelContextProtocol.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<SandboxService>();

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

// API key authentication middleware
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/health"))
    {
        await next();
        return;
    }

    var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault()
        ?? context.Request.Query["api_key"].FirstOrDefault();

    var expectedKey = builder.Configuration["ApiKey"];
    if (string.IsNullOrEmpty(expectedKey) || apiKey == expectedKey)
    {
        await next();
    }
    else
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Unauthorized: Invalid API Key");
    }
});

app.MapMcp("/mcp");
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "Dev.MCP.LUNA" }));

app.Run();
