using Dev.MCP.LUNA;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("portal", client =>
{
    var portalUrl = builder.Configuration["PortalUrl"] ?? "http://localhost:5000";
    client.BaseAddress = new Uri(portalUrl);
});

builder.Services.AddScoped<IPortalClient, PortalClient>();

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<SandboxTools>()
    .WithTools<GitHubTools>()
    .WithTools<DotnetTools>()
    .WithTools<PythonTools>()
    .WithTools<NodeTools>();

var app = builder.Build();

// API key middleware - validates Bearer token against Portal.LUNA
app.Use(async (context, next) =>
{
    // Skip health check
    if (context.Request.Path.StartsWithSegments("/health"))
    {
        await next();
        return;
    }

    // Allow initialize request without auth (MCP protocol requirement)
    if (context.Request.Path.StartsWithSegments("/mcp") && context.Request.Method == "POST")
    {
        context.Request.EnableBuffering();
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        context.Request.Body.Position = 0;
        
        if (body.Contains("\"method\":\"initialize\""))
        {
            await next();
            return;
        }
    }

    if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Missing Authorization header.");
        return;
    }
    var token = authHeader.ToString();
    if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        token = token["Bearer ".Length..].Trim();

    var factory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
    var portalClient = factory.CreateClient("portal");
    var response = await portalClient.GetAsync($"/api/api-keys/validate?apiKey={Uri.EscapeDataString(token)}");
    if (!response.IsSuccessStatusCode)
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Invalid API key.");
        return;
    }

    // Store API key in HttpContext for tools
    context.Items["ApiKey"] = token;
    await next();
});

app.MapGet("/health", () => "OK");
app.MapMcp("/mcp");

app.Run();
